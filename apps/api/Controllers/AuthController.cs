using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using api.DTOs;
using api.Enums;
using api.Models;
using api.Services;

namespace api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly JwtTokenService _jwtService;

    public AuthController(UserManager<User> userManager, JwtTokenService jwtService)
    {
        _userManager = userManager;
        _jwtService = jwtService;
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

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse(token, user.Id.ToString(), user.Name, user.Email!, user.Role.ToString()));
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null)
            return Unauthorized(new { error = "Invalid credentials" });

        var valid = await _userManager.CheckPasswordAsync(user, req.Password);
        if (!valid)
            return Unauthorized(new { error = "Invalid credentials" });

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse(token, user.Id.ToString(), user.Name, user.Email!, user.Role.ToString()));
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user is null) return NotFound();

        return Ok(new { user.Id, user.Name, user.Email, Role = user.Role.ToString() });
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
            Role = role
        };

        var result = await _userManager.CreateAsync(user, req.Password);

        if (!result.Succeeded)
            return BadRequest(new { error = result.Errors.Select(e => e.Description) });

        return Ok(new { user.Id, user.Name, user.Email, Role = user.Role.ToString() });
    }
}