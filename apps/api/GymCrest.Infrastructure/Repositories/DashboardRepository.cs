using GymCrest.Contracts.SuperAdmin.Dashboard;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _dbContext;

        public DashboardRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            IQueryable<SaaSPaymentTransaction> query = _dbContext.SaaSPaymentTransactions.Where(s => s.Status == "Paid");
            
            if (startDate.HasValue)
                query = query.Where(s => s.CreatedOn >= startDate.Value);
            
            if (endDate.HasValue)
                query = query.Where(s => s.CreatedOn <= endDate.Value);

            return await query.SumAsync(s => s.Amount);
        }

        public async Task<int> GetActiveSubscriptionsCountAsync(DateTime? asOfDate = null)
        {
            DateTime referenceDate = asOfDate ?? DateTime.UtcNow;
            return await _dbContext.SubscriptionRecords
                .CountAsync(s => s.IsActive && s.EndDate > referenceDate);
        }

        public async Task<int> GetTotalGymsCountAsync()
        {
            return await _dbContext.Gyms.CountAsync();
        }

        public async Task<int> GetPendingVerificationsCountAsync()
        {
            return await _dbContext.Gyms.CountAsync(g => !g.IsVerified);
        }

        public async Task<List<PlanDistributionDto>> GetPlanDistributionAsync()
        {
            List<SubscriptionRecord> activeSubscriptions = await _dbContext.SubscriptionRecords
                .Where(s => s.IsActive)
                .Include(s => s.Plan)
                .ToListAsync();

            int totalActive = activeSubscriptions.Count;
            if (totalActive == 0) return new List<PlanDistributionDto>();

            List<PlanDistributionDto> distribution = activeSubscriptions
                .GroupBy(s => s.Plan.Name)
                .Select(g => new PlanDistributionDto
                {
                    PlanName = g.Key,
                    Count = g.Count(),
                    Percentage = Math.Round((double)g.Count() / totalActive * 100, 1),
                })
                .OrderByDescending(d => d.Count)
                .ToList();

            return distribution;
        }

        public async Task<List<RecentGymRegistrationDto>> GetRecentGymRegistrationsAsync(int count)
        {
            return await _dbContext.Gyms
                .Include(g => g.Owner)
                .OrderByDescending(g => g.CreatedOn)
                .Take(count)
                .Select(g => new RecentGymRegistrationDto
                {
                    Id = g.Id,
                    GymName = g.GymName,
                    OwnerName = g.Owner.FirstName + " " + g.Owner.LastName,
                    DateJoined = g.CreatedOn,
                    Tier = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => s.Plan.Name)
                        .FirstOrDefault() ?? "No Plan",
                    Status = g.IsActive ? "Active" : "Inactive",
                    Initials = g.GymName.Length >= 2 ? g.GymName.Substring(0, 2).ToUpper() : g.GymName.ToUpper()
                })
                .ToListAsync();
        }
    }
}
