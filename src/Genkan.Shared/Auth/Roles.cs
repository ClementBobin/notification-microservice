namespace Genkan.Shared.Auth;

public static class Roles
{
    public const string User = "User";
    public const string Reception = "Reception";
    public const string Admin = "Admin";
    public static readonly string[] All = [User, Reception, Admin];
}
