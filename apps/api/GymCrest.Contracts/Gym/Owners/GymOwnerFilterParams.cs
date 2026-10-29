using GymCrest.Contracts.Common;

namespace GymCrest.Contracts.Gym.Owners
{
    public class GymOwnerFilterParams : PaginationParams
    {
        public string? Status { get; set; }

        public string? InvitationStatus { get; set; }
    }
}
