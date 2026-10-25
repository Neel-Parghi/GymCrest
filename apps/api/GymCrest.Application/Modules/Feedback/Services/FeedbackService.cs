using GymCrest.Application.Modules.Feedback.Interfaces;
using GymCrest.Contracts.Feedback;
using GymCrest.Domain.Interface;

namespace GymCrest.Application.Modules.Feedback.Services
{
    public class FeedbackService : IFeedbackService
    {
        public const int MaxMessageLength = 2000;
        private static readonly string[] Categories = ["Bug", "Idea", "Other"];

        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IUnitOfWork _unitOfWork;

        public FeedbackService(IFeedbackRepository feedbackRepository, IUnitOfWork unitOfWork)
        {
            _feedbackRepository = feedbackRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task SubmitAsync(Guid userId, string? email, string? role, Guid? gymId, string? userAgent, CreateFeedbackRequest request)
        {
            string message = (request.Message ?? string.Empty).Trim();
            if (message.Length < 5)
                throw new ArgumentException("Please describe the problem or idea in a few words.");

            await _feedbackRepository.AddAsync(new Domain.Entities.Feedback
            {
                Id = Guid.NewGuid(),
                UserId = userId == Guid.Empty ? null : userId,
                UserEmail = email,
                UserRole = role,
                GymId = gymId,
                Category = Categories.FirstOrDefault(c => c.Equals(request.Category, StringComparison.OrdinalIgnoreCase)) ?? "Other",
                Message = Truncate(message, MaxMessageLength)!,
                PageUrl = Truncate(request.PageUrl?.Trim(), 500),
                UserAgent = Truncate(userAgent, 300)
            });
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<FeedbackDto>> GetAllAsync()
        {
            return (await _feedbackRepository.GetAllAsync())
                .Select(f => new FeedbackDto
                {
                    Id = f.Id,
                    Category = f.Category,
                    Message = f.Message,
                    PageUrl = f.PageUrl,
                    UserAgent = f.UserAgent,
                    UserEmail = f.UserEmail,
                    UserRole = f.UserRole,
                    IsResolved = f.IsResolved,
                    CreatedOn = f.CreatedOn
                })
                .ToList();
        }

        public async Task<bool> SetResolvedAsync(Guid id, bool isResolved)
        {
            Domain.Entities.Feedback? feedback = await _feedbackRepository.GetByIdAsync(id);
            if (feedback == null) return false;

            feedback.IsResolved = isResolved;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static string? Truncate(string? value, int max) =>
            value == null ? null : value.Length <= max ? value : value[..max];
    }
}
