namespace OrderService.Domain.Exceptions;

public sealed class OrderDomainException : Exception
{
    public OrderDomainException(string message) : base(message) { }
}
