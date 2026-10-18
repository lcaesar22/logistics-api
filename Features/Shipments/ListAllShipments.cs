using FluentValidation;
using Logistics.Api.Common.Auth;
using Logistics.Api.Common.Endpoints;
using Logistics.Api.Common.Pagination;
using Logistics.Api.Common.Validation;
using Logistics.Api.Domain;
using Logistics.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Features.Shipments;

public sealed record ListShipmentQueryRequest(
    [FromQuery(Name = "page")] int Page = 1,
    [FromQuery(Name = "pageSize")] int PageSize = 20);

public sealed record ShipmentSummary(
    Guid Id,
    string TrackingNumber,
    string ShipmentStatus,
    string OriginCity,
    string DestinationCity,
    decimal WeightKg,
    DateTimeOffset CreatedAt)
{
    public static ShipmentSummary From(Shipment s) => new(
        s.Id, s.TrackingNumber, s.ShipmentStatus.ToString(), s.Origin.City, s.Destination.City, s.WeightKg,
        s.CreatedAt);
}

// List validator
internal sealed class ListShipmentsQueryValidator : AbstractValidator<ListShipmentQueryRequest>
{
    public ListShipmentsQueryValidator()
    {
        RuleFor(q => q.Page).InclusiveBetween(1, 1000);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}

// GetAllShipment endpoint
internal sealed class ListShipmentsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/shipments", HandleAsync).WithName("ListAllShipments").WithTags("Shipments")
            .RequireAuthorization(Policies.ReadShipments).WithValidation<ListShipmentsQueryValidator>();
        
    }

    public static async Task<Ok<PaginationResponse<ShipmentSummary>>> HandleAsync(
        [AsParameters] ListShipmentQueryRequest query,
        LogisticsDbContext db,
        CancellationToken ct)
    {
        var shipments = db.Shipments.AsNoTracking();
        var totalCount = await shipments.CountAsync(ct);
        
        var page = await shipments
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);
        
        var items = page.Select(ShipmentSummary.From).ToList();
        
        return TypedResults.Ok(new PaginationResponse<ShipmentSummary>(items, query.Page, query.PageSize, totalCount));
    }
}