using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Announcements;
using GymCrest.Domain.Entities;

namespace GymCrest.Application.Modules.Gym.Services
{
    public class GymAnnouncementService : IGymAnnouncementService
    {
        private readonly IGymAnnouncementRepository _repository;

        public GymAnnouncementService(IGymAnnouncementRepository repository)
        {
            _repository = repository;
        }

        public async Task<GymAnnouncementResponse> CreateAnnouncementAsync(Guid gymId, Guid? branchId, GymAnnouncementRequest request)
        {
            GymAnnouncement announcement = new()
            {
                GymId = gymId,
                BranchId = branchId,
                Title = request.Title,
                Message = request.Message,
                IsActive = request.IsActive,
                ValidUntil = request.ValidUntil
            };

            await _repository.AddAsync(announcement);

            return MapToResponse(announcement);
        }

        public async Task<IEnumerable<GymAnnouncementResponse>> GetAllAnnouncementsAsync(Guid gymId)
        {
            IEnumerable<GymAnnouncement> announcements = await _repository.GetAllByGymIdAsync(gymId);
            IEnumerable<GymAnnouncementResponse> responses = announcements.Select(MapToResponse);
            return responses;
        }

        public async Task<IEnumerable<GymAnnouncementResponse>> GetActiveAnnouncementsAsync(Guid gymId)
        {
            IEnumerable<GymAnnouncement> announcements = await _repository.GetActiveByGymIdAsync(gymId);

            IEnumerable<GymAnnouncementResponse> responses = announcements
                .Where(a => a.ValidUntil == null || a.ValidUntil > DateTime.UtcNow)
                .Select(MapToResponse);
            return responses;
        }

        public async Task<GymAnnouncementResponse?> UpdateAnnouncementAsync(Guid id, Guid gymId, GymAnnouncementRequest request)
        {
            GymAnnouncement? announcement = await _repository.GetByIdAsync(id);
            
            if (announcement == null || announcement.GymId != gymId)
            {
                return null;
            }

            announcement.Title = request.Title;
            announcement.Message = request.Message;
            announcement.IsActive = request.IsActive;
            announcement.ValidUntil = request.ValidUntil;

            await _repository.UpdateAsync(announcement);

            return MapToResponse(announcement);
        }

        public async Task<bool> DeleteAnnouncementAsync(Guid id, Guid gymId)
        {
            GymAnnouncement? announcement = await _repository.GetByIdAsync(id);
            
            if (announcement == null || announcement.GymId != gymId)
            {
                return false;
            }

            await _repository.DeleteAsync(announcement);
            return true;
        }

        private static GymAnnouncementResponse MapToResponse(GymAnnouncement announcement)
        {
            return new GymAnnouncementResponse
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Message = announcement.Message,
                IsActive = announcement.IsActive,
                ValidUntil = announcement.ValidUntil,
                CreatedOn = announcement.CreatedOn
            };
        }
    }
}
