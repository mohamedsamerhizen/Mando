using System.Net;
using System.Net.Http.Json;
using Mando.Api.IntegrationTests.Contracts.Auth;
using Mando.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace Mando.Api.IntegrationTests.Auth;

public sealed class AuthLoginTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthLoginTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorizedEnvelope()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = TestHostSettings.AdminEmail,
            Password = "WrongPassword123"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var payload = await response.ReadErrorAsync();
        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.Equal("invalid_credentials", payload.Code);
    }
}
