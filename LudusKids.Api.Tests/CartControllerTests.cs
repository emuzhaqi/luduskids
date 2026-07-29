using System.Net;
using System.Net.Http.Json;

namespace LudusKids.Api.Tests;

public class CartControllerTests : IDisposable
{
    private readonly LudusKidsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<int> SeedProductAsync(HttpClient admin, string name = "Wooden Blocks Set", decimal price = 24.99m)
    {
        var category = await (await admin.PostAsJsonAsync("/api/categories", new { name = "Building Blocks" }))
            .Content.ReadFromJsonAsync<CategoryResponse>();

        var product = await (await admin.PostAsJsonAsync("/api/products", new
        {
            name,
            description = "A great toy",
            price,
            imageUrl = "https://example.com/product.jpg",
            stockQuantity = 10,
            categoryId = category!.Id,
        })).Content.ReadFromJsonAsync<ProductResponse>();

        return product!.Id;
    }

    [Fact]
    public async Task GetCart_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/cart/items");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_ThenGetCart_ReturnsTheItem()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin@luduskids.test", asAdmin: true);
        var productId = await SeedProductAsync(admin);
        var customer = await _factory.CreateAuthenticatedClientAsync("customer@luduskids.test");

        await customer.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 2 });
        var cart = await (await customer.GetAsync("/api/cart/items")).Content.ReadFromJsonAsync<List<CartItemResponse>>();

        Assert.Single(cart!);
        Assert.Equal(2, cart![0].Quantity);
        Assert.Equal(productId, cart[0].ProductId);
    }

    [Fact]
    public async Task AddItem_SameProductTwice_IncrementsQuantityInsteadOfDuplicating()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin@luduskids.test", asAdmin: true);
        var productId = await SeedProductAsync(admin);
        var customer = await _factory.CreateAuthenticatedClientAsync("customer@luduskids.test");

        await customer.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 1 });
        await customer.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 3 });
        var cart = await (await customer.GetAsync("/api/cart/items")).Content.ReadFromJsonAsync<List<CartItemResponse>>();

        Assert.Single(cart!);
        Assert.Equal(4, cart![0].Quantity);
    }

    [Fact]
    public async Task UpdateItem_ChangesQuantity()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin@luduskids.test", asAdmin: true);
        var productId = await SeedProductAsync(admin);
        var customer = await _factory.CreateAuthenticatedClientAsync("customer@luduskids.test");
        var added = await (await customer.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 1 }))
            .Content.ReadFromJsonAsync<CartItemResponse>();

        var updateResponse = await customer.PutAsJsonAsync($"/api/cart/items/{added!.Id}", new { quantity = 5 });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
        var cart = await (await customer.GetAsync("/api/cart/items")).Content.ReadFromJsonAsync<List<CartItemResponse>>();
        Assert.Equal(5, cart![0].Quantity);
    }

    [Fact]
    public async Task RemoveItem_RemovesItFromCart()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin@luduskids.test", asAdmin: true);
        var productId = await SeedProductAsync(admin);
        var customer = await _factory.CreateAuthenticatedClientAsync("customer@luduskids.test");
        var added = await (await customer.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 1 }))
            .Content.ReadFromJsonAsync<CartItemResponse>();

        var deleteResponse = await customer.DeleteAsync($"/api/cart/items/{added!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var cart = await (await customer.GetAsync("/api/cart/items")).Content.ReadFromJsonAsync<List<CartItemResponse>>();
        Assert.Empty(cart!);
    }

    [Fact]
    public async Task Cart_IsScopedPerUser()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin@luduskids.test", asAdmin: true);
        var productId = await SeedProductAsync(admin);
        var customerA = await _factory.CreateAuthenticatedClientAsync("customerA@luduskids.test");
        var customerB = await _factory.CreateAuthenticatedClientAsync("customerB@luduskids.test");

        var itemA = await (await customerA.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 1 }))
            .Content.ReadFromJsonAsync<CartItemResponse>();

        var bCart = await (await customerB.GetAsync("/api/cart/items")).Content.ReadFromJsonAsync<List<CartItemResponse>>();
        Assert.Empty(bCart!);

        var bTriesToUpdateAsItem = await customerB.PutAsJsonAsync($"/api/cart/items/{itemA!.Id}", new { quantity = 99 });
        Assert.Equal(HttpStatusCode.NotFound, bTriesToUpdateAsItem.StatusCode);
    }

    private record CategoryResponse(int Id, string Name);
    private record ProductResponse(int Id, string Name);
    private record CartItemResponse(int Id, int ProductId, string ProductName, decimal UnitPrice, int Quantity);
}
