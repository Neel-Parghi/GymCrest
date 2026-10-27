using GymCrest.Contracts.Announcements;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface ITemplateService
    {
        Task<List<AnnouncementTemplateResponse>> GetTemplatesAsync(Guid gymId, Guid? branchId);
       
        Task<AnnouncementTemplateResponse?> GetTemplateByIdAsync(Guid id, Guid gymId);
        
        Task<AnnouncementTemplateResponse> CreateTemplateAsync(AnnouncementTemplateRequest request, Guid gymId, Guid? branchId, Guid createdBy);
        
        Task<AnnouncementTemplateResponse> UpdateTemplateAsync(Guid id, AnnouncementTemplateRequest request, Guid gymId, Guid updatedBy);
        
        Task DeleteTemplateAsync(Guid id, Guid gymId);

    }
}
