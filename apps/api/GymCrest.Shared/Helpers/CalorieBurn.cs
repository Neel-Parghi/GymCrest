namespace GymCrest.Shared.Helpers
{
    public static class CalorieBurn
    {
        public const double StrengthMet = 5.0;
        public const double DefaultBodyWeightKg = 70.0;

        public static int AutoDailyTarget(double? bodyWeightKg, int trainingMinutes)
        {
            double weight = bodyWeightKg is double w && w > 0 ? w : DefaultBodyWeightKg;
            return (int)Math.Round(weight * StrengthMet * trainingMinutes / 60.0);
        }
    }
}
