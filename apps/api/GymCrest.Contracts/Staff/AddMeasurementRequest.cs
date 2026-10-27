using System.ComponentModel.DataAnnotations;
namespace GymCrest.Contracts.Staff
{
    public class AddMeasurementRequest
    {
        public double? Weight { get; set; }
        public double? Height { get; set; }
        public double? BodyFatPercentage { get; set; }
        public double? BMI { get; set; }
        public bool IsAdvanced { get; set; }
        public double? Neck { get; set; }
        public double? Shoulders { get; set; }
        public double? Chest { get; set; }
        public double? LeftBicep { get; set; }
        public double? RightBicep { get; set; }
        public double? LeftForearm { get; set; }
        public double? RightForearm { get; set; }
        public double? UpperAbs { get; set; }
        public double? LowerAbs { get; set; }
        public double? Waist { get; set; }
        public double? Hips { get; set; }
        public double? LeftThigh { get; set; }
        public double? RightThigh { get; set; }
        public double? LeftCalf { get; set; }
        public double? RightCalf { get; set; }
        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}
