using GymCrest.Contracts.Auth;
using GymCrest.Domain.Entities;

namespace GymCrest.Application.Modules.Auth.Interface
{
    public interface IJwtService
    {
        TokenResponseDto GenerateToken(User user, Guid? branchId = null);
        string GenerateRefreshToken();
        DateTime GetRefreshTokenExpiry();
    }
}
