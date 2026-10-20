using GymForge.Application.Modules.Diet.Interfaces;
using GymForge.Application.Modules.SuperAdmin.Interfaces;
using GymForge.Contracts.DietTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymForge.Api.Controllers.User
{
    [ApiController]
    [Route("api/user/diet-tracking")]
    [Authorize(Roles = "User")]
    public class UserDietTrackingController : BaseApiController
    {
        private const int MaxFoodSearchLength = 200;

        private readonly IDietTrackingService _dietTrackingService;
        private readonly INutritionApiService _nutritionApiService;
        private readonly IPlatformStatusService _platformStatus;

        public UserDietTrackingController(IDietTrackingService dietTrackingService, INutritionApiService nutritionApiService, IPlatformStatusService platformStatus)
        {
            _dietTrackingService = dietTrackingService;
            _nutritionApiService = nutritionApiService;
            _platformStatus = platformStatus;
        }

        private Guid GetMemberId()
        {
            return UserId;
        }

        [HttpGet("{date}")]
        public async Task<IActionResult> GetDietLog(DateTime date)
        {
            DietLogDto log = await _dietTrackingService.GetDietLogAsync(GetMemberId(), date);
            return Ok(log);
        }

        [HttpPost("meals")]
        public async Task<IActionResult> AddMealEntry([FromBody] AddMealEntryRequestDto request)
        {
            DietLogDto log = await _dietTrackingService.AddMealEntryAsync(GetMemberId(), request);
            return Ok(log);
        }

        [HttpPut("meals/{mealEntryId}")]
        public async Task<IActionResult> UpdateMealEntry(Guid mealEntryId, [FromBody] UpdateMealEntryRequestDto request)
        {
            DietLogDto log = await _dietTrackingService.UpdateMealEntryAsync(GetMemberId(), mealEntryId, request);
            return Ok(log);
        }

        [HttpDelete("meals/{mealEntryId}")]
        public async Task<IActionResult> RemoveMealEntry(Guid mealEntryId)
        {
            DietLogDto log = await _dietTrackingService.RemoveMealEntryAsync(GetMemberId(), mealEntryId);
            return Ok(log);
        }

        [HttpGet("summary/{endDate}")]
        public async Task<IActionResult> GetWeeklySummary(DateTime endDate)
        {
            List<DietLogSummaryDto> summary = await _dietTrackingService.GetWeeklySummaryAsync(GetMemberId(), endDate);
            return Ok(summary);
        }

        [HttpGet("search-food")]
        [EnableRateLimiting("FoodSearchPolicy")]
        public async Task<IActionResult> SearchFood([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query cannot be empty.");

            if (query.Length > MaxFoodSearchLength)
                return BadRequest($"Please keep the description under {MaxFoodSearchLength} characters.");

            // Switched off by the super admin (e.g. to stop AI lookup costs)
            if (!(await _platformStatus.GetStatusAsync()).FoodSearchEnabled)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { code = "FOOD_SEARCH_OFF", message = "Food search is temporarily unavailable." });

            FoodNutritionDto? result = await _nutritionApiService.GetNutritionForFoodAsync(query);
            if (result == null)
                return NotFound("No nutrition data found for the given query.");

            return Ok(result);
        }
    }
}
