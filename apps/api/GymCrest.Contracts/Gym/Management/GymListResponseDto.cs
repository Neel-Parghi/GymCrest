using System;
using GymCrest.Contracts.Gym.Shared;

namespace GymCrest.Contracts.Gym.Management
{
    public class GymListResponseDto
    {
        public Guid Id { get; set; }
        
        // Grid Display Fields
        public string GymName { get; set; } = string.Empty;
        public string? BrandName { get; set; }
        public string? OwnerName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int BranchesCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }

        // Extra Detail Fields
        public string? Description { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? GstNumber { get; set; }
        public string? RegistrationNumber { get; set; }
        public DateTime? EstablishedDate { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public AddressDto? Address { get; set; }

        // Owner Link
        public Guid OwnerId { get; set; }
        public string? OwnerEmail { get; set; }
        public string? OwnerPhone { get; set; }
        public bool OwnerIsActive { get; set; }

        // People Counts
        public int MembersCount { get; set; }
        public int StaffCount { get; set; }

        // Gym Tax and Billing Configuration Settings
        public string? InvoicePrefix { get; set; }
        public decimal DefaultTaxRate { get; set; }
        public int OverdueGraceDays { get; set; }
        public bool AutoEmailReceipts { get; set; }

        // Subscription Information
        public string? PlanName { get; set; }
        public DateTime? SubscriptionExpiry { get; set; }
        public DateTime? SubscriptionStart { get; set; }
        public bool IsTrialPlan { get; set; }
        public string? PaymentStatus { get; set; }
        public bool HasActiveSubscription => !string.IsNullOrEmpty(PlanName);
        
        public bool AwaitingFirstPayment { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }
}
