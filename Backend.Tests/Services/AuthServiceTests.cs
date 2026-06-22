namespace Backend.Tests.Services;

using Backend.Data;
using Backend.Models.DTOs;
using Backend.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

public class AuthServiceTests
{
    [Fact]
    public async Task Register_NormalizesEmailBeforePersisting()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.Register(new RegisterDto
        {
            Name = "Test User",
            Email = "  Test.User@Example.COM ",
            Password = "Password123!"
        });

        Assert.True(result.Success);
        var user = await context.Users.SingleAsync();
        Assert.Equal("test.user@example.com", user.Email);
    }

    [Fact]
    public async Task Login_AllowsCaseAndWhitespaceDifferencesInEmail()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await service.Register(new RegisterDto
        {
            Name = "Test User",
            Email = "test.user@example.com",
            Password = "Password123!"
        });

        var result = await service.Login(new LoginDto
        {
            Email = "  TEST.User@Example.COM ",
            Password = "Password123!"
        });

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.NotNull(result.User);
        Assert.Equal("test.user@example.com", result.User!.Email);
    }

    [Fact]
    public async Task Register_RejectsDuplicateEmailWithDifferentCase()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await service.Register(new RegisterDto
        {
            Name = "Test User",
            Email = "test.user@example.com",
            Password = "Password123!"
        });

        var duplicateResult = await service.Register(new RegisterDto
        {
            Name = "Duplicate",
            Email = "TEST.USER@example.com",
            Password = "Password123!"
        });

        Assert.False(duplicateResult.Success);
        Assert.Equal("Email already registered", duplicateResult.Message);
    }

    [Fact]
    public async Task Register_ReturnsRefreshToken()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.Register(new RegisterDto
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "Password123!"
        });

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.NotNull(result.RefreshTokenExpiry);
        Assert.True(result.RefreshTokenExpiry > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ReturnsRefreshToken()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await service.Register(new RegisterDto
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "Password123!"
        });

        var result = await service.Login(new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        });

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.NotNull(result.RefreshTokenExpiry);
    }

    [Fact]
    public async Task RefreshToken_ReturnsNewAccessAndRefreshToken()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var loginResult = await service.Login(await RegisterAndLogin(service));

        var refreshResult = await service.RefreshTokenAsync(loginResult.RefreshToken!);

        Assert.True(refreshResult.Success);
        Assert.False(string.IsNullOrWhiteSpace(refreshResult.Token));
        Assert.False(string.IsNullOrWhiteSpace(refreshResult.RefreshToken));
        Assert.NotEqual(loginResult.RefreshToken, refreshResult.RefreshToken);
        Assert.NotNull(refreshResult.User);
    }

    [Fact]
    public async Task RefreshToken_RevokesOldToken()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var loginResult = await service.Login(await RegisterAndLogin(service));
        var oldRefreshToken = loginResult.RefreshToken!;

        // First refresh should succeed
        var refreshResult = await service.RefreshTokenAsync(oldRefreshToken);
        Assert.True(refreshResult.Success);

        // Reusing the old token should fail (token reuse detection)
        var reuseResult = await service.RefreshTokenAsync(oldRefreshToken);
        Assert.False(reuseResult.Success);
    }

    [Fact]
    public async Task RefreshToken_RejectsInvalidToken()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.RefreshTokenAsync("totally-invalid-token");

        Assert.False(result.Success);
        Assert.Equal("Invalid refresh token", result.Message);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var loginResult = await service.Login(await RegisterAndLogin(service));

        await service.LogoutAsync(loginResult.RefreshToken!);

        // Trying to use the revoked token should fail
        var refreshResult = await service.RefreshTokenAsync(loginResult.RefreshToken!);
        Assert.False(refreshResult.Success);
    }

    [Fact]
    public async Task RevokeAll_RevokesAllUserTokens()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var dto = await RegisterAndLogin(service);

        // Login multiple times to create multiple refresh tokens
        var login1 = await service.Login(dto);
        var login2 = await service.Login(dto);

        // Get the user ID
        var userId = login1.User!.Id;

        // Revoke all
        await service.RevokeAllTokensAsync(userId);

        // Both tokens should be revoked
        var refresh1 = await service.RefreshTokenAsync(login1.RefreshToken!);
        var refresh2 = await service.RefreshTokenAsync(login2.RefreshToken!);

        Assert.False(refresh1.Success);
        Assert.False(refresh2.Success);
    }

    private static async Task<LoginDto> RegisterAndLogin(AuthService service)
    {
        await service.Register(new RegisterDto
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "Password123!"
        });

        return new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AuthService CreateService(AppDbContext context)
    {
        var jwtOptions = Options.Create(new JwtSettingsOptions
        {
            Secret = "test-secret-key-that-is-long-enough-for-hmac",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        });

        return new AuthService(context, jwtOptions, NullLogger<AuthService>.Instance);
    }
}
