namespace Mando.Api.Common;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesRep = "SalesRep";

    public static readonly string[] All =
    [
        Admin,
        Manager,
        SalesRep
    ];
}