using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Mando.Api.Configurations;
using Mando.Api.Data;
using Mando.Api.Entities.Identity;
using Mando.Api.Filters;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Audit;
using Mando.Api.Interfaces.Auth;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Customers;
using Mando.Api.Interfaces.Dashboard;
using Mando.Api.Interfaces.Financials;
using Mando.Api.Interfaces.Notifications;
using Mando.Api.Interfaces.Operations;
using Mando.Api.Interfaces.Orders;
using Mando.Api.Interfaces.Payments;
using Mando.Api.Interfaces.Products;
using Mando.Api.Interfaces.Reports;
using Mando.Api.Interfaces.Users;
using Mando.Api.Interfaces.Visits;
using Mando.Api.Middleware;
using Mando.Api.Services.Audit;
using Mando.Api.Services.Auth;
using Mando.Api.Services.Common;
using Mando.Api.Services.Customers;
using Mando.Api.Services.Dashboard;
using Mando.Api.Services.Financials;
using Mando.Api.Services.Health;
using Mando.Api.Services.Notifications;
using Mando.Api.Services.Operations;
using Mando.Api.Services.Orders;
using Mando.Api.Services.Payments;
using Mando.Api.Services.Products;
using Mando.Api.Services.Reports;
using Mando.Api.Services.Users;
using Mando.Api.Services.Visits;

