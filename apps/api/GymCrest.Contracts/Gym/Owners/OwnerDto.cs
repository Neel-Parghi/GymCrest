namespace GymCrest.Contracts.Gym.Owners
{
    public class OwnerDto
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;

        public bool SendInvite { get; set; } = true;
    }
}
