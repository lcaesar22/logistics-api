namespace Logistics.Api.Common.Endpoints;

public interface IEndpoint
{
    void Map(IEndpointRouteBuilder routes);
}