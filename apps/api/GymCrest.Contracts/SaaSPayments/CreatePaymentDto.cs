namespace GymCrest.Contracts.SaaSPayments
{
    public class CreatePaymentDto
    {
        public Guid GymId { get; set; }
        public Guid PlanId { get; set; }
    }
}
