namespace LudusKids.Api.Dtos;

public record CartItemResponse(int Id, int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record AddCartItemRequest(int ProductId, int Quantity);

public record UpdateCartItemRequest(int Quantity);
