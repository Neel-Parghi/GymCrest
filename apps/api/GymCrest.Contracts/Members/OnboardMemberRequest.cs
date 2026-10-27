using System.ComponentModel.DataAnnotations;
using GymCrest.Contracts.Gym.Shared;
using GymCrest.Shared.Enums;

namespace GymCrest.Contracts.Members
{
    public record OnboardMemberRequest(
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        DateTime DateOfBirth,
        Gender Gender,
        string? EmergencyContactName,
        string? EmergencyContactPhone,
        string? BloodGroup,
        AddressDto? Address,
        [MaxLength(2000)] string? MedicalConditions,
        List<string>? FitnessGoals,
        Guid GymPlanId,
        DateTime? StartDate = null,
        PaymentStatus? PaymentStatus = null,
        Guid? BranchId = null,
        decimal? AmountPaid = null
    );
}
