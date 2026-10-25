using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IFeedbackRepository
    {
        Task AddAsync(Feedback feedback);
        Task<Feedback?> GetByIdAsync(Guid id);
        Task<List<Feedback>> GetAllAsync();
    }
}
