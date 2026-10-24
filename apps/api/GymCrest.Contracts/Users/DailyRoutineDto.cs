using System;

namespace GymCrest.Contracts.Users
{
    public class DailyRoutineDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Time { get; set; }
        public string? Amount { get; set; }
        public bool Completed { get; set; }
        public int Order { get; set; }
    }

    public class CreateDailyRoutineDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Time { get; set; }
        public string? Amount { get; set; }
        public int Order { get; set; }
    }

    public class UpdateDailyRoutineDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Time { get; set; }
        public string? Amount { get; set; }
        public int Order { get; set; }
    }
}
