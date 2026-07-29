using System.Net;
using System.Net.Http.Json;

namespace LudusKids.Api.Tests;

public class CatalogControllerTests : IDisposable
{
    private readonly LudusKidsApiFactory _factory = new();
    private readonly HttpClient _client;

    public CatalogControllerTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetProducts_Unauthenticated_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProductById_Unknown_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new { name = "Puzzles" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsCustomer_ReturnsForbidden()
    {
        var customer = await _factory.CreateAuthenticatedClientAsync("customer@luduskids.test");

        var response = await customer.PostAsJsonAsync("/api/categories", new { name = "Puzzles" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsAdmin_Succeeds()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin@luduskids.test", asAdmin: true);

        var response = await admin.PostAsJsonAsync("/api/categories", new { name = "Puzzles" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.Equal("Puzzles", body!.Name);
    }

    [Fact]
    public async Task CreateProduct_WithUnknownCategory_ReturnsBadRequest()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin2@luduskids.test", asAdmin: true);

        var response = await admin.PostAsJsonAsync("/api/products", new
        {
            name = "Toy Train",
            description = "Choo choo",
            price = 15.99,
            imageUrl = "https://example.com/train.jpg",
            stockQuantity = 5,
            categoryId = 999,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_AppearsInPublicCatalog()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin3@luduskids.test", asAdmin: true);
        var category = await (await admin.PostAsJsonAsync("/api/categories", new { name = "Vehicles" }))
            .Content.ReadFromJsonAsync<CategoryResponse>();

        var createResponse = await admin.PostAsJsonAsync("/api/products", new
        {
            name = "Toy Train",
            description = "Choo choo",
            price = 15.99,
            imageUrl = "https://example.com/train.jpg",
            stockQuantity = 5,
            categoryId = category!.Id,
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var products = await (await _client.GetAsync("/api/products")).Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.Contains(products!, p => p.Name == "Toy Train");
    }

    [Fact]
    public async Task DeleteProduct_AsAdmin_RemovesIt()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin4@luduskids.test", asAdmin: true);
        var category = await (await admin.PostAsJsonAsync("/api/categories", new { name = "Books" }))
            .Content.ReadFromJsonAsync<CategoryResponse>();
        var product = await (await admin.PostAsJsonAsync("/api/products", new
        {
            name = "Picture Book",
            description = "A fun read",
            price = 9.99,
            imageUrl = "https://example.com/book.jpg",
            stockQuantity = 3,
            categoryId = category!.Id,
        })).Content.ReadFromJsonAsync<ProductResponse>();

        var deleteResponse = await admin.DeleteAsync($"/api/products/{product!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private record CategoryResponse(int Id, string Name);
    private record ProductResponse(int Id, string Name, string Description, decimal Price, string ImageUrl, int StockQuantity, int CategoryId, string CategoryName);
}
