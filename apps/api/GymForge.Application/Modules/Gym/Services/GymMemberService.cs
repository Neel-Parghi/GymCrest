using AutoMapper;
using GymForge.Application.Modules.Gym.Interfaces;
using GymForge.Contracts.Common;
using GymForge.Contracts.Members;
using GymForge.Domain.Entities;
using GymForge.Domain.Interface;
using GymForge.Shared.Enums;
using GymForge.Shared.Models;

namespace GymForge.Application.Modules.Gym.Services
{
    public class GymMemberService : IGymMemberService
    {
        private readonly IGymMemberRepository _memberRepository;
        private readonly IGymPlanRepository _planRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAddressRepository _addressRepository;
        private readonly IAuthRepository _authRepository;
        private readonly IEmailService _emailService;

        public GymMemberService(IGymMemberRepository memberRepository, IGymPlanRepository planRepository, IMapper mapper, IUnitOfWork unitOfWork, IAddressRepository addressRepository, IAuthRepository authRepository, IEmailService emailService)
        {
            _memberRepository = memberRepository;
            _planRepository = planRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _addressRepository = addressRepository;
            _authRepository = authRepository;
            _emailService = emailService;
        }

        public async Task<GymMemberResponse> OnboardMemberAsync(Guid gymId, OnboardMemberRequest request, Guid createdBy)
        {
            if (await _memberRepository.ExistsByEmailAsync(request.Email, gymId))
            {
                throw new InvalidOperationException("A member with this email already exists in your gym.");
            }

            GymPlan? planTemplate = await _planRepository.GetPlanByIdAsync(request.GymPlanId);
            if (planTemplate == null)
            {
                throw new KeyNotFoundException("The selected membership plan was not found.");
            }

            GymMember member = _mapper.Map<GymMember>(request);
            member.GymId = gymId;
            member.BranchId = request.BranchId;
            member.JoiningDate = request.StartDate ?? DateTime.UtcNow;
            member.Status = MemberStatus.Active;
            member.MembershipNumber = $"MEM-{DateTime.UtcNow.Ticks.ToString().Substring(10)}";
            member.CreatedBy = createdBy;
            member.CreatedOn = DateTime.UtcNow;
            
            if (member.Address != null)
            {
                member.Address.Id = Guid.NewGuid();
                member.Address.CreatedOn = DateTime.UtcNow;
            }

            User? existingUser = await _authRepository.GetByUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                member.UserId = existingUser.Id;
                if (existingUser.GymId == null) 
                {
                    existingUser.GymId = gymId;
                }
            }

            await _memberRepository.AddAsync(member);

            DateTime startDate = request.StartDate ?? DateTime.UtcNow;
            int duration = planTemplate.DurationMonths;
            int bonus = planTemplate.ExtendedMonths ?? 0;

            MemberSubscription subscription = new()
            {
                Member = member,
                GymPlanId = planTemplate.Id,
                PlanNameSnapshot = planTemplate.Name,
                PricePaid = planTemplate.IsOffer && planTemplate.DiscountedPrice.HasValue 
                            ? planTemplate.DiscountedPrice.Value 
                            : planTemplate.Price,
                DurationMonths = duration,
                ExtendedMonths = bonus,
                StartDate = startDate,
                EndDate = startDate.AddMonths(duration + bonus),
                IsActive = true,
                PaymentStatus = request.PaymentStatus ?? PaymentStatus.Paid,
                CreatedBy = createdBy,
                CreatedOn = DateTime.UtcNow
            };
            ApplyAmountPaid(subscription, request.AmountPaid);

            await _memberRepository.AddSubscriptionAsync(subscription);

            await _unitOfWork.SaveChangesAsync();

            if (existingUser != null)
            {
                await _emailService.SendEmailAsync(
                    existingUser.Email,
                    $"{existingUser.FirstName} {existingUser.LastName}",
                    "GymCrest: Gym Membership Linked",
                    $"<p>Hi {existingUser.FirstName},</p><p>A gym has just linked a new membership to your account! You can now log into your User Portal to view your gym membership, plans, and tracker.</p>"
                );
            }

            return _mapper.Map<GymMemberResponse>(member);
        }

