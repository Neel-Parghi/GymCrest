using System.ComponentModel.DataAnnotations;
namespace GymCrest.Contracts.Staff
{
    public class StaffCheckInRequest
    {
        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}
