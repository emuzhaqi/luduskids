using System.Net;
using System.Net.Http.Json;

namespace LudusKids.Api.Tests;

public class AuthControllerTests : IDisposable
{
    private readonly LudusKidsApiFactory _factory = new();
    private readonly HttpClient _client;

    public AuthControllerTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Register_WithNewEmail_ReturnsTokenAndCustomerRole()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email = "parent@luduskids.test", password = "Password123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(body!.Token));
        Assert.Equal("parent@luduskids.test", body.Email);
        Assert.Equal("Customer", body.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new { email = "dup@luduskids.test", password = "Password123!" });

        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email = "dup@luduskids.test", password = "Password123!" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new { email = "login@luduskids.test", password = "Password123!" });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email = "login@luduskids.test", password = "Password123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(body!.Token));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new { email = "wrongpass@luduskids.test", password = "Password123!" });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email = "wrongpass@luduskids.test", password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsCurrentUser()
    {
        var authedClient = await _factory.CreateAuthenticatedClientAsync("me@luduskids.test");

        var response = await authedClient.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("me@luduskids.test", body!.Email);
    }

    private record AuthResponse(string Token, string Email, string Role);
    private record UserResponse(string Email, string Role);
}
