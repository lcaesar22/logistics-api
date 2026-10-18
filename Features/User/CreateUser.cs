using Logistics.Api.Common.Auth;
using Logistics.Api.Domain;
using FluentValidation;
using Logistics.Api.Common.Endpoints;
using Logistics.Api.Common.Validation;
using Logistics.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Internal;

namespace Logistics.Api.Features.User;

public sealed record CreateUserRequest(string Email, string Password, string Role);
public sealed record CreateUserResponse(Guid Id, string Email, string Role);

internal sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    private static readonly string[] RoleNames = Enum.GetNames<UserRole>();
    
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Role).NotEmpty().Must(role => RoleNames.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Invalid role");
    }
}



internal sealed class CreateUserEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder routes) => routes.MapPost("/users", HandleAsync)
        .WithName("CreateUser")
        .WithTags("Users")
        .RequireAuthorization(Policies.ManageUsers)
        .WithValidation<CreateUserRequest>()
        .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<CreatedAtRoute<CreateUserResponse>, ProblemHttpResult>> HandleAsync(
        CreateUserRequest request,
        LogisticsDbContext db,
        IPasswordHasher<Domain.User> hasher,
        TimeProvider timeProvider,
        ILogger<CreateUserEndpoint> logger,
        CancellationToken ct)
    {
        var email = Domain.User.NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return TypedResults.Problem(
                title: "email already exists",
                detail:"A user with this email already exists",
                statusCode: StatusCodes.Status409Conflict);
        }
        var role = Enum.Parse<UserRole>(request.Role, ignoreCase:true);
        var user = Domain.User.Create(email, role, timeProvider.GetUtcNow());
        user.SetPasswordHash(hasher.HashPassword(user, request.Password));
        
        logger.LogInformation("User created: {@User}", user);
        
        var response = new CreateUserResponse(user.Id, user.Email, user.Role.ToString());
        return TypedResults.CreatedAtRoute(response, "GetUser", new { id = user.Id });
    }
}