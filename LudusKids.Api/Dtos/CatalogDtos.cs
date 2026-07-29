namespace LudusKids.Api.Dtos;

public record CategoryResponse(int Id, string Name);

public record CategoryRequest(string Name);

public record ProductResponse(
    int Id,
    string Name,
    string Description,
    decimal Price,
    string ImageUrl,
    int StockQuantity,
    int CategoryId,
    string CategoryName);

public record ProductRequest(
    string Name,
    string Description,
    decimal Price,
    string ImageUrl,
    int StockQuantity,
    int CategoryId);
