using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class SaaSConfigurationRepository : ISaaSConfigurationRepository
    {
        private readonly AppDbContext _dbContext;

        public SaaSConfigurationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SaaSConfiguration> GetConfigurationAsync()
        {
            SaaSConfiguration? config = await _dbContext.SaaSConfigurations.FirstOrDefaultAsync();
            if (config == null)
            {
                config = new SaaSConfiguration();
                _dbContext.SaaSConfigurations.Add(config);
                await _dbContext.SaveChangesAsync();
            }
            return config;
        }

        public async Task UpdateConfigurationAsync(SaaSConfiguration configuration)
        {
            _dbContext.SaaSConfigurations.Update(configuration);
            await _dbContext.SaveChangesAsync();
        }
    }
}
