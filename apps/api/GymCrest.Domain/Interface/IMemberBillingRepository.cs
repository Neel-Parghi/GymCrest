using GymCrest.Domain.Entities;
using GymCrest.Shared.Enums;

namespace GymCrest.Domain.Interface
{
    public interface IMemberBillingRepository
    {
        Task<IEnumerable<MemberSubscription>> GetSubscriptionsByMonthAsync(Guid gymId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<SaleTransaction>> GetTransactionsByMonthAsync(Guid gymId, DateTime startDate, DateTime endDate);

        Task AddSaleTransactionAsync(SaleTransaction transaction);
        Task<InventoryItem?> GetInventoryItemByNameAsync(Guid gymId, string name);
        Task<InventoryItem?> GetAnyInventoryItemAsync(Guid gymId);
        Task<Branch?> GetMainBranchAsync(Guid gymId);
        Task<Gym?> GetGymByIdAsync(Guid gymId);
        Task AddInventoryItemAsync(InventoryItem item);
        Task<MemberSubscription?> GetSubscriptionByIdAsync(Guid recordId, Guid gymId);
        Task<SaleTransaction?> GetTransactionByIdAsync(Guid recordId, Guid gymId);

        // Custom Invoice methods
        Task AddCustomInvoiceAsync(CustomInvoice invoice);
        Task<CustomInvoice?> GetCustomInvoiceByIdAsync(Guid recordId, Guid gymId);
        Task<IEnumerable<CustomInvoice>> GetCustomInvoicesByMonthAsync(Guid gymId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<CustomInvoice>> GetCustomInvoicesByTrainerAndMonthAsync(Guid trainerId, DateTime startDate, DateTime endDate);

        // Payment ledger methods
        Task AddPaymentRecordAsync(PaymentRecord record);
        Task<IEnumerable<PaymentRecord>> GetPaymentRecordsBySourceAsync(PaymentSourceType sourceType, Guid sourceId, Guid gymId);
        Task<Dictionary<Guid, List<string>>> GetPaymentMethodsBySourcesAsync(PaymentSourceType sourceType, IEnumerable<Guid> sourceIds);

        // Staff Payroll repository methods
        Task<IEnumerable<StaffPayrollRule>> GetPayrollRulesAsync(Guid gymId, Guid? branchId = null);
        Task<StaffPayrollRule?> GetPayrollRuleByStaffIdAsync(Guid staffId);
        Task AddPayrollRuleAsync(StaffPayrollRule rule);
        Task<IEnumerable<StaffPayoutLog>> GetPayoutLogsByMonthAsync(Guid gymId, string monthKey, Guid? branchId = null);
        Task<StaffPayoutLog?> GetPayoutLogAsync(Guid staffId, string monthKey);
        Task AddPayoutLogAsync(StaffPayoutLog log);
    }
}
