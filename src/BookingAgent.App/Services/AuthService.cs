using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookingAgent.App.Services;

public interface IAuthService
{
    Task<SignInResult> SignInAsync(string username, string password, bool rememberMe = false);
    Task SignOutAsync();
    Task<UserRecord?> GetCurrentUserAsync();
}

public sealed class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger, IPasswordHasher passwordHasher)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task<SignInResult> SignInAsync(string username, string password, bool rememberMe = false)
    {
        try
        {
            var user = await GetUserAsync(username);
            if (user is null || !user.IsActive)
            {
                return SignInResult.Failed("Invalid username or inactive account.");
            }

            if (!_passwordHasher.Verify(password, user.PasswordSalt, user.PasswordHash))
            {
                return SignInResult.Failed("Invalid username or password.");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email!));
            }
            if (!string.IsNullOrWhiteSpace(user.DisplayName))
            {
                claims.Add(new Claim("display_name", user.DisplayName!));
            }

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var props = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext available.");
            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
            return SignInResult.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for user {Username}", username);
            return SignInResult.Failed("Login failed due to an internal error.");
        }
    }

    public async Task SignOutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return;
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task<UserRecord?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true) return null;
        var username = httpContext.User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return null;
        return await GetUserAsync(username);
    }

    private async Task<UserRecord?> GetUserAsync(string username)
    {
        var cs = _configuration.GetConnectionString("BookingAgent");
        if (string.IsNullOrWhiteSpace(cs))
        {
            _logger.LogWarning("Connection string BookingAgent is not configured.");
            return null;
        }

        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT TOP 1 u.Id, u.Username, u.PasswordHash, u.PasswordSalt, u.Email, u.DisplayName, u.IsActive
FROM dbo.Users u WITH (NOLOCK)
WHERE u.Username = @Username;

SELECT r.Code
FROM dbo.UserRoles ur WITH (NOLOCK)
JOIN dbo.Roles r WITH (NOLOCK) ON ur.RoleId = r.Id
JOIN dbo.Users u WITH (NOLOCK) ON ur.UserId = u.Id
WHERE u.Username = @Username;";
        cmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 100) { Value = username });

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        var user = new UserRecord
        {
            Id = reader.GetInt64(0),
            Username = reader.GetString(1),
            PasswordHash = reader.IsDBNull(2) ? Array.Empty<byte>() : (byte[])reader.GetValue(2),
            PasswordSalt = reader.IsDBNull(3) ? Array.Empty<byte>() : (byte[])reader.GetValue(3),
            Email = reader.IsDBNull(4) ? null : reader.GetString(4),
            DisplayName = reader.IsDBNull(5) ? null : reader.GetString(5),
            IsActive = reader.GetBoolean(6),
            Roles = new List<string>()
        };

        var roles = new List<string>();
        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                roles.Add(reader.GetString(0));
            }
        }
        user.Roles = roles;
        return user;
    }
}

public record SignInResult(bool Success, string? Error)
{
    public static SignInResult Successful() => new(true, null);
    public static SignInResult Failed(string message) => new(false, message);
}

public record UserRecord
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
