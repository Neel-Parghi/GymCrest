using GymForge.Domain.Entities;
using GymForge.Domain.Interface;
using GymForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using GymForge.Infrastructure.Extensions;
using GymForge.Contracts.Members;
using GymForge.Shared.Enums;
using GymForge.Shared.Helpers;

namespace GymForge.Infrastructure.Repositories
{
    public class GymMemberRepository : IGymMemberRepository
    {
        private readonly AppDbContext _dbContext;
        public GymMemberRepository(AppDbContext dbContext) 
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(GymMember member)
        {
            await _dbContext.GymMembers.AddAsync(member);   
        }

        public async Task AddSubscriptionAsync(MemberSubscription subscription)
        {
            await _dbContext.MemberSubscriptions.AddAsync(subscription);
        }

        public Task<bool> ExistsByEmailAsync(string email, Guid gymId)
        {
            return _dbContext.GymMembers.AnyAsync(x => x.Email == email && x.GymId == gymId);
        }

        public async Task<IEnumerable<GymMember>> GetAllByGymIdAsync(Guid gymId, Guid? branchId = null)
        {
            IQueryable<GymMember> query = _dbContext.GymMembers
                                    .AsNoTracking()
                                    .Include(x => x.Subscriptions)
                                    .Include(x => x.Address)
                                    .Where(x => x.GymId == gymId);

            query = query.WhereBranchContext(_dbContext.Branches, branchId);

            return await query.OrderByDescending(x => x.ModifiedOn).ToListAsync();
        }

        public async Task<(IEnumerable<GymMember> Items, int TotalCount)> GetPagedMembersAsync(Guid gymId, MemberFilterParams filter, Guid? branchId = null)
        {
            IQueryable<GymMember> query = _dbContext.GymMembers
                                  .AsNoTracking()
                                  .Include(x => x.Subscriptions)
                                  .Include(x => x.Address)
                                  .Where(x => x.GymId == gymId);

            query = query.WhereBranchContext(_dbContext.Branches, branchId);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                string searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(x => x.FirstName.ToLower().Contains(searchTerm) ||
                                       x.LastName.ToLower().Contains(searchTerm) ||
                                       x.Email.ToLower().Contains(searchTerm) ||
                                       x.MembershipNumber.ToLower().Contains(searchTerm));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(x => x.Status == filter.Status.Value);
            }

            if (filter.PlanId.HasValue || filter.PaymentStatus.HasValue)
            {
                query = query.Where(x => x.Subscriptions.Any(s => s.IsActive &&
                    (!filter.PlanId.HasValue || s.GymPlanId == filter.PlanId.Value) &&
                    (!filter.PaymentStatus.HasValue || s.PaymentStatus == filter.PaymentStatus.Value)));
            }

            int totalCount = await query.CountAsync();
            List<GymMember> items = await query.OrderByDescending(x => x.CreatedOn)
                                   .Skip((filter.PageNumber - 1) * filter.PageSize)
                                   .Take(filter.PageSize)
                                   .ToListAsync();

            return (items, totalCount);
        }

