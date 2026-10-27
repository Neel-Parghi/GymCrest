using GymCrest.Contracts.Gym.Billing;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface IMemberBillingService
    {
        Task<MemberBillingOverviewDto> GetMemberBillingOverviewAsync(Guid gymId, Guid? branchId, string monthKey);
        Task<bool> CreateCustomInvoiceAsync(Guid gymId, Guid? branchId, CreateCustomInvoiceRequest request);
        Task<RecordPaymentResult> RecordPaymentAsync(Guid gymId, Guid? branchId, Guid recordId, RecordPaymentRequest request);
        Task<IEnumerable<PaymentRecordDto>> GetPaymentHistoryAsync(Guid gymId, Guid recordId);
    }
}
