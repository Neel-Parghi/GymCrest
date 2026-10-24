namespace GymCrest.Contracts.Gym.Shared
{
    public class AddressDto
    {
        public string Line1 { get; set; } = null!;
        public string? Line2 { get; set; }

        public string City { get; set; } = null!;
        public string State { get; set; } = null!;
        public string Country { get; set; } = null!;
        public string PostalCode { get; set; } = null!;
    }
}
