using AutoMapper;
using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.SaaSPlan;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;

namespace GymCrest.Application.Modules.Gym.Services
{
    internal class SaaSPlanService : ISaaSPlanService
    {
        private readonly ISaaSPlanRepository _saasPlanRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;


        public SaaSPlanService(ISaaSPlanRepository saasPlanRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _saasPlanRepository = saasPlanRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<SaaSPlanDto> AddPlanAsync(CreateSaaSPlanDto createPlanDto)
        {
            Plan createPlan = _mapper.Map<Plan>(createPlanDto);
            Plan createdPlan = await _saasPlanRepository.AddPlanAsync(createPlan);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<SaaSPlanDto>(createdPlan); 
        }

        public async Task<bool> DeletePlanAsync(Guid id)
        {
            if (await _saasPlanRepository.IsPlanInUseAsync(id))
            {
                return false;
            }

            bool result = await _saasPlanRepository.DeletePlanAsync(id);
            if (result)
            {
                await _unitOfWork.SaveChangesAsync();
            }
            return result;
        }

        public async Task<List<SaaSPlanDto>> GetAllPlansAsync()
        {
            List<Plan> plansList = await _saasPlanRepository.GetAllPlansAsync();
            return _mapper.Map<List<SaaSPlanDto>>(plansList);
        }

        public async Task<SaaSPlanDto?> GetPlanByIdAsync(Guid id)
        {
            Plan? plan = await _saasPlanRepository.GetPlanByIdAsync(id);
            if (plan != null)
            {   
                return _mapper.Map<SaaSPlanDto>(plan);
            }
            return null;
        }

        public async Task<SaaSPlanDto> UpdatePlanAsync(UpdateSaaSPlanDto updateSaaSPlanDto)
        {
            Plan? existingPlan = await _saasPlanRepository.GetPlanByIdAsync(updateSaaSPlanDto.Id);
            if (existingPlan == null)
            {
                throw new Exception("Plan not found.");
            }

            _mapper.Map(updateSaaSPlanDto, existingPlan);

            Plan updatedPlan = _saasPlanRepository.UpdatePlanAsync(existingPlan);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaaSPlanDto>(updatedPlan);
        }
    }
}
