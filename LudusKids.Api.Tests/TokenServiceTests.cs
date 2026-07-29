using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LudusKids.Api.Models;
using LudusKids.Api.Services;
using Microsoft.Extensions.Configuration;

namespace LudusKids.Api.Tests;

public class TokenServiceTests
{
    private static TokenService CreateService(string expiresMinutes = "60")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "unit-test-signing-key-at-least-32-characters-long",
                ["Jwt:Issuer"] = "LudusKidsApi.Tests",
                ["Jwt:ExpiresMinutes"] = expiresMinutes,
            })
            .Build();

        return new TokenService(config);
    }

    [Fact]
    public void CreateToken_IncludesUserIdEmailAndRoleClaims()
    {
        var service = CreateService();
        var user = new User { Id = 42, Email = "parent@luduskids.test", Role = "Admin" };

        var token = service.CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("42", jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("parent@luduskids.test", jwt.Claims.Single(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Admin", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void CreateToken_SetsIssuerAndAudienceFromConfig()
    {
        var service = CreateService();
        var user = new User { Id = 1, Email = "a@b.test", Role = "Customer" };

        var token = service.CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("LudusKidsApi.Tests", jwt.Issuer);
        Assert.Equal("LudusKidsApi.Tests", jwt.Audiences.Single());
    }

    [Fact]
    public void CreateToken_ExpiresApproximatelyAfterConfiguredMinutes()
    {
        var service = CreateService(expiresMinutes: "30");
        var user = new User { Id = 1, Email = "a@b.test", Role = "Customer" };

        var token = service.CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var expectedExpiry = DateTime.UtcNow.AddMinutes(30);
        Assert.True(Math.Abs((jwt.ValidTo - expectedExpiry).TotalMinutes) < 1);
    }
}
