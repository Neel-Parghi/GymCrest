using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using GymCrest.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class MemberBillingRepository : IMemberBillingRepository
    {

        private readonly AppDbContext _dbContext;

        public MemberBillingRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddSaleTransactionAsync(SaleTransaction transaction)
        {
            await _dbContext.SaleTransactions.AddAsync(transaction);
        }

        public async Task<InventoryItem?> GetAnyInventoryItemAsync(Guid gymId)
        {
            return await _dbContext.InventoryItems.FirstOrDefaultAsync(i => i.GymId == gymId);
        }

        public async Task<InventoryItem?> GetInventoryItemByNameAsync(Guid gymId, string name)
        {
            return await _dbContext.InventoryItems.FirstOrDefaultAsync(i => i.GymId == gymId && i.Name == name);
        }

        public async Task<IEnumerable<MemberSubscription>> GetSubscriptionsByMonthAsync(Guid gymId, DateTime startDate, DateTime endDate)
        {
            return await _dbContext.MemberSubscriptions
                .Include(s => s.Member)
                .ThenInclude(m => m.Subscriptions)
                .Include(s => s.Member)
                .ThenInclude(m => m.Branch)
                .ThenInclude(b => b.Address)
                .Include(s => s.GymPlan)
                .Where(s => s.Member.GymId == gymId && s.StartDate >= startDate && s.StartDate <= endDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<SaleTransaction>> GetTransactionsByMonthAsync(Guid gymId, DateTime startDate, DateTime endDate)
        {
            return await _dbContext.SaleTransactions
                .Include(t => t.Member)
                .Include(t => t.InventoryItem)
                .Include(t => t.Branch)
                .ThenInclude(b => b.Address)
                .Where(x => x.GymId == gymId && x.TransactionDate >= startDate && x.TransactionDate <= endDate)
                .ToListAsync();
        }

        public async Task<Branch?> GetMainBranchAsync(Guid gymId)
        {
            return await _dbContext.Branches
                .Include(b => b.Address)
                .FirstOrDefaultAsync(b => b.GymId == gymId && b.IsMainBranch);
        }

        public async Task<Gym?> GetGymByIdAsync(Guid gymId)
        {
            Gym? gym = await _dbContext.Gyms.FindAsync(gymId);
            if (gym == null)
            {
                gym = await _dbContext.Gyms.FirstOrDefaultAsync();
            }
            return gym;
        }

        public async Task AddInventoryItemAsync(InventoryItem item)
        {
            await _dbContext.InventoryItems.AddAsync(item);
        }

        public async Task<MemberSubscription?> GetSubscriptionByIdAsync(Guid recordId)
        {
            return await _dbContext.MemberSubscriptions.FindAsync(recordId);
        }

        public async Task<SaleTransaction?> GetTransactionByIdAsync(Guid recordId)
        {
            return await _dbContext.SaleTransactions.FindAsync(recordId);
        }

        public async Task<IEnumerable<StaffPayrollRule>> GetPayrollRulesAsync(Guid gymId, Guid? branchId = null)
        {
            IQueryable<StaffPayrollRule> query = _dbContext.StaffPayrollRules.Where(x => x.GymId == gymId);
            if (branchId.HasValue)
            {
                query = query.Where(x => x.BranchId == branchId.Value);
            }
            return await query.ToListAsync();
        }

        public async Task<StaffPayrollRule?> GetPayrollRuleByStaffIdAsync(Guid staffId)
        {
            return await _dbContext.StaffPayrollRules.FirstOrDefaultAsync(x => x.StaffId == staffId);
        }

        public async Task AddPayrollRuleAsync(StaffPayrollRule rule)
        {
            await _dbContext.StaffPayrollRules.AddAsync(rule);
        }

        public async Task<IEnumerable<StaffPayoutLog>> GetPayoutLogsByMonthAsync(Guid gymId, string monthKey, Guid? branchId = null)
        {
            IQueryable<StaffPayoutLog> query = _dbContext.StaffPayoutLogs.Where(x => x.GymId == gymId && x.MonthKey == monthKey);
            if (branchId.HasValue)
            {
                query = query.Where(x => x.BranchId == branchId.Value);
            }
            return await query.ToListAsync();
        }

        public async Task<StaffPayoutLog?> GetPayoutLogAsync(Guid staffId, string monthKey)
        {
            return await _dbContext.StaffPayoutLogs.FirstOrDefaultAsync(x => x.StaffId == staffId && x.MonthKey == monthKey);
        }

        public async Task AddPayoutLogAsync(StaffPayoutLog log)
        {
            await _dbContext.StaffPayoutLogs.AddAsync(log);
        }

        public async Task AddCustomInvoiceAsync(CustomInvoice invoice)
        {
            await _dbContext.CustomInvoices.AddAsync(invoice);
        }

        public async Task<CustomInvoice?> GetCustomInvoiceByIdAsync(Guid recordId)
        {
            return await _dbContext.CustomInvoices.FindAsync(recordId);
        }

        public async Task<IEnumerable<CustomInvoice>> GetCustomInvoicesByMonthAsync(Guid gymId, DateTime startDate, DateTime endDate)
        {
            return await _dbContext.CustomInvoices
                .Include(c => c.Member)
                .Include(c => c.Branch)
                .ThenInclude(b => b.Address)
                .Where(x => x.GymId == gymId && x.TransactionDate >= startDate && x.TransactionDate <= endDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomInvoice>> GetCustomInvoicesByTrainerAndMonthAsync(Guid trainerId, DateTime startDate, DateTime endDate)
        {
            return await _dbContext.CustomInvoices
                .Where(x => x.TrainerId == trainerId
                         && x.TransactionDate >= startDate
                         && x.TransactionDate <= endDate
                         && !x.PaymentMethod.StartsWith("Pending"))
                .ToListAsync();
        }

        public async Task AddPaymentRecordAsync(PaymentRecord record)
        {
            await _dbContext.PaymentRecords.AddAsync(record);
        }

        public async Task<Dictionary<Guid, List<string>>> GetPaymentMethodsBySourcesAsync(PaymentSourceType sourceType, IEnumerable<Guid> sourceIds)
        {
            List<Guid> ids = sourceIds.Distinct().ToList();
            
            if (ids.Count == 0) 
                return [];

            var rows = await _dbContext.PaymentRecords
                .Where(x => x.SourceType == sourceType && ids.Contains(x.SourceId))
                .OrderBy(x => x.PaidAt)
                .Select(x => new { x.SourceId, x.PaymentMethod })
                .ToListAsync();

            return rows
                .GroupBy(x => x.SourceId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.PaymentMethod).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList());
        }

        public async Task<IEnumerable<PaymentRecord>> GetPaymentRecordsBySourceAsync(PaymentSourceType sourceType, Guid sourceId)
        {
            return await _dbContext.PaymentRecords
                .Where(x => x.SourceType == sourceType && x.SourceId == sourceId)
                .OrderByDescending(x => x.PaidAt)
                .ToListAsync();
        }
    }
}
