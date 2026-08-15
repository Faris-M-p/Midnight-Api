using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MidnightApi.Auth;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateAdminToken(long accountId, long familyId, string username)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);
        var claims = new List<Claim>
        {
            new(AuthClaimTypes.AuthType, AuthTypes.Admin),
            new(AuthClaimTypes.AccountId, accountId.ToString()),
            new(AuthClaimTypes.FamilyId, familyId.ToString()),
            new(AuthClaimTypes.Permission, AuthPermissions.AdminFull),
            new(ClaimTypes.NameIdentifier, accountId.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, AuthRoles.Admin),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return IssueToken(claims, expiresAt);
    }

    public (string Token, DateTime ExpiresAtUtc) CreateAccessTokenSession(
        long familyId,
        long accessTokenId,
        string displayName,
        string permission,
        string scope,
        long? scopeMemberId,
        DateTimeOffset accessTokenExpiresOn)
    {
        var jwtExpiry = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);
        var tokenExpiryUtc = accessTokenExpiresOn.UtcDateTime;
        var expiresAt = jwtExpiry < tokenExpiryUtc ? jwtExpiry : tokenExpiryUtc;
        if (expiresAt <= DateTime.UtcNow)
        {
            expiresAt = DateTime.UtcNow.AddMinutes(1);
        }

        var claims = new List<Claim>
        {
            new(AuthClaimTypes.AuthType, AuthTypes.AccessToken),
            new(AuthClaimTypes.FamilyId, familyId.ToString()),
            new(AuthClaimTypes.AccessTokenId, accessTokenId.ToString()),
            new(AuthClaimTypes.Permission, permission),
            new(AuthClaimTypes.Scope, scope),
            new(ClaimTypes.NameIdentifier, $"token:{accessTokenId}"),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Role, AuthRoles.TokenUser),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (scopeMemberId is > 0)
        {
            claims.Add(new Claim(AuthClaimTypes.ScopeMemberId, scopeMemberId.Value.ToString()));
        }

        return IssueToken(claims, expiresAt);
    }

    private (string Token, DateTime ExpiresAtUtc) IssueToken(IEnumerable<Claim> claims, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
