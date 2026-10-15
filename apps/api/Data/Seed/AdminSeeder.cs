using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Identity;

namespace api.Data.Seed;

public static class AdminSeeder
{
    private const string DefaultEmail = "admin@upts.lk";
    private const string DefaultPassword = "admin123";

    // Makes sure the bootstrap admin account exists and is active.
    public static async Task SeedAsync(UserManager<User> userManager, IConfiguration configuration)
    {
        var email = configuration["BootstrapAdmin:Email"] ?? DefaultEmail;
        var password = configuration["BootstrapAdmin:Password"] ?? DefaultPassword;

        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            await CreateAdminAsync(userManager, email, password);
            return;
        }

        if (!admin.IsActive)
        {
            admin.IsActive = true;
            await userManager.UpdateAsync(admin);
        }
    }

    private static async Task CreateAdminAsync(UserManager<User> userManager, string email, string password)
    {
        var admin = new User { UserName = email, Email = email, Name = "UPTSLK Super Admin", Role = UserRole.Admin };
        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Could not create the bootstrap admin: {errors}");
        }
    }
}
