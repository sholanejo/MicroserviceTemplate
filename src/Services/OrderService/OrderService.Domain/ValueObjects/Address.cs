namespace OrderService.Domain.ValueObjects;

public sealed record Address(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);
