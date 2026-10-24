using GymCrest.Domain.Entities;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface IGymAnnouncementRepository
    {
        Task<GymAnnouncement> AddAsync(GymAnnouncement announcement);
        Task<GymAnnouncement?> GetByIdAsync(Guid id);
        Task<IEnumerable<GymAnnouncement>> GetAllByGymIdAsync(Guid gymId);
        Task<IEnumerable<GymAnnouncement>> GetActiveByGymIdAsync(Guid gymId);
        Task UpdateAsync(GymAnnouncement announcement);
        Task DeleteAsync(GymAnnouncement announcement);
    }
}
