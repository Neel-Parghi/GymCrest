using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface ISaaSPaymentRepository
    {
        Task AddAsync(SaaSPaymentTransaction transaction);

        Task<List<SaaSPaymentTransaction>> GetTransactionsAsync();
        Task<List<SubscriptionRecord>> GetActiveSubscriptionsAsync();

        Task<SaaSPaymentTransaction?> GetByGatewayIdAsync(string gatewayId);
        Task<SubscriptionRecord?> GetLatestSubscriptionByGymIdAsync(Guid gymId);

        Task UpdateStatusAsync(Guid id, string status, string? gatewayResponse = null);
    }
}
