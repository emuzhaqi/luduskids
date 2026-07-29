using System.Net;
using System.Net.Http.Json;

namespace LudusKids.Api.Tests;

public class OrdersControllerTests : IDisposable
{
    private readonly LudusKidsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<int> SeedProductAsync(HttpClient admin, decimal price = 24.99m)
    {
        var category = await (await admin.PostAsJsonAsync("/api/categories", new { name = "Building Blocks" }))
            .Content.ReadFromJsonAsync<CategoryResponse>();

        var product = await (await admin.PostAsJsonAsync("/api/products", new
        {
            name = "Wooden Blocks Set",
            description = "A great toy",
            price,
            imageUrl = "https://example.com/product.jpg",
            stockQuantity = 10,
            categoryId = category!.Id,
        })).Content.ReadFromJsonAsync<ProductResponse>();

        return product!.Id;
    }

    [Fact]
    public async Task Checkout_WithEmptyCart_ReturnsBadRequest()
    {
        var customer = await _factory.CreateAuthenticatedClientAsync("customer@luduskids.test");

        var response = await customer.PostAsync("/api/orders", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_WithItemsInCart_CreatesOrderWithCorrectTotalAndEmptiesCart()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin@luduskids.test", asAdmin: true);
        var productId = await SeedProductAsync(admin, price: 10m);
        var customer = await _factory.CreateAuthenticatedClientAsync("customer@luduskids.test");
        await customer.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 3 });

        var checkoutResponse = await customer.PostAsync("/api/orders", null);

        Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);
        var order = await checkoutResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(30m, order!.TotalAmount);
        Assert.Equal("Pending", order.Status);
        Assert.Single(order.Items);
        Assert.Equal(3, order.Items[0].Quantity);

        var cart = await (await customer.GetAsync("/api/cart/items")).Content.ReadFromJsonAsync<List<object>>();
        Assert.Empty(cart!);
    }

    [Fact]
    public async Task GetOrders_AsCustomer_OnlyReturnsOwnOrders()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin@luduskids.test", asAdmin: true);
        var productId = await SeedProductAsync(admin);
        var customerA = await _factory.CreateAuthenticatedClientAsync("customerA@luduskids.test");
        var customerB = await _factory.CreateAuthenticatedClientAsync("customerB@luduskids.test");

        await customerA.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 1 });
        await customerA.PostAsync("/api/orders", null);
        await customerB.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 1 });
        await customerB.PostAsync("/api/orders", null);

        var ordersA = await (await customerA.GetAsync("/api/orders")).Content.ReadFromJsonAsync<List<OrderResponse>>();

        Assert.Single(ordersA!);
        Assert.Equal("customerA@luduskids.test", ordersA![0].UserEmail);
    }

    [Fact]
    public async Task GetOrders_AsAdmin_ReturnsOrdersFromAllUsers()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin@luduskids.test", asAdmin: true);
        var productId = await SeedProductAsync(admin);
        var customerA = await _factory.CreateAuthenticatedClientAsync("customerA@luduskids.test");
        var customerB = await _factory.CreateAuthenticatedClientAsync("customerB@luduskids.test");
        await customerA.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 1 });
        await customerA.PostAsync("/api/orders", null);
        await customerB.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 1 });
        await customerB.PostAsync("/api/orders", null);

        var allOrders = await (await admin.GetAsync("/api/orders")).Content.ReadFromJsonAsync<List<OrderResponse>>();

        Assert.Equal(2, allOrders!.Count);
    }

    [Fact]
    public async Task GetOrderById_AsDifferentCustomer_ReturnsForbidden()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin@luduskids.test", asAdmin: true);
        var productId = await SeedProductAsync(admin);
        var customerA = await _factory.CreateAuthenticatedClientAsync("customerA@luduskids.test");
        var customerB = await _factory.CreateAuthenticatedClientAsync("customerB@luduskids.test");
        await customerA.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 1 });
        var order = await (await customerA.PostAsync("/api/orders", null)).Content.ReadFromJsonAsync<OrderResponse>();

        var response = await customerB.GetAsync($"/api/orders/{order!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_AsCustomer_ReturnsForbidden()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin@luduskids.test", asAdmin: true);
        var productId = await SeedProductAsync(admin);
        var customer = await _factory.CreateAuthenticatedClientAsync("customer@luduskids.test");
        await customer.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 1 });
        var order = await (await customer.PostAsync("/api/orders", null)).Content.ReadFromJsonAsync<OrderResponse>();

        var response = await customer.PutAsJsonAsync($"/api/orders/{order!.Id}/status", new { status = "Shipped" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_AsAdmin_Succeeds()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin@luduskids.test", asAdmin: true);
        var productId = await SeedProductAsync(admin);
        var customer = await _factory.CreateAuthenticatedClientAsync("customer@luduskids.test");
        await customer.PostAsJsonAsync("/api/cart/items", new { productId, quantity = 1 });
        var order = await (await customer.PostAsync("/api/orders", null)).Content.ReadFromJsonAsync<OrderResponse>();

        var updateResponse = await admin.PutAsJsonAsync($"/api/orders/{order!.Id}/status", new { status = "Shipped" });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
        var updatedOrder = await (await admin.GetAsync($"/api/orders/{order.Id}")).Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Shipped", updatedOrder!.Status);
    }

    private record CategoryResponse(int Id, string Name);
    private record ProductResponse(int Id, string Name);
    private record OrderItemResponse(int ProductId, string ProductName, int Quantity, decimal UnitPrice);
    private record OrderResponse(int Id, DateTime CreatedAt, string Status, decimal TotalAmount, string UserEmail, List<OrderItemResponse> Items);
}
