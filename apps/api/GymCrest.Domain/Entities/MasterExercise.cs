using System;

namespace GymCrest.Domain.Entities
{
    public class MasterExercise : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        
        public string Slug { get; set; } = string.Empty;
        
        public string Category { get; set; } = string.Empty; // e.g. Back, Chest, Legs
        
        public string? SubCategory { get; set; } // e.g. Lats, Upper Chest, Quads
        
        public string Equipment { get; set; } = string.Empty; // e.g. Barbell, Dumbbell, Cable, Bodyweight
        
        public string? Force { get; set; } // Push, Pull, Static
        
        public string Level { get; set; } = "Beginner"; // Beginner, Intermediate, Advanced
        
        public string? Description { get; set; }
        
        public string? Instructions { get; set; } // Semi-colon separated steps
        
        public string? ImageUrl { get; set; }
    }
}
