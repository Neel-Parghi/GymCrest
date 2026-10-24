using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IAnnouncementTemplateRepository
    {
        Task<AnnouncementTemplate?> GetByIdAsync(Guid id);
        Task<List<AnnouncementTemplate>> GetByGymIdAsync(Guid gymId, Guid? branchId = null);
        Task AddAsync(AnnouncementTemplate template);
        void Update(AnnouncementTemplate template);
        Task DeleteAsync(Guid id);
    }
}
