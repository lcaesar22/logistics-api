using FluentValidation;
using Logistics.Api.Common.Auth;
using Logistics.Api.Common.Endpoints;
using Logistics.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Features.Shipments;

public sealed record UpdateShipmentRequest(AddressDto Origin, AddressDto Destination, decimal WeightKg);

// Validate UpdateShipmentRequest
internal sealed class UpdateShipmentValidator : AbstractValidator<UpdateShipmentRequest>
{
    public UpdateShipmentValidator()
    {
        var address = new AddressDtoValidator();
        
        RuleFor(x => x.Origin).NotNull().SetValidator(address);
        RuleFor(x => x.Destination).NotNull().SetValidator(address).NotEqual(x => x.Origin)
            .WithMessage("Destination must differ from origin");
        RuleFor(x => x.WeightKg).InclusiveBetween(0.001m, 50_000m);
    }
}

internal sealed class UpdateShipmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPut("/shipments/{id:guid}", HandleAsync).WithName("UpdateShipment").WithTags("Shipment")
            .RequireAuthorization(Policies.WriteShipments).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> HandleAsync(
        Guid id, UpdateShipmentRequest request, LogisticsDbContext db, ILogger<UpdateShipmentEndpoint> logger, CancellationToken ct)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shipment is null) return TypedResults.NotFound();

        var updated = shipment.TryUpdate(request.Origin.ToDomain(), request.Destination.ToDomain(), request.WeightKg);

        if (!updated)
        {
            return TypedResults.Problem(
                title:"Shipment can no longer be changed",
                detail: $"Shipment { shipment.TrackingNumber} is {shipment.ShipmentStatus}" + "Only shipments in Created status can be changes",
                statusCode: StatusCodes.Status409Conflict);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Shipment {TrackingNumber} has been updated", shipment.TrackingNumber);
        return TypedResults.NoContent();
    }
}