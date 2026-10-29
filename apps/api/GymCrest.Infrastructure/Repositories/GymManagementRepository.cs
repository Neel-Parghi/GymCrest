using GymCrest.Contracts.Gym.Management;
using GymCrest.Contracts.Gym.Owners;
using GymCrest.Contracts.Gym.Shared;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using GymCrest.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GymCrest.Infrastructure.Repositories
{
    public class GymManagementRepository : IGymManagementRepository
    {
        private readonly AppDbContext _dbContext;

        public GymManagementRepository(AppDbContext dbContext) 
        {
            _dbContext = dbContext;
        }

        public async Task AddGymAsync(Gym gym)
        {
            await _dbContext.Gyms.AddAsync(gym);
        }

        public async Task AddBranchAsync(Branch branch)
        {
            await _dbContext.Branches.AddAsync(branch);
        }

        public async Task AddGymSubscriptionAsync(SubscriptionRecord subscription)
        {
            await _dbContext.SubscriptionRecords.AddAsync(subscription);
        }

        public async Task<(List<GymOwnersDto> Items, int TotalCount)> GetGymOwnersList(int pageNumber, int pageSize, string? searchTerm)
        {
            IQueryable<User> query = _dbContext.Users.Where(x => x.Role == UserRole.GymOwner);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u => u.FirstName.ToLower().Contains(searchTerm) ||
                                       u.LastName.ToLower().Contains(searchTerm) ||
                                       u.Email.ToLower().Contains(searchTerm) ||
                                       (u.Profile != null && u.Profile.Phone.ToLower().Contains(searchTerm)) ||
                                       (u.Gyms != null && u.Gyms.Any(g => g.GymName.ToLower().Contains(searchTerm))));
            }

            int totalCount = await query.CountAsync();

            List<GymOwnersDto> items = await query.Select(u => new GymOwnersDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Name = u.FirstName + " " + u.LastName,
                Email = u.Email,
                Phone = u.Profile != null ? u.Profile.Phone : string.Empty,
                GymsOwned = u.Gyms != null ? u.Gyms.Count : 0,
                Location = u.Profile != null && u.Profile.Address != null
                    ? u.Profile.Address.City
                    : u.Gyms!.OrderBy(g => g.CreatedOn).Select(g => g.Address != null ? g.Address.City : string.Empty).FirstOrDefault() ?? string.Empty,
                JoinedDate = u.CreatedOn,
                Status = u.IsActive ? "Active" : "Inactive",
                InvitationStatus = (u.Security != null && u.Security.IsInvitationAccepted) ? "Accepted" :
                                     ((u.Security != null && u.Security.InvitationExpiry > DateTime.UtcNow) ? "Pending" : "Expired"),
                IsOnboarded = u.Profile != null && u.Profile.IsOnboarded,
                Gyms = u.Gyms!
                    .OrderBy(g => g.CreatedOn)
                    .Select(g => new OwnedGymDto
                    {
                        Id = g.Id,
                        GymName = g.GymName,
                        City = g.Address != null ? g.Address.City : null,
                        IsActive = g.IsActive,
                        IsVerified = g.IsVerified,
                        BranchesCount = g.Branches != null ? g.Branches.Count : 0,
                        CreatedOn = g.CreatedOn
                    })
                    .ToList()
            })
            .OrderByDescending(u => u.JoinedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

            return (items, totalCount);
        }

        public async Task<User?> GetGymOwnerByIdAsync(Guid id)
        {
            User? user = await _dbContext.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(x => x.Id == id);
            return user;
        }

        public async Task<(List<GymListResponseDto> Items, int TotalCount)> GetGymListAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            IQueryable<Gym> query = _dbContext.Gyms.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(g => g.GymName.ToLower().Contains(searchTerm) ||
                                       (g.BrandName != null && g.BrandName.ToLower().Contains(searchTerm)) ||
                                       (g.Email != null && g.Email.ToLower().Contains(searchTerm)) ||
                                       (g.Owner.FirstName + " " + g.Owner.LastName).ToLower().Contains(searchTerm) ||
                                       g.Owner.Email.ToLower().Contains(searchTerm));
            }

            int totalCount = await query.CountAsync();

            List<GymListResponseDto> items = await query
                .Include(x => x.Owner)
                .Include(x => x.Branches)
                .Select(g => new GymListResponseDto
                {
                    Id = g.Id,
                    GymName = g.GymName,
                    BrandName = g.BrandName,
                    OwnerName = g.Owner.FirstName + " " + g.Owner.LastName,
                    OwnerId = g.OwnerUserId,
                    OwnerEmail = g.Owner.Email,
                    OwnerPhone = g.Owner.Profile != null ? g.Owner.Profile.Phone : null,
                    OwnerIsActive = g.Owner.IsActive,
                    MembersCount = _dbContext.GymMembers.Count(m => m.GymId == g.Id),
                    StaffCount = _dbContext.Staff.Count(s => s.GymId == g.Id),
                    Address = g.Address == null ? null : new AddressDto
                    {
                        Line1 = g.Address.Address1,
                        Line2 = g.Address.Address2,
                        City = g.Address.City,
                        State = g.Address.State,
                        Country = g.Address.Country,
                        PostalCode = g.Address.PostalCode
                    },
                    Email = g.Email,
                    Phone = g.Phone,
                    IsActive = g.IsActive,
                    IsVerified = g.IsVerified,
                    BranchesCount = g.Branches != null ? g.Branches.Count : 0,
                    Description = g.Description,
                    WebsiteUrl = g.WebsiteUrl,
                    GstNumber = g.GstNumber,
                    RegistrationNumber = g.RegistrationNumber,
                    EstablishedDate = g.EstablishedDate,
                    LogoUrl = g.LogoUrl,
                    BannerUrl = g.BannerUrl,
                    InvoicePrefix = g.InvoicePrefix,
                    DefaultTaxRate = g.DefaultTaxRate,
                    OverdueGraceDays = g.OverdueGraceDays,
                    AutoEmailReceipts = g.AutoEmailReceipts,
                    CreatedOn = g.CreatedOn,
                    ModifiedOn = (DateTime)g.ModifiedOn!,
                    // Get latest active subscription
                    PlanName = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => s.Plan.Name)
                        .FirstOrDefault(),
                    SubscriptionExpiry = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => (DateTime?)s.EndDate)
                        .FirstOrDefault(),
                    SubscriptionStart = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => (DateTime?)s.StartDate)
                        .FirstOrDefault(),
                    IsTrialPlan = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => s.IsTrial)
                        .FirstOrDefault(),
                    PaymentStatus = _dbContext.SaaSPaymentTransactions
                        .Any(t => t.GymId == g.Id && t.Status == "Paid") ? "Paid" :
                        (_dbContext.SubscriptionRecords.Any(s => s.GymId == g.Id) ? "Pending" : "Unpaid")
                })
                .OrderByDescending(g => g.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public User UpdateGymOwner(User gymOwner)
        {
            _dbContext.Users.Update(gymOwner);
            return gymOwner;
        }

        public async Task<Gym?> GetGymByIdAsync(Guid id)
        {
            return await _dbContext.Gyms
                .Include(g => g.Configuration)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public Gym UpdateGym(Gym gym)
        {
            _dbContext.Gyms.Update(gym);
            return gym;
        }

        public async Task<bool> GymHasUsersAsync(Guid gymId, Guid ownerUserId)
        {
            return await _dbContext.GymMembers.AnyAsync(m => m.GymId == gymId)
                || await _dbContext.Staff.AnyAsync(s => s.GymId == gymId)
                || await _dbContext.Users.AnyAsync(u => u.GymId == gymId && u.Id != ownerUserId);
        }

        public async Task<bool> OwnerHasGymsAsync(Guid ownerId)
        {
            return await _dbContext.Gyms.AnyAsync(g => g.OwnerUserId == ownerId);
        }

        public async Task DeleteGymOwnerAsync(User owner)
        {
            IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();

                await _dbContext.GymMembers
                    .Where(m => m.UserId == owner.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.UserId, (Guid?)null));
                await _dbContext.Staff
                    .Where(s => s.UserId == owner.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(st => st.UserId, (Guid?)null));

                await _dbContext.GymPlans.Where(p => p.GymOwnerId == owner.Id).ExecuteDeleteAsync();
                await _dbContext.WorkoutPlans.IgnoreQueryFilters().Where(p => p.CreatedBy == owner.Id && p.IsCustom).ExecuteDeleteAsync();
                await _dbContext.DietPlans.IgnoreQueryFilters().Where(p => p.CreatedBy == owner.Id && p.IsCustom).ExecuteDeleteAsync();

                Guid? profileAddressId = owner.Profile?.AddressId;
                await _dbContext.Users.Where(u => u.Id == owner.Id).ExecuteDeleteAsync();
                if (profileAddressId.HasValue)
                {
                    await _dbContext.Addresses.Where(a => a.Id == profileAddressId.Value).ExecuteDeleteAsync();
                }

                await transaction.CommitAsync();
            });
        }

        public async Task DeleteGymAsync(Guid gymId)
        {
            IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();

                Gym? gym = await _dbContext.Gyms.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gymId);
                if (gym == null) return;

                List<Guid> addressIds = await _dbContext.Branches
                    .Where(b => b.GymId == gymId)
                    .Select(b => b.AddressId)
                    .ToListAsync();
                if (gym.AddressId.HasValue) addressIds.Add(gym.AddressId.Value);

                await _dbContext.UserProfiles
                    .Where(p => p.UserId == gym.OwnerUserId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.IsOnboarded, false)
                        .SetProperty(p => p.CurrentOnboardingStep, 0));
                await _dbContext.Users
                    .Where(u => u.GymId == gymId)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.GymId, (Guid?)null));
                await _dbContext.Feedbacks
                    .Where(f => f.GymId == gymId)
                    .ExecuteUpdateAsync(s => s.SetProperty(f => f.GymId, (Guid?)null));

                await _dbContext.WorkoutPlans.IgnoreQueryFilters().Where(p => p.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.DietPlans.IgnoreQueryFilters().Where(p => p.GymId == gymId).ExecuteDeleteAsync();

                await _dbContext.SaaSPaymentTransactions.Where(t => t.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.SubscriptionRecords.Where(s => s.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.AnnouncementTemplates.Where(t => t.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.GymAnnouncements.Where(a => a.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.UserNotifications.Where(n => n.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.GymHolidays.Where(h => h.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.MaintenanceLogs.Where(l => l.Equipment.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.Equipment.Where(e => e.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.InventoryItems.Where(i => i.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.StaffPayrollRules.Where(r => r.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.StaffPayoutLogs.Where(l => l.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.StaffAttendanceLogs.Where(l => l.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.GymConfigurations.Where(c => c.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.Branches.Where(b => b.GymId == gymId).ExecuteDeleteAsync();
                await _dbContext.Gyms.Where(g => g.Id == gymId).ExecuteDeleteAsync();
                await _dbContext.Addresses.Where(a => addressIds.Contains(a.Id)).ExecuteDeleteAsync();

                await transaction.CommitAsync();
            });
        }

        public async Task<List<Branch>> GetBranchesByGymIdAsync(Guid gymId)
        {
            return await _dbContext.Branches
                .Include(b => b.Address)
                .Where(b => b.GymId == gymId)
                .OrderByDescending(b => b.CreatedOn)
                .ToListAsync();
        }

        public async Task<Branch?> GetBranchByIdAsync(Guid id)
        {
            return await _dbContext.Branches
                .Include(b => b.Address)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public Branch UpdateBranch(Branch branch)
        {
            _dbContext.Branches.Update(branch);
            return branch;
        }

        public async Task<GymListResponseDto?> GetGymByOwnerIdAsync(Guid ownerId)
        {
            return await _dbContext.Gyms
                .Where(g => g.OwnerUserId == ownerId)
                .Include(x => x.Owner)
                .Include(x => x.Branches)
                .Select(g => new GymListResponseDto
                {
                    Id = g.Id,
                    GymName = g.GymName,
                    BrandName = g.BrandName,
                    OwnerName = g.Owner.FirstName + " " + g.Owner.LastName,
                    Email = g.Email,
                    Phone = g.Phone,
                    IsActive = g.IsActive,
                    IsVerified = g.IsVerified,
                    BranchesCount = g.Branches != null ? g.Branches.Count : 0,
                    Description = g.Description,
                    WebsiteUrl = g.WebsiteUrl,
                    GstNumber = g.GstNumber,
                    RegistrationNumber = g.RegistrationNumber,
                    EstablishedDate = g.EstablishedDate,
                    LogoUrl = g.LogoUrl,
                    BannerUrl = g.BannerUrl,
                    InvoicePrefix = g.InvoicePrefix,
                    DefaultTaxRate = g.DefaultTaxRate,
                    OverdueGraceDays = g.OverdueGraceDays,
                    AutoEmailReceipts = g.AutoEmailReceipts,
                    CreatedOn = g.CreatedOn,
                    ModifiedOn = (DateTime)g.ModifiedOn!,
                    PlanName = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => s.Plan.Name)
                        .FirstOrDefault(),
                    SubscriptionExpiry = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => (DateTime?)s.EndDate)
                        .FirstOrDefault(),
                    IsTrialPlan = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => s.IsTrial)
                        .FirstOrDefault(),
                    PaymentStatus = _dbContext.SaaSPaymentTransactions
                        .Any(t => t.GymId == g.Id && t.Status == "Paid") ? "Paid" :
                        (_dbContext.SubscriptionRecords.Any(s => s.GymId == g.Id) ? "Pending" : "Unpaid"),
                    AwaitingFirstPayment = !_dbContext.SaaSPaymentTransactions.Any(t => t.GymId == g.Id && t.Status == "Paid")
                        && !_dbContext.SubscriptionRecords.Any(s => s.GymId == g.Id && (s.IsActive || s.IsTrial))
                })
                .FirstOrDefaultAsync();
        }

        public async Task<GymListResponseDto?> GetGymDetailsResponseByIdAsync(Guid gymId)
        {
            return await _dbContext.Gyms
                .Where(g => g.Id == gymId)
                .Include(x => x.Owner)
                .Include(x => x.Branches)
                .Select(g => new GymListResponseDto
                {
                    Id = g.Id,
                    GymName = g.GymName,
                    BrandName = g.BrandName,
                    OwnerName = g.Owner.FirstName + " " + g.Owner.LastName,
                    Email = g.Email,
                    Phone = g.Phone,
                    IsActive = g.IsActive,
                    IsVerified = g.IsVerified,
                    BranchesCount = g.Branches != null ? g.Branches.Count : 0,
                    Description = g.Description,
                    WebsiteUrl = g.WebsiteUrl,
                    GstNumber = g.GstNumber,
                    RegistrationNumber = g.RegistrationNumber,
                    EstablishedDate = g.EstablishedDate,
                    LogoUrl = g.LogoUrl,
                    BannerUrl = g.BannerUrl,
                    InvoicePrefix = g.InvoicePrefix,
                    DefaultTaxRate = g.DefaultTaxRate,
                    OverdueGraceDays = g.OverdueGraceDays,
                    AutoEmailReceipts = g.AutoEmailReceipts,
                    CreatedOn = g.CreatedOn,
                    ModifiedOn = (DateTime)g.ModifiedOn!,
                    PlanName = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => s.Plan.Name)
                        .FirstOrDefault(),
                    SubscriptionExpiry = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => (DateTime?)s.EndDate)
                        .FirstOrDefault(),
                    IsTrialPlan = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => s.IsTrial)
                        .FirstOrDefault(),
                    PaymentStatus = _dbContext.SaaSPaymentTransactions
                        .Any(t => t.GymId == g.Id && t.Status == "Paid") ? "Paid" :
                        (_dbContext.SubscriptionRecords.Any(s => s.GymId == g.Id) ? "Pending" : "Unpaid")
                })
                .FirstOrDefaultAsync();
        }
        public async Task<List<GymListResponseDto>> GetAllGymsAsync()
        {
            return await _dbContext.Gyms
                .Include(x => x.Owner)
                .Include(x => x.Branches)
                .Select(g => new GymListResponseDto
                {
                    Id = g.Id,
                    GymName = g.GymName,
                    BrandName = g.BrandName,
                    OwnerName = g.Owner.FirstName + " " + g.Owner.LastName,
                    Email = g.Email,
                    Phone = g.Phone,
                    IsActive = g.IsActive,
                    IsVerified = g.IsVerified,
                    BranchesCount = g.Branches != null ? g.Branches.Count : 0,
                    Description = g.Description,
                    WebsiteUrl = g.WebsiteUrl,
                    GstNumber = g.GstNumber,
                    RegistrationNumber = g.RegistrationNumber,
                    EstablishedDate = g.EstablishedDate,
                    LogoUrl = g.LogoUrl,
                    BannerUrl = g.BannerUrl,
                    InvoicePrefix = g.InvoicePrefix,
                    DefaultTaxRate = g.DefaultTaxRate,
                    OverdueGraceDays = g.OverdueGraceDays,
                    AutoEmailReceipts = g.AutoEmailReceipts,
                    CreatedOn = g.CreatedOn,
                    ModifiedOn = (DateTime)g.ModifiedOn!,
                    PlanName = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => s.Plan.Name)
                        .FirstOrDefault(),
                    SubscriptionExpiry = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => (DateTime?)s.EndDate)
                        .FirstOrDefault(),
                    IsTrialPlan = _dbContext.SubscriptionRecords
                        .Where(s => s.GymId == g.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedOn)
                        .Select(s => s.IsTrial)
                        .FirstOrDefault(),
                    PaymentStatus = _dbContext.SaaSPaymentTransactions
                        .Any(t => t.GymId == g.Id && t.Status == "Paid") ? "Paid" :
                        (_dbContext.SubscriptionRecords.Any(s => s.GymId == g.Id) ? "Pending" : "Unpaid")
                })
                .OrderByDescending(g => g.CreatedOn)
                .ToListAsync();
        }

        public async Task<List<Staff>> GetBranchManagersAsync(Guid gymId)
        {
            return await _dbContext.Staff
                .Where(s => s.GymId == gymId && s.Role == StaffRole.Manager && s.IsActive)
                .ToListAsync();
        }

        public async Task<List<GymHoliday>> GetHolidaysAsync(Guid gymId)
        {
            return await _dbContext.GymHolidays
                .Include(h => h.Branch)
                .Where(h => h.GymId == gymId)
                .OrderBy(h => h.Date)
                .ToListAsync();
        }

        public async Task AddHolidayAsync(GymHoliday holiday)
        {
            await _dbContext.GymHolidays.AddAsync(holiday);
        }

        public async Task DeleteHolidayAsync(Guid gymId, Guid holidayId)
        {
            GymHoliday? holiday = await _dbContext.GymHolidays
                .FirstOrDefaultAsync(h => h.GymId == gymId && h.Id == holidayId);
            if (holiday != null)
            {
                _dbContext.GymHolidays.Remove(holiday);
            }
        }

        public async Task<GymConfiguration?> GetGymConfigurationAsync(Guid gymId)
        {
            return await _dbContext.GymConfigurations.FirstOrDefaultAsync(c => c.GymId == gymId);
        }
    }
}
