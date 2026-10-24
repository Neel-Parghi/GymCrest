using GymCrest.Application.Modules.Auth.Interface;
using GymCrest.Contracts.Auth;
using GymCrest.Domain.Entities;
using GymCrest.Shared.Enums;
using GymCrest.Domain.Interface;

namespace GymCrest.Application.Modules.Auth.Service
{
    public class AuthService : IAuthService
    {
        private const int MaxActiveSessions = 5;

        private readonly IPasswordService _passwordService;
        private readonly IJwtService _jwtService;
        private readonly IAuthRepository _authRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public AuthService(IPasswordService passwordService, IJwtService jwtService, IAuthRepository authRepository, IUserRepository userRepository, IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _passwordService = passwordService;
            _jwtService = jwtService;
            _authRepository = authRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<TokenResponseDto> RegisterSuperAdmin(RegisterRequestDto userDto)
        {
            User user = new()
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                PasswordHash = _passwordService.HashPassword(userDto.Password),
                Role = UserRole.SuperAdmin,
                IsActive = true,
                Profile = new UserProfile 
                {
                    Phone = userDto.Phone ?? string.Empty
                }
            };

            await _authRepository.RegisterSuperAdmin(user);
            await _unitOfWork.SaveChangesAsync();

            return await GenerateAndSaveTokens(user);
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto userDto)
        {
            if (userDto.Role != UserRole.User && userDto.Role != UserRole.GymOwner)
            {
                throw new Exception("Invalid role for registration. Only User or GymOwner roles are allowed.");
            }

            User? existingUser = await _authRepository.GetByUserByEmailAsync(userDto.Email);
            if (existingUser != null)
            {
                if (existingUser.Security == null || !existingUser.Security.IsEmailVerified)
                {
                    // Allow re-registering or just regenerating OTP if email is not verified
                    existingUser.Security ??= new UserSecurity { CreatedOn = DateTime.UtcNow };
                    string existingOtp = new Random().Next(100000, 999999).ToString();
                    existingUser.Security.OtpCode = existingOtp;
                    existingUser.Security.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
                    
                    await _unitOfWork.SaveChangesAsync();
                    await _emailService.SendOtpEmailAsync(existingUser.Email, existingUser.FirstName, existingOtp);
                    
                    return new RegisterResponseDto { Message = "OTP sent to email.", RequiresOtp = true, Email = existingUser.Email };
                }
                throw new Exception("A user with this email already exists.");
            }

            string otp = new Random().Next(100000, 999999).ToString();

            User user = new()
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName ?? string.Empty,
                Email = userDto.Email,
                PasswordHash = _passwordService.HashPassword(userDto.Password),
                Role = userDto.Role,
                IsActive = true,
                Profile = new UserProfile
                {
                    Phone = userDto.Phone ?? string.Empty
                },
                Security = new UserSecurity
                {
                    IsEmailVerified = false,
                    OtpCode = otp,
                    OtpExpiry = DateTime.UtcNow.AddMinutes(10)
                }
            };

            await _authRepository.AddUserAsync(user);
            await _userRepository.LinkUserToGymMembersAsync(user);
            await _unitOfWork.SaveChangesAsync();

            await _emailService.SendOtpEmailAsync(user.Email, user.FirstName, otp);

            return new RegisterResponseDto { Message = "OTP sent to email.", RequiresOtp = true, Email = user.Email };
        }

        public async Task<TokenResponseDto> VerifyOtpAsync(VerifyOtpRequestDto dto)
        {
            User? user = await _authRepository.GetByUserByEmailAsync(dto.Email);
            if (user == null)
                throw new Exception("User not found.");
            
            if (user.Security == null)
                throw new Exception("Invalid security state.");

            if (user.Security.IsEmailVerified)
                throw new Exception("Email is already verified.");
                
            if (user.Security.OtpCode != dto.OtpCode || user.Security.OtpExpiry < DateTime.UtcNow)
                throw new Exception("Invalid or expired OTP code.");
                
            user.Security.IsEmailVerified = true;
            user.Security.OtpCode = null;
            user.Security.OtpExpiry = null;
            
            await _unitOfWork.SaveChangesAsync();
            
            return await GenerateAndSaveTokens(user);
        }

        public async Task<bool> ResendOtpAsync(ResendOtpRequestDto dto)
        {
            User? user = await _authRepository.GetByUserByEmailAsync(dto.Email);
            if (user == null)
                throw new Exception("User not found.");
                
            if (user.Security == null)
                throw new Exception("Invalid security state.");

            if (user.Security.IsEmailVerified)
                throw new Exception("Email is already verified.");
                
            string otp = new Random().Next(100000, 999999).ToString();
            user.Security.OtpCode = otp;
            user.Security.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            
            await _unitOfWork.SaveChangesAsync();
            await _emailService.SendOtpEmailAsync(user.Email, user.FirstName, otp);
            
            return true;
        }

