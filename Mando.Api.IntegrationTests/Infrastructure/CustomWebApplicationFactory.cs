using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Mando.Api.Data;
using Mando.Api.Data.Seeders;

namespace Mando.Api.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath;
    private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;

    public CustomWebApplicationFactory()
    {
        _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"mando-integration-tests-{Guid.NewGuid():N}.db");

        _configurationOverrides = TestHostSettings.BuildConfiguration(_databasePath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Testing.json"),
                optional: true,
                reloadOnChange: false);

            configBuilder.AddInMemoryCollection(_configurationOverrides);
        });

        builder.ConfigureServices((context, services) =>
        {
            var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Testing connection string is missing.");

            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<AppDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();

        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        IdentitySeeder.SeedAsync(host.Services, configuration)
            .GetAwaiter()
            .GetResult();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        TryDeleteDatabaseFile();
    }

    private void TryDeleteDatabaseFile()
    {
        try
        {
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
        catch
        {
            // Best-effort cleanup for test-owned temporary database files.
        }
    }
}
