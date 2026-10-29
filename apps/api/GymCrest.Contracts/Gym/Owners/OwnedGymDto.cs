namespace GymCrest.Contracts.Gym.Owners
{
    public class OwnedGymDto
    {
        public Guid Id { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string? City { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public int BranchesCount { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
