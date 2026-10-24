namespace GymCrest.Contracts.Auth
{
    public class RegisterResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public bool RequiresOtp { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
