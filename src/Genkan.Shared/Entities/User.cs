using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Shared.Entities;

[Table("users")]
[Index(nameof(Email), IsUnique = true)]
public class User
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(320)]
    [Required]
    public string Email { get; set; } = "";

    [MaxLength(200)]
    public string DisplayName { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    [MaxLength(20)]
    public string Role { get; set; } = "User";

    public string? FcmToken { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}