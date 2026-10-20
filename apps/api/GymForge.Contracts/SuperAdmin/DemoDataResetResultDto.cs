namespace GymForge.Contracts.SuperAdmin
{
    public class DemoDataResetResultDto
    {
        public string Email { get; set; } = string.Empty;
        public int WorkoutsLogged { get; set; }
        public int RestDaysLogged { get; set; }
        public int DaysOfMealsLogged { get; set; }
        public int MealEntriesLogged { get; set; }
        public int HealthCheckIns { get; set; }
    }
}
