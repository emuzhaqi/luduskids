using System.Net.Http.Headers;
using System.Net.Http.Json;
using LudusKids.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LudusKids.Api.Tests;

public static class TestClientExtensions
{
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        this LudusKidsApiFactory factory,
        string email,
        string password = "Password123!",
        bool asAdmin = false)
    {
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        registerResponse.EnsureSuccessStatusCode();

        if (asAdmin)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LudusKidsDbContext>();
            var user = await db.Users.SingleAsync(u => u.Email == email);
            user.Role = "Admin";
            await db.SaveChangesAsync();
        }

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        return client;
    }

    private record AuthResponse(string Token, string Email, string Role);
}
