using GymForge.Contracts.SaaSPayments;
using GymForge.Contracts.SuperAdmin.Configuration;

namespace GymForge.Application.Modules.Payments.Interfaces
{
    public interface ISaaSPaymentService
    {
        Task<InitiatePaymentResponseDto> InitiateSaaSPaymentAsync(CreatePaymentDto paymentDto);
        
        Task<bool> CanManageGymAsync(Guid gymId, Guid userId);

        Task<bool> ProcessSuccessfulPaymentAsync(string orderId, string paymentId, string signature);

        Task<bool> HandleWebhookAsync(string payload, string signature);
        
        Task<List<PaymentTransactionDto>> GetAllTransactionsAsync();

        Task<PaymentStatsDto> GetPaymentStatsAsync();

        Task<GymSubscriptionStatusDto> GetSubscriptionStatusAsync(Guid gymId);

        Task<GymSubscriptionStatusDto> RenewGymSubscriptionAsync(Guid gymId, Guid planId);

        Task<List<PaymentTransactionDto>> GetGymTransactionsAsync(Guid gymId);
    }
}
