using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
        using var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = TestHostSettings.AdminEmail,
            Password = TestHostSettings.AdminPassword
        });

        loginResponse.EnsureSuccessStatusCode();

        var loginEnvelope = await loginResponse.ReadSuccessAsync<LoginResponseDto>();
        Assert.NotNull(loginEnvelope);
        Assert.NotNull(loginEnvelope!.Data);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginEnvelope.Data!.Token);

        var response = await client.GetAsync("/api/auth/me");

        response.EnsureSuccessStatusCode();

        var payload = await response.ReadSuccessAsync<CurrentUserResponseDto>();
        Assert.NotNull(payload);
        Assert.NotNull(payload!.Data);
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
