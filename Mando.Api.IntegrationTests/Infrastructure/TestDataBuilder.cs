using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mando.Api.Data;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.IntegrationTests.Infrastructure;

public static class TestDataBuilder
{
    public static async Task<AppUser> GetUserAsync(
        CustomWebApplicationFactory factory,
        string email)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await context.Users.SingleAsync(x => x.Email == email);
    }

    public static async Task<Guid> CreateCustomerAsync(
        CustomWebApplicationFactory factory,
        string salesRepEmail,
        decimal openingBalance = 0m,
        decimal creditLimit = 1000m,
        CustomerStatus status = CustomerStatus.Active)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var salesRep = await context.Users.SingleAsync(x => x.Email == salesRepEmail);
        var token = Guid.NewGuid().ToString("N");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = $"Integration Customer {token[..8]}",
            Code = $"ITC-{token[..12]}",
            ContactPersonName = "Integration Contact",
            PhoneNumber = $"077{Random.Shared.Next(10000000, 99999999)}",
            Address = "Integration Test Address",
            City = "Baghdad",
            Region = "Test",
            Latitude = 33.315200m,
            Longitude = 44.366100m,
            Status = status,
            CreditLimit = creditLimit,
            OpeningBalance = openingBalance,
            AssignedSalesRepId = salesRep.Id,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        return customer.Id;
    }

    public static async Task<Guid> CreateProductAsync(
        CustomWebApplicationFactory factory,
        decimal unitPrice = 5m,
        ProductStatus status = ProductStatus.Active)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var token = Guid.NewGuid().ToString("N");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = $"Integration Product {token[..8]}",
            Code = $"ITP-{token[..12]}",
            Description = "Integration test product",
            UnitPrice = unitPrice,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        return product.Id;
    }

    public static async Task<Guid> CreateVisitAsync(
        CustomWebApplicationFactory factory,
        string salesRepEmail,
        Guid customerId,
        VisitStatus status = VisitStatus.InProgress)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var salesRep = await context.Users.SingleAsync(x => x.Email == salesRepEmail);
        var customer = await context.Customers.SingleAsync(x => x.Id == customerId);
        var now = DateTime.UtcNow;

        var visit = new Visit
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            SalesRepId = salesRep.Id,
            CheckInAtUtc = now,
            CheckInLatitude = customer.Latitude,
            CheckInLongitude = customer.Longitude,
            CheckInAccuracyInMeters = 10m,
            CheckOutAtUtc = status == VisitStatus.InProgress ? null : now.AddMinutes(30),
            CheckOutLatitude = status == VisitStatus.InProgress ? null : customer.Latitude,
            CheckOutLongitude = status == VisitStatus.InProgress ? null : customer.Longitude,
            CheckOutAccuracyInMeters = status == VisitStatus.InProgress ? null : 10m,
            DistanceFromCustomerInMeters = 0d,
            Status = status,
            Outcome = status switch
            {
                VisitStatus.Completed => VisitOutcome.Successful,
                VisitStatus.Cancelled => VisitOutcome.Cancelled,
                _ => VisitOutcome.Pending
            },
            CreatedAtUtc = now
        };

        context.Visits.Add(visit);
        await context.SaveChangesAsync();

        return visit.Id;
    }
}
