namespace GymForge.Shared.Enums
{
    /// <summary>Records whose access is checked by id against the caller's gym or account.</summary>
    public enum OwnedResource
    {
        Staff,
        InventoryItem,
        SaleTransaction,
        Equipment,
        MaintenanceLog,
        GymPlan,
        Branch,
        WorkoutPlan,
        DietPlan,
        DailyRoutine
    }
}
