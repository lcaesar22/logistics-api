using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Logistics.Api.Common.Endpoints;

// Scans the assembly for every IEndpoint

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var descriptors = assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IEndpoint)))
            .Select(t => ServiceDescriptor.Transient(typeof(IEndpoint), t))
            .ToArray();
        
        services.TryAddEnumerable(descriptors);
        return services;
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder routes)
    {
        foreach (var endpoint in routes.ServiceProvider.GetServices<IEndpoint>())
        {
            endpoint.Map(routes);
        }
        return routes;
    }
}