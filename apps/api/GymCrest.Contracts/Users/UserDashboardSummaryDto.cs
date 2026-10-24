using System;
using System.Collections.Generic;

namespace GymCrest.Contracts.Users
{
    public class UserDashboardSummaryDto
    {
        // Header info
        public string Greeting { get; set; } = "Good morning";
        public string UserName { get; set; } = string.Empty;
        public bool ShowGymBuddy { get; set; } = true;
        
        // Goals & Targets
        public string? GoalTitle { get; set; } 
        public int GoalProgressPct { get; set; } 
        public double? TargetWeight { get; set; }
        public int TargetCalories { get; set; }
        public int TargetTrainingTime { get; set; } // in minutes
        public int TargetCaloriesBurn { get; set; } 
        
        // Today's Activity
        public int CaloriesBurnedToday { get; set; }
        public int ActiveTrainingTimeMinutes { get; set; }
        public int CardioMinutesToday { get; set; }
        public int StrengthSetsToday { get; set; }
        public int WorkoutStreak { get; set; }
        public bool StreakAtRisk { get; set; }
        
        // Monthly stats
        public int MonthlySessionCount { get; set; }
        public int MonthlySessionTarget { get; set; }
        public int MonthlyCompletionPct { get; set; }
        
        // Body Composition
        public double CurrentWeight { get; set; }
        public double BodyFat { get; set; }
        public double BMI { get; set; }
        
        // Active Plans
        public ActivePlanSummaryDto? ActiveWorkoutPlan { get; set; }
        public ActivePlanSummaryDto? ActiveDietPlan { get; set; }
        
        public List<PersonalRecordDto> PersonalRecords { get; set; } = new();
        public List<MuscleRecoveryDto> MuscleRecovery { get; set; } = new();
        
        public List<DailyRoutineDto> DailyRoutines { get; set; } = new();
        public List<MuscleHeatmapDto> MuscleHeatmap { get; set; } = new();
    }

    public class ActivePlanSummaryDto
    {
        public Guid PlanId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class PersonalRecordDto
    {
        public string Name { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
    }

    public class MuscleRecoveryDto
    {
        public string Name { get; set; } = string.Empty;
        public int Pct { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class MuscleHeatmapDto
    {
        public string Name { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
