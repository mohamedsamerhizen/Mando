using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;

namespace Mando.Api.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderActionHistory> OrderActionHistories => Set<OrderActionHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentActionHistory> PaymentActionHistories => Set<PaymentActionHistory>();
    public DbSet<OperationsAlertReview> OperationsAlertReviews => Set<OperationsAlertReview>();

    public DbSet<CustomerActionHistory> CustomerActionHistories => Set<CustomerActionHistory>();
    public DbSet<ProductActionHistory> ProductActionHistories => Set<ProductActionHistory>();
    public DbSet<UserActionHistory> UserActionHistories => Set<UserActionHistory>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<VisitAttemptLog> VisitAttemptLogs => Set<VisitAttemptLog>();
    public DbSet<VisitImage> VisitImages => Set<VisitImage>();
    public DbSet<VisitActionHistory> VisitActionHistories => Set<VisitActionHistory>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}