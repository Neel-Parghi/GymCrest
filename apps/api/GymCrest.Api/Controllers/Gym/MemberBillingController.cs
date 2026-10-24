using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Gym.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymCrest.Api.Filters;

namespace GymCrest.Api.Controllers.Gym
{
    [Route("api/billing/members")]
    [Authorize(Roles = "GymOwner,Staff")]
    [RequireModuleAccess("billing")]
    [ApiController]
    public class MemberBillingController : BaseApiController
    {

        private readonly IMemberBillingService _billingService;

        public MemberBillingController(IMemberBillingService billingService)
        {
            _billingService = billingService;
        }

        [HttpGet("overview")]
        public async Task<ActionResult> GetOverview([FromQuery] string monthKey)
        {
            if (GymId == null) 
                return Unauthorized();

            if (string.IsNullOrEmpty(monthKey))
            {
                monthKey = DateTime.UtcNow.ToString("yyyy-MM");
            }

            MemberBillingOverviewDto overview = await _billingService.GetMemberBillingOverviewAsync(GymId.Value, SecureBranchId, monthKey);
            return Ok(overview);
        }


        [HttpPost("invoice")]
        public async Task<ActionResult> CreateCustomInvoice([FromBody] CreateCustomInvoiceRequest request)
        {
            if (GymId == null) 
                return Unauthorized();
           
            bool success = await _billingService.CreateCustomInvoiceAsync(GymId.Value, SecureBranchId, request);
            
            if (success)
            {
                return Ok(new { message = "Custom invoice recorded successfully" });
            }

            return BadRequest(new { message = "Failed to record custom invoice" });
        }

        [HttpPost("pay/{recordId}")]
        public async Task<ActionResult> RecordPayment(Guid recordId, [FromBody] RecordPaymentRequest request)
        {
            if (GymId == null)
                return Unauthorized();

            RecordPaymentResult result = await _billingService.RecordPaymentAsync(GymId.Value, SecureBranchId, recordId, request);
            if (result.Success)
            {
                return Ok(new { message = "Payment recorded successfully" });
            }

            return BadRequest(new { message = result.ErrorMessage ?? "Failed to record payment" });
        }

        [HttpGet("pay/{recordId}/history")]
        public async Task<ActionResult> GetPaymentHistory(Guid recordId)
        {
            if (GymId == null)
                return Unauthorized();

            IEnumerable<PaymentRecordDto> history = await _billingService.GetPaymentHistoryAsync(recordId);
            return Ok(history);
        }
    }
}
