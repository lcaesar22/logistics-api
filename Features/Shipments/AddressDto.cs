using FluentValidation;
using Logistics.Api.Domain;

namespace Logistics.Api.Features.Shipments;

// Shared DTO within the Shipments feature

public sealed record AddressDto(string Line1, string City, string PostalCode, string Country)
{
    public Address ToDomain() => new Address(Line1, City, PostalCode, Country);
    
    public static AddressDto From(Address address) => new(address.Line1, address.City, address.PostalCode, address.Country);
    
}

// Validate the addressdto
internal sealed class AddressDtoValidator : AbstractValidator<AddressDto>
{
    public AddressDtoValidator()
    {
        RuleFor(a => a.Line1).NotEmpty().MaximumLength(200);
        RuleFor(a => a.City).NotEmpty().MaximumLength(100);
        RuleFor(a => a.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(a => a.Country).NotEmpty().Matches("^[A-Za-z]{2}$")
            .WithMessage("Country must be a 2-letter ISO code (eg. NL).");
    }
}