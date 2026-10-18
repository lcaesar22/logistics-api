using Logistics.Api.Common.Auth;
using Logistics.Api.Common.Endpoints;
using Logistics.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Features.Shipments;

public sealed record GetShipmentResponse(
    Guid Id,
    string TrackingNumber,
    string ShipmentStatus,
    AddressDto Origin,
    AddressDto Destination,
    decimal WeightKg,
    DateTimeOffset CreatedAt);

internal sealed class GetShipmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder routes) =>
    routes.MapGet("/shipment/{id:guid}", HandleAsync).WithName("GetShipment").WithTags("Shipments").RequireAuthorization(Policies.ReadShipments);

    private static async Task<Results<Ok<GetShipmentResponse>, NotFound>> HandleAsync(
        Guid id,
        LogisticsDbContext db,
        CancellationToken ct)
    {
        var shipment = await db.Shipments.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);

        if (shipment is null) return TypedResults.NotFound();
        
        return TypedResults.Ok(new GetShipmentResponse(
            shipment.Id,
            shipment.TrackingNumber,
            shipment.ShipmentStatus.ToString(),
            AddressDto.From(shipment.Origin),
            AddressDto.From(shipment.Destination),
            shipment.WeightKg,
            shipment.CreatedAt));
    }
}