using System.ComponentModel.DataAnnotations;
namespace GymCrest.Contracts.Auth
{
    public class RefreshTokenRequestDto
    {
        [MaxLength(4000)]
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
