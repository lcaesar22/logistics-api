using System.Globalization;
using System.Security.Cryptography;

namespace Logistics.Api.Domain;

public sealed class Shipment
{
    public Guid Id { get; private set; }
    public string TrackingNumber { get; private set; }
    public ShipmentStatus ShipmentStatus { get; private set; }
    public Address Origin { get; private set; }
    public Address Destination { get; private set; }
    public decimal WeightKg { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }


    public static Shipment CreateShipment(
        Address origin,
        Address destination,
        decimal weightKg,
        DateTimeOffset createdAt) => new()
    {
        Id = Guid.CreateVersion7(),
        TrackingNumber = NewTrackingNumber(createdAt),
        ShipmentStatus = ShipmentStatus.Created,
        Origin = origin,
        Destination = destination,
        WeightKg = weightKg,
        CreatedAt = createdAt
    };

    private static string NewTrackingNumber(DateTimeOffset createdAt) => string.Create(CultureInfo.InvariantCulture,
        $"LGX-{createdAt:yy-MM-dd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}");
    
    // Origin, destination and weight can be deleted until pickup
    public bool CanBeChanged => ShipmentStatus == ShipmentStatus.Created;

    public bool TryUpdate(Address origin, Address destination, decimal weightKg)
    {
        if (!CanBeChanged)
        {
            return false;
        }
        
        Origin = origin;
        Destination = destination;
        WeightKg = weightKg;
        return true;
    }
}


public enum ShipmentStatus
{
    Created,
    PickedUp,
    InTransit,
    OutOfDelivery,
    Delivered,
    Cancelled
}



public sealed record Address(string Line1, string City, string PostalCode, string Country);