using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IAddressRepository
    {
        Task AddAsync(Address address);
        Task Update(Address address);
        Task<Address?> GetByIdAsync(Guid id);
        Task DeleteAsync(Guid id);
    }
}
