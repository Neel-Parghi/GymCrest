using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly AppDbContext _dbContext;

        public AddressRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Address address)
        {
            await _dbContext.Addresses.AddAsync(address);
        }

        public Task<Address?> GetByIdAsync(Guid id)
        {
            return _dbContext.Addresses.FirstOrDefaultAsync(a => a.Id == id);
        }

        public Task Update(Address address)
        {
            _dbContext.Addresses.Update(address);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id)
        {
            Address? address = await _dbContext.Addresses.FindAsync(id);
            if (address != null)
            {
                _dbContext.Addresses.Remove(address);
            }
        }
    }
}
