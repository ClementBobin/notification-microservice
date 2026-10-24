namespace Genkan.Shared;

public static class Extension
{
    // make an extension method for string to normalize email
    public static string NormalizeEmail(this string email) => email.Trim().ToLowerInvariant();
}