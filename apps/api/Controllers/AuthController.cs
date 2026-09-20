using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using api.DTOs;
using api.Enums;
using api.Models;
using api.Services;
using api.Data;

namespace api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly JwtTokenService _jwtService;
    private readonly AppDbContext _db;

    public AuthController(UserManager<User> userManager, JwtTokenService jwtService, AppDbContext db)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _db = db;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var user = new User
        {
            UserName = req.Email,
            Email = req.Email,
            Name = req.Name,
            Role = UserRole.Commuter
        };

        var result = await _userManager.CreateAsync(user, req.Password);

        if (!result.Succeeded)
            return BadRequest(new { error = result.Errors.Select(e => e.Description) });

        var passenger = new Passenger { UserId = user.Id, FullName = user.Name, PhoneNumber = $"pending-{user.Id:N}"[..28], Email = user.Email, Category = PassengerCategory.Adult };
        _db.Passengers.Add(passenger);
        _db.Wallets.Add(new Wallet { Passenger = passenger });
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            await _userManager.DeleteAsync(user);
            return BadRequest(new { error = "Unable to create the commuter profile. Ensure the latest database migration is applied." });
        }

        var token = _jwtService.GenerateToken(user);
        SetAuthCookies(user);

        return Ok(new AuthResponse(token, user.Id.ToString(), user.Name, user.Email!, user.Role.ToString(), user.CentreId));
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null)
            return Unauthorized(new { error = "Invalid credentials" });
        if (!user.IsActive)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Your account is temporarily disabled. Contact an administrator." });

        var valid = await _userManager.CheckPasswordAsync(user, req.Password);
        if (!valid)
            return Unauthorized(new { error = "Invalid credentials" });

        var token = _jwtService.GenerateToken(user);
        SetAuthCookies(user);

        return Ok(new AuthResponse(token, user.Id.ToString(), user.Name, user.Email!, user.Role.ToString(), user.CentreId));
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user is null) return NotFound();

        return Ok(new { user.Id, user.Name, user.Email, Role = user.Role.ToString(), user.CentreId, user.IsActive, user.HomeLocation, user.NicNumber, user.Gender, user.ProfilePhotoUrl, user.NicVerificationStatus });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("upts_access_token");
        Response.Cookies.Delete("upts_refresh_token");
        return NoContent();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refresh = Request.Cookies["upts_refresh_token"];
        if (string.IsNullOrWhiteSpace(refresh)) return Unauthorized();
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(refresh, new TokenValidationParameters
            {
                ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
                ValidIssuer = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Issuer"],
                ValidAudience = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Key"]!))
            }, out _);
            var id = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(id!);
            if (user is null || !user.IsActive) return Unauthorized();
            SetAuthCookies(user);
            return NoContent();
        }
        catch (SecurityTokenException) { return Unauthorized(); }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user is null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { error = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    [HttpPatch("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "Name is required" });
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user is null) return Unauthorized();
        user.Name = req.Name.Trim();
        user.HomeLocation = req.HomeLocation?.Trim();
        user.NicNumber = req.NicNumber?.Trim();
        user.Gender = req.Gender?.Trim();
        user.ProfilePhotoUrl = req.ProfilePhotoUrl?.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        var passenger = await _db.Passengers.SingleOrDefaultAsync(item => item.UserId == user.Id);
        if (passenger is not null)
        {
            passenger.FullName = user.Name;
            passenger.Email = user.Email;
        }
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(new { error = result.Errors.Select(e => e.Description) });
        return Ok(new { user.Id, user.Name, user.Email, Role = user.Role.ToString(), user.CentreId, user.IsActive, user.HomeLocation, user.NicNumber, user.Gender, user.ProfilePhotoUrl });
    }

    [HttpPost("me/verify-nic")]
    [Authorize]
    public async Task<IActionResult> VerifyNic(VerifyNicRequest req)
    {
        var value = req.NicNumber.Trim().ToUpperInvariant();
        var validFormat = System.Text.RegularExpressions.Regex.IsMatch(value, "^(\\d{9}[VX]|\\d{12})$");
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user is null) return Unauthorized();
        user.NicNumber = value;
        user.NicVerificationStatus = validFormat ? "Verified" : "Invalid";
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        return Ok(new { user.NicNumber, user.NicVerificationStatus, user.Gender });
    }

    [HttpPost("create-user")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser(CreateUserRequest req)
    {
        if (!Enum.TryParse<UserRole>(req.Role, true, out var role))
            return BadRequest(new { error = "Invalid role" });

        var user = new User
        {
            UserName = req.Email,
            Email = req.Email,
            Name = req.Name,
            Role = role,
            CentreId = req.CentreId
        };

        var result = await _userManager.CreateAsync(user, req.Password);

        if (!result.Succeeded)
            return BadRequest(new { error = result.Errors.Select(e => e.Description) });

        return Ok(new { user.Id, user.Name, user.Email, Role = user.Role.ToString(), user.CentreId });
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users([FromQuery] bool staffOnly = false)
    {
        var query = _userManager.Users.AsNoTracking();
        if (staffOnly)
            query = query.Where(user => user.Role != UserRole.Commuter && user.Role != UserRole.Admin);

        var users = await query
            .OrderBy(user => user.Name)
            .Select(user => new { user.Id, user.Name, user.Email, Role = user.Role.ToString(), user.CentreId, user.IsActive })
            .ToListAsync();
        return Ok(users);
    }

    [HttpPatch("users/{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] bool isActive)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    [HttpPost("users/{id:guid}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword)) return BadRequest(new { error = "A new password is required" });
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded) return BadRequest(new { error = result.Errors.Select(e => e.Description) });
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    private void SetAuthCookies(User user)
    {
        var access = _jwtService.GenerateToken(user);
        var refresh = _jwtService.GenerateToken(user, TimeSpan.FromDays(30));
        Response.Cookies.Append("upts_access_token", access, new CookieOptions { HttpOnly = true, Secure = false, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddHours(8) });
        Response.Cookies.Append("upts_refresh_token", refresh, new CookieOptions { HttpOnly = true, Secure = false, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(30) });
    }
}
