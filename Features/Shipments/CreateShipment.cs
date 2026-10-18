using FluentValidation;
using Logistics.Api.Common.Auth;
using Logistics.Api.Common.Endpoints;
using Logistics.Api.Common.Validation;
using Logistics.Api.Domain;
using Logistics.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Logistics.Api.Features.Shipments;

public sealed record CreateShipmentRequest(AddressDto Origin, AddressDto Destination, decimal WeightKg);
public sealed record CreateShipmentResponse(Guid Id, string TrackingNumber, string Status, DateTimeOffset CreatedAt);

public sealed class CreateShipmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder routes) =>
        routes.MapPost("/shipments", HandleAsync).WithName("CreateShipment").WithTags("Shipments")
            .RequireAuthorization(Policies.WriteShipments)
            .WithValidation<CreateShipmentRequest>();

        private static async Task<CreatedAtRoute<CreateShipmentResponse>> HandleAsync(
            CreateShipmentRequest request,
            LogisticsDbContext db,
            TimeProvider clock,
            ILogger<CreateShipmentEndpoint> logger,
            CancellationToken ct)
        {
            var shipment = Shipment.CreateShipment(
                request.Origin.ToDomain(),
                request.Destination.ToDomain(),
                request.WeightKg,
                clock.GetUtcNow());
            
            db.Shipments.Add(shipment);
            await db.SaveChangesAsync(ct);
            
            logger.LogInformation("Shipment {TrackingNumber} created", shipment.TrackingNumber);
            
            var response = new CreateShipmentResponse(shipment.Id,shipment.TrackingNumber,shipment.ShipmentStatus.ToString(),shipment.CreatedAt);
            
            return TypedResults.CreatedAtRoute<CreateShipmentResponse>(response, "GetShipment", new { id = shipment.Id });
            
        }
    
}

// Validation for creating shipments
internal sealed class CreateShipmentRequestValidator : AbstractValidator<CreateShipmentRequest>
{
    public  CreateShipmentRequestValidator()
    {
        var address = new AddressDtoValidator();
        RuleFor(a => a.Origin).NotNull().SetValidator(address);
        RuleFor(a => a.Destination).NotNull().SetValidator(address).NotEqual(a => a.Origin).WithMessage("Destination must differ from origin");
        RuleFor(a => a.WeightKg).InclusiveBetween(0.001m, 50_000m);
        
    }
}