using FluentValidation;
using Logistics.Api.Common.Auth;
using Logistics.Api.Common.Endpoints;
using Logistics.Api.Common.Validation;
using Logistics.Api.Domain;
using Logistics.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Features.Auth;


public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt);

internal sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

internal sealed class LoginEndpoint : IEndpoint
{
    private static readonly Domain.User dummyUser =
        User.Create("timmyturner@gmail.com", UserRole.Driver, DateTimeOffset.UnixEpoch);
    private static readonly string DummyHash = new PasswordHasher<Domain.User>().HashPassword(dummyUser, "not-a-real-password");

    public void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/auth/login", HandleAsync).WithName("Login").WithTags("Auth").WithValidation<LoginRequest>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> HandleAsync(
            LoginRequest loginRequest,
            LogisticsDbContext db,
            IPasswordHasher<Domain.User> hasher,
            JwtTokenService tokens,
            ILogger<LoginEndpoint> logger,
            CancellationToken ct)
        {
            var email = User.NormalizeEmail(loginRequest.Email);
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user == null)
            {
                hasher.VerifyHashedPassword(dummyUser, DummyHash, loginRequest.Password);
                logger.LogWarning("Login failed: unknown email");
                return InvalidCredentials();
            }
            var verified = hasher.VerifyHashedPassword(user, user.PasswordHash, loginRequest.Password);
            if (verified == PasswordVerificationResult.Failed)
            {
                logger.LogWarning("Login failed: invalid password");
                return InvalidCredentials();
            }

            var (token, expiresAt) = tokens.CreateAccessToken(user);
            logger.LogInformation("User {User.Id} logged in", user.Id);

            return TypedResults.Ok(new LoginResponse(token, "Bearer", expiresAt));

        }
    
    private static ProblemHttpResult InvalidCredentials() => TypedResults.Problem(
        title:"Invalid credentials",
        detail:"The email or password is incorrect",
        statusCode:StatusCodes.Status401Unauthorized);
    
}