using Microsoft.AspNetCore.Identity;
using api.Enums;

namespace api.Models;

public class User : IdentityUser<Guid>
{

    public string Name { get; set; } = default!;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
