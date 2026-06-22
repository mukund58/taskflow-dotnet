namespace Backend.Services.Implementations;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Backend.Data;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtSettingsOptions _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, IOptions<JwtSettingsOptions> jwtSettings, ILogger<AuthService> logger)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Register(RegisterDto dto)
    {
        var normalizedEmail = NormalizeEmail(dto.Email);
        var existingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail && !x.IsDeleted);

        if (existingUser != null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Email already registered"
            };
        }

        var user = new User
        {
            Name = dto.Name,
            Email = normalizedEmail,
            PasswordHash = BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var accessToken = GenerateJwtToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return new AuthResponseDto
        {
            Success = true,
            Message = "User registered successfully",
            Token = accessToken,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiry = refreshToken.ExpiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            }
        };
    }

    public async Task<AuthResponseDto> Login(LoginDto dto)
    {
        var normalizedEmail = NormalizeEmail(dto.Email);
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail && !x.IsDeleted);

        if (user == null || !BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid credentials"
            };
        }

        var accessToken = GenerateJwtToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful",
            Token = accessToken,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiry = refreshToken.ExpiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            }
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid refresh token"
            };
        }

        if (storedToken.IsRevoked)
        {
            // Possible token reuse detected — revoke the entire family
            _logger.LogWarning(
                "Revoked refresh token reuse detected for UserId={UserId}, Token={TokenPrefix}...",
                storedToken.UserId,
                storedToken.Token[..8]);

            await RevokeAllTokensAsync(storedToken.UserId);

            return new AuthResponseDto
            {
                Success = false,
                Message = "Token has been revoked. All sessions have been invalidated for security."
            };
        }

        if (storedToken.IsExpired)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Refresh token has expired"
            };
        }

        if (storedToken.User.IsDeleted)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "User account has been deleted"
            };
        }

        // Rotate: revoke old token and create new one
        storedToken.RevokedAt = DateTime.UtcNow;
        var newRefreshToken = await CreateRefreshTokenAsync(storedToken.UserId);

        storedToken.ReplacedByTokenId = newRefreshToken.Id;
        await _context.SaveChangesAsync();

        var accessToken = GenerateJwtToken(storedToken.User);

        _logger.LogInformation(
            "Rotated refresh token for UserId={UserId}",
            storedToken.UserId);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Token refreshed successfully",
            Token = accessToken,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiry = newRefreshToken.ExpiresAt,
            User = new UserDto
            {
                Id = storedToken.User.Id,
                Name = storedToken.User.Name,
                Email = storedToken.User.Email
            }
        };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken == null || storedToken.IsRevoked)
            return; // Idempotent — no error for already-revoked or missing tokens

        storedToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Revoked refresh token for UserId={UserId} (logout)",
            storedToken.UserId);
    }

    public async Task RevokeAllTokensAsync(Guid userId)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Revoked {Count} active refresh token(s) for UserId={UserId}",
            activeTokens.Count,
            userId);
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role) // 🔥 IMPORTANT
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        _logger.LogInformation(
            "Generated JWT token for UserId={UserId} with issuer={Issuer}, audience={Audience}, expirationMinutes={ExpirationMinutes}",
            user.Id,
            _jwtSettings.Issuer,
            _jwtSettings.Audience,
            _jwtSettings.AccessTokenExpirationMinutes);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId)
    {
        var refreshToken = new RefreshToken
        {
            Token = GenerateRefreshTokenString(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    private static string GenerateRefreshTokenString()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
