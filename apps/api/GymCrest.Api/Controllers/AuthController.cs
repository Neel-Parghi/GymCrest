using GymCrest.Application.BackgroundJobs;
using GymCrest.Application.Modules.Auth.Interface;
using GymCrest.Contracts.Auth;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using GymCrest.Api.Filters;
using GymCrest.Api.Middlewares;

namespace GymCrest.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private const int DefaultDeletionGracePeriodHours = 24;

        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // [HttpPost("register-superadmin")]
        // public async Task<IActionResult> RegisterSuperAdmin(RegisterRequestDto dto)
        // {
        //     TokenResponseDto response = await _authService.RegisterSuperAdmin(dto);
        //     return Ok(response);
        // }

        [HttpPost("register")]
        [EnableRateLimiting("OtpPolicy")]
        public async Task<IActionResult> Register(RegisterRequestDto dto)
        {
            try
            {
                RegisterResponseDto response = await _authService.RegisterAsync(dto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [HttpPost("verify-otp")]
        [EnableRateLimiting("OtpPolicy")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpRequestDto dto)
        {
            try
            {
                TokenResponseDto response = await _authService.VerifyOtpAsync(dto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [HttpPost("resend-otp")]
        [EnableRateLimiting("OtpPolicy")]
        public async Task<IActionResult> ResendOtp(ResendOtpRequestDto dto)
        {
            try
            {
                await _authService.ResendOtpAsync(dto);
                return Ok(new { message = "OTP resent successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            TokenResponseDto response = await _authService.Login(dto);
            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequestDto dto)
        {
            try
            {
                TokenResponseDto response = await _authService.RefreshTokenAsync(dto);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenRequestDto dto)
        {
            await _authService.LogoutAsync(dto.RefreshToken);
            return NoContent();
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("OtpPolicy")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto dto)
        {
            try
            {
                await _authService.ForgotPasswordAsync(dto);
                return Ok(new { message = "If the email is registered, a password reset link has been sent." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("OtpPolicy")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto dto)
        {
            try
            {
                await _authService.ResetPasswordAsync(dto);
                return Ok(new { message = "Password reset successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            string? userId = User.FindFirst("userId")?.Value;
            string? email = User.FindFirst(ClaimTypes.Email)?.Value;
            string? role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new { userId, email, role });
        }

        [Authorize]
        [BlockDemoAccount]
        [HttpDelete("request-deletion")]
        public async Task<IActionResult> RequestAccountDeletion(
            [FromServices] IBackgroundJobClient backgroundJobs,
            [FromServices] AccountDeletionJob deletionJob,
            [FromServices] IConfiguration config)
        {
            string? userIdStr = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            try
            {
                bool needsReview = await _authService.RequestAccountDeletionAsync(userId);
                if (needsReview)
                {
                    return Accepted(new
                    {
                        message = "Your deletion request has been sent to the GymCrest team. We'll review it and contact you before your gym is removed.",
                        deleted = false,
                        pendingReview = true
                    });
                }

                int gracePeriodHours = config.GetValue("AccountDeletion:GracePeriodHours", DefaultDeletionGracePeriodHours);
                if (gracePeriodHours <= 0)
                {
                    await deletionJob.ExecuteAsync(userId);
                    return Ok(new { message = "Your account has been deleted.", deleted = true });
                }

                // 2. Schedule the job to run once the grace period ends
                backgroundJobs.Schedule<AccountDeletionJob>(job => job.ExecuteAsync(userId), TimeSpan.FromHours(gracePeriodHours));

                return Accepted(new
                {
                    message = $"Your account deletion has been scheduled and will be completed in {gracePeriodHours} hours.",
                    deleted = false
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ClientErrorMessage.For(ex) });
            }
        }
    }
}