namespace Mando.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMandoApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ApiResponseEnvelopeFilter>();

        services
            .AddControllers(options =>
            {
                options.Filters.AddService<ApiResponseEnvelopeFilter>();
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(x => x.Value is not null && x.Value.Errors.Count > 0)
                        .ToDictionary(
                            x => x.Key,
                            x => x.Value!.Errors
                                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                                    ? "Invalid value."
                                    : e.ErrorMessage)
                                .ToArray());

                    var response = ApiResponseFactory.Build(
                        context.HttpContext,
                        "validation_error",
                        "One or more validation errors occurred.",
                        errors);

                    return new BadRequestObjectResult(response);
                };
            });

        services.AddHttpContextAccessor();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Mando API",
                Version = "v1"
            });

            var bearerSecurityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT Bearer token only",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            };

            options.AddSecurityDefinition("Bearer", bearerSecurityScheme);

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [bearerSecurityScheme] = Array.Empty<string>()
            });
        });

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services
            .AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                settings => !string.Equals(
                    settings.Key,
                    "CHANGE_ME",
                    StringComparison.OrdinalIgnoreCase),
                "Jwt:Key must not use a placeholder value.")
            .ValidateOnStart();

        services
            .AddOptions<GpsSettings>()
            .Bind(configuration.GetSection(GpsSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<StartupExecutionSettings>()
            .Bind(configuration.GetSection(StartupExecutionSettings.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<ForwardedHeadersSettings>()
            .Bind(configuration.GetSection(ForwardedHeadersSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<StartupExecutionSettings>, StartupExecutionSettingsValidator>();
        services.AddSingleton<IValidateOptions<ForwardedHeadersSettings>, ForwardedHeadersSettingsValidator>();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            var forwardedHeadersSettings = configuration
                .GetSection(ForwardedHeadersSettings.SectionName)
                .Get<ForwardedHeadersSettings>() ?? new ForwardedHeadersSettings();

            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.RequireHeaderSymmetry = false;
            options.ForwardLimit = Math.Max(1, forwardedHeadersSettings.ForwardLimit);
            options.KnownProxies.Clear();

            foreach (var knownProxy in forwardedHeadersSettings.KnownProxies
                         .Where(value => !string.IsNullOrWhiteSpace(value))
                         .Select(value => IPAddress.Parse(value.Trim())))
            {
                options.KnownProxies.Add(knownProxy);
            }
        });

        var defaultConnectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(defaultConnectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                defaultConnectionString,
                sqlServerOptions =>
                {
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);

                    sqlServerOptions.CommandTimeout(30);
                }));

        services
            .AddIdentity<AppUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;

                options.User.RequireUniqueEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt settings are missing.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Key))
            throw new InvalidOperationException("Jwt:Key is missing.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var principal = context.Principal;
                        if (principal is null)
                        {
                            context.Fail("Authenticated principal is missing.");
                            return;
                        }

                        var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

                        if (string.IsNullOrWhiteSpace(currentUserId))
                        {
                            context.Fail("Authenticated user identifier is missing.");
                            return;
                        }

                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                        var currentUser = await userManager.FindByIdAsync(currentUserId);

                        if (currentUser is null)
                        {
                            context.Fail("Authenticated user no longer exists.");
                            return;
                        }

                        if (!currentUser.IsActive)
                        {
                            context.Fail("Authenticated user is inactive.");
                            return;
                        }

                        if (currentUser.LockoutEnabled &&
                            currentUser.LockoutEnd.HasValue &&
                            currentUser.LockoutEnd.Value > DateTimeOffset.UtcNow)
                        {
                            context.Fail("Authenticated user is locked out.");
                            return;
                        }

                        var currentSecurityStamp = await userManager.GetSecurityStampAsync(currentUser);
                        var tokenSecurityStamp = principal.FindFirstValue(AuthClaimTypes.SecurityStamp);
                        if (string.IsNullOrWhiteSpace(tokenSecurityStamp) ||
                            !string.Equals(tokenSecurityStamp, currentSecurityStamp, StringComparison.Ordinal))
                        {
                            context.Fail("Authenticated token is stale.");
                            return;
                        }

                        var currentRoles = await userManager.GetRolesAsync(currentUser);
                        var currentRoleSet = currentRoles
                            .Where(role => !string.IsNullOrWhiteSpace(role))
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        var tokenRoleSet = principal.FindAll(ClaimTypes.Role)
                            .Select(claim => claim.Value)
                            .Where(role => !string.IsNullOrWhiteSpace(role))
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        if (!currentRoleSet.SetEquals(tokenRoleSet))
                        {
                            context.Fail("Authenticated role claims are stale.");
                        }
                    }
                };
            });

        services.AddAuthorization();

        services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy("Application process is running."),
                tags: ["live"])
            .AddCheck<DatabaseReadinessHealthCheck>(
                "database_readiness",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"]);

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<IWorkflowSideEffectService, WorkflowSideEffectService>();
        services.AddScoped<IAuthWorkflowService, AuthWorkflowService>();

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationQueryService, NotificationQueryService>();
        services.AddScoped<INotificationWorkflowService, NotificationWorkflowService>();

        services.AddScoped<IDocumentNumberGenerator, DocumentNumberGenerator>();
        services.AddScoped<ICustomerBalanceService, CustomerBalanceService>();
        services.AddScoped<ICustomerFinancialLockService, CustomerFinancialLockService>();
        services.AddScoped<ICustomerWorkflowService, CustomerWorkflowService>();
        services.AddScoped<ICustomerQueryService, CustomerQueryService>();

        services.AddScoped<IOrderWorkflowService, OrderWorkflowService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();

        services.AddScoped<IPaymentWorkflowService, PaymentWorkflowService>();
        services.AddScoped<IPaymentQueryService, PaymentQueryService>();

        services.AddScoped<IVisitWorkflowService, VisitWorkflowService>();
        services.AddScoped<IVisitQueryService, VisitQueryService>();
        services.AddScoped<IVisitLifecycleLockService, VisitLifecycleLockService>();
        services.AddScoped<IVisitImageStorage, LocalVisitImageStorage>();
        services.AddScoped<IVisitMediaService, VisitMediaService>();

        services.AddScoped<IProductWorkflowService, ProductWorkflowService>();
        services.AddScoped<IProductQueryService, ProductQueryService>();

        services.AddScoped<IUserWorkflowService, UserWorkflowService>();
        services.AddScoped<IUserStatusLockService, UserStatusLockService>();
        services.AddScoped<IUserQueryService, UserQueryService>();

        services.AddScoped<IOperationsQueryService, OperationsQueryService>();
        services.AddScoped<IReportQueryService, ReportQueryService>();
        services.AddScoped<IPerformanceReportQueryService, PerformanceReportQueryService>();
        services.AddScoped<IDashboardQueryService, DashboardQueryService>();

        services.AddScoped<IOperationsAlertWorkflowService, OperationsAlertWorkflowService>();

        return services;
    }
}