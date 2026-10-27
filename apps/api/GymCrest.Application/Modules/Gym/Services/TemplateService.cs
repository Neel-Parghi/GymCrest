using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Announcements;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Enums;
using GymCrest.Domain.Interface;

namespace GymCrest.Application.Modules.Gym.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly IAnnouncementTemplateRepository _templateRepository;

        public TemplateService(IAnnouncementTemplateRepository templateRepository)
        {
            _templateRepository = templateRepository;
        }

        public async Task<List<AnnouncementTemplateResponse>> GetTemplatesAsync(Guid gymId, Guid? branchId)
        {
            List<AnnouncementTemplate>? templates = await _templateRepository.GetByGymIdAsync(gymId, branchId);
            return templates.Select(MapToResponse).ToList();
        }

        public async Task<AnnouncementTemplateResponse?> GetTemplateByIdAsync(Guid id, Guid gymId)
        {
            AnnouncementTemplate? template = await _templateRepository.GetByIdAsync(id);
            if (template == null || template.GymId != gymId)
            {
                return null;
            }
            return MapToResponse(template);
        }

        public async Task<AnnouncementTemplateResponse> CreateTemplateAsync(AnnouncementTemplateRequest request, Guid gymId, Guid? branchId, Guid createdBy)
        {
            AnnouncementTemplate template = new()
            {
                GymId = gymId,
                BranchId = branchId,
                Name = request.Name,
                Type = (TemplateType)request.Type,
                TitleTemplate = request.TitleTemplate,
                MessageTemplate = request.MessageTemplate,
                IsActive = request.IsActive,
                CreatedBy = createdBy,
                CreatedOn = DateTime.UtcNow
            };

            await _templateRepository.AddAsync(template);
            return MapToResponse(template);
        }

        public async Task<AnnouncementTemplateResponse> UpdateTemplateAsync(Guid id, AnnouncementTemplateRequest request, Guid gymId, Guid updatedBy)
        {
            AnnouncementTemplate? template = await _templateRepository.GetByIdAsync(id);
            if (template == null || template.GymId != gymId)
            {
                throw new Exception("Template not found.");
            }

            template.Name = request.Name;
            template.Type = (TemplateType)request.Type;
            template.TitleTemplate = request.TitleTemplate;
            template.MessageTemplate = request.MessageTemplate;
            template.IsActive = request.IsActive;
            template.ModifiedBy = updatedBy;
            template.ModifiedOn = DateTime.UtcNow;

            _templateRepository.Update(template);
            return MapToResponse(template);
        }

        public async Task DeleteTemplateAsync(Guid id, Guid gymId)
        {
            AnnouncementTemplate? template = await _templateRepository.GetByIdAsync(id);
            if (template == null || template.GymId != gymId)
            {
                throw new Exception("Template not found.");
            }

            await _templateRepository.DeleteAsync(id);
        }

        private static AnnouncementTemplateResponse MapToResponse(AnnouncementTemplate entity)
        {
            return new AnnouncementTemplateResponse
            {
                Id = entity.Id,
                GymId = entity.GymId,
                BranchId = entity.BranchId,
                Name = entity.Name,
                Type = (int)entity.Type,
                TitleTemplate = entity.TitleTemplate,
                MessageTemplate = entity.MessageTemplate,
                IsActive = entity.IsActive,
                CreatedOn = entity.CreatedOn
            };
        }
    }
}
