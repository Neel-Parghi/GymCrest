using GymCrest.Contracts.Auth;
using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IAuthRepository
    {
        Task RegisterSuperAdmin(User user);
        
        Task AddUserAsync(User user);

        Task<User?> GetByTokenAsync(string token);
        
        Task<User?> GetByRefreshTokenAsync(string refreshToken);
        
        Task<User?> GetByUserByEmailAsync(string email);
        
        Task<User?> Login(LoginRequestDto user);
    }
}
