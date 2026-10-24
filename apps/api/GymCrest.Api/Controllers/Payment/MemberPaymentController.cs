using GymCrest.Application.Modules.Payments.Interfaces;
using GymCrest.Contracts.Payments;
using GymCrest.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrest.Api.Controllers.Payment
{
    [Route("api/member-payment")]
    [Authorize(Roles = "User")]
    public class MemberPaymentController : BaseApiController
    {
        private readonly IMemberPaymentService _paymentService;

        public MemberPaymentController(IMemberPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("initiate-checkout")]
        public async Task<ActionResult> InitiateCheckout([FromBody] InitiateMemberPaymentRequest request)
        {
            request.UserId = UserId;
            ApiResponse<PaymentInitiationResponse> response = await _paymentService.InitiateCheckoutAsync(request);
            if (!response.Success)
            {
                return BadRequest(response.Message);
            }
            return Ok(response);
        }

        [HttpPost("verify-checkout")]
        public async Task<ActionResult> VerifyCheckout([FromBody] VerifyMemberPaymentRequest request)
        {
            request.UserId = UserId;
            ApiResponse<PaymentVerificationResponse> response = await _paymentService.VerifyCheckoutAsync(request);
            if (!response.Success)
            {
                return BadRequest(response.Message);
            }
            return Ok(response);
        }

        [HttpPost("offline-checkout")]
        public async Task<ActionResult> OfflineCheckout([FromBody] OfflineCheckoutRequest request)
        {
            request.UserId = UserId;
            ApiResponse<OfflineCheckoutResponse> response = await _paymentService.InitiateOfflineCheckoutAsync(request);
            if (!response.Success)
            {
                return BadRequest(response.Message);
            }
            return Ok(response);
        }
    }
}
