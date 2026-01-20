using MediatR;
using OrderService.Application.Interfaces;
using BuildingBlocks.Common.Models;

namespace OrderService.Application.Commands;

public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<Result<bool>>;

public sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelOrderCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, ct);
        if (order is null)
            return Result<bool>.Failure($"Order {request.OrderId} not found.");

        order.Cancel(request.Reason);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}
