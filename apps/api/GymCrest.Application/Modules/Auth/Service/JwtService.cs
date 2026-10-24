using GymCrest.Application.Modules.Auth.Interface;
using GymCrest.Contracts.Auth;
using GymCrest.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GymCrest.Application.Modules.Auth.Service
{
    public class JwtService : IJwtService
    {
        private const int DefaultAccessTokenMinutes = 15;
        private const int DefaultRefreshTokenDays = 30;

        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public TokenResponseDto GenerateToken(User user, Guid? branchId = null)
        {
            List<Claim> claims = new()
            {
                new Claim("userId", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("role", user.Role.ToString()),
                new Claim("gymId", user.GymId?.ToString() ?? string.Empty),
                new Claim("isOnboarded", (user.Profile?.IsOnboarded ?? false).ToString()),
                new Claim("currentOnboardingStep", (user.Profile?.CurrentOnboardingStep ?? 0).ToString()),
            };

            if (branchId.HasValue)
            {
                claims.Add(new Claim("branchId", branchId.Value.ToString()));
            }

            SymmetricSecurityKey key = new (Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            SigningCredentials creds = new (key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new (
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(ReadPositiveInt("Jwt:AccessTokenExpiryMinutes", DefaultAccessTokenMinutes)),
                signingCredentials: creds
            );

            return new TokenResponseDto
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = GenerateRefreshToken()
            };
        }

        public string GenerateRefreshToken()
        {
            byte[] randomNumber = new byte[64];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public DateTime GetRefreshTokenExpiry()
        {
            return DateTime.UtcNow.AddDays(ReadPositiveInt("Jwt:RefreshTokenExpiryDays", DefaultRefreshTokenDays));
        }

        private int ReadPositiveInt(string key, int fallback)
        {
            return int.TryParse(_config[key], out int value) && value > 0 ? value : fallback;
        }
    }
}