        public async Task<TokenResponseDto> Login(LoginRequestDto userDto)
        {
            User? user = await _authRepository.Login(userDto);

            if (user == null)
                throw new Exception("Invalid credentials");

            if (user.Security == null || !user.Security.IsEmailVerified)
                throw new Exception("EMAIL_NOT_VERIFIED");

            bool validPassword = _passwordService.VerifyPasword(userDto.Password, user.PasswordHash!);

            if (!validPassword)
                throw new Exception("Invalid credentials");

            return await GenerateAndSaveTokens(user);
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            User? user = await _authRepository.GetByRefreshTokenAsync(dto.RefreshToken);
            RefreshToken? refreshToken = user?.RefreshTokens.FirstOrDefault(x => x.Token == dto.RefreshToken);

            if (user == null || refreshToken == null || !refreshToken.IsActive)
                throw new UnauthorizedAccessException("Invalid or expired refresh token");

            if (refreshToken.Revoked != null && !string.IsNullOrEmpty(refreshToken.ReplacedByToken))
            {
                RefreshToken? replacement = user.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken.ReplacedByToken);
                if (replacement != null && replacement.IsActive)
                {
                    Guid? branchId = await ResolveBranchIdAsync(user);
                    return new TokenResponseDto
                    {
                        AccessToken = _jwtService.GenerateToken(user, branchId).AccessToken,
                        RefreshToken = replacement.Token
                    };
                }
            }

            Guid? activeBranchId = await ResolveBranchIdAsync(user);
            TokenResponseDto newTokens = _jwtService.GenerateToken(user, activeBranchId);
            
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.GracePeriodExpires = DateTime.UtcNow.AddSeconds(60);
            refreshToken.ReplacedByToken = newTokens.RefreshToken;

            return await SaveTokens(user, newTokens);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequestDto dto)
        {
            User? user = await _authRepository.GetByUserByEmailAsync(dto.Email);
            if (user == null)
                return;
                
            string token = Guid.NewGuid().ToString("N");
            user.Security ??= new UserSecurity { CreatedOn = DateTime.UtcNow };
            user.Security.OtpCode = token;
            user.Security.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            
            await _unitOfWork.SaveChangesAsync();
            await _emailService.SendPasswordResetLinkEmailAsync(user.Email, user.FirstName, token);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequestDto dto)
        {
            User? user = await _authRepository.GetByUserByEmailAsync(dto.Email);
            if (user == null)
                throw new Exception("Invalid or expired reset token.");

            if (user.Security == null || user.Security.OtpCode != dto.Token || user.Security.OtpExpiry < DateTime.UtcNow)
                throw new Exception("Invalid or expired reset token.");

            user.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
            user.Security.OtpCode = null;
            user.Security.OtpExpiry = null;
            
            user.RefreshTokens.Clear();

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task LogoutAsync(string refreshToken)
        {
            User? user = await _authRepository.GetByRefreshTokenAsync(refreshToken);
            RefreshToken? token = user?.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken);

            if (token != null)
            {
                user!.RefreshTokens.Remove(token);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task RequestAccountDeletionAsync(Guid userId)
        {
            User? user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            user.Security ??= new UserSecurity { CreatedOn = DateTime.UtcNow };
            user.Security.DeletionRequestedOn = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<Guid?> ResolveBranchIdAsync(User user)
        {
            if (user.Role == UserRole.Staff || user.Role == UserRole.Trainer)
            {
                return await _userRepository.GetBranchIdByUserIdAsync(user.Id);
            }
            return null;
        }

        private async Task<TokenResponseDto> GenerateAndSaveTokens(User user)
        {
            Guid? branchId = await ResolveBranchIdAsync(user);
            TokenResponseDto tokenResponse = _jwtService.GenerateToken(user, branchId);
            return await SaveTokens(user, tokenResponse);
        }

        private async Task<TokenResponseDto> SaveTokens(User user, TokenResponseDto tokenResponse)
        {
            RefreshToken refreshToken = new()
            {
                Token = tokenResponse.RefreshToken,
                Expires = _jwtService.GetRefreshTokenExpiry(),
                UserId = user.Id,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = user.Id
            };

            user.RefreshTokens.Add(refreshToken);
            
            List<RefreshToken> staleTokens = [.. user.RefreshTokens.Where(t => t.Id != Guid.Empty && !t.IsActive)];

            foreach (RefreshToken stale in staleTokens)
            {
                user.RefreshTokens.Remove(stale);
            }

            List<RefreshToken> liveTokens = [.. user.RefreshTokens
                .Where(t => t.IsActive && t.Revoked == null)
                .OrderByDescending(t => t.CreatedOn)];

            foreach (RefreshToken oldToken in liveTokens.Skip(MaxActiveSessions))
            {
                user.RefreshTokens.Remove(oldToken);
            }

            await _unitOfWork.SaveChangesAsync();

            return tokenResponse;
        }
    }
}
