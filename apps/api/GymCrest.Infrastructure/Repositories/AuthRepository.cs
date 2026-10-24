using GymCrest.Contracts.Auth;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using GymCrest.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    { 
        private readonly AppDbContext _dbContext;

        public AuthRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task RegisterSuperAdmin(User user)
        {
            await _dbContext.Users.AddAsync(user);
        }

        public async Task AddUserAsync(User user)
        {
            await _dbContext.Users.AddAsync(user);
        }

        public async Task<User?> Login(LoginRequestDto userRequest)
        {
            User? user = await _dbContext.Users
                .Include(x => x.Profile)
                .Include(x => x.Preference)
                .Include(x => x.Security)
                .Include(x => x.RefreshTokens)
                .FirstOrDefaultAsync(x => x.Email.ToLower() == userRequest.Email.ToLower());

            return user;
        }

        public async Task<User?> GetByTokenAsync(string token)
        {
            return await _dbContext.Users
                .Include(u => u.Security)
                .FirstOrDefaultAsync(u => u.Security != null && u.Security.InvitationToken == token);
        }

        public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        {
            RefreshToken? token = await _dbContext.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u.RefreshTokens)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            
            return token?.User;
        }

        public async Task<User?> GetByUserByEmailAsync(string email)
        {
            return await _dbContext.Users
                .Include(u => u.Profile)
                .Include(u => u.Preference)
                .Include(u => u.Security)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }


    }
}
