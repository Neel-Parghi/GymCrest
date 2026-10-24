using AutoMapper;
using GymCrest.Application.Modules.Diet.Interface;
using GymCrest.Contracts.DietPlan;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;

namespace GymCrest.Application.Modules.Diet.Services
{
    public class MemberDietService : IMemberDietService
    {
        private readonly IMemberDietRepository _memberDietRepository;
        private readonly IDietPlanRepository _dietPlanRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public MemberDietService(
            IMemberDietRepository memberDietRepository,
            IDietPlanRepository dietPlanRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _memberDietRepository = memberDietRepository;
            _dietPlanRepository = dietPlanRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<MemberDietAssignmentDto?> GetActiveDietForMemberAsync(Guid memberId)
        {
            MemberDietAssignment? assignment = await _memberDietRepository.GetActiveDietAssignmentAsync(memberId);
            if (assignment == null) return null;

            return new MemberDietAssignmentDto
            {
                Id = assignment.Id,
                MemberId = assignment.MemberId,
                DietPlanId = assignment.DietPlanId,
                AssignedAt = assignment.AssignedAt,
                IsActive = assignment.IsActive,
                DietPlan = assignment.DietPlan == null ? null : new DietPlanDto
                {
                    Id = assignment.DietPlan.Id,
                    Name = assignment.DietPlan.Name,
                    Description = assignment.DietPlan.Description,
                    Protein = assignment.DietPlan.Protein,
                    Carbs = assignment.DietPlan.Carbs,
                    Fats = assignment.DietPlan.Fats,
                    Calories = assignment.DietPlan.Calories,
                    Goal = assignment.DietPlan.Goal,
                    GymId = assignment.DietPlan.GymId,
                    IsCustom = assignment.DietPlan.IsCustom,
                    IsTemplate = assignment.DietPlan.IsTemplate,
                    Meals = assignment.DietPlan.Meals.Select(m => new DietPlanMealDto
                    {
                        Id = m.Id,
                        DietPlanId = m.DietPlanId,
                        Name = m.Name,
                        Time = m.Time,
                        Calories = m.Calories,
                        Protein = m.Protein,
                        Carbs = m.Carbs,
                        Fats = m.Fats,
                        Items = m.Items,
                        SortOrder = m.SortOrder
                    }).OrderBy(m => m.SortOrder).ToList()
                }
            };
        }

        public async Task<bool> AssignDietToMemberAsync(Guid memberId, Guid dietPlanId, Guid? gymId)
        {
            MemberDietAssignment? activeAssignment = await _memberDietRepository.GetActiveDietAssignmentAsync(memberId);
            if (activeAssignment != null)
            {
                activeAssignment.IsActive = false;
            }

            MemberDietAssignment newAssignment = new()
            {
                DietPlanId = dietPlanId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };

            if (gymId.HasValue) newAssignment.MemberId = memberId;
            else newAssignment.UserId = memberId;

            await _memberDietRepository.AddDietAssignmentAsync(newAssignment);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignCustomDietToMemberAsync(Guid memberId, CreateDietPlanRequest request, Guid? gymId, Guid createdById)
        {
            DietPlan plan = _mapper.Map<DietPlan>(request);
            plan.GymId = gymId;
            plan.CreatedBy = createdById;
            plan.IsCustom = true;
            plan.IsTemplate = false;
            plan.Calories = Convert.ToInt32((plan.Protein * 4) + (plan.Carbs * 4) + (plan.Fats * 9));

            await _dietPlanRepository.CreatePlanAsync(plan);

            MemberDietAssignment? activeAssignment = await _memberDietRepository.GetActiveDietAssignmentAsync(memberId);
            if (activeAssignment != null)
            {
                activeAssignment.IsActive = false;
            }

            MemberDietAssignment newAssignment = new()
            {
                DietPlan = plan,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };

            if (gymId.HasValue) newAssignment.MemberId = memberId;
            else newAssignment.UserId = memberId;

            await _memberDietRepository.AddDietAssignmentAsync(newAssignment);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnassignActiveDietAsync(Guid memberId, Guid? gymId)
        {
            MemberDietAssignment? activeAssignment = await _memberDietRepository.GetActiveDietAssignmentAsync(memberId);
            if (activeAssignment == null) return false;

            activeAssignment.IsActive = false;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
