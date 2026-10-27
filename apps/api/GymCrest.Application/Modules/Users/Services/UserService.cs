using AutoMapper;
using GymCrest.Application.BackgroundJobs;
using GymCrest.Application.Modules.Auth.Interface;
using GymCrest.Application.Modules.Common.Interfaces;
using GymCrest.Application.Modules.PlanGeneration.Interface;
using GymCrest.Application.Modules.Users.Interface;
using GymCrest.Contracts.PlanGeneration;
using GymCrest.Contracts.Users;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Shared.Constants;
using GymCrest.Shared.Enums;
using GymCrest.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GymCrest.Contracts.Common;
using GymCrest.Shared.Models;

namespace GymCrest.Application.Modules.Users.Services
{
    public class UserService : IUserService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IConfiguration _config;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAddressRepository _addressRepository;
        private readonly IMapper _mapper;
        private readonly IPlanGenerationService _planGenerationService;
        private readonly ILogger<UserService> _logger;
        private readonly AccountDeletionJob _accountDeletionJob;

        /// <summary>Steps in the member onboarding wizard; completing it leaves the member on the last one.</summary>
        private const int OnboardingStepCount = 4;

        public UserService(
            IAuthRepository authRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            IEmailService emailService,
            ICurrentUserService currentUserService,
            IConfiguration config,
            IFileStorageService fileStorageService,
            IAddressRepository addressRepository,
            IMapper mapper,
            IPlanGenerationService planGenerationService,
            ILogger<UserService> logger,
            AccountDeletionJob accountDeletionJob)
        {
            _authRepository = authRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _emailService = emailService;
            _currentUserService = currentUserService;
            _config = config;
            _fileStorageService = fileStorageService;
            _addressRepository = addressRepository;
            _mapper = mapper;
            _planGenerationService = planGenerationService;
            _logger = logger;
            _accountDeletionJob = accountDeletionJob;
        }

        public async Task InviteOwnerAsync(InviteOwnerRequestDto inviteOwnerRequestDto)
        {
            string token = Guid.NewGuid().ToString();

            User user = new()
            {
                FirstName = inviteOwnerRequestDto.FirstName,
                LastName = inviteOwnerRequestDto.LastName,
                Email = inviteOwnerRequestDto.Email,
                Role = UserRole.GymOwner,
                IsActive = true,
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                Security = new UserSecurity
                {
                    InvitationToken = token,
                    InvitationExpiry = DateTime.UtcNow.AddDays(7),
                    IsInvitationAccepted = false,
                    IsEmailVerified = true
                },
                Profile = new UserProfile
                {
                    Phone = inviteOwnerRequestDto.Phone,
                    CreatedOn = DateTime.UtcNow,
                    ModifiedOn = DateTime.UtcNow
                }
            };

            await _authRepository.AddUserAsync(user);
            await _unitOfWork.SaveChangesAsync();

            await _emailService.SendInvitationEmailAsync(user.Email, $"{user.FirstName} {user.LastName}", token);
        }

