using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Shared.Enums;
using GymCrest.Domain.Interface;

namespace GymCrest.Application.Modules.Gym.Services
{
    public class ResourceAccessService : IResourceAccessService
    {
        private readonly IResourceOwnershipRepository _ownershipRepository;
        private readonly IGymMemberRepository _gymMemberRepository;

        public ResourceAccessService(IResourceOwnershipRepository ownershipRepository, IGymMemberRepository gymMemberRepository)
        {
            _ownershipRepository = ownershipRepository;
            _gymMemberRepository = gymMemberRepository;
        }

        public async Task<bool> CanAccessAsync(OwnedResource resource, Guid id, ResourceCaller caller, bool write)
        {
            ResourceOwnership? owner = await _ownershipRepository.GetOwnershipAsync(resource, id);

            // Unknown ids are left to the action, which answers 404.
            if (owner == null)
            {
                return true;
            }

            return resource switch
            {
                OwnedResource.DailyRoutine => owner.OwnerUserId == caller.UserId,
                OwnedResource.WorkoutPlan or OwnedResource.DietPlan => await CanAccessPlanAsync(owner, caller, write),
                OwnedResource.GymPlan => await CanAccessGymPlanAsync(owner, caller),
                // A staff member may always see their own record.
                OwnedResource.Staff when !write && owner.OwnerUserId == caller.UserId => true,
                _ => WorksAtGym(owner, caller)
            };
        }

        /// <summary>Owners: a gym they own. Staff and trainers: the gym they belong to.</summary>
        private static bool WorksAtGym(ResourceOwnership owner, ResourceCaller caller) =>
            owner.GymId.HasValue
            && ((caller.IsGymOwner && owner.GymOwnerUserId == caller.UserId)
                || (caller.IsGymStaff && caller.GymId == owner.GymId));

        /// <summary>The owner who created the plan, or staff and trainers of a gym that owner runs.</summary>
        private async Task<bool> CanAccessGymPlanAsync(ResourceOwnership plan, ResourceCaller caller)
        {
            if (plan.GymOwnerUserId == caller.UserId)
            {
                return true;
            }

            return caller.IsGymStaff
                && caller.GymId.HasValue
                && await _ownershipRepository.GetGymOwnerUserIdAsync(caller.GymId.Value) == plan.GymOwnerUserId;
        }

        private async Task<bool> CanAccessPlanAsync(ResourceOwnership plan, ResourceCaller caller, bool write)
        {
            if (plan.CreatedBy == caller.UserId)
            {
                return true;
            }

            // Shared library templates: anyone may read them, only their creator may change them.
            if (plan.IsTemplate)
            {
                return !write;
            }

            if (!plan.GymId.HasValue)
            {
                return false;
            }

            if (WorksAtGym(plan, caller))
            {
                return true;
            }

            // Members may read their gym's shared (non-custom) plans.
            if (write || plan.IsCustom)
            {
                return false;
            }

            List<Domain.Entities.GymMember> memberships = await _gymMemberRepository.GetAllByUserIdAsync(caller.UserId);
            return memberships.Any(m => m.GymId == plan.GymId.Value);
        }
    }
}
