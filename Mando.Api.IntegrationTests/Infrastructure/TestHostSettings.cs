namespace Mando.Api.IntegrationTests.Infrastructure;

public static class TestHostSettings
{
    public const string JwtKey = "THIS_IS_A_TESTING_ONLY_SUPER_SECRET_KEY_12345";
    public const string JwtIssuer = "Mando.Testing";
    public const string JwtAudience = "Mando.TestingUsers";
    public const string JwtExpiryMinutes = "120";

    public const string AdminFullName = "System Admin";
    public const string AdminEmail = "admin@mando.local";
    public const string AdminPassword = "Admin123";

    public const string ManagerFullName = "Operations Manager";
    public const string ManagerEmail = "manager@mando.local";
    public const string ManagerPassword = "Manager123";

    public const string AliFullName = "Ali Hassan";
    public const string AliEmail = "ali@mando.local";

    public const string SaraFullName = "Sara Ahmed";
    public const string SaraEmail = "sara@mando.local";

    public const string OmarFullName = "Omar Khalid";
    public const string OmarEmail = "omar@mando.local";

    public const string SalesRepPassword = "Sales123";

    public static IReadOnlyDictionary<string, string?> BuildConfiguration(string databasePath)
    {
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = BuildSqliteConnectionString(databasePath),
            ["Startup:ApplyMigrationsOnStartup"] = "false",
            ["Startup:RunSeedOnStartup"] = "false",
            ["SeedData:Enabled"] = "false",
            ["Jwt:Key"] = JwtKey,
            ["Jwt:Issuer"] = JwtIssuer,
            ["Jwt:Audience"] = JwtAudience,
            ["Jwt:ExpiryMinutes"] = JwtExpiryMinutes,
            ["SeedAdmin:FullName"] = AdminFullName,
            ["SeedAdmin:Email"] = AdminEmail,
            ["SeedAdmin:Password"] = AdminPassword,
            ["SeedManager:FullName"] = ManagerFullName,
            ["SeedManager:Email"] = ManagerEmail,
            ["SeedManager:Password"] = ManagerPassword,
            ["SeedSalesReps:0:FullName"] = AliFullName,
            ["SeedSalesReps:0:Email"] = AliEmail,
            ["SeedSalesReps:0:Password"] = SalesRepPassword,
            ["SeedSalesReps:1:FullName"] = SaraFullName,
            ["SeedSalesReps:1:Email"] = SaraEmail,
            ["SeedSalesReps:1:Password"] = SalesRepPassword,
            ["SeedSalesReps:2:FullName"] = OmarFullName,
            ["SeedSalesReps:2:Email"] = OmarEmail,
            ["SeedSalesReps:2:Password"] = SalesRepPassword
        };
    }

    private static string BuildSqliteConnectionString(string databasePath)
    {
        return $"Data Source={databasePath}";
    }
}
