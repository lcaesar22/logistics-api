using Logistics.Api.Common.Auth;
using Logistics.Api.Common.Endpoints;
using Logistics.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace Logistics.Api.Features.Shipments;

internal sealed class DeleteShipmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder routes)
    {
        routes.MapDelete("/shipments/{id:guid}", HandleAsync).WithName("DeleteShipment").WithTags("Shipments").RequireAuthorization(Policies.DeleteShipments)
            .ProducesProblem(StatusCodes.Status409Conflict);

    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> HandleAsync(
        Guid id, LogisticsDbContext db, ILogger<DeleteShipmentEndpoint> logger, CancellationToken ct)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shipment is null) return TypedResults.NotFound();

        if (!shipment.CanBeChanged)
        {
            return TypedResults.Problem(
                title:"Shipment can no longer be deleted",
                detail:$"Shipment {shipment.TrackingNumber} is {shipment.ShipmentStatus}." +
                       "Only shipments in Created status can be deleted.",
                statusCode: StatusCodes.Status409Conflict);
        }
        
        db.Shipments.Remove(shipment);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Shipment {TrackingNumber} has been deleted", shipment.TrackingNumber);
        return TypedResults.NoContent();
    }


}