        public async Task SetPasswordAsync(SetPasswordRequestDto setPasswordRequestDto)
        {
            User? user = await _authRepository.GetByTokenAsync(setPasswordRequestDto.Token);

            if (user == null || user.Security == null || user.Security.InvitationExpiry < DateTime.UtcNow)
                throw new Exception("The invitation link is invalid or has expired.");

            user.PasswordHash = _passwordService.HashPassword(setPasswordRequestDto.Password);
            user.Security.IsInvitationAccepted = true;
            user.Security.IsEmailVerified = true;
            user.Security.InvitationToken = null;
            user.Security.InvitationExpiry = null;
            user.ModifiedOn = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> ValidateInvitationTokenAsync(string token)
        {
            User? user = await _authRepository.GetByTokenAsync(token);
            return user != null && user.Security != null && user.Security.InvitationExpiry > DateTime.UtcNow;
        }

        public async Task ReInviteOwnerAsync(Guid userId)
        {
            User? user = await _userRepository.GetUserByIdAsync(userId);
            
            if (user == null)
                throw new Exception("User not found.");

            if (user.Security != null && user.Security.IsInvitationAccepted)
                throw new Exception("Invitation has already been accepted.");

            string token = Guid.NewGuid().ToString();
            user.Security ??= new UserSecurity { CreatedOn = DateTime.UtcNow };
            user.Security.InvitationToken = token;
            user.Security.InvitationExpiry = DateTime.UtcNow.AddDays(7);
            user.ModifiedOn = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            await _emailService.SendInvitationEmailAsync(user.Email, $"{user.FirstName} {user.LastName}", token);
        }

        public async Task<UserProfileDto> GetUserProfileAsync(Guid userId)
        {
            User? user = await _userRepository.GetUserByIdAsync(userId);
            
            if (user == null)
                throw new Exception("User not found.");

            return new UserProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Profile?.Phone ?? string.Empty,
                ProfilePictureUrl = user.Profile?.ProfilePictureUrl ?? string.Empty,
                Role = user.Role.ToString(),
                GymId = user.GymId,
                IsOnboarded = user.Profile?.IsOnboarded ?? false,
                CurrentOnboardingStep = user.Profile?.CurrentOnboardingStep ?? 0,
                AddressLine1 = user.Profile?.Address?.Address1,
                AddressLine2 = user.Profile?.Address?.Address2,
                City = user.Profile?.Address?.City,
                State = user.Profile?.Address?.State,
                ZipCode = user.Profile?.Address?.PostalCode
            };
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto dto)
        {
            User? user = await _userRepository.GetUserByIdAsync(userId);
            
            if (user == null)
                throw new Exception("User not found.");

            bool isValid = _passwordService.VerifyPasword(dto.CurrentPassword, user.PasswordHash!);
            if (!isValid)
                throw new Exception("Invalid current password.");

            user.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
            user.ModifiedOn = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateUserProfileAsync(Guid userId, UpdateUserProfileDto dto)
        {
            User? user = await _userRepository.GetUserByIdAsync(userId);
            
            if (user == null)
                throw new Exception("User not found.");

            user.Profile ??= new UserProfile { CreatedOn = DateTime.UtcNow };

            // Delete old picture if changed
            if (!string.IsNullOrEmpty(user.Profile.ProfilePictureUrl) && 
                user.Profile.ProfilePictureUrl != dto.ProfilePictureUrl && 
                !user.Profile.ProfilePictureUrl.Contains("placeholder"))
            {
                await _fileStorageService.DeleteFileAsync(user.Profile.ProfilePictureUrl);
            }

            user.ModifiedOn = DateTime.UtcNow;
            user.Profile.ModifiedOn = DateTime.UtcNow;

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Profile.Phone = dto.Phone ?? string.Empty;
            user.Profile.ProfilePictureUrl = dto.ProfilePictureUrl ?? string.Empty;

            bool isNewAddress = user.Profile.Address == null;

            if (isNewAddress)
            {
                user.Profile.Address = new Address
                {
                    Id = Guid.NewGuid(),
                    Address1 = dto.AddressLine1 ?? string.Empty,
                    Address2 = dto.AddressLine2 ?? string.Empty,
                    City = dto.City ?? string.Empty,
                    State = dto.State ?? string.Empty,
                    PostalCode = dto.ZipCode ?? string.Empty,
                    CreatedOn = DateTime.UtcNow
                };
                await _addressRepository.AddAsync(user.Profile.Address);
                user.Profile.AddressId = user.Profile.Address.Id;
            }
            else if (user.Profile.Address != null)
            {
                user.Profile.Address.Address1 = dto.AddressLine1 ?? string.Empty;
                user.Profile.Address.Address2 = dto.AddressLine2 ?? string.Empty;
                user.Profile.Address.City = dto.City ?? string.Empty;
                user.Profile.Address.State = dto.State ?? string.Empty;
                user.Profile.Address.PostalCode = dto.ZipCode ?? string.Empty;
                user.Profile.Address.ModifiedOn = DateTime.UtcNow;
                user.Profile.Address.ModifiedBy = userId;
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<UserProfileDto> GetMyProfileAsync()
        {
            Guid userId = _currentUserService.UserId ?? throw new Exception("Unauthorized access.");
            return await GetUserProfileAsync(userId);
        }

        public async Task UpdateMyProfileAsync(UpdateUserProfileDto dto)
        {
            Guid userId = _currentUserService.UserId ?? throw new Exception("Unauthorized access.");
            await UpdateUserProfileAsync(userId, dto);
        }

        public async Task<UserPreferenceDto> GetMyPreferencesAsync()
        {
            Guid userId = _currentUserService.UserId ?? throw new Exception("Unauthorized access.");
            User? user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            double? weightKg = (await _userRepository.GetLatestBodyMetricsAsync(userId)).WeightKg;

            return new UserPreferenceDto
            {
                PrimaryGoal = user.Preference?.PrimaryGoal == null ? null : FitnessGoals.Normalize(user.Preference.PrimaryGoal),
                TargetWeight = user.Preference?.TargetWeight,
                TargetCalories = user.Preference?.TargetCalories,
                TargetProtein = user.Preference?.TargetProtein,
                TargetCarbs = user.Preference?.TargetCarbs,
                TargetFats = user.Preference?.TargetFats,
                TargetTrainingTime = user.Preference?.TargetTrainingTime,
                TargetCaloriesBurn = user.Preference?.TargetCaloriesBurn,
                AutoCaloriesBurn = CalorieBurn.AutoDailyTarget(weightKg, user.Preference?.TargetTrainingTime ?? 60),
                DietPreference = user.Preference?.DietPreference,
                ActivityLevel = user.Preference?.ActivityLevel,
                TrainingDaysPerWeek = user.Preference?.TrainingDaysPerWeek,
                EmailNotificationsEnabled = user.Preference?.EmailNotificationsEnabled ?? true,
                ShowGymBuddy = user.Preference?.ShowGymBuddy ?? true
            };
        }

        public async Task UpdateMyPreferencesAsync(UpdateUserPreferenceDto dto)
        {
            Guid userId = _currentUserService.UserId ?? throw new Exception("Unauthorized access.");
            User? user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            user.Preference ??= new UserPreference { CreatedOn = DateTime.UtcNow };
            
            user.Preference.PrimaryGoal = string.IsNullOrWhiteSpace(dto.PrimaryGoal) ? null : FitnessGoals.Normalize(dto.PrimaryGoal);

            // A new target starts its progress from today's weight. Saving other settings keeps the start.
            bool targetChanged = dto.TargetWeight.HasValue != user.Preference.TargetWeight.HasValue
                || (dto.TargetWeight.HasValue && Math.Abs(dto.TargetWeight.Value - user.Preference.TargetWeight!.Value) > 0.01);
            if (targetChanged)
            {
                user.Preference.TargetWeightStart = dto.TargetWeight.HasValue
                    ? (await _userRepository.GetLatestBodyMetricsAsync(userId)).WeightKg
                    : null;
            }
            user.Preference.TargetWeight = dto.TargetWeight;
            user.Preference.TargetCalories = dto.TargetCalories ?? user.Preference.TargetCalories;
            user.Preference.TargetProtein = dto.TargetProtein ?? user.Preference.TargetProtein;
            user.Preference.TargetCarbs = dto.TargetCarbs ?? user.Preference.TargetCarbs;
            user.Preference.TargetFats = dto.TargetFats ?? user.Preference.TargetFats;
            user.Preference.TargetTrainingTime = dto.TargetTrainingTime;
            user.Preference.TargetCaloriesBurn = dto.TargetCaloriesBurn > 0 ? dto.TargetCaloriesBurn : null;
            user.Preference.DietPreference = DietTypes.Normalize(dto.DietPreference) ?? user.Preference.DietPreference;
            user.Preference.ActivityLevel = ActivityLevels.Normalize(dto.ActivityLevel) ?? user.Preference.ActivityLevel;
            user.Preference.TrainingDaysPerWeek = dto.TrainingDaysPerWeek is int days
                ? Math.Clamp(days, 1, 7)
                : user.Preference.TrainingDaysPerWeek;
            user.Preference.EmailNotificationsEnabled = dto.EmailNotificationsEnabled ?? true;
            user.Preference.ShowGymBuddy = dto.ShowGymBuddy ?? true;
            user.Preference.ModifiedOn = DateTime.UtcNow;

            await _userRepository.UpdateUserAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ChangeMyPasswordAsync(ChangePasswordRequestDto dto)
        {
            Guid userId = _currentUserService.UserId ?? throw new Exception("Unauthorized access.");
            await ChangePasswordAsync(userId, dto);
        }

        public async Task<string> UploadAvatarAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            using (Stream stream = file.OpenReadStream())
            {
                return await _fileStorageService.SaveFileAsync(stream, file.FileName, "avatars");
            }
        }

        public async Task<IEnumerable<DeletionRequestDto>> GetPendingDeletionRequestsAsync()
        {
            IEnumerable<User> users = await _userRepository.GetPendingDeletionRequestsAsync();

            return [.. users.Select(u => new DeletionRequestDto
            {
                UserId = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.Role.ToString(),
                DeletionRequestedOn = u.Security!.DeletionRequestedOn!.Value,
                ScheduledDeletionTime = u.Security!.DeletionRequestedOn!.Value.AddHours(24)
            })];
        }

        public async Task DeleteUserAsAdminAsync(Guid userId)
        {
            User user = await _userRepository.GetUserByIdAsync(userId)
                ?? throw new Exception("User not found.");

            if (user.Id == _currentUserService.UserId)
            {
                throw new Exception("You can't delete your own account from here.");
            }
            if (user.Role == UserRole.SuperAdmin)
            {
                throw new Exception("Super admin accounts can't be deleted.");
            }
            if (user.Role == UserRole.GymOwner)
            {
                throw new Exception("Gym owners are managed from Gym Owners.");
            }

            string[] demoEmails = _config.GetSection("Demo:Emails").Get<string[]>() ?? [];
            if (demoEmails.Contains(user.Email, StringComparer.OrdinalIgnoreCase))
            {
                throw new Exception("Demo accounts can't be deleted.");
            }

            _logger.LogInformation("Super admin {AdminId} deleted user {UserId}", _currentUserService.UserId, userId);
            await _accountDeletionJob.ExecuteAsync(userId);
        }

        public async Task<PagedResponse<StandaloneUserDto>> GetStandaloneUsersAsync(PaginationParams pagination)
        {
            (IEnumerable<User> Items, int TotalCount) result = await _userRepository.GetStandaloneUsersAsync(pagination.PageNumber, pagination.PageSize, pagination.SearchTerm);

            IEnumerable<StandaloneUserDto> items = result.Items.Select(u => new StandaloneUserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Phone = u.Profile?.Phone ?? string.Empty,
                ProfilePictureUrl = u.Profile?.ProfilePictureUrl ?? string.Empty,
                Role = u.Role.ToString(),
                CreatedOn = u.CreatedOn,
                DeletionRequestedOn = u.Security?.DeletionRequestedOn,
                IsEmailVerified = u.Security?.IsEmailVerified ?? false
            });

            return new PagedResponse<StandaloneUserDto>(items, result.TotalCount, pagination.PageNumber, pagination.PageSize);
        }
        
        public async Task SaveOnboardingStepAsync(Guid userId, int step)
        {
            User? user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            user.Profile ??= new UserProfile { CreatedOn = DateTime.UtcNow };
            user.Profile.CurrentOnboardingStep = step;
            await _userRepository.UpdateUserAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CompleteUserOnboardingAsync(Guid userId, CompleteUserOnboardingDto dto)
        {
            User? user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            user.Profile ??= new UserProfile { CreatedOn = DateTime.UtcNow };
            user.Profile.IsOnboarded = true;
            user.Profile.CurrentOnboardingStep = OnboardingStepCount;

            user.Preference ??= new UserPreference { CreatedOn = DateTime.UtcNow };

            user.Profile.DateOfBirth = dto.DOB;
            if (Enum.TryParse<Gender>(dto.Gender, true, out Gender genderEnum))
            {
                user.Profile.Gender = genderEnum;
            }

            if (dto.TargetWeight.HasValue)
            {
                user.Preference.TargetWeight = (double)dto.TargetWeight.Value;
                // Onboarding records today's weight below; the goal starts from it.
                user.Preference.TargetWeightStart = (double)dto.Weight;
            }
            user.Preference.PrimaryGoal = FitnessGoals.Normalize(dto.PrimaryGoal);
            user.Preference.ExperienceLevel = dto.ExperienceLevel;
            user.Preference.ActivityLevel = ActivityLevels.Normalize(dto.ActivityLevel);
            user.Preference.DietPreference = DietTypes.Normalize(dto.DietPreference);
            user.Preference.TrainingDaysPerWeek = dto.TrainingDaysPerWeek;
            user.Preference.TargetTrainingTime = dto.SessionLengthMinutes ?? user.Preference.TargetTrainingTime;
            user.Preference.Limitations = string.IsNullOrWhiteSpace(dto.Limitations) ? null : dto.Limitations.Trim();

            double heightInMeters = (double)dto.Height / 100.0;
            double bmi = 0;
            if (heightInMeters > 0)
            {
                bmi = Math.Round((double)dto.Weight / (heightInMeters * heightInMeters), 1);
            }

            MemberMeasurement measurement = new MemberMeasurement
            {
                UserId = user.Id,
                Height = (double)dto.Height,
                Weight = (double)dto.Weight,
                BMI = bmi,
                Date = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow,
                IsAdvanced = false
            };

            user.Measurements ??= new List<MemberMeasurement>();
            user.Measurements.Add(measurement);

            await _userRepository.UpdateUserAsync(user);
            await _unitOfWork.SaveChangesAsync();

            try
            {
                StarterPlanResultDto starterPlans = await _planGenerationService.GenerateStarterPlansAsync(user.Id, new StarterPlanProfileDto
                {
                    PrimaryGoal = user.Preference.PrimaryGoal,
                    ExperienceLevel = dto.ExperienceLevel,
                    ActivityLevel = user.Preference.ActivityLevel,
                    DietPreference = user.Preference.DietPreference,
                    TrainingDaysPerWeek = dto.TrainingDaysPerWeek,
                    SessionLengthMinutes = dto.SessionLengthMinutes,
                    HeightCm = dto.Height,
                    WeightKg = dto.Weight,
                    DateOfBirth = dto.DOB,
                    Gender = dto.Gender
                });

                ApplyTargets(user.Preference, starterPlans);
                await _userRepository.UpdateUserAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Starter plan generation is a best-effort enhancement - onboarding must succeed regardless.
                _logger.LogError(ex, "Failed to generate starter plans for user {UserId} during onboarding.", user.Id);
            }
        }

        public async Task<StarterPlanResultDto?> GetSuggestedTargetsAsync(string? goal, string? activityLevel)
        {
            Guid userId = _currentUserService.UserId ?? throw new Exception("Unauthorized access.");
            User? user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            StarterPlanProfileDto? profile = await BuildPlanProfileAsync(user, goal, activityLevel);
            return profile == null ? null : _planGenerationService.CalculateTargets(profile);
        }

        public StarterPlanResultDto PreviewTargets(StarterPlanProfileDto profile) =>
            _planGenerationService.CalculateTargets(profile);

        public async Task<StarterPlanResultDto> RegenerateStarterPlansAsync()
        {
            Guid userId = _currentUserService.UserId ?? throw new Exception("Unauthorized access.");
            User? user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            StarterPlanProfileDto profile = await BuildPlanProfileAsync(user, null, null)
                ?? throw new Exception("Add your height, weight, date of birth and gender before generating a plan.");

            StarterPlanResultDto targets = await _planGenerationService.GenerateStarterPlansAsync(user.Id, profile);

            user.Preference ??= new UserPreference { CreatedOn = DateTime.UtcNow };
            ApplyTargets(user.Preference, targets);
            await _userRepository.UpdateUserAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return targets;
        }

        private async Task<StarterPlanProfileDto?> BuildPlanProfileAsync(User user, string? goalOverride, string? activityLevelOverride)
        {
            (double? heightCm, double? weightKg) = await _userRepository.GetLatestBodyMetricsAsync(user.Id);
            if (heightCm is not > 0 || weightKg is not > 0 || user.Profile?.DateOfBirth == null) return null;

            UserPreference? preference = user.Preference;
            return new StarterPlanProfileDto
            {
                PrimaryGoal = string.IsNullOrWhiteSpace(goalOverride) ? preference?.PrimaryGoal : goalOverride,
                ExperienceLevel = preference?.ExperienceLevel,
                ActivityLevel = string.IsNullOrWhiteSpace(activityLevelOverride) ? preference?.ActivityLevel : activityLevelOverride,
                DietPreference = preference?.DietPreference,
                TrainingDaysPerWeek = preference?.TrainingDaysPerWeek,
                SessionLengthMinutes = preference?.TargetTrainingTime,
                HeightCm = (decimal)heightCm.Value,
                WeightKg = (decimal)weightKg.Value,
                DateOfBirth = user.Profile.DateOfBirth.Value,
                Gender = user.Profile.Gender?.ToString() ?? string.Empty
            };
        }

        private static void ApplyTargets(UserPreference preference, StarterPlanResultDto targets)
        {
            preference.TargetCalories = targets.TargetCalories;
            preference.TargetProtein = targets.TargetProtein;
            preference.TargetCarbs = targets.TargetCarbs;
            preference.TargetFats = targets.TargetFats;
            preference.ModifiedOn = DateTime.UtcNow;
        }

        public async Task<UserDashboardSummaryDto> GetUserDashboardSummaryAsync()
        {
            Guid userId = _currentUserService.UserId ?? throw new Exception("Unauthorized access.");
            return await _userRepository.GetUserDashboardSummaryAsync(userId);
        }

        public async Task<IEnumerable<DailyRoutineDto>> GetDailyRoutinesAsync()
        {
            Guid userId = _currentUserService.UserId ?? throw new Exception("Unauthorized access.");
            IEnumerable<DailyRoutine> routines = await _userRepository.GetDailyRoutinesAsync(userId);
            DailyRoutineCompletion? completion = await _userRepository.GetDailyRoutineCompletionAsync(userId, AppTimeZone.TodayAsUtcDate);
            List<Guid> completedIds = completion?.CompletedRoutineIds ?? new List<Guid>();

            return routines.Select(r => new DailyRoutineDto
            {
                Id = r.Id,
                Title = r.Title,
                Time = r.Time,
                Amount = r.Amount,
                Order = r.Order,
                Completed = completedIds.Contains(r.Id)
            });
        }

        public async Task<DailyRoutineDto> CreateDailyRoutineAsync(CreateDailyRoutineDto dto)
        {
            Guid userId = _currentUserService.UserId ?? throw new Exception("Unauthorized access.");
            DailyRoutine routine = new DailyRoutine
            {
                UserId = userId,
                Title = dto.Title,
                Time = dto.Time,
                Amount = dto.Amount,
                Order = dto.Order,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            await _userRepository.AddDailyRoutineAsync(routine);
            await _unitOfWork.SaveChangesAsync();

            return new DailyRoutineDto
            {
                Id = routine.Id,
                Title = routine.Title,
                Time = routine.Time,
                Amount = routine.Amount,
                Order = routine.Order,
                Completed = false
            };
        }

        public async Task UpdateDailyRoutineAsync(Guid id, UpdateDailyRoutineDto dto)
        {
            DailyRoutine? routine = await _userRepository.GetDailyRoutineByIdAsync(id);
            if (routine == null) throw new Exception("Routine not found");

            routine.Title = dto.Title;
            routine.Time = dto.Time;
            routine.Amount = dto.Amount;
            routine.Order = dto.Order;
            routine.ModifiedOn = DateTime.UtcNow;

            await _userRepository.UpdateDailyRoutineAsync(routine);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteDailyRoutineAsync(Guid id)
        {
            DailyRoutine? routine = await _userRepository.GetDailyRoutineByIdAsync(id);
            if (routine == null) throw new Exception("Routine not found");

            routine.IsActive = false;
            routine.ModifiedOn = DateTime.UtcNow;

            await _userRepository.UpdateDailyRoutineAsync(routine);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ToggleDailyRoutineAsync(Guid id)
        {
            Guid userId = _currentUserService.UserId ?? throw new Exception("Unauthorized access.");
            DailyRoutineCompletion? completion = await _userRepository.GetDailyRoutineCompletionAsync(userId, AppTimeZone.TodayAsUtcDate);

            if (completion == null)
            {
                completion = new DailyRoutineCompletion
                {
                    UserId = userId,
                    Date = AppTimeZone.TodayAsUtcDate,
                    CompletedRoutineIds = new List<Guid> { id },
                    CreatedOn = DateTime.UtcNow
                };
                await _userRepository.AddDailyRoutineCompletionAsync(completion);
            }
            else
            {
                if (completion.CompletedRoutineIds.Contains(id))
                {
                    completion.CompletedRoutineIds.Remove(id);
                }
                else
                {
                    completion.CompletedRoutineIds.Add(id);
                }
                completion.ModifiedOn = DateTime.UtcNow;
                await _userRepository.UpdateDailyRoutineCompletionAsync(completion);
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
