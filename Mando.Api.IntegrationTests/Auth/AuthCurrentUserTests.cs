using System.Net;
using Mando.Api.IntegrationTests.Contracts.Auth;
using Mando.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace Mando.Api.IntegrationTests.Auth;

public sealed class AuthCurrentUserTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthCurrentUserTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsCurrentUser()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(
            TestHostSettings.AdminEmail,
            TestHostSettings.AdminPassword);

        var response = await client.GetAsync("/api/auth/me");

        response.EnsureSuccessStatusCode();

        var payload = await response.ReadSuccessAsync<CurrentUserResponseDto>();
        Assert.NotNull(payload);
        Assert.NotNull(payload!.Data);
        Assert.True(payload.Success);
        Assert.Equal(TestHostSettings.AdminEmail, payload.Data!.Email);
        Assert.Contains("Admin", payload.Data.Roles);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
