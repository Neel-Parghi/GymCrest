using AutoMapper;
using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Common;
using GymCrest.Contracts.Gym.Management;
using GymCrest.Contracts.Gym.Onboarding;
using GymCrest.Contracts.Gym.Owners;
using GymCrest.Contracts.Gym.Shared;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Shared.Models;

namespace GymCrest.Application.Modules.Gym.Services
{
    public class GymManagementService : IGymManagementService
    {
        private readonly IGymManagementRepository _gymManagementRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAddressRepository _addressRepository;
        private readonly ISaaSPlanRepository _saasPlanRepository;

        /// <summary>Used only when no trial plan is set up in Pricing.</summary>
        private const int DefaultTrialDays = 14;
        /// <summary>Used only when a plan has no duration set.</summary>
        private const int DefaultPlanDays = 30;

        public GymManagementService(
            IGymManagementRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAddressRepository addressRepository,
            ISaaSPlanRepository saasPlanRepository)
        {
            _gymManagementRepository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _addressRepository = addressRepository;
            _saasPlanRepository = saasPlanRepository;
        }

        public async Task<Guid> OnboardGymAsync(Guid ownerId, GymOnboardingDto gymOnboardingDto, bool activateImmediately)
        {
            // 1 Setup and Add Address
            Address address = _mapper.Map<Address>(gymOnboardingDto.Address);
            address.Id = Guid.NewGuid();
            await _addressRepository.AddAsync(address);

            // 2 Setup and Add Gym
            Domain.Entities.Gym gym = _mapper.Map<Domain.Entities.Gym>(gymOnboardingDto);
            gym.Id = Guid.NewGuid();
            gym.AddressId = address.Id;
            gym.OwnerUserId = ownerId;
            gym.IsActive = true;
            gym.IsVerified = false;
            await _gymManagementRepository.AddGymAsync(gym);

            // 3 Link User to Gym
            User? user = await _gymManagementRepository.GetGymOwnerByIdAsync(ownerId);
            if (user != null)
            {
                user.GymId = gym.Id;
                user.Profile ??= new UserProfile { CreatedOn = DateTime.UtcNow };
                user.Profile.IsOnboarded = true;
                user.Profile.CurrentOnboardingStep = 5; // Final step
                _gymManagementRepository.UpdateGymOwner(user);
            }

            // 4 Setup and Add Branches
            bool isFirstBranch = true;
            foreach(BranchDto branchDto in gymOnboardingDto.Branches)
            {
                Address branchAddress = _mapper.Map<Address>(branchDto.Address);
                branchAddress.Id = Guid.NewGuid();
                await _addressRepository.AddAsync(branchAddress);

                Branch branch = _mapper.Map<Branch>(branchDto);
                branch.Id = Guid.NewGuid();
                branch.GymId = gym.Id;
                branch.AddressId = branchAddress.Id;
                branch.IsMainBranch = isFirstBranch;
                branch.IsActive = true;
                
                await _gymManagementRepository.AddBranchAsync(branch);
                isFirstBranch = false;
            }

            // 5 Setup and Add Gym Subscription Plan
            Plan? plan = gymOnboardingDto.PlanId != Guid.Empty ? await _saasPlanRepository.GetPlanByIdAsync(gymOnboardingDto.PlanId) : null;
            Guid planId = gymOnboardingDto.PlanId;
            int durationDays;
            if (gymOnboardingDto.IsTrial)
            {
                // Trial length comes from the trial plan in Pricing, so the super admin controls it there
                Plan? trialPlan = plan is { IsTrial: true }
                    ? plan
                    : (await _saasPlanRepository.GetAllPlansAsync()).FirstOrDefault(p => p.IsTrial && p.IsActive);
                durationDays = trialPlan?.DurationInDays > 0 ? trialPlan.DurationInDays : DefaultTrialDays;
                if (plan == null && trialPlan != null)
                {
                    plan = trialPlan;
                    planId = trialPlan.Id;
                }
            }
            else
            {
                durationDays = plan?.DurationInDays > 0 ? plan.DurationInDays : DefaultPlanDays;
            }
            decimal amount = plan?.Price ?? 0m;

            SubscriptionRecord subscription = new() 
            {
                Id = Guid.NewGuid(),
                GymId = gym.Id,
                PlanId = planId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(durationDays),
                IsTrial = gymOnboardingDto.IsTrial,
                IsActive = activateImmediately,
                PriceAtPurchase = amount,
                Notes = "Initial Onboarding Subscription"
            };
            await _gymManagementRepository.AddGymSubscriptionAsync(subscription);

            // 6 Commit Transaction
            await _unitOfWork.SaveChangesAsync();

            return gym.Id;
        }
    
