using GymForge.Application.Modules.Gym.Interfaces;
using GymForge.Contracts.Announcements;
using GymForge.Domain.Entities;
using GymForge.Domain.Enums;
using GymForge.Domain.Interface;

namespace GymForge.Application.Modules.Gym.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly IAnnouncementTemplateRepository _templateRepository;
        private readonly IUserNotificationRepository _notificationRepository;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IGymManagementRepository _gymRepository;

        public TemplateService(
            IAnnouncementTemplateRepository templateRepository,
            IUserNotificationRepository notificationRepository,
            IEmailService emailService,
            IUserRepository userRepository,
            IGymManagementRepository gymRepository)
        {
            _templateRepository = templateRepository;
            _notificationRepository = notificationRepository;
            _emailService = emailService;
            _userRepository = userRepository;
            _gymRepository = gymRepository;
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

        public async Task TestTemplateAsync(Guid id, Guid gymId, Guid userId)
        {
            AnnouncementTemplate? template = await _templateRepository.GetByIdAsync(id);
            if (template == null || template.GymId != gymId)
            {
                throw new Exception("Template not found.");
            }

            User? user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var gym = await _gymRepository.GetGymByIdAsync(gymId);
            string gymName = gym?.GymName ?? "Test Gym";

            string userName = $"{user.FirstName} {user.LastName}";
            string planName = "Test Plan";
            string expiryDate = DateTime.UtcNow.AddDays(3).ToString("MMM dd, yyyy");

            string title = ReplaceTestVariables(template.TitleTemplate, userName, gymName, planName, expiryDate);
            string message = ReplaceTestVariables(template.MessageTemplate, userName, gymName, planName, expiryDate);

            UserNotification notification = new()
            {
                GymId = gymId,
                UserId = userId,
                Title = "[TEST] " + title,
                Message = message,
                IsRead = false,
                CreatedOn = DateTime.UtcNow
            };
            await _notificationRepository.AddAsync(notification);

            if (!string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    await _emailService.SendGymAnnouncementEmailAsync(user.Email, userName, gymName, "[TEST] " + title, message);
                }
                catch
                {
                    // Ignore email failures in test mode
                }
            }
        }

        private static string ReplaceTestVariables(string text, string userName, string gymName, string planName, string expiryDate)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text
                .Replace("{{UserName}}", userName)
                .Replace("{{GymName}}", gymName)
                .Replace("{{PlanName}}", planName)
                .Replace("{{ExpiryDate}}", expiryDate);
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
