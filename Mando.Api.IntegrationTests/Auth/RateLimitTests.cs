using System.Net;
using System.Net.Http.Json;
using Mando.Api.DTOs.Payments;
using Mando.Api.DTOs.Visits;
using Mando.Api.IntegrationTests.Contracts.Auth;
using Mando.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace Mando.Api.IntegrationTests.Auth;

public sealed class RateLimitTests
{
    [Fact]
    public async Task Login_WhenLimitExceeded_ReturnsRateLimitEnvelope()
    {
        using var factory = new CustomWebApplicationFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:Login:PermitLimit"] = "1",
            ["RateLimiting:Login:WindowSeconds"] = "60"
        });
        using var client = factory.CreateClient();

        var request = new LoginRequestDto
        {
            Email = TestHostSettings.AdminEmail,
            Password = "WrongPassword123"
        };

        var firstResponse = await client.PostAsJsonAsync("/api/auth/login", request);
        var secondResponse = await client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, firstResponse.StatusCode);
        await AssertRateLimitedAsync(secondResponse);
    }

    [Fact]
    public async Task PaymentApproval_WhenLimitExceeded_ReturnsRateLimitEnvelope()
    {
        using var factory = CreateSensitiveMutationLimitedFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            TestHostSettings.AdminEmail,
            TestHostSettings.AdminPassword);

        var request = new ApprovePaymentRequestDto
        {
            RowVersion = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
            ReviewComment = "Reviewed for rate-limit verification.",
            AcknowledgeStalePayment = true,
            AcknowledgeHighBalanceImpact = true,
            AcknowledgeMultiplePendingPayments = true,
            AcknowledgeDuplicateReference = true
        };

        var path = $"/api/payments/{Guid.NewGuid()}/approve";
        var firstResponse = await client.PatchAsJsonAsync(path, request);
        var secondResponse = await client.PatchAsJsonAsync(path, request);

        Assert.NotEqual(HttpStatusCode.TooManyRequests, firstResponse.StatusCode);
        await AssertRateLimitedAsync(secondResponse);
    }

    [Fact]
    public async Task VisitStart_WhenLimitExceeded_ReturnsRateLimitEnvelope()
    {
        using var factory = CreateSensitiveMutationLimitedFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            TestHostSettings.AliEmail,
            TestHostSettings.SalesRepPassword);

        var request = new StartVisitRequestDto
        {
            CustomerId = Guid.NewGuid(),
            Latitude = 33.315200m,
            Longitude = 44.366100m,
            AccuracyInMeters = 20m
        };

        var firstResponse = await client.PostAsJsonAsync("/api/visits/start", request);
        var secondResponse = await client.PostAsJsonAsync("/api/visits/start", request);

        Assert.NotEqual(HttpStatusCode.TooManyRequests, firstResponse.StatusCode);
        await AssertRateLimitedAsync(secondResponse);
    }

    private static CustomWebApplicationFactory CreateSensitiveMutationLimitedFactory()
    {
        return new CustomWebApplicationFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:SensitiveMutation:PermitLimit"] = "1",
            ["RateLimiting:SensitiveMutation:WindowSeconds"] = "60"
        });
    }

    private static async Task AssertRateLimitedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Retry-After", out _));

        var payload = await response.ReadErrorAsync();
        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.Equal("rate_limit_exceeded", payload.Code);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }
}
