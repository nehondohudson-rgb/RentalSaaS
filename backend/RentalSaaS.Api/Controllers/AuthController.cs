using Microsoft.AspNetCore.Authorization;

using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RentalSaaS.Api.Data;
using RentalSaaS.Api.Models;

namespace RentalSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<ApplicationUser> _passwordHasher = new();

    public AuthController(
        ApplicationDbContext db,
        IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpPost("bootstrap-admin")]
    public async Task<IActionResult> BootstrapAdmin(
        [FromBody] BootstrapAdminRequest request)
    {
        var expectedKey = _configuration["Bootstrap:SetupKey"];
        var suppliedKey = Request.Headers["X-Setup-Key"].ToString();

        if (string.IsNullOrWhiteSpace(expectedKey) ||
            string.IsNullOrWhiteSpace(suppliedKey) ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(suppliedKey),
                Encoding.UTF8.GetBytes(expectedKey)))
        {
            return Unauthorized(new { message = "Invalid setup key." });
        }

        if (string.IsNullOrWhiteSpace(request.Email) ||
            !System.Net.Mail.MailAddress.TryCreate(request.Email, out _))
        {
            return BadRequest(new { message = "Enter a valid email address." });
        }

        if (string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 12)
        {
            return BadRequest(new
            {
                message = "Password must contain at least 12 characters."
            });
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        if (await _db.ApplicationUsers.AnyAsync())
        {
            return Conflict(new
            {
                message = "Administrator setup has already been completed."
            });
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var user = new ApplicationUser
        {
            Email = email,
            Role = "SuperAdmin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(user, request.Password);

        _db.ApplicationUsers.Add(user);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(CreateAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Email and password are required."
            });
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.ApplicationUsers
            .SingleOrDefaultAsync(u => u.Email == email);

        if (user is null || !user.IsActive)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash =
                _passwordHasher.HashPassword(user, request.Password);

            await _db.SaveChangesAsync();
        }

        return Ok(CreateAuthResponse(user));
    }

    private object CreateAuthResponse(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(
            Convert.FromBase64String(
                _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT signing key is missing.")));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new
        {
            userId = user.Id,
            email = user.Email,
            role = user.Role,
            accessToken = new JwtSecurityTokenHandler()
                .WriteToken(token),
            expiresAtUtc = expiresAt
        };
    }
}

public class BootstrapAdminRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

