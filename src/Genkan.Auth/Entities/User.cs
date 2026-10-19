namespace Genkan.Auth.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";
    public bool TwoFactorEnabled { get; set; }
    public string? TotpSecret { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
