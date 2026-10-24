using GymCrest.Domain.Entities;
using GymCrest.Shared.Enums;

namespace GymCrest.Application.Modules.Gym.Services
{
    public static class MemberFreezeCalculator
    {
        public static void Unfreeze(GymMember member, DateTime now)
        {
            int frozenDays = member.FreezeStartDate.HasValue
                ? Math.Max((now.Date - member.FreezeStartDate.Value.Date).Days, 0)
                : 0;

            if (frozenDays > 0)
            {
                MemberSubscription? activeSub = member.Subscriptions
                    .OrderByDescending(s => s.CreatedOn)
                    .FirstOrDefault(s => s.IsActive);

                if (activeSub != null)
                {
                    activeSub.EndDate = activeSub.EndDate.AddDays(frozenDays);
                }
            }

            member.Status = MemberStatus.Active;
            member.FreezeStartDate = null;
            member.FreezeUntil = null;
        }
    }
}
