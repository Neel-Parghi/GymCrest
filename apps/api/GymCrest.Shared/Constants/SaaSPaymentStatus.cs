namespace GymCrest.Shared.Constants
{
    /// <summary>Status values stored on SaaS payment transactions.</summary>
    public static class SaaSPaymentStatus
    {
        public const string Pending = "Pending";
        public const string Paid = "Paid";
        public const string Failed = "Failed";

        /// <summary>Granted by a SuperAdmin without a gateway payment (comps, corrections).</summary>
        public const string Manual = "Manual";
    }
}
