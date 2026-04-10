using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Mando.Api.IntegrationTests.Contracts.Auth;
using Mando.Api.IntegrationTests.Contracts.Common;

namespace Mando.Api.IntegrationTests.Infrastructure;

public static class TestApiClientExtensions
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        this CustomWebApplicationFactory factory,
        string email,
        string password)
    {
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        var loginEnvelope = await loginResponse.ReadSuccessAsync<LoginResponseDto>();
        loginEnvelope.Should().NotBeNull();
        loginEnvelope!.Data.Should().NotBeNull();
        loginEnvelope.Data!.Token.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginEnvelope.Data.Token);
        return client;
    }

    public static async Task<ApiSuccessResponseDto<T>?> ReadSuccessAsync<T>(this HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<ApiSuccessResponseDto<T>>(JsonOptions);
    }

    public static async Task<ApiErrorResponseDto?> ReadErrorAsync(this HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<ApiErrorResponseDto>(JsonOptions);
    }
}
