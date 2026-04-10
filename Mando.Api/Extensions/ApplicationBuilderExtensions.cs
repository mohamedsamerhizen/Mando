using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Mando.Api.Configurations;
using Mando.Api.Data;
using Mando.Api.Data.Seeders;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Visits;
using Mando.Api.Middleware;

namespace Mando.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task UseMandoApiPipelineAsync(this WebApplication app)
    {
        app.UseCorrelationId();
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        var includeDetailedHealthDiagnostics =
            app.Environment.IsDevelopment() ||
            app.Environment.IsEnvironment("Testing");

        var forwardedHeadersSettings = app.Services
            .GetRequiredService<IOptions<ForwardedHeadersSettings>>()
            .Value;

        if (forwardedHeadersSettings.Enabled)
        {
            app.UseForwardedHeaders();
        }

        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
        {
            app.UseHsts();
        }

        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseHttpsRedirection();
        }

        app.UseRequestLogging();

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/uploads/visit-images", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next();
        });

        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/", (HttpContext httpContext) =>
            Results.Ok(ApiResponseFactory.BuildSuccess(
                httpContext,
                "Mando API is running.",
                "API is running successfully.")))
           .AllowAnonymous();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = (context, report) => WriteHealthResponseAsync(context, report, includeDetailedHealthDiagnostics)
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = (context, report) => WriteHealthResponseAsync(context, report, includeDetailedHealthDiagnostics)
        }).AllowAnonymous();

        app.MapControllers();

        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var startupSettings = services
            .GetRequiredService<IOptions<StartupExecutionSettings>>()
            .Value;

        try
        {
            var visitImageStorage = services.GetService<IVisitImageStorage>();
            visitImageStorage?.CleanupTransientWorkDirectories(TimeSpan.FromDays(1));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Visit media transient work directory cleanup failed during startup.");
        }

        var startupMutationAllowed =
            app.Environment.IsDevelopment() ||
            app.Environment.IsEnvironment("Testing");

        if (!startupMutationAllowed &&
            (startupSettings.ApplyMigrationsOnStartup || startupSettings.RunSeedOnStartup))
        {
            logger.LogCritical(
                "Automatic migrations/seeding on startup are blocked outside Development and Testing. Environment: {EnvironmentName} | ApplyMigrationsOnStartup: {ApplyMigrationsOnStartup}, RunSeedOnStartup: {RunSeedOnStartup}",
                app.Environment.EnvironmentName,
                startupSettings.ApplyMigrationsOnStartup,
                startupSettings.RunSeedOnStartup);

            throw new InvalidOperationException(
                "Automatic migrations and seeding on startup are blocked outside Development and Testing.");
        }

        try
        {
            var dbContext = services.GetRequiredService<AppDbContext>();

            if (startupSettings.ApplyMigrationsOnStartup)
            {
                logger.LogInformation("Applying database migrations on startup...");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migrations completed successfully.");
            }
            else
            {
                logger.LogInformation("Skipping automatic database migrations on startup.");
            }

            if (startupSettings.RunSeedOnStartup)
            {
                logger.LogInformation("Running identity/demo seeding on startup...");
                await IdentitySeeder.SeedAsync(services, app.Configuration);
                logger.LogInformation("Identity/demo seeding completed successfully.");
            }
            else
            {
                logger.LogInformation("Skipping automatic identity/demo seeding on startup.");
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(
                ex,
                "Startup initialization failed. TraceId unavailable because failure happened outside request scope.");

            throw;
        }
    }

    private static async Task WriteHealthResponseAsync(
        HttpContext context,
        HealthReport report,
        bool includeDetailedDiagnostics)
    {
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store, no-cache";

        var entries = report.Entries.ToDictionary(
            entry => entry.Key,
            entry =>
            {
                var payload = new Dictionary<string, object?>
                {
                    ["status"] = entry.Value.Status.ToString(),
                    ["durationMs"] = entry.Value.Duration.TotalMilliseconds,
                    ["description"] = entry.Value.Description
                };

                if (includeDetailedDiagnostics)
                {
                    payload["exception"] = entry.Value.Exception?.Message;
                    payload["data"] = entry.Value.Data.ToDictionary(x => x.Key, x => x.Value);
                }

                return payload;
            });

        var response = new Dictionary<string, object?>
        {
            ["traceId"] = context.TraceIdentifier,
            ["status"] = report.Status.ToString(),
            ["totalDurationMs"] = report.TotalDuration.TotalMilliseconds,
            ["entries"] = entries
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}