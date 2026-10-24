using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GymCrest.Application.BackgroundJobs
{
    public class AccountDeletionJob
    {
        private readonly IUserRepository _userRepository;
        private readonly IGymMemberRepository _gymMemberRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly IAddressRepository _addressRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountDeletionJob> _logger;

        public AccountDeletionJob(
            IUserRepository userRepository,
            IGymMemberRepository gymMemberRepository,
            IStaffRepository staffRepository,
            IAddressRepository addressRepository,
            IUnitOfWork unitOfWork,
            ILogger<AccountDeletionJob> logger)
        {
            _userRepository = userRepository;
            _gymMemberRepository = gymMemberRepository;
            _staffRepository = staffRepository;
            _addressRepository = addressRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid userId)
        {
            _logger.LogInformation($"AccountDeletionJob started for user {userId}");

            User? user = await _userRepository.GetUserByIdAsync(userId);
            
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found for deletion.");
                return;
            }

            // Prevent Gym Owners from hard deleting their account via this job.
            // If they are a gym owner, they must delete the gym or transfer ownership.
            if (user.Role == Shared.Enums.UserRole.GymOwner)
            {
                _logger.LogWarning($"User {userId} is a Gym Owner. Deletion aborted to prevent cascading gym data loss.");
                return;
            }


            List<GymMember> members = await _gymMemberRepository.GetAllByUserIdAsync(userId);
            foreach (GymMember member in members)
            {
                member.UserId = null;
                await _gymMemberRepository.UpdateAsync(member);
                _logger.LogInformation($"Unlinked GymMember {member.Id} from User {userId}");
            }

            Staff? staff = await _staffRepository.GetByUserIdAsync(userId);
            
            if (staff != null)
            {
                staff.UserId = null;
                await _staffRepository.UpdateAsync(staff);
                _logger.LogInformation($"Unlinked Staff {staff.Id} from User {userId}");
            }

            if (user.Profile?.AddressId.HasValue == true)
            {
                await _addressRepository.DeleteAsync(user.Profile.AddressId.Value);
                _logger.LogInformation($"Deleted Address {user.Profile.AddressId.Value} for User {userId}");
            }

            await _userRepository.DeleteUserAsync(user);
            _logger.LogInformation($"Deleted core User record {userId}");

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"AccountDeletionJob completed for user {userId}");
        }
    }
}
