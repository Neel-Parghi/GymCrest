using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class SaaSPaymentRepository : ISaaSPaymentRepository
    {
        public readonly AppDbContext _dbContext;

        public SaaSPaymentRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(SaaSPaymentTransaction transaction)
        {
            _dbContext.SaaSPaymentTransactions.Add(transaction);
            await Task.CompletedTask;
        }

        public async Task<SaaSPaymentTransaction?> GetByGatewayIdAsync(string gatewayId)
        {
            return await _dbContext.SaaSPaymentTransactions
                .Include(x => x.Subscription)
                .FirstOrDefaultAsync(x => x.GatewayTransactionId == gatewayId);
        }

        public async Task<SubscriptionRecord?> GetLatestSubscriptionByGymIdAsync(Guid gymId)
        {
            return await _dbContext.SubscriptionRecords
                .Include(x => x.Plan)
                .Where(x => x.GymId == gymId)
                .OrderByDescending(x => x.CreatedOn)
                .FirstOrDefaultAsync();
        }

        public async Task<List<SaaSPaymentTransaction>> GetTransactionsAsync()
        {
            return await _dbContext.SaaSPaymentTransactions
                .Include(x => x.Gym)
                .Include(x => x.Subscription)
                .ThenInclude(x => x.Plan)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();
        }

        public async Task<List<SubscriptionRecord>> GetActiveSubscriptionsAsync()
        {
            return await _dbContext.SubscriptionRecords
                .Include(x => x.Plan)
                .Where(x => x.IsActive)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task UpdateStatusAsync(Guid id, string status, string? gatewayResponse = null)
        {
            SaaSPaymentTransaction? paymentTransaction = await _dbContext.SaaSPaymentTransactions.FindAsync(id);

            if (paymentTransaction != null)
            {
                paymentTransaction.Status = status;
                if (gatewayResponse != null)
                    paymentTransaction.GatewayResponse = gatewayResponse;
            }
        }
    }
}