        public async Task<PagedResponse<GymOwnersDto>> GetGymOwnersList(PaginationParams pagination)
        {
            (List<GymOwnersDto>items, int totalCount) = await _gymManagementRepository.GetGymOwnersList(
                pagination.PageNumber,
                pagination.PageSize,
                pagination.SearchTerm);

            return new PagedResponse<GymOwnersDto>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PagedResponse<GymListResponseDto>> GetGymListAsync(PaginationParams pagination)
        {
            (List<GymListResponseDto> items, int totalCount) = await _gymManagementRepository.GetGymListAsync(
                pagination.PageNumber,
                pagination.PageSize,
                pagination.SearchTerm);

            return new PagedResponse<GymListResponseDto>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<GymOwnersDto> UpdateGymOwner(UpdateGymOwnerDto updateGymOwnerDto)
        {
            User? user = await _gymManagementRepository.GetGymOwnerByIdAsync(updateGymOwnerDto.Id);
            if (user == null)
            {
                throw new Exception("Gym Owner not found.");
            }

            _mapper.Map(updateGymOwnerDto, user);

            user.Profile ??= new UserProfile { CreatedOn = DateTime.UtcNow };
            user.Profile.Phone = updateGymOwnerDto.Phone;

            User updatedOwner = _gymManagementRepository.UpdateGymOwner(user);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<GymOwnersDto>(updatedOwner);
        }

        public async Task UpdateGymAsync(UpdateGymDto updateGymDto)
        {
            Domain.Entities.Gym? gym = await _gymManagementRepository.GetGymByIdAsync(updateGymDto.Id);
            if (gym == null)
            {
                throw new Exception("Gym not found.");
            }

            _mapper.Map(updateGymDto, gym);
            _gymManagementRepository.UpdateGym(gym);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> DeleteGymAsync(Guid gymId)
        {
            Domain.Entities.Gym? gym = await _gymManagementRepository.GetGymByIdAsync(gymId);
            if (gym == null)
            {
                throw new Exception("Gym not found.");
            }

            if (await _gymManagementRepository.GymHasUsersAsync(gymId, gym.OwnerUserId))
            {
                return false;
            }

            await _gymManagementRepository.DeleteGymAsync(gymId);
            return true;
        }

        public async Task<bool> DeleteGymOwnerAsync(Guid ownerId)
        {
            User? owner = await _gymManagementRepository.GetGymOwnerByIdAsync(ownerId);
            if (owner == null)
            {
                throw new Exception("Gym Owner not found.");
            }

            if (await _gymManagementRepository.OwnerHasGymsAsync(ownerId))
            {
                return false;
            }

            await _gymManagementRepository.DeleteGymOwnerAsync(owner);
            return true;
        }
        
        public async Task AddBranchAsync(Guid gymId, BranchDto branchDto)
        {
            Address branchAddress = _mapper.Map<Address>(branchDto.Address);
            branchAddress.Id = Guid.NewGuid();
            await _addressRepository.AddAsync(branchAddress);

            Branch branch = _mapper.Map<Branch>(branchDto);
            branch.Id = Guid.NewGuid();
            branch.GymId = gymId;
            branch.AddressId = branchAddress.Id;
            branch.IsMainBranch = false;
            branch.IsActive = true;
            
            await _gymManagementRepository.AddBranchAsync(branch);

            // Reassign branch manager if provided
            if (branchDto.ManagerId.HasValue && branchDto.ManagerId.Value != Guid.Empty)
            {
                List<Staff> existingManagers = await _gymManagementRepository.GetBranchManagersAsync(gymId);
                Staff? selectedManager = existingManagers.FirstOrDefault(m => m.Id == branchDto.ManagerId.Value);
                if (selectedManager != null)
                {
                    selectedManager.BranchId = branch.Id;
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<BranchDto>> GetBranchesByGymIdAsync(Guid gymId)
        {
            List<Branch> branches = await _gymManagementRepository.GetBranchesByGymIdAsync(gymId);
            List<BranchDto> dtos = _mapper.Map<List<BranchDto>>(branches);

            List<Staff> managers = await _gymManagementRepository.GetBranchManagersAsync(gymId);

            foreach (BranchDto dto in dtos)
            {
                Staff? manager = managers.FirstOrDefault(m => m.BranchId == dto.Id);
                dto.ManagerName = manager != null ? $"{manager.FirstName} {manager.LastName}" : "Pending Hire";
                dto.ManagerId = manager?.Id;
            }

            return dtos;
        }

        public async Task UpdateBranchAsync(Guid branchId, BranchDto branchDto)
        {
            Branch? branch = await _gymManagementRepository.GetBranchByIdAsync(branchId);
            if (branch == null)
            {
                throw new Exception("Branch not found.");
            }

            branch.Name = branchDto.Name;
            branch.ContactNumber = branchDto.ContactNumber;
            branch.OpenTime = branchDto.OpenTime;
            branch.CloseTime = branchDto.CloseTime;

            if (branch.Address != null && branchDto.Address != null)
            {
                _mapper.Map(branchDto.Address, branch.Address);
            }

            _gymManagementRepository.UpdateBranch(branch);

            List<Staff> existingManagers = await _gymManagementRepository.GetBranchManagersAsync(branch.GymId);

            IEnumerable<Staff> currentBranchManagers = existingManagers.Where(m => m.BranchId == branchId);
            foreach (Staff currentManager in currentBranchManagers)
            {
                currentManager.BranchId = null;
            }

            // Assign new manager
            if (branchDto.ManagerId.HasValue && branchDto.ManagerId.Value != Guid.Empty)
            {
                Staff? selectedManager = existingManagers.FirstOrDefault(m => m.Id == branchDto.ManagerId.Value);
                if (selectedManager != null)
                {
                    selectedManager.BranchId = branchId;
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<GymListResponseDto?> GetGymByOwnerIdAsync(Guid ownerId)
        {
            return await _gymManagementRepository.GetGymByOwnerIdAsync(ownerId);
        }

        public async Task<GymListResponseDto?> GetGymDetailsByIdAsync(Guid gymId)
        {
            return await _gymManagementRepository.GetGymDetailsResponseByIdAsync(gymId);
        }

        public async Task UpdateMyGymAsync(Guid ownerId, UpdateMyGymDto updateMyGymDto)
        {
            Domain.Entities.Gym? gym = await _gymManagementRepository.GetGymByIdAsync((await _gymManagementRepository.GetGymByOwnerIdAsync(ownerId))?.Id ?? Guid.Empty);
            if (gym == null)
            {
                throw new Exception("Gym not found for this owner.");
            }

            if (string.IsNullOrWhiteSpace(updateMyGymDto.GymName))
            {
                updateMyGymDto.GymName = gym.GymName;
            }

            _mapper.Map(updateMyGymDto, gym);
            _gymManagementRepository.UpdateGym(gym);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<GymSettingsDto> GetGymSettingsAsync(Guid gymId)
        {
            Domain.Entities.Gym? gym = await _gymManagementRepository.GetGymByIdAsync(gymId);
            if (gym == null)
            {
                throw new Exception("Gym not found.");
            }

            return new GymSettingsDto
            {
                RoleRightsMatrixJson = gym.RoleRightsMatrixJson,
                PlanExpirationTriggerDays = gym.Configuration?.PlanExpirationTriggerDays ?? 7
            };
        }

        public async Task UpdateGymSettingsAsync(Guid gymId, GymSettingsDto dto)
        {
            Domain.Entities.Gym? gym = await _gymManagementRepository.GetGymByIdAsync(gymId);
            if (gym == null)
            {
                throw new Exception("Gym not found.");
            }

            gym.RoleRightsMatrixJson = dto.RoleRightsMatrixJson;

            if (gym.Configuration == null)
            {
                gym.Configuration = new GymConfiguration { GymId = gym.Id };
            }
            gym.Configuration.PlanExpirationTriggerDays = dto.PlanExpirationTriggerDays;

            _gymManagementRepository.UpdateGym(gym);
            await _unitOfWork.SaveChangesAsync();
        }
        
        public async Task<List<GymHolidayDto>> GetHolidaysAsync(Guid gymId)
        {
            List<GymHoliday> holidays = await _gymManagementRepository.GetHolidaysAsync(gymId);
            return holidays.Select(h => new GymHolidayDto
            {
                Id = h.Id,
                Name = h.Name,
                Date = h.Date,
                BranchId = h.BranchId,
                BranchName = h.BranchId == null ? "All Locations" : h.Branch?.Name
            }).ToList();
        }

        public async Task AddHolidayAsync(Guid gymId, GymHolidayDto dto)
        {
            GymHoliday holiday = new GymHoliday
            {
                Id = Guid.NewGuid(),
                GymId = gymId,
                BranchId = dto.BranchId,
                Name = dto.Name,
                Date = dto.Date.ToUniversalTime(),
                CreatedOn = DateTime.UtcNow
            };

            await _gymManagementRepository.AddHolidayAsync(holiday);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteHolidayAsync(Guid gymId, Guid holidayId)
        {
            await _gymManagementRepository.DeleteHolidayAsync(gymId, holidayId);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
