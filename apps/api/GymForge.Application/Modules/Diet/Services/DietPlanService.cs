using AutoMapper;
using GymForge.Application.Modules.Diet.Interface;
using GymForge.Contracts.DietPlan;
using GymForge.Domain.Entities;
using GymForge.Domain.Interface;
using GymForge.Shared.Constants;

namespace GymForge.Application.Modules.Diet.Services
{
    public class DietPlanService : IDietPlanService
    {
        private readonly IDietPlanRepository _dietPlanRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public DietPlanService(IDietPlanRepository dietPlanRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _dietPlanRepository = dietPlanRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<DietPlanDto>> GetPlansAsync(Guid? gymId, Guid userId, string? goal = null, bool restrictToOwnPlans = false)
        {
            IEnumerable<DietPlan> plans = await _dietPlanRepository.GetPlansAsync(gymId, userId, goal, restrictToOwnPlans);
            return _mapper.Map<IEnumerable<DietPlanDto>>(plans);
        }

        public async Task<DietPlanDto?> GetPlanByIdAsync(Guid id)
        {
            DietPlan? plan = await _dietPlanRepository.GetPlanByIdAsync(id);
            return plan == null ? null : _mapper.Map<DietPlanDto>(plan);
        }

        public async Task<DietPlanDto> CreatePlanAsync(CreateDietPlanRequest request, Guid? gymId, Guid createdById)
        {
            DietPlan plan = _mapper.Map<DietPlan>(request);
            plan.GymId = gymId;
            plan.CreatedBy = createdById;
            plan.IsCustom = request.IsCustom;
            plan.IsTemplate = request.IsTemplate;
            plan.DietType = DietTypes.Normalize(request.DietType);
            plan.Calories = Convert.ToInt32((plan.Protein * 4) + (plan.Carbs * 4) + (plan.Fats * 9));

            DietPlan created = await _dietPlanRepository.CreatePlanAsync(plan);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DietPlanDto>(created);
        }

        public async Task<DietPlanDto> UpdatePlanAsync(UpdateDietPlanRequest request, Guid? gymId, Guid modifiedById)
        {
            DietPlan? existingPlan = await _dietPlanRepository.GetPlanByIdAsync(request.Id);
            
            if (existingPlan == null)
            {
                throw new KeyNotFoundException("Diet Plan not found.");
            }

            existingPlan.Name = request.Name;
            existingPlan.Description = request.Description;
            existingPlan.Protein = request.Protein;
            existingPlan.Carbs = request.Carbs;
            existingPlan.Fats = request.Fats;
            existingPlan.Calories = Convert.ToInt32((request.Protein * 4) + (request.Carbs * 4) + (request.Fats * 9));
            existingPlan.Goal = request.Goal;
            existingPlan.DietType = DietTypes.Normalize(request.DietType);
            existingPlan.IsCustom = request.IsCustom;
            existingPlan.IsTemplate = request.IsTemplate;
            existingPlan.ModifiedBy = modifiedById;
            existingPlan.ModifiedOn = DateTime.UtcNow;

            existingPlan.Meals.Clear();

            foreach (CreateDietPlanMealDto mealDto in request.Meals)
            {
                DietPlanMeal meal = _mapper.Map<DietPlanMeal>(mealDto);
                existingPlan.Meals.Add(meal);
            }

            await _dietPlanRepository.UpdatePlanAsync(existingPlan);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DietPlanDto>(existingPlan);
        }

        public async Task<bool> DeletePlanAsync(Guid id)
        {
            bool success = await _dietPlanRepository.DeletePlanAsync(id);
            if (success)
            {
                await _unitOfWork.SaveChangesAsync();
            }
            return success;
        }
    }
}
