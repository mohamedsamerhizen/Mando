using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mando.Api.Common;
using Mando.Api.Data;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Data.Seeders;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var role in AppRoles.All)
        {
            var exists = await roleManager.RoleExistsAsync(role);
            if (!exists)
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Name = role
                });
            }
        }

        var adminUser = await EnsureUserAsync(
            userManager,
            configuration["SeedAdmin:FullName"],
            configuration["SeedAdmin:Email"],
            configuration["SeedAdmin:Password"],
            AppRoles.Admin);

        var managerUser = await EnsureUserAsync(
            userManager,
            configuration["SeedManager:FullName"],
            configuration["SeedManager:Email"],
            configuration["SeedManager:Password"],
            AppRoles.Manager);

        var salesReps = new List<AppUser>();

        var salesRepSections = configuration.GetSection("SeedSalesReps").GetChildren().ToList();

        foreach (var section in salesRepSections)
        {
            var salesRep = await EnsureUserAsync(
                userManager,
                section["FullName"],
                section["Email"],
                section["Password"],
                AppRoles.SalesRep);

            if (salesRep is not null)
            {
                salesReps.Add(salesRep);
            }
        }

        var seedDataEnabled = configuration.GetValue<bool>("SeedData:Enabled");
        if (!seedDataEnabled)
            return;

        await SeedProductsAsync(context);
        await SeedCustomersAsync(context, salesReps);
        await SeedVisitsOrdersPaymentsAsync(context, salesReps, adminUser, managerUser);
    }

    private static async Task<AppUser?> EnsureUserAsync(
        UserManager<AppUser> userManager,
        string? fullName,
        string? email,
        string? password,
        string role)
    {
        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            var existingRoles = await userManager.GetRolesAsync(existingUser);
            if (!existingRoles.Contains(role))
            {
                await userManager.AddToRoleAsync(existingUser, role);
            }

            return existingUser;
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            UserName = email,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(" | ", createResult.Errors.Select(x => x.Description));
            throw new Exception($"Seed user failed for '{email}': {errors}");
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(" | ", roleResult.Errors.Select(x => x.Description));
            throw new Exception($"Assign role failed for '{email}': {errors}");
        }

        return user;
    }

    private static async Task SeedProductsAsync(AppDbContext context)
    {
        if (await context.Products.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Mineral Water 1.5L",
                Code = "PRD-001",
                Description = "Packaged mineral water bottle 1.5 liter",
                UnitPrice = 1.25m,
                Status = ProductStatus.Active,
                CreatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Soft Drink Cola 330ml",
                Code = "PRD-002",
                Description = "Carbonated soft drink can 330ml",
                UnitPrice = 0.75m,
                Status = ProductStatus.Active,
                CreatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Orange Juice 1L",
                Code = "PRD-003",
                Description = "Orange juice bottle 1 liter",
                UnitPrice = 2.10m,
                Status = ProductStatus.Active,
                CreatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Energy Drink 250ml",
                Code = "PRD-004",
                Description = "Energy drink can 250ml",
                UnitPrice = 1.10m,
                Status = ProductStatus.Active,
                CreatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Sparkling Water 500ml",
                Code = "PRD-005",
                Description = "Sparkling water bottle 500ml",
                UnitPrice = 0.95m,
                Status = ProductStatus.Active,
                CreatedAtUtc = now
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCustomersAsync(AppDbContext context, List<AppUser> salesReps)
    {
        if (await context.Customers.AnyAsync())
            return;

        if (salesReps.Count == 0)
            return;

        var ali = salesReps.ElementAtOrDefault(0);
        var sara = salesReps.ElementAtOrDefault(1);
        var omar = salesReps.ElementAtOrDefault(2);

        var now = DateTime.UtcNow;
        var customers = new List<Customer>();

        if (ali is not null)
        {
            customers.AddRange(
            [
                new Customer
                {
                    Id = Guid.NewGuid(),
                    Name = "Al Noor Market",
                    Code = "CUS-001",
                    ContactPersonName = "Hussein Kareem",
                    PhoneNumber = "07700000001",
                    Address = "Baghdad - Karrada",
                    City = "Baghdad",
                    Region = "Karrada",
                    Latitude = 33.315200m,
                    Longitude = 44.366100m,
                    Status = CustomerStatus.Active,
                    CreditLimit = 1500m,
                    OpeningBalance = 250m,
                    Notes = "Key grocery customer",
                    AssignedSalesRepId = ali.Id,
                    CreatedAtUtc = now
                },
                new Customer
                {
                    Id = Guid.NewGuid(),
                    Name = "Al Waha Mini Market",
                    Code = "CUS-002",
                    ContactPersonName = "Ali Jabbar",
                    PhoneNumber = "07700000002",
                    Address = "Baghdad - Jadriya",
                    City = "Baghdad",
                    Region = "Jadriya",
                    Latitude = 33.277800m,
                    Longitude = 44.383600m,
                    Status = CustomerStatus.Active,
                    CreditLimit = 1200m,
                    OpeningBalance = 100m,
                    Notes = "Fast moving beverages",
                    AssignedSalesRepId = ali.Id,
                    CreatedAtUtc = now
                }
            ]);
        }

        if (sara is not null)
        {
            customers.AddRange(
            [
                new Customer
                {
                    Id = Guid.NewGuid(),
                    Name = "Tigris Supermarket",
                    Code = "CUS-003",
                    ContactPersonName = "Sara Mahmoud",
                    PhoneNumber = "07700000003",
                    Address = "Baghdad - Mansour",
                    City = "Baghdad",
                    Region = "Mansour",
                    Latitude = 33.309300m,
                    Longitude = 44.338200m,
                    Status = CustomerStatus.Active,
                    CreditLimit = 2000m,
                    OpeningBalance = 400m,
                    Notes = "High-value customer",
                    AssignedSalesRepId = sara.Id,
                    CreatedAtUtc = now
                },
                new Customer
                {
                    Id = Guid.NewGuid(),
                    Name = "Family Grocery",
                    Code = "CUS-004",
                    ContactPersonName = "Mustafa Adnan",
                    PhoneNumber = "07700000004",
                    Address = "Baghdad - Yarmouk",
                    City = "Baghdad",
                    Region = "Yarmouk",
                    Latitude = 33.295700m,
                    Longitude = 44.311500m,
                    Status = CustomerStatus.Active,
                    CreditLimit = 900m,
                    OpeningBalance = 80m,
                    Notes = "Neighborhood retailer",
                    AssignedSalesRepId = sara.Id,
                    CreatedAtUtc = now
                }
            ]);
        }

        if (omar is not null)
        {
            customers.AddRange(
            [
                new Customer
                {
                    Id = Guid.NewGuid(),
                    Name = "Dijlah Retail Store",
                    Code = "CUS-005",
                    ContactPersonName = "Omar Saad",
                    PhoneNumber = "07700000005",
                    Address = "Baghdad - Adhamiya",
                    City = "Baghdad",
                    Region = "Adhamiya",
                    Latitude = 33.377600m,
                    Longitude = 44.379900m,
                    Status = CustomerStatus.Active,
                    CreditLimit = 1300m,
                    OpeningBalance = 300m,
                    Notes = "Medium-size retailer",
                    AssignedSalesRepId = omar.Id,
                    CreatedAtUtc = now
                },
                new Customer
                {
                    Id = Guid.NewGuid(),
                    Name = "Babylon Shop",
                    Code = "CUS-006",
                    ContactPersonName = "Zainab Qasim",
                    PhoneNumber = "07700000006",
                    Address = "Baghdad - Sadr City",
                    City = "Baghdad",
                    Region = "Sadr City",
                    Latitude = 33.381500m,
                    Longitude = 44.465900m,
                    Status = CustomerStatus.Active,
                    CreditLimit = 1100m,
                    OpeningBalance = 150m,
                    Notes = "Cash-heavy customer",
                    AssignedSalesRepId = omar.Id,
                    CreatedAtUtc = now
                }
            ]);
        }

        if (customers.Count == 0)
            return;

        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();
    }

    private static async Task SeedVisitsOrdersPaymentsAsync(
        AppDbContext context,
        List<AppUser> salesReps,
        AppUser? adminUser,
        AppUser? managerUser)
    {
        if (await context.Visits.AnyAsync() || await context.Orders.AnyAsync() || await context.Payments.AnyAsync())
            return;

        if (salesReps.Count == 0)
            return;

        var customers = await context.Customers
            .OrderBy(x => x.Code)
            .ToListAsync();

        var products = await context.Products
            .OrderBy(x => x.Code)
            .ToListAsync();

        if (customers.Count < 6 || products.Count < 5)
            return;

        var ali = salesReps.ElementAtOrDefault(0);
        var sara = salesReps.ElementAtOrDefault(1);
        var omar = salesReps.ElementAtOrDefault(2);

        var approver = managerUser ?? adminUser;

        if (ali is not null)
        {
            await CreateVisitWithOrderAndPaymentAsync(
                context,
                salesRep: ali,
                customer: customers[0],
                products: [products[0], products[1]],
                quantities: [40m, 25m],
                paymentAmount: 35m,
                paymentStatus: PaymentStatus.Approved,
                reviewedByUserId: approver?.Id,
                reviewedAtUtc: DateTime.UtcNow.AddDays(-7));

            await CreateVisitWithOrderAndPaymentAsync(
                context,
                salesRep: ali,
                customer: customers[1],
                products: [products[2], products[3]],
                quantities: [18m, 12m],
                paymentAmount: 20m,
                paymentStatus: PaymentStatus.Pending,
                reviewedByUserId: null,
                reviewedAtUtc: null);
        }

        if (sara is not null)
        {
            await CreateVisitWithOrderAndPaymentAsync(
                context,
                salesRep: sara,
                customer: customers[2],
                products: [products[0], products[2], products[4]],
                quantities: [30m, 16m, 20m],
                paymentAmount: 50m,
                paymentStatus: PaymentStatus.Approved,
                reviewedByUserId: approver?.Id,
                reviewedAtUtc: DateTime.UtcNow.AddDays(-5));

            await CreateVisitWithOrderAndPaymentAsync(
                context,
                salesRep: sara,
                customer: customers[3],
                products: [products[1], products[3]],
                quantities: [22m, 10m],
                paymentAmount: 15m,
                paymentStatus: PaymentStatus.Rejected,
                reviewedByUserId: approver?.Id,
                reviewedAtUtc: DateTime.UtcNow.AddDays(-3),
                rejectionReason: "Amount mismatch with supporting receipt.");
        }

        if (omar is not null)
        {
            await CreateVisitWithOrderOnlyAsync(
                context,
                salesRep: omar,
                customer: customers[4],
                products: [products[2], products[4]],
                quantities: [14m, 28m]);

            await CreateVisitWithOrderAndPaymentAsync(
                context,
                salesRep: omar,
                customer: customers[5],
                products: [products[0], products[3]],
                quantities: [26m, 15m],
                paymentAmount: 25m,
                paymentStatus: PaymentStatus.Approved,
                reviewedByUserId: approver?.Id,
                reviewedAtUtc: DateTime.UtcNow.AddDays(-2));
        }
    }

    private static async Task CreateVisitWithOrderAndPaymentAsync(
        AppDbContext context,
        AppUser salesRep,
        Customer customer,
        List<Product> products,
        List<decimal> quantities,
        decimal paymentAmount,
        PaymentStatus paymentStatus,
        Guid? reviewedByUserId,
        DateTime? reviewedAtUtc,
        string? rejectionReason = null)
    {
        var baseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(2, 10));
        var visitId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var visit = new Visit
        {
            Id = visitId,
            CustomerId = customer.Id,
            SalesRepId = salesRep.Id,
            CheckInAtUtc = baseDate,
            CheckInLatitude = customer.Latitude,
            CheckInLongitude = customer.Longitude,
            CheckOutAtUtc = baseDate.AddMinutes(30),
            CheckOutLatitude = customer.Latitude,
            CheckOutLongitude = customer.Longitude,
            Status = VisitStatus.Completed,
            Outcome = VisitOutcome.Successful,
            Notes = "Seeded completed visit.",
            CreatedAtUtc = baseDate,
            UpdatedAtUtc = baseDate.AddMinutes(30)
        };

        var order = new Order
        {
            Id = orderId,
            OrderNumber = $"ORD-SEED-{Random.Shared.Next(10000, 99999)}",
            VisitId = visitId,
            CustomerId = customer.Id,
            SalesRepId = salesRep.Id,
            PaymentType = PaymentType.Credit,
            Notes = "Seeded order",
            CreatedAtUtc = baseDate.AddMinutes(10)
        };

        for (int i = 0; i < products.Count; i++)
        {
            var product = products[i];
            var quantity = quantities[i];
            var lineTotal = quantity * product.UnitPrice;

            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.UnitPrice,
                LineTotal = lineTotal
            });
        }

        order.TotalAmount = order.Items.Sum(x => x.LineTotal);

        var payment = new Payment
        {
            Id = paymentId,
            PaymentNumber = $"PAY-SEED-{Random.Shared.Next(10000, 99999)}",
            VisitId = visitId,
            CustomerId = customer.Id,
            SalesRepId = salesRep.Id,
            Amount = paymentAmount,
            PaymentMethod = PaymentMethod.Cash,
            Status = paymentStatus,
            Reference = $"REF-{Random.Shared.Next(1000, 9999)}",
            Notes = "Seeded payment",
            ReviewedByUserId = paymentStatus == PaymentStatus.Pending ? null : reviewedByUserId,
            ReviewedAtUtc = paymentStatus == PaymentStatus.Pending ? null : reviewedAtUtc,
            RejectionReason = paymentStatus == PaymentStatus.Rejected ? rejectionReason : null,
            CreatedAtUtc = baseDate.AddMinutes(20),
            UpdatedAtUtc = paymentStatus == PaymentStatus.Pending ? null : baseDate.AddMinutes(25)
        };

        context.Visits.Add(visit);
        context.Orders.Add(order);
        context.Payments.Add(payment);

        await context.SaveChangesAsync();
    }

    private static async Task CreateVisitWithOrderOnlyAsync(
        AppDbContext context,
        AppUser salesRep,
        Customer customer,
        List<Product> products,
        List<decimal> quantities)
    {
        var baseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(2, 10));
        var visitId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var visit = new Visit
        {
            Id = visitId,
            CustomerId = customer.Id,
            SalesRepId = salesRep.Id,
            CheckInAtUtc = baseDate,
            CheckInLatitude = customer.Latitude,
            CheckInLongitude = customer.Longitude,
            CheckOutAtUtc = baseDate.AddMinutes(25),
            CheckOutLatitude = customer.Latitude,
            CheckOutLongitude = customer.Longitude,
            Status = VisitStatus.Completed,
            Outcome = VisitOutcome.Successful,
            Notes = "Seeded visit with order only.",
            CreatedAtUtc = baseDate,
            UpdatedAtUtc = baseDate.AddMinutes(25)
        };

        var order = new Order
        {
            Id = orderId,
            OrderNumber = $"ORD-SEED-{Random.Shared.Next(10000, 99999)}",
            VisitId = visitId,
            CustomerId = customer.Id,
            SalesRepId = salesRep.Id,
            PaymentType = PaymentType.Credit,
            Notes = "Seeded order without payment",
            CreatedAtUtc = baseDate.AddMinutes(8)
        };

        for (int i = 0; i < products.Count; i++)
        {
            var product = products[i];
            var quantity = quantities[i];
            var lineTotal = quantity * product.UnitPrice;

            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.UnitPrice,
                LineTotal = lineTotal
            });
        }

        order.TotalAmount = order.Items.Sum(x => x.LineTotal);

        context.Visits.Add(visit);
        context.Orders.Add(order);

        await context.SaveChangesAsync();
    }
}
