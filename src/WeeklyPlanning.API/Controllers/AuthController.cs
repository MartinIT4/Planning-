using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace WeeklyPlanning.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] AuthLoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email y contraseña son requeridos." });

        var client = _httpClientFactory.CreateClient("ChrobiAuth");
        using var response = await client.PostAsJsonAsync("auth/login", new
        {
            email = request.Email,
            password = request.Password
        }, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Unauthorized();

        var externalLogin = await response.Content.ReadFromJsonAsync<ChrobiLoginResponse>(JsonOptions, cancellationToken);
        if (externalLogin is null || string.IsNullOrWhiteSpace(externalLogin.AccessToken))
            return Unauthorized();

        var userInfo = ExtractUserInfo(externalLogin, request.Email);
        var token = GenerateJwt(userInfo.Email, userInfo.UserName);

        return Ok(new AuthLoginResponse(token, userInfo.UserName, userInfo.Email));
    }

    private AuthenticatedUserInfo ExtractUserInfo(ChrobiLoginResponse externalLogin, string fallbackEmail)
    {
        var userName = externalLogin.UserName;
        var email = externalLogin.Email;

        if (!string.IsNullOrWhiteSpace(externalLogin.Name))
            userName ??= externalLogin.Name;

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email))
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(externalLogin.AccessToken))
            {
                var token = handler.ReadJwtToken(externalLogin.AccessToken);
                userName ??= FindClaim(token,
                    JwtRegisteredClaimNames.Name,
                    ClaimTypes.Name,
                    "name",
                    "unique_name",
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
                email ??= FindClaim(token,
                    JwtRegisteredClaimNames.Email,
                    ClaimTypes.Email,
                    "email",
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
            }
        }

        email = string.IsNullOrWhiteSpace(email) ? fallbackEmail : email;
        userName = string.IsNullOrWhiteSpace(userName) ? email : userName;

        return new AuthenticatedUserInfo(userName, email);
    }

    private string GenerateJwt(string email, string userName)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is required.");
        var issuer = _configuration["Jwt:Issuer"] ?? "WeeklyPlanningApp";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);

        var token = new JwtSecurityToken(
            issuer: issuer,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Name, userName),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Email, email)
            ],
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string? FindClaim(JwtSecurityToken token, params string[] claimTypes) =>
        token.Claims.FirstOrDefault(c => claimTypes.Contains(c.Type, StringComparer.OrdinalIgnoreCase))?.Value;

    private sealed record ChrobiLoginResponse(string AccessToken, string? RefreshToken, string? UserName, string? Name, string? Email);
    private sealed record AuthenticatedUserInfo(string UserName, string Email);
}

public record AuthLoginRequest(string Email, string Password);
public record AuthLoginResponse(string Token, string UserName, string Email);
