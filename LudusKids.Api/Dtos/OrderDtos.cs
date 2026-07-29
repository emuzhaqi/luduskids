namespace LudusKids.Api.Dtos;

public record OrderItemResponse(int ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record OrderResponse(
    int Id,
    DateTime CreatedAt,
    string Status,
    decimal TotalAmount,
    string UserEmail,
    List<OrderItemResponse> Items);

public record UpdateOrderStatusRequest(string Status);
