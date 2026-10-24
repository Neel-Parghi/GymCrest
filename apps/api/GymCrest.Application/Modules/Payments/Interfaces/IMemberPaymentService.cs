using GymCrest.Contracts.Payments;
using GymCrest.Shared.Models;
using System.Threading.Tasks;

namespace GymCrest.Application.Modules.Payments.Interfaces
{
    public interface IMemberPaymentService
    {
        Task<ApiResponse<PaymentInitiationResponse>> InitiateCheckoutAsync(InitiateMemberPaymentRequest request);
        Task<ApiResponse<PaymentVerificationResponse>> VerifyCheckoutAsync(VerifyMemberPaymentRequest request);
        Task<ApiResponse<OfflineCheckoutResponse>> InitiateOfflineCheckoutAsync(OfflineCheckoutRequest request);
    }
}
