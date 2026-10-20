using GymForge.Api.Filters;
using GymForge.Application.Modules.Payments.Interfaces;
using GymForge.Contracts.SaaSPayments;
using GymForge.Contracts.SuperAdmin.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymForge.Api.Controllers.Payment
{
    [Route("api/payments")]
    [ApiController]
    [AllowExpiredSubscription]
    public class PaymentController : BaseApiController
    {
        private readonly ISaaSPaymentService _paymentService;
        public PaymentController(ISaaSPaymentService saaSPaymentService)
        {
            _paymentService = saaSPaymentService;
        }

        [HttpGet("history")]
        [Authorize(Roles = "GymOwner")]
        public async Task<IActionResult> GetGymTransactionHistory()
        {
            if (GymId == null) return Unauthorized();

            List<PaymentTransactionDto> history = await _paymentService.GetGymTransactionsAsync(GymId.Value);
            return Ok(history);
        }

        [HttpPost("initiate")]
        [Authorize(Roles = "GymOwner,SuperAdmin")]
        public async Task<IActionResult> InitiatePayment([FromBody] CreatePaymentDto request)
        {
            if (!User.IsInRole("SuperAdmin") && !await _paymentService.CanManageGymAsync(request.GymId, UserId))
            {
                return Forbid();
            }

            InitiatePaymentResponseDto response = await _paymentService.InitiateSaaSPaymentAsync(request);
            return Ok(response);
        }

        [HttpPost("verify")]
        [Authorize(Roles = "GymOwner,SuperAdmin")]
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequestDto request)
        {
            bool verified = await _paymentService.ProcessSuccessfulPaymentAsync(request.OrderId, request.PaymentId, request.Signature);
            if (!verified)
            {
                return BadRequest(new { message = "Payment verification failed." });
            }

            return Ok(new { message = "Payment verified successfully." });
        }

        /// <summary>Manual renewal without a gateway payment - SuperAdmin only (comps, corrections).</summary>
        [HttpPost("renew")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> RenewSubscription([FromBody] RenewSaaSRequestDto request)
        {
            if (request.GymId == Guid.Empty) return BadRequest(new { message = "GymId is required." });

            GymSubscriptionStatusDto status = await _paymentService.RenewGymSubscriptionAsync(request.GymId, request.PlanId);
            return Ok(status);
        }

        /// <summary>
        /// Razorpay webhook (payment.captured / order.paid / payment.failed). Activates the plan even when the
        /// browser closes before /verify runs. Authenticated by the X-Razorpay-Signature HMAC, not a user token.
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            using StreamReader reader = new(Request.Body);
            string payload = await reader.ReadToEndAsync();
            string signature = Request.Headers["X-Razorpay-Signature"].ToString();

            bool accepted = await _paymentService.HandleWebhookAsync(payload, signature);
            return accepted ? Ok() : BadRequest();
        }

        [HttpGet("stats")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetStats()
        {
            PaymentStatsDto stats = await _paymentService.GetPaymentStatsAsync();
            return Ok(stats);
        }

        [HttpGet("transactions")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetTransactions()
        {
            List<PaymentTransactionDto> transactions = await _paymentService.GetAllTransactionsAsync();
            return Ok(transactions);
        }
    }
}
