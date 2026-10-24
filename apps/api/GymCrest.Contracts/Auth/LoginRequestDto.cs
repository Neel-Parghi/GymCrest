using System.ComponentModel.DataAnnotations;

namespace GymCrest.Contracts.Auth
{
    public class LoginRequestDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }
}
