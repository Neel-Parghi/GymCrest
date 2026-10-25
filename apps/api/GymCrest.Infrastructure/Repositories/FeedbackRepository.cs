using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly AppDbContext _dbContext;

        public FeedbackRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Feedback feedback)
        {
            await _dbContext.Feedbacks.AddAsync(feedback);
        }

        public async Task<Feedback?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Feedbacks.FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<List<Feedback>> GetAllAsync()
        {
            return await _dbContext.Feedbacks
                .OrderBy(f => f.IsResolved)
                .ThenByDescending(f => f.CreatedOn)
                .ToListAsync();
        }
    }
}
