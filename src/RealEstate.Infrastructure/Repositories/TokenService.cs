using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RealEstate.Core.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;

namespace RealEstate.Infrastructure.Repositories
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly IRepository<RefreshToken> _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public TokenService(IConfiguration config, IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _config = config;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _refreshTokenRepository = unitOfWork.RefreshTokens;
        }

        public string CreateToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add roles to claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Secret"] ?? string.Empty));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = creds,
                Issuer = _config["JWT:Issuer"],
                Audience = _config["JWT:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public async Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return CreateToken(user, roles);
        }

        public async Task<string> GenerateRefreshTokenAsync(ApplicationUser user)
        {
            var refreshToken = await CreateRefreshTokenAsync(user.Id);
            return refreshToken.Token;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Secret"] ?? string.Empty)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(string userId)
        {
            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = GenerateRefreshToken(),
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokenRepository.AddAsync(refreshToken);
            await _unitOfWork.CompleteAsync();

            return refreshToken;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string token)
        {
            var refreshToken = await _refreshTokenRepository.SingleOrDefaultAsync(r => r.Token == token);

            if (refreshToken == null)
                return false;

            refreshToken.IsRevoked = true;
            _refreshTokenRepository.Update(refreshToken);
            await _unitOfWork.CompleteAsync();

            return true;
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            var token = await _refreshTokenRepository.SingleOrDefaultAsync(r => r.Token == refreshToken && !r.IsRevoked && r.ExpiryDate > DateTime.UtcNow);

            if (token == null)
                return null;

            var user = await _userManager.FindByIdAsync(token.UserId);
            if (user == null)
                return null;

            // Generate new tokens
            var newAccessToken = await GenerateTokenAsync(user);
            var newRefreshToken = await GenerateRefreshTokenAsync(user);

            // Revoke old refresh token
            token.IsRevoked = true;
            _refreshTokenRepository.Update(token);
            await _unitOfWork.CompleteAsync();

            return new AuthResponseDto
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                Expiration = DateTime.UtcNow.AddHours(1),
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    ProfilePictureUrl = user.ProfilePictureUrl ?? string.Empty
                }
            };
        }

        public async Task RevokeAllRefreshTokensAsync(string userId)
        {
            var tokens = await _refreshTokenRepository.FindAsync(r => r.UserId == userId && !r.IsRevoked);
            
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                _refreshTokenRepository.Update(token);
            }

            await _unitOfWork.CompleteAsync();
        }
    }
}
