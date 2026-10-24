using AutoMapper;
using GymCrest.Application.Modules.Payments.Interfaces;
using GymCrest.Contracts.Payments;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Shared.Enums;
using GymCrest.Shared.Models;
using Microsoft.Extensions.Configuration;
using Razorpay.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GymCrest.Application.Modules.Payments.Services
{
    public class MemberPaymentService : IMemberPaymentService
    {
        private readonly IGymMemberRepository _gymMemberRepository;
        private readonly IGymManagementRepository _gymManagementRepository;
        private readonly IGymPlanRepository _gymPlanRepository;
        private readonly IUnitOfWork _uow;

        public MemberPaymentService(
            IGymMemberRepository gymMemberRepository,
            IGymManagementRepository gymManagementRepository,
            IGymPlanRepository gymPlanRepository,
            IUnitOfWork uow)
        {
            _gymMemberRepository = gymMemberRepository;
            _gymManagementRepository = gymManagementRepository;
            _gymPlanRepository = gymPlanRepository;
            _uow = uow;
        }

        public async Task<ApiResponse<PaymentInitiationResponse>> InitiateCheckoutAsync(InitiateMemberPaymentRequest request)
        {
            GymMember? member = await _gymMemberRepository.GetByUserIdAsync(request.UserId);
            if (member == null) return new ApiResponse<PaymentInitiationResponse> { Success = false, Message = "Member not found." };

            var gym = await _gymManagementRepository.GetGymByIdAsync(member.GymId);
            if (gym == null) return new ApiResponse<PaymentInitiationResponse> { Success = false, Message = "Gym not found." };

            GymConfiguration? config = await _gymManagementRepository.GetGymConfigurationAsync(gym.Id);
            if (config == null || string.IsNullOrEmpty(config.RazorpayKeyId) || string.IsNullOrEmpty(config.RazorpayKeySecret))
            {
                return new ApiResponse<PaymentInitiationResponse> { Success = false, Message = "Gym has not configured payment gateway." };
            }

            GymPlan? plan = await _gymPlanRepository.GetPlanByIdAsync(request.PlanId);
            if (plan == null) return new ApiResponse<PaymentInitiationResponse> { Success = false, Message = "Plan not found." };

            decimal amountToPay = plan.IsOffer && plan.DiscountedPrice.HasValue ? plan.DiscountedPrice.Value : plan.Price;

            RazorpayClient client = new RazorpayClient(config.RazorpayKeyId, config.RazorpayKeySecret);
            Dictionary<string, object> options = new Dictionary<string, object>
            {
                { "amount", (int)(amountToPay * 100) },
                { "currency", "INR" },
                { "receipt", Guid.NewGuid().ToString() }
            };

            Order order = client.Order.Create(options);
            string razorpayOrderId = order["id"].ToString();

            return new ApiResponse<PaymentInitiationResponse> {
                Success = true,
                Data = new PaymentInitiationResponse
                {
                    OrderId = razorpayOrderId,
                    Amount = amountToPay,
                    Currency = "INR",
                    KeyId = config.RazorpayKeyId
                }
            };
        }

        public async Task<ApiResponse<PaymentVerificationResponse>> VerifyCheckoutAsync(VerifyMemberPaymentRequest request)
        {
            GymMember? member = await _gymMemberRepository.GetByUserIdAsync(request.UserId);
            if (member == null) return new ApiResponse<PaymentVerificationResponse> { Success = false, Message = "Member not found." };

            GymConfiguration? config = await _gymManagementRepository.GetGymConfigurationAsync(member.GymId);
            if (config == null || string.IsNullOrEmpty(config.RazorpayKeyId) || string.IsNullOrEmpty(config.RazorpayKeySecret))
            {
                return new ApiResponse<PaymentVerificationResponse> { Success = false, Message = "Gym has not configured payment gateway." };
            }

            try
            {
                Dictionary<string, string> attributes = new Dictionary<string, string>
                {
                    { "razorpay_order_id", request.RazorpayOrderId },
                    { "razorpay_payment_id", request.RazorpayPaymentId },
                    { "razorpay_signature", request.RazorpaySignature }
                };

                Utils.verifyPaymentSignature(attributes);
            }
            catch (Exception)
            {
                return new ApiResponse<PaymentVerificationResponse> { Success = false, Message = "Signature verification failed." };
            }

            GymPlan? plan = await _gymPlanRepository.GetPlanByIdAsync(request.PlanId);
            if (plan == null) return new ApiResponse<PaymentVerificationResponse> { Success = false, Message = "Plan not found." };

            decimal amountPaid = plan.IsOffer && plan.DiscountedPrice.HasValue ? plan.DiscountedPrice.Value : plan.Price;
            int totalMonths = plan.DurationMonths + (plan.IsOffer && plan.ExtendedMonths.HasValue ? plan.ExtendedMonths.Value : 0);

            MemberSubscription subscription = new MemberSubscription
            {
                Id = Guid.NewGuid(),
                MemberId = member.Id,
                GymPlanId = plan.Id,
                PlanNameSnapshot = plan.Name,
                PricePaid = amountPaid,
                AmountPaid = amountPaid,
                DurationMonths = plan.DurationMonths,
                ExtendedMonths = plan.IsOffer && plan.ExtendedMonths.HasValue ? plan.ExtendedMonths.Value : 0,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(totalMonths),
                IsActive = true,
                IsComplementary = false,
                PaymentStatus = PaymentStatus.Paid,
                CreatedOn = DateTime.UtcNow
            };

            await _gymMemberRepository.AddSubscriptionAsync(subscription);
            await _uow.SaveChangesAsync();

            return new ApiResponse<PaymentVerificationResponse> {
                Success = true,
                Data = new PaymentVerificationResponse
                {
                    IsSuccess = true,
                    SubscriptionId = subscription.Id
                }
            };
        }

        public async Task<ApiResponse<OfflineCheckoutResponse>> InitiateOfflineCheckoutAsync(OfflineCheckoutRequest request)
        {
            GymMember? member = await _gymMemberRepository.GetByUserIdAsync(request.UserId);
            if (member == null) return new ApiResponse<OfflineCheckoutResponse> { Success = false, Message = "Member not found." };

            GymPlan? plan = await _gymPlanRepository.GetPlanByIdAsync(request.PlanId);
            if (plan == null) return new ApiResponse<OfflineCheckoutResponse> { Success = false, Message = "Plan not found." };

            decimal amountPaid = plan.IsOffer && plan.DiscountedPrice.HasValue ? plan.DiscountedPrice.Value : plan.Price;
            int totalMonths = plan.DurationMonths + (plan.IsOffer && plan.ExtendedMonths.HasValue ? plan.ExtendedMonths.Value : 0);

            MemberSubscription subscription = new MemberSubscription
            {
                Id = Guid.NewGuid(),
                MemberId = member.Id,
                GymPlanId = plan.Id,
                PlanNameSnapshot = plan.Name,
                PricePaid = amountPaid,
                AmountPaid = 0,
                DurationMonths = plan.DurationMonths,
                ExtendedMonths = plan.IsOffer && plan.ExtendedMonths.HasValue ? plan.ExtendedMonths.Value : 0,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(totalMonths),
                IsActive = true,
                IsComplementary = false,
                PaymentStatus = PaymentStatus.Pending,
                CreatedOn = DateTime.UtcNow
            };

            await _gymMemberRepository.AddSubscriptionAsync(subscription);
            await _uow.SaveChangesAsync();

            return new ApiResponse<OfflineCheckoutResponse> {
                Success = true,
                Data = new OfflineCheckoutResponse
                {
                    IsSuccess = true,
                    SubscriptionId = subscription.Id
                }
            };
        }
    }
}
