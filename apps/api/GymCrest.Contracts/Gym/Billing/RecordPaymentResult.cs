namespace GymCrest.Contracts.Gym.Billing
{
    public class RecordPaymentResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public static RecordPaymentResult Ok() => new() { Success = true };
        public static RecordPaymentResult Fail(string message) => new() { Success = false, ErrorMessage = message };
    }
}
