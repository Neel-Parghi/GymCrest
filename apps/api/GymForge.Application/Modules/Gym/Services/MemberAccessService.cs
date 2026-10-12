using GymForge.Application.Modules.Gym.Interfaces;
using GymForge.Domain.Interface;

namespace GymForge.Application.Modules.Gym.Services
{
    public class MemberAccessService : IMemberAccessService
    {
        private readonly IGymMemberRepository _gymMemberRepository;

        public MemberAccessService(IGymMemberRepository gymMemberRepository)
        {
            _gymMemberRepository = gymMemberRepository;
        }

        public async Task<bool> CanAccessMemberAsync(Guid memberId, Guid callerUserId, Guid? callerGymId, bool isGymOwner, bool isGymStaff)
        {
            // Standalone users store their data against their own account id.
            if (memberId == callerUserId)
            {
                return true;
            }

            (Guid GymId, Guid OwnerUserId, Guid? MemberUserId)? ownership = await _gymMemberRepository.GetGymOwnershipAsync(memberId);
            if (ownership == null)
            {
                return false;
            }

            if (ownership.Value.MemberUserId == callerUserId)
            {
                return true;
            }

            if (isGymOwner)
            {
                return ownership.Value.OwnerUserId == callerUserId;
            }

            return isGymStaff && callerGymId.HasValue && ownership.Value.GymId == callerGymId.Value;
        }
    }
}