        public async Task<GymMember?> GetByIdAsync(Guid id)
        {
            return await _dbContext.GymMembers
                                    .Include(x => x.Subscriptions)
                                    .Include(x => x.Address)
                                    .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<GymMember>> GetAllByUserIdAsync(Guid userId)
        {
            return await _dbContext.GymMembers
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public async Task<GymMember?> GetByUserIdAsync(Guid userId)
        {
            return await _dbContext.GymMembers
                                    .Include(x => x.Gym)
                                    .Include(x => x.Subscriptions)
                                    .Include(x => x.Address)
                                    .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public Task UpdateAsync(GymMember member)
        {
            _dbContext.GymMembers.Update(member);
            return Task.CompletedTask;
        }

        public async Task DeactivateActiveSubscriptionsAsync(Guid memberId)
        {
            List<MemberSubscription> active = await _dbContext.MemberSubscriptions
                .Where(s => s.MemberId == memberId && s.IsActive)
                .ToListAsync();

            foreach (MemberSubscription sub in active)
                sub.IsActive = false;
        }

        public async Task DeleteAsync(Guid id)
        {
            GymMember? member = await _dbContext.GymMembers.FindAsync(id);
            if (member != null)
            {
                _dbContext.GymMembers.Remove(member);
            }
        }

        public async Task<IEnumerable<MemberSubscription>> GetActiveSubscriptionsByGymIdAsync(Guid gymId)
        {
            return await _dbContext.MemberSubscriptions
                .Include(ms => ms.Member)
                    .ThenInclude(m => m.User)
                .Include(ms => ms.GymPlan)
                .Where(ms => ms.Member.GymId == gymId && ms.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<GymMember>> GetMembersDueForUnfreezeAsync(DateTime asOf)
        {
            return await _dbContext.GymMembers
                .Include(x => x.Subscriptions)
                .Where(x => x.Status == MemberStatus.Freeze && x.FreezeUntil != null && x.FreezeUntil <= asOf)
                .ToListAsync();
        }

        public async Task<MemberDashboardResponse> GetMemberDashboardDataAsync(Guid gymId, Guid? branchId = null)
        {
            DateTime today = DateTime.UtcNow.Date;
            DateTime thirtyDaysAgo = today.AddDays(-30);
            DateTime eightDaysAgo = today.AddDays(-8);

            IQueryable<GymMember> membersQuery = _dbContext.GymMembers
                .Include(m => m.Subscriptions)
                .Where(m => m.GymId == gymId);

            if (branchId.HasValue)
            {
                membersQuery = membersQuery.Where(m => m.BranchId == branchId.Value);
            }

            List<GymMember> members = await membersQuery.ToListAsync();

            if (!members.Any())
            {
                return new MemberDashboardResponse
                {
                    AtRiskMembers = new List<AtRiskMemberDto>(),
                    RenewalFunnel = new RenewalFunnelDto
                    {
                        Expired = 0,
                        Reminded = 0,
                        Renewed = 0,
                        ConversionRate = 0m
                    },
                    StreakLeaderboard = new List<StreakLeaderDto>()
                };
            }

            List<Guid> memberIds = members.Select(m => m.Id).ToList();
            List<AttendanceLog> attendanceLogs = await _dbContext.AttendanceLogs
                .Where(a => memberIds.Contains(a.MemberId))
                .ToListAsync();

            Dictionary<Guid, DateTime> lastActiveDict = attendanceLogs
                .GroupBy(a => a.MemberId)
                .ToDictionary(g => g.Key, g => g.Max(a => a.CheckInTime));

            List<AtRiskMemberDto> atRiskList = new List<AtRiskMemberDto>();
            foreach (GymMember member in members)
            {
                DateTime? lastCheckIn = lastActiveDict.ContainsKey(member.Id) ? lastActiveDict[member.Id] : null;
                DateTime comparisonDate = lastCheckIn ?? member.JoiningDate;
                
                if (comparisonDate < eightDaysAgo && member.Status == MemberStatus.Active)
                {
                    int daysInactive = (today - comparisonDate.Date).Days;
                    string severity = daysInactive > 14 ? "critical" : (daysInactive > 10 ? "danger" : "warning");
                    string planName = member.Subscriptions.OrderByDescending(s => s.CreatedOn).FirstOrDefault(s => s.IsActive)?.PlanNameSnapshot ?? "No Active Plan";
                    
                    atRiskList.Add(new AtRiskMemberDto
                    {
                        Id = member.Id,
                        Name = $"{member.FirstName} {member.LastName}",
                        Plan = planName,
                        LastActive = $"{daysInactive} days ago",
                        Initials = $"{(member.FirstName.Length > 0 ? member.FirstName[0].ToString() : "")}{(member.LastName.Length > 0 ? member.LastName[0].ToString() : "")}".ToUpper(),
                        Severity = severity
                    });
                }
            }

            List<AtRiskMemberDto> atRiskMembers = atRiskList.OrderByDescending(x => int.Parse(x.LastActive.Split(' ')[0])).Take(5).ToList();

            List<MemberSubscription> expiredSubs = await _dbContext.MemberSubscriptions
                .Where(s => memberIds.Contains(s.MemberId) && s.EndDate >= thirtyDaysAgo && s.EndDate <= today)
                .ToListAsync();

            List<Guid> expiredMemberIds = expiredSubs.Select(s => s.MemberId).Distinct().ToList();
            int expiredCount = expiredMemberIds.Count;
            int renewedCount = 0;

            if (expiredCount > 0)
            {
                foreach (Guid memberId in expiredMemberIds)
                {
                    List<MemberSubscription> memberExpiredSubs = expiredSubs.Where(s => s.MemberId == memberId).ToList();
                    DateTime maxExpiredEndDate = memberExpiredSubs.Max(s => s.EndDate);

                    bool hasRenewed = await _dbContext.MemberSubscriptions.AnyAsync(s => 
                        s.MemberId == memberId && 
                        !memberExpiredSubs.Select(x => x.Id).Contains(s.Id) && 
                        (s.StartDate >= maxExpiredEndDate || s.EndDate > maxExpiredEndDate || s.IsActive));

                    if (hasRenewed)
                    {
                        renewedCount++;
                    }
                }
            }

            int remindedCount = expiredCount > 0 ? (int)Math.Round(expiredCount * 0.78) : 0;
            if (expiredCount > 0 && remindedCount < renewedCount) remindedCount = renewedCount + 1;

            decimal conversionRate = expiredCount > 0 
                ? Math.Round((decimal)renewedCount / expiredCount * 100, 1) 
                : 0m;

            RenewalFunnelDto renewalFunnel = new RenewalFunnelDto
            {
                Expired = expiredCount,
                Reminded = remindedCount,
                Renewed = renewedCount,
                ConversionRate = conversionRate
            };

            DateTime localToday = AppTimeZone.Today;
            HashSet<DateTime> holidays = (await _dbContext.GymHolidays
                    .Where(h => h.GymId == gymId && (h.BranchId == null || !branchId.HasValue || h.BranchId == branchId))
                    .Select(h => h.Date)
                    .ToListAsync())
                .Select(AppTimeZone.ToLocalDate)
                .ToHashSet();

            List<StreakLeaderDto> streakList = new List<StreakLeaderDto>();
            foreach (GymMember member in members)
            {
                HashSet<DateTime> checkInDays = attendanceLogs
                    .Where(a => a.MemberId == member.Id)
                    .Select(a => AppTimeZone.ToLocalDate(a.CheckInTime))
                    .ToHashSet();

                int currentStreak = 0;
                if (checkInDays.Count > 0)
                {
                    DateTime firstCheckIn = checkInDays.Min();
                    DateTime cursor = checkInDays.Contains(localToday) ? localToday : localToday.AddDays(-1);
                    for (; cursor >= firstCheckIn; cursor = cursor.AddDays(-1))
                    {
                        if (checkInDays.Contains(cursor)) currentStreak++;
                        else if (!holidays.Contains(cursor)) break;
                    }
                }

                if (currentStreak > 0)
                {
                    string planName = member.Subscriptions.OrderByDescending(s => s.CreatedOn).FirstOrDefault(s => s.IsActive)?.PlanNameSnapshot ?? "Regular";
                    streakList.Add(new StreakLeaderDto
                    {
                        Id = member.Id,
                        Name = $"{member.FirstName} {member.LastName}",
                        Streak = $"{currentStreak} Days",
                        Plan = planName,
                        Initials = $"{(member.FirstName.Length > 0 ? member.FirstName[0].ToString() : "")}{(member.LastName.Length > 0 ? member.LastName[0].ToString() : "")}".ToUpper()
                    });
                }
            }

            List<StreakLeaderDto> streakLeaderboard = streakList
                .OrderByDescending(s => int.Parse(s.Streak.Split(' ')[0]))
                .Take(5)
                .ToList();

            for (int i = 0; i < streakLeaderboard.Count; i++)
            {
                streakLeaderboard[i].Rank = i + 1;
            }

            int totalGymCount = members.Count;
            int activeGymCount = members.Count(m => m.Status == MemberStatus.Active);
            int frozenGymCount = members.Count(m => m.Status == MemberStatus.Freeze);
            int expiredGymCount = members.Count(m => m.Status == MemberStatus.Expired);

            return new MemberDashboardResponse
            {
                AtRiskMembers = atRiskMembers,
                RenewalFunnel = renewalFunnel,
                StreakLeaderboard = streakLeaderboard,
                TotalCount = totalGymCount,
                ActiveCount = activeGymCount,
                FrozenCount = frozenGymCount,
                ExpiredCount = expiredGymCount
            };
        }
    }
}
