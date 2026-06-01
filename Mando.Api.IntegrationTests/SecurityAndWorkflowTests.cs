using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Mando.Api.DTOs.Orders;
using Mando.Api.DTOs.Payments;
using Mando.Api.DTOs.Visits;
using Mando.Api.Enums;
using Mando.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace Mando.Api.IntegrationTests;

public sealed class SecurityAndWorkflowTests
{
    [Theory]
    [InlineData("/api/customers")]
    [InlineData("/api/orders")]
    [InlineData("/api/payments")]
    [InlineData("/api/users")]
    public async Task ProtectedEndpoints_WithoutAuthentication_ReturnUnauthorized(string path)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SalesRep_CannotReadAnotherSalesRepsCustomer()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            TestHostSettings.AliEmail,
            TestHostSettings.SalesRepPassword);

        var saraCustomerId = await TestDataBuilder.CreateCustomerAsync(factory, TestHostSettings.SaraEmail);

        var response = await client.GetAsync($"/api/customers/{saraCustomerId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SalesRep_CannotReadAnotherSalesRepsVisit()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            TestHostSettings.AliEmail,
            TestHostSettings.SalesRepPassword);

        var saraCustomerId = await TestDataBuilder.CreateCustomerAsync(factory, TestHostSettings.SaraEmail);
        var saraVisitId = await TestDataBuilder.CreateVisitAsync(factory, TestHostSettings.SaraEmail, saraCustomerId);

        var response = await client.GetAsync($"/api/visits/{saraVisitId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminManagerEndpoint_WithSalesRepToken_ReturnsForbidden()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            TestHostSettings.AliEmail,
            TestHostSettings.SalesRepPassword);

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_DuplicateNormalizedReference_ReturnsConflict()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            TestHostSettings.AliEmail,
            TestHostSettings.SalesRepPassword);

        var customerId = await TestDataBuilder.CreateCustomerAsync(
            factory,
            TestHostSettings.AliEmail,
            openingBalance: 100m);
        var visitId = await TestDataBuilder.CreateVisitAsync(factory, TestHostSettings.AliEmail, customerId);

        var firstResponse = await client.PostAsJsonAsync("/api/payments", new CreatePaymentRequestDto
        {
            VisitId = visitId,
            Amount = 10m,
            PaymentMethod = PaymentMethod.BankTransfer,
            Reference = "bank-ref-001"
        });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await client.PostAsJsonAsync("/api/payments", new CreatePaymentRequestDto
        {
            VisitId = visitId,
            Amount = 5m,
            PaymentMethod = PaymentMethod.BankTransfer,
            Reference = "BANKREF001"
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var payload = await duplicateResponse.ReadErrorAsync();
        Assert.NotNull(payload);
        Assert.Equal("duplicate_pending_reference", payload!.Code);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidProduct_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            TestHostSettings.AliEmail,
            TestHostSettings.SalesRepPassword);

        var customerId = await TestDataBuilder.CreateCustomerAsync(factory, TestHostSettings.AliEmail);
        var visitId = await TestDataBuilder.CreateVisitAsync(factory, TestHostSettings.AliEmail, customerId);

        var response = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequestDto
        {
            VisitId = visitId,
            PaymentType = PaymentType.Credit,
            Items =
            [
                new CreateOrderItemRequestDto
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1m
                }
            ]
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.ReadErrorAsync();
        Assert.NotNull(payload);
        Assert.Equal("invalid_or_inactive_products", payload!.Code);
    }

    [Fact]
    public async Task StartVisit_WithWeakGpsAccuracy_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            TestHostSettings.AliEmail,
            TestHostSettings.SalesRepPassword);

        var customerId = await TestDataBuilder.CreateCustomerAsync(factory, TestHostSettings.AliEmail);

        var response = await client.PostAsJsonAsync("/api/visits/start", new StartVisitRequestDto
        {
            CustomerId = customerId,
            Latitude = 33.315200m,
            Longitude = 44.366100m,
            AccuracyInMeters = 1000m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.ReadErrorAsync();
        Assert.NotNull(payload);
        Assert.Equal("weak_location_accuracy", payload!.Code);
    }

    [Fact]
    public async Task InvalidJwt_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredJwt_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateExpiredToken());

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string CreateExpiredToken()
    {
        var now = DateTime.UtcNow;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestHostSettings.JwtKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestHostSettings.JwtIssuer,
            audience: TestHostSettings.JwtAudience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            ],
            notBefore: now.AddHours(-2),
            expires: now.AddHours(-1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
