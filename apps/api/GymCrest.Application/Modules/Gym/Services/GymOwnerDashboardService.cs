using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Gym.Dashboard;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Shared.Enums;

namespace GymCrest.Application.Modules.Gym.Services
{
    public class GymOwnerDashboardService : IGymOwnerDashboardService
    {
        private readonly IGymMemberRepository _memberRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly IGymManagementRepository _gymManagementRepository;
        private readonly IAttendanceRepository _attendanceRepository;

        public GymOwnerDashboardService(
            IGymMemberRepository memberRepository,
            IInventoryRepository inventoryRepository,
            IEquipmentRepository equipmentRepository,
            IStaffRepository staffRepository,
            IGymManagementRepository gymManagementRepository,
            IAttendanceRepository attendanceRepository)
        {
            _memberRepository = memberRepository;
            _inventoryRepository = inventoryRepository;
            _equipmentRepository = equipmentRepository;
            _staffRepository = staffRepository;
            _gymManagementRepository = gymManagementRepository;
            _attendanceRepository = attendanceRepository;
        }

        public async Task<GymOwnerDashboardDto> GetGymOwnerDashboardStatsAsync(Guid gymId, Guid? branchId = null)
        {
            IEnumerable<GymMember>? members = await _memberRepository.GetAllByGymIdAsync(gymId);
            List<InventoryItem>? products = await _inventoryRepository.GetProductsByGymIdAsync(gymId);
            List<Equipment>? equipment = await _equipmentRepository.GetEquipmentByGymIdAsync(gymId, branchId);
            IEnumerable<Staff>? staff = await _staffRepository.GetAllByGymIdAsync(gymId);
            List<SaleTransaction> sales = await _inventoryRepository.GetSalesByGymIdAsync(gymId);

            Domain.Entities.Gym? gym = await _gymManagementRepository.GetGymByIdAsync(gymId);
            int triggerDays = gym?.Configuration?.PlanExpirationTriggerDays ?? 14;

            List<Branch>? branches = await _gymManagementRepository.GetBranchesByGymIdAsync(gymId);
            int branchCount = branches?.Count ?? 0;

            if (branchId.HasValue)
            {
                Guid? mainBranchId = null;
                Branch? mainBranch = branches?.FirstOrDefault(b => b.IsMainBranch);
                if (mainBranch != null)
                {
                    mainBranchId = mainBranch.Id;
                }

                bool isMain = branchId.Value == mainBranchId;

                members = members.Where(m => m.BranchId == branchId.Value || (isMain && m.BranchId == null));
                products = products.Where(p => p.BranchId == branchId.Value || (isMain && p.BranchId == null)).ToList();
                staff = staff.Where(s => s.BranchId == branchId.Value || (isMain && s.BranchId == null));
                sales = sales.Where(s => s.BranchId == branchId.Value || 
                                         (s.InventoryItem != null && s.InventoryItem.BranchId == branchId.Value) || 
                                         (isMain && s.BranchId == null)).ToList();
            }

            DateTime now = DateTime.UtcNow;
            DateTime firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

            List<MemberSubscription> subscriptions = members.SelectMany(m => m.Subscriptions).ToList();
            
            int totalMembersNow = members.Count();
            int totalMembersLastMonth = members.Count(m => m.CreatedOn < firstDayOfMonth);
            double growth = 0;
            if (totalMembersLastMonth > 0)
            {
                growth = ((double)(totalMembersNow - totalMembersLastMonth) / totalMembersLastMonth) * 100;
            }
            else if (totalMembersNow > 0)
            {
                growth = 100;
            }

            decimal membershipRevenue = subscriptions
                .Where(s => s.CreatedOn >= firstDayOfMonth && s.PaymentStatus == PaymentStatus.Paid)
                .Sum(s => s.PricePaid);

            if (membershipRevenue == 0)
            {
                membershipRevenue = subscriptions
                    .Where(s => s.IsActive && s.PaymentStatus == PaymentStatus.Paid)
                    .Sum(s => s.PricePaid / (s.DurationMonths > 0 ? s.DurationMonths : 1));
            }

            decimal productSalesRevenue = sales
                .Where(s => s.TransactionDate >= firstDayOfMonth)
                .Sum(s => s.TotalAmount);

            decimal monthlyRevenue = membershipRevenue + productSalesRevenue;

            decimal prevMembershipRevenue = subscriptions
                .Where(s => s.CreatedOn >= firstDayOfMonth.AddMonths(-1) && s.CreatedOn < firstDayOfMonth && s.PaymentStatus == PaymentStatus.Paid)
                .Sum(s => s.PricePaid);
            
            double revenueTrendPercentage = 0;
            if (prevMembershipRevenue > 0)
            {
                revenueTrendPercentage = (double)Math.Round(((monthlyRevenue - prevMembershipRevenue) / prevMembershipRevenue) * 100, 1);
            }
            else if (monthlyRevenue > 0)
            {
                revenueTrendPercentage = 100;
            }

            List<RecentEnrollmentDto> recentEnrollments = members
                .OrderByDescending(m => m.CreatedOn)
                .Take(5)
                .Select(m => new RecentEnrollmentDto
                {
                    MemberName = $"{m.FirstName} {m.LastName}",
                    Email = m.Email,
                    EnrollmentDate = m.CreatedOn,
                    Status = m.Status.ToString(),
                    PlanName = m.Subscriptions.OrderByDescending(s => s.CreatedOn).Select(s => s.PlanNameSnapshot).FirstOrDefault() ?? "No Plan",
                    Initials = (m.FirstName.Length > 0 ? m.FirstName[0].ToString() : "") + (m.LastName.Length > 0 ? m.LastName[0].ToString() : "")
                })
                .ToList();

            List<UpcomingRenewalDto> upcomingRenewals = members
                .Where(m => m.Subscriptions.Any(s => s.IsActive && s.EndDate > now))
                .Select(m => new { 
                    Member = m, 
                    Sub = m.Subscriptions.Where(s => s.IsActive && s.EndDate > now).OrderBy(s => s.EndDate).First() 
                })
                .Where(x => x.Sub.EndDate <= now.AddDays(triggerDays))
                .OrderBy(x => x.Sub.EndDate)
                .Take(5)
                .Select(x => new UpcomingRenewalDto
                {
                    MemberName = $"{x.Member.FirstName} {x.Member.LastName}",
                    EndDate = x.Sub.EndDate,
                    DaysRemaining = (x.Sub.EndDate - now).Days
                })
                .ToList();

             (IEnumerable<AttendanceLog> allRecentLogs, int totalcount) = await _attendanceRepository.GetLogsPagedAsync(gymId, branchId, null, null, null, 1, 1000);
            DateTime localToday = DateTime.UtcNow.AddHours(5.5).Date;
            List<AttendanceLog> todayLogs = allRecentLogs
                .Where(x => x.CheckInTime.AddHours(5.5).Date == localToday)
                .ToList();
            int todayAttendanceCount = todayLogs.Count;

            List<HourlyOccupancyDto> hourlyData = [];
            string[] hoursList = new[] { "6 AM", "8 AM", "10 AM", "12 PM", "2 PM", "4 PM", "6 PM", "8 PM", "10 PM" };
            Dictionary<string, int> hourMap = new()
            {
                { "6 AM", 0 }, { "8 AM", 0 }, { "10 AM", 0 }, { "12 PM", 0 },
                { "2 PM", 0 }, { "4 PM", 0 }, { "6 PM", 0 }, { "8 PM", 0 }, { "10 PM", 0 }
            };

            foreach (AttendanceLog log in todayLogs)
            {
                int hr = log.CheckInTime.ToLocalTime().Hour;
                string key;
                if (hr < 8) key = "6 AM";
                else if (hr < 10) key = "8 AM";
                else if (hr < 12) key = "10 AM";
                else if (hr < 14) key = "12 PM";
                else if (hr < 16) key = "2 PM";
                else if (hr < 18) key = "4 PM";
                else if (hr < 20) key = "6 PM";
                else if (hr < 22) key = "8 PM";
                else key = "10 PM";

                if (hourMap.ContainsKey(key))
                {
                    hourMap[key]++;
                }
            }

            foreach (string hr in hoursList)
            {
                hourlyData.Add(new HourlyOccupancyDto
                {
                    Hour = hr,
                    OccupancyCount = hourMap[hr]
                });
            }

            string[] daysList = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
            Dictionary<string, int> dayMap = new()
            {
                { "Mon", 0 }, { "Tue", 0 }, { "Wed", 0 }, { "Thu", 0 }, { "Fri", 0 }, { "Sat", 0 }, { "Sun", 0 }
            };

            DateTime thirtyDaysAgo = localToday.AddDays(-30);
            List<AttendanceLog> recentMonthLogs = allRecentLogs
                .Where(x => x.CheckInTime.AddHours(5.5).Date >= thirtyDaysAgo)
                .ToList();

            foreach (AttendanceLog log in recentMonthLogs)
            {
                string dayKey = log.CheckInTime.AddHours(5.5).ToString("ddd");
                if (dayMap.ContainsKey(dayKey))
                {
                    dayMap[dayKey]++;
                }
            }

            List<WeeklyOccupancyDto> weeklyData = [];
            foreach (string d in daysList)
            {
                double rawAverage = dayMap[d] / 4.2;
                int avgCount = (int)Math.Max(Math.Round(rawAverage), 0);

                if (recentMonthLogs.Count <= 10)
                {
                    avgCount = dayMap[d];
                }

                weeklyData.Add(new WeeklyOccupancyDto
                {
                    Day = d,
                    OccupancyCount = avgCount
                });
            }

            List<MemberSubscription?> activeSubs = members
                .Where(m => m.Status == MemberStatus.Active)
                .Select(m => m.Subscriptions.OrderByDescending(s => s.CreatedOn).FirstOrDefault())
                .Where(s => s != null && s.IsActive)
                .ToList();

            List<(string PlanName, int Count)> planGroups = activeSubs
                .GroupBy(s => s!.PlanNameSnapshot)
                .Select(g => (PlanName: g.Key, Count: g.Count()))
                .OrderByDescending(x => x.Count)
                .ToList();

            List<MembershipDistributionDto> distributionData = [];
            int totalActive = members.Count(m => m.Status == MemberStatus.Active);

            if (planGroups.Count > 0)
            {
                string[] colors = new[] { "#0b2545", "#7a9acb", "#d4e1fa", "#64748b" };
                for (int i = 0; i < planGroups.Count; i++)
                {
                    (string PlanName, int Count) group = planGroups[i];
                    double pct = totalActive > 0 ? Math.Round((double)group.Count / totalActive * 100) : 0;
                    distributionData.Add(new MembershipDistributionDto
                    {
                        TierName = group.PlanName,
                        Count = group.Count,
                        Percentage = pct,
                        Color = colors[Math.Min(i, colors.Length - 1)]
                    });
                }
            }

            List<string> todayCheckedInInitials = todayLogs
                .Where(log => log.Member != null)
                .Select(log => log.Member!)
                .Select(m => m.FirstName.Length > 0 && m.LastName.Length > 0 ? $"{m.FirstName[0]}{m.LastName[0]}".ToUpper() : "??")
                .Distinct()
                .Take(3)
                .ToList();

            return new GymOwnerDashboardDto
            {
                TotalMembers = totalMembersNow,
                MemberGrowthPercentage = Math.Round(growth, 1),
                ActiveMembers = members.Count(m => m.Status == MemberStatus.Active),
                FrozenMembers = members.Count(m => m.Status == MemberStatus.Freeze),
                TodayAttendance = todayAttendanceCount,
                MonthlyRevenue = monthlyRevenue,
                MembershipRevenue = membershipRevenue,
                ProductSalesRevenue = productSalesRevenue,
                LowStockItems = products.Count(p => p.StockQuantity <= p.ReorderLevel),
                TotalProductsCount = products.Count(),
                BranchesCount = branchCount,
                ActiveTrainers = staff.Count(s => s.Role == StaffRole.Trainer && s.IsActive),
                SupportStaffCount = staff.Count(s => s.Role != StaffRole.Trainer && s.IsActive),
                RecentEnrollments = recentEnrollments,
                UpcomingRenewals = upcomingRenewals,
                HourlyOccupancy = hourlyData,
                WeeklyOccupancy = weeklyData,
                MembershipDistribution = distributionData,
                TodayCheckedInInitials = todayCheckedInInitials,
                RevenueTrendPercentage = revenueTrendPercentage
            };
        }
    }
}
