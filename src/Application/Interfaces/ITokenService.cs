
using MyApp.Domain.Entities;

namespace MyApp.Application.Interfaces
{
    public interface ITokenService
    {

        Task<string> CreateJwtToken(ApplicationUser user);
        Task<RefreshTokens> GenerateRefreshToken(string userId);
        Task<RefreshTokens?> GetRefreshToken(string token);
        Task RevokeRefreshToken(RefreshTokens token);

    }
}
