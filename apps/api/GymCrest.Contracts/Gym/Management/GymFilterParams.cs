using GymCrest.Contracts.Common;

namespace GymCrest.Contracts.Gym.Management
{
    public class GymFilterParams : PaginationParams
    {
        public bool? IsActive { get; set; }
    }
}
