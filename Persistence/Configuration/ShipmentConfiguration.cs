using Logistics.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Api.Persistence.Configuration;

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.TrackingNumber).HasMaxLength(32);
        builder.HasIndex(s => s.TrackingNumber).IsUnique();
        
        builder.Property(s => s.ShipmentStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(s => s.WeightKg).HasPrecision(10, 3);

        builder.ComplexProperty(s => s.Origin, ConfigureAddress);
        builder.ComplexProperty(s => s.Destination, ConfigureAddress);

        
    }

    private static void ConfigureAddress(ComplexPropertyBuilder<Address> address)
    {
        address.Property(a => a.Line1).HasMaxLength(200);
        address.Property(a => a.City).HasMaxLength(100);
        address.Property(a => a.PostalCode).HasMaxLength(20);
        address.Property(a => a.Country).HasMaxLength(4);
    }
}