        public async Task<PagedResponse<GymMemberResponse>> GetGymMembersAsync(Guid gymId, MemberFilterParams filter, Guid? branchId = null)
        {
            if (filter.BypassPagination == true)
            {
                IEnumerable<GymMember> allMembers = await _memberRepository.GetAllByGymIdAsync(gymId, branchId);
                
                // For simplicity, apply the same filters when bypass pagination is used
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    string term = filter.SearchTerm.ToLower();
                    allMembers = allMembers.Where(x => x.FirstName.ToLower().Contains(term) ||
                                                       x.LastName.ToLower().Contains(term) ||
                                                       x.Email.ToLower().Contains(term) ||
                                                       x.MembershipNumber.ToLower().Contains(term));
                }

                if (filter.Status.HasValue)
                {
                    allMembers = allMembers.Where(x => x.Status == filter.Status.Value);
                }

                if (filter.PlanId.HasValue || filter.PaymentStatus.HasValue)
                {
                    allMembers = allMembers.Where(x => x.Subscriptions.Any(s => s.IsActive &&
                        (!filter.PlanId.HasValue || s.GymPlanId == filter.PlanId.Value) &&
                        (!filter.PaymentStatus.HasValue || s.PaymentStatus == filter.PaymentStatus.Value)));
                }
                
                IEnumerable<GymMemberResponse> mapped = _mapper.Map<IEnumerable<GymMemberResponse>>(allMembers);
                int count = mapped.Count();
                return new PagedResponse<GymMemberResponse>(mapped, count, 1, count > 0 ? count : 1);
            }

            (IEnumerable<GymMember> members, int totalCount) = await _memberRepository.GetPagedMembersAsync(
                gymId,
                filter,
                branchId);

            IEnumerable<GymMemberResponse> items = _mapper.Map<IEnumerable<GymMemberResponse>>(members);

