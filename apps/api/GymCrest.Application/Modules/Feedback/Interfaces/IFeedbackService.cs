using GymCrest.Contracts.Feedback;

namespace GymCrest.Application.Modules.Feedback.Interfaces
{
    public interface IFeedbackService
    {
        Task SubmitAsync(Guid userId, string? email, string? role, Guid? gymId, string? userAgent, CreateFeedbackRequest request);
        Task<List<FeedbackDto>> GetAllAsync();
        Task<bool> SetResolvedAsync(Guid id, bool isResolved);
    }
}
