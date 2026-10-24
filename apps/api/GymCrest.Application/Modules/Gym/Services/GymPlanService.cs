using AutoMapper;
using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Gym.Management;
using GymCrest.Contracts.GymPlans;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;

namespace GymCrest.Application.Modules.Gym.Services
{
    public class GymPlanService : IGymPlanService
    {
        private readonly IGymPlanRepository _gymPlanRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGymManagementRepository _gymManagementRepository;
        private readonly IGymMemberRepository _gymMemberRepository;
        private readonly IUserNotificationRepository _userNotificationRepository;

        public GymPlanService(
            IGymPlanRepository gymPlanRepository, 
            IMapper mapper, 
            IUnitOfWork unitOfWork,
            IGymManagementRepository gymManagementRepository,
            IGymMemberRepository gymMemberRepository,
            IUserNotificationRepository userNotificationRepository) 
        {
            _gymPlanRepository = gymPlanRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _gymManagementRepository = gymManagementRepository;
            _gymMemberRepository = gymMemberRepository;
            _userNotificationRepository = userNotificationRepository;
        }

        public async Task<GymPlanDto> AddGymPlanAsync(CreateGymPlanRequest createGymPlan)
        {
            GymPlan gymPlan = _mapper.Map<GymPlan>(createGymPlan);
            GymPlan createdPlan = await _gymPlanRepository.AddGymPlanAsync(gymPlan);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<GymPlanDto>(createdPlan);
        }

        public async Task<GymPlanDto?> GetPlanByIdAsync(Guid planId)
        {
            GymPlan? gymPlan = await _gymPlanRepository.GetPlanByIdAsync(planId);
            if (gymPlan != null)
            {
                return _mapper.Map<GymPlanDto>(gymPlan);
            }
            return null;
        }

        public async Task<IEnumerable<GymPlanDto>> GetPlansByOwnerIdAsync(Guid ownerId)
        {
            IEnumerable<GymPlan> gymPlans = await _gymPlanRepository.GetPlansByOwnerIdAsync(ownerId);
            return _mapper.Map<IEnumerable<GymPlanDto>>(gymPlans);
        }

        public async Task<GymPlanDto> UpdateGymPlanAsync(UpdateGymPlanRequest updateGymPlan)
        {
            GymPlan? existingPlan = await _gymPlanRepository.GetPlanByIdAsync(updateGymPlan.Id);
            if (existingPlan == null)
            {
                throw new KeyNotFoundException("Gym Plan not found");
            }

            _mapper.Map(updateGymPlan, existingPlan);
            _gymPlanRepository.UpdateGymPlan(existingPlan);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<GymPlanDto>(existingPlan);
        }

        public async Task<bool> DeleteGymPlanAsync(Guid planId)
        {
            bool deleted = await _gymPlanRepository.DeleteGymPlanAsync(planId);
            if (deleted)
            {
                await _unitOfWork.SaveChangesAsync();
            }
            return deleted;
        }

        public async Task<bool> PromotePlanAsync(Guid planId, Guid ownerId)
        {
            GymPlan? plan = await _gymPlanRepository.GetPlanByIdAsync(planId);
            if (plan == null) 
                return false;

            GymListResponseDto? gymResponse = await _gymManagementRepository.GetGymByOwnerIdAsync(ownerId);
            if (gymResponse == null) 
                return false;

            IEnumerable<GymMember> members = await _gymMemberRepository.GetAllByGymIdAsync(gymResponse.Id, null);
            if (!members.Any()) 
                return false;

            foreach (var member in members)
            {
                UserNotification notification = new UserNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = member.UserId ?? Guid.Empty,
                    Title = $"New Plan: {plan.Name}",
                    Message = $"Check out our new plan! For just ₹{plan.Price} you can get {(plan.DurationMonths)} months of access.",
                    IsRead = false,
                    CreatedOn = DateTime.UtcNow
                };
                await _userNotificationRepository.AddAsync(notification);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<GymPlanDto>> GetAvailablePlansForMemberAsync(Guid userId)
        {
            GymMember? member = await _gymMemberRepository.GetByUserIdAsync(userId);
            if (member == null) 
                return Enumerable.Empty<GymPlanDto>();

            Domain.Entities.Gym? gym = await _gymManagementRepository.GetGymByIdAsync(member.GymId);
            if (gym == null) 
                return Enumerable.Empty<GymPlanDto>();

            IEnumerable<GymPlan> plans = await _gymPlanRepository.GetPlansByOwnerIdAsync(gym.OwnerUserId);
            IEnumerable<GymPlan> activePlans = plans.Where(p => p.IsActive);
            
            return _mapper.Map<IEnumerable<GymPlanDto>>(activePlans);
        }
    }
}