            return new PagedResponse<GymMemberResponse>(
                items,
                totalCount,
                filter.PageNumber,
                filter.PageSize);
        }

        public async Task<GymMemberResponse?> GetMemberByIdAsync(Guid id)
        {
            GymMember? member = await _memberRepository.GetByIdAsync(id);
            return member != null ? _mapper.Map<GymMemberResponse>(member) : null;
        }

        public async Task<GymMemberResponse> UpdateMemberAsync(Guid id, OnboardMemberRequest request, Guid updatedBy)
        {
            GymMember? member = await _memberRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Member not found");

            Guid? oldUserId = member.UserId;

            _mapper.Map(request, member);
            member.BranchId = request.BranchId;
            member.ModifiedBy = updatedBy;
            member.ModifiedOn = DateTime.UtcNow;

            User? existingUser = await _authRepository.GetByUserByEmailAsync(member.Email);
            if (existingUser != null)
            {
                member.UserId = existingUser.Id;
            }
            else
            {
                member.UserId = null;
            }
            
            bool isNewlyLinked = oldUserId == null && member.UserId != null;

            if (request.Address != null)
            {
                if (member.Address == null)
                {
                    member.Address = _mapper.Map<Address>(request.Address);
                    await _addressRepository.AddAsync(member.Address);
                }
                else
                {
                    _mapper.Map(request.Address, member.Address);
                }
            }

            if (request.PaymentStatus.HasValue)
            {
                MemberSubscription? activeSub = member.Subscriptions.OrderByDescending(s => s.CreatedOn).FirstOrDefault(s => s.IsActive);
                if (activeSub != null)
                {
                    activeSub.PaymentStatus = request.PaymentStatus.Value;
                    ApplyAmountPaid(activeSub, request.AmountPaid);
                    activeSub.ModifiedBy = updatedBy;
                    activeSub.ModifiedOn = DateTime.UtcNow;
                }
            }

            if (request.GymPlanId != Guid.Empty)
            {
                MemberSubscription? activeSub = member.Subscriptions.OrderByDescending(s => s.CreatedOn).FirstOrDefault(s => s.IsActive);
                if (activeSub != null && activeSub.GymPlanId != request.GymPlanId)
                {
                    GymPlan? planTemplate = await _planRepository.GetPlanByIdAsync(request.GymPlanId);
                    if (planTemplate != null)
                    {
                        activeSub.GymPlanId = planTemplate.Id;
                        activeSub.PlanNameSnapshot = planTemplate.Name;
                        activeSub.PricePaid = planTemplate.IsOffer && planTemplate.DiscountedPrice.HasValue
                                    ? planTemplate.DiscountedPrice.Value
                                    : planTemplate.Price;
                        activeSub.DurationMonths = planTemplate.DurationMonths;
                        activeSub.ExtendedMonths = planTemplate.ExtendedMonths ?? 0;
                        activeSub.EndDate = activeSub.StartDate.AddMonths(planTemplate.DurationMonths + (planTemplate.ExtendedMonths ?? 0));
                        ApplyAmountPaid(activeSub, request.AmountPaid);
                        activeSub.ModifiedBy = updatedBy;
                        activeSub.ModifiedOn = DateTime.UtcNow;
                    }
                }
                else if (activeSub == null)
                {
                    GymPlan? planTemplate = await _planRepository.GetPlanByIdAsync(request.GymPlanId);
                    if (planTemplate != null)
                    {
                        MemberSubscription newSub = new()
                        {
                            MemberId = id,
                            GymPlanId = planTemplate.Id,
                            PlanNameSnapshot = planTemplate.Name,
                            PricePaid = planTemplate.IsOffer && planTemplate.DiscountedPrice.HasValue
                                        ? planTemplate.DiscountedPrice.Value
                                        : planTemplate.Price,
                            DurationMonths = planTemplate.DurationMonths,
                            ExtendedMonths = planTemplate.ExtendedMonths ?? 0,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(planTemplate.DurationMonths + (planTemplate.ExtendedMonths ?? 0)),
                            IsActive = true,
                            PaymentStatus = request.PaymentStatus ?? PaymentStatus.Paid,
                            CreatedBy = updatedBy,
                            CreatedOn = DateTime.UtcNow
                        };
                        ApplyAmountPaid(newSub, request.AmountPaid);
                        await _memberRepository.AddSubscriptionAsync(newSub);
                    }
                }
            }

            await _memberRepository.UpdateAsync(member);
            await _unitOfWork.SaveChangesAsync();

            if (isNewlyLinked && existingUser != null)
            {
                await _emailService.SendEmailAsync(
                    existingUser.Email,
                    $"{existingUser.FirstName} {existingUser.LastName}",
                    "GymCrest: Gym Membership Linked",
                    $"<p>Hi {existingUser.FirstName},</p><p>A gym has just linked a new membership to your account! You can now log into your User Portal to view your gym membership, plans, and tracker.</p>"
                );
            }

            return _mapper.Map<GymMemberResponse>(member);
        }

        public async Task<bool> ToggleMemberStatusAsync(Guid id)
        {
            GymMember? member = await _memberRepository.GetByIdAsync(id);
            if (member == null) return false;

            member.Status = member.Status == MemberStatus.Active ? MemberStatus.Inactive : MemberStatus.Active;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> FreezeMemberAsync(Guid id, Guid updatedBy, DateTime freezeUntil)
        {
            GymMember? member = await _memberRepository.GetByIdAsync(id);
            if (member == null) return false;

            if (freezeUntil.Date <= DateTime.UtcNow.Date)
                throw new InvalidOperationException("Freeze end date must be in the future.");

            member.Status = MemberStatus.Freeze;
            member.FreezeStartDate = DateTime.UtcNow;
            member.FreezeUntil = freezeUntil;
            member.ModifiedBy = updatedBy;
            member.ModifiedOn = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnfreezeMemberAsync(Guid id, Guid updatedBy)
        {
            GymMember? member = await _memberRepository.GetByIdAsync(id);
            if (member == null) return false;

            MemberFreezeCalculator.Unfreeze(member, DateTime.UtcNow);
            member.ModifiedBy = updatedBy;
            member.ModifiedOn = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<GymMemberResponse> RenewSubscriptionAsync(Guid memberId, RenewSubscriptionRequest request, Guid updatedBy)
        {
            GymMember? member = await _memberRepository.GetByIdAsync(memberId)
                ?? throw new KeyNotFoundException("Member not found.");

            GymPlan? plan = await _planRepository.GetPlanByIdAsync(request.GymPlanId)
                ?? throw new KeyNotFoundException("The selected plan was not found.");

            await _memberRepository.DeactivateActiveSubscriptionsAsync(memberId);

            DateTime startDate = request.StartDate ?? DateTime.UtcNow;
            int duration = plan.DurationMonths;
            int bonus = plan.ExtendedMonths ?? 0;

            MemberSubscription renewal = new()
            {
                MemberId = memberId,
                GymPlanId = plan.Id,
                PlanNameSnapshot = plan.Name,
                PricePaid = plan.IsOffer && plan.DiscountedPrice.HasValue
                            ? plan.DiscountedPrice.Value
                            : plan.Price,
                DurationMonths = duration,
                ExtendedMonths = bonus,
                StartDate = startDate,
                EndDate = startDate.AddMonths(duration + bonus),
                IsActive = true,
                PaymentStatus = request.PaymentStatus ?? PaymentStatus.Paid,
                CreatedBy = updatedBy,
                CreatedOn = DateTime.UtcNow
            };
            ApplyAmountPaid(renewal, null);

            await _memberRepository.AddSubscriptionAsync(renewal);

            if (member.Status == MemberStatus.Expired || member.Status == MemberStatus.Inactive)
            {
                member.Status = MemberStatus.Active;
                member.ModifiedBy = updatedBy;
                member.ModifiedOn = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();

            GymMember? refreshed = await _memberRepository.GetByIdAsync(memberId);
            return _mapper.Map<GymMemberResponse>(refreshed!);
        }

        private static void ApplyAmountPaid(MemberSubscription subscription, decimal? requestedAmountPaid)
        {
            subscription.AmountPaid = subscription.PaymentStatus switch
            {
                PaymentStatus.Paid => subscription.PricePaid,
                PaymentStatus.Partial => ValidatePartialAmount(requestedAmountPaid, subscription.PricePaid),
                _ => 0m
            };
        }

        private static decimal ValidatePartialAmount(decimal? amountPaid, decimal price)
        {
            if (!amountPaid.HasValue || amountPaid.Value <= 0)
                throw new InvalidOperationException("Amount paid is required and must be greater than zero when payment status is Partial.");

            if (amountPaid.Value >= price)
                throw new InvalidOperationException("Partial payment amount must be less than the total plan price.");

            return amountPaid.Value;
        }

        public async Task<bool> DeleteMemberAsync(Guid id)
        {
            await _memberRepository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<MemberSubscriptionResponse>> GetSubscriptionHistoryAsync(Guid memberId)
        {
            GymMember? member = await _memberRepository.GetByIdAsync(memberId);

            if (member == null) 
                return Enumerable.Empty<MemberSubscriptionResponse>();

            IOrderedEnumerable<MemberSubscription> subscriptions = member.Subscriptions.OrderByDescending(s => s.StartDate);
            return _mapper.Map<IEnumerable<MemberSubscriptionResponse>>(subscriptions);
        }

        public async Task<IEnumerable<MemberSubscriptionResponse>> GetSubscriptionHistoryByUserIdAsync(Guid userId)
        {
            GymMember? member = await _memberRepository.GetByUserIdAsync(userId);

            if (member == null) 
                return Enumerable.Empty<MemberSubscriptionResponse>();

            IOrderedEnumerable<MemberSubscription> subscriptions = member.Subscriptions.OrderByDescending(s => s.StartDate);
            return _mapper.Map<IEnumerable<MemberSubscriptionResponse>>(subscriptions);
        }

        public async Task<byte[]> ExportMembersAsync(Guid gymId, Guid? branchId = null)
        {
            IEnumerable<GymMember> members = await _memberRepository.GetAllByGymIdAsync(gymId, branchId);
            
            using StringWriter sw = new();
            sw.WriteLine("MembershipID,FirstName,LastName,Email,Phone,Gender,Status,JoiningDate,CurrentPlan,PlanExpiry");

            foreach (GymMember m in members)
            {
                MemberSubscription? sub = m.Subscriptions.OrderByDescending(s => s.CreatedOn).FirstOrDefault(s => s.IsActive);
                sw.WriteLine($"{m.MembershipNumber},{m.FirstName},{m.LastName},{m.Email},{m.PhoneNumber},{m.Gender},{m.Status},{m.JoiningDate:yyyy-MM-dd},{sub?.PlanNameSnapshot ?? "N/A"},{sub?.EndDate:yyyy-MM-dd}");
            }

            return System.Text.Encoding.UTF8.GetBytes(sw.ToString());
        }

        public Task<MemberDashboardResponse> GetMemberDashboardDataAsync(Guid gymId, Guid? branchId = null)
        {
            return _memberRepository.GetMemberDashboardDataAsync(gymId, branchId);
        }

        public async Task<MyGymMembershipResponse?> GetMyGymMembershipAsync(Guid userId)
        {
            GymMember? member = await _memberRepository.GetByUserIdAsync(userId);
            if (member == null) return null;

            MemberSubscriptionResponse? currentSub = null;
            MemberSubscription? activeSub = member.Subscriptions.OrderByDescending(s => s.CreatedOn).FirstOrDefault(s => s.IsActive);
            if (activeSub != null)
            {
                currentSub = _mapper.Map<MemberSubscriptionResponse>(activeSub);
            }

            return new MyGymMembershipResponse
            {
                GymId = member.GymId,
                GymName = member.Gym?.GymName ?? "Unknown Gym",
                GymLogoUrl = member.Gym?.LogoUrl,
                MembershipNumber = member.MembershipNumber,
                Status = member.Status.ToString(),
                JoiningDate = member.JoiningDate,
                CurrentSubscription = currentSub
            };
        }
    }
}
