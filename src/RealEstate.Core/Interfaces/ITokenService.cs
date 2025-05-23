using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using RealEstate.Core.DTOs;
using RealEstate.Core.Entities;

namespace RealEstate.Core.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(ApplicationUser user, IList<string> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        Task<RefreshToken> CreateRefreshTokenAsync(string userId);
        Task<bool> RevokeRefreshTokenAsync(string token);
        
        // New methods for phone authentication
        Task<string> GenerateTokenAsync(ApplicationUser user);
        Task<string> GenerateRefreshTokenAsync(ApplicationUser user);
        Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken);
        Task RevokeAllRefreshTokensAsync(string userId);
    }
}
