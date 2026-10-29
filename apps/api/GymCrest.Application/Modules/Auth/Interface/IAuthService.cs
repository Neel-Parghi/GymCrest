using GymCrest.Contracts.Auth;

namespace GymCrest.Application.Modules.Auth.Interface
{
    public interface IAuthService
    {
        Task<TokenResponseDto> RegisterSuperAdmin(RegisterRequestDto dto);
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto dto);

        Task<TokenResponseDto> VerifyOtpAsync(VerifyOtpRequestDto dto);
        Task<bool> ResendOtpAsync(ResendOtpRequestDto dto);

        Task<TokenResponseDto> Login(LoginRequestDto dto);

        Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task ForgotPasswordAsync(ForgotPasswordRequestDto dto);
        Task ResetPasswordAsync(ResetPasswordRequestDto dto);
        Task LogoutAsync(string refreshToken);
        Task<bool> RequestAccountDeletionAsync(Guid userId);
    }
}
