using System;

namespace HealthCalendar.Models
{
    public class Availability
    {
        // Primary Key
        public int AvailabilityId { get; set; }
        public TimeOnly From { get; set; }
        public TimeOnly To { get; set; }

        // Specifies Day of Week 
        public DayOfWeek DayOfWeek { get; set; }
        // Specifies sepcific date, overrides corresponding DayOfWeek
        public DateOnly? Date { get; set; }

        // Foreign Key (User.Id)
        public int UserId { get; set; }
        // Navigation Property
        public virtual User Patient { get; set; } = default!;
    }
}