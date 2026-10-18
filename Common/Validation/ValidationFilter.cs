using System.Text.Json;
using FluentValidation;

namespace Logistics.Api.Common.Validation;

public sealed class ValidationFilter<TRequest> : IEndpointFilter where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault()
                      ?? throw new InvalidOperationException(
                          $"No {typeof(TRequest).Name} argument found on this endpoint.");
        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<TRequest>>();
        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);

        if (result.IsValid) return await next(context);
        
        var errors = result.Errors.GroupBy(e => ToCamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        
        return TypedResults.BadRequest(errors);
    }
    // FluentValidation message from json to camelcase
    private static string ToCamelCase(string input) =>
        string.Join('.', input.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}

public static class ValidationExtensions
{
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder) where TRequest : class =>
        builder.AddEndpointFilter<ValidationFilter<TRequest>>().ProducesValidationProblem();   
        
}
