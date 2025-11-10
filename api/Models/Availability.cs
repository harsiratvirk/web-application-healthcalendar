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
        // Specifies sepcific date, overrides overlapping Availability where Date is null
        public DateOnly? Date { get; set; }

        // Foreign Key (User.Id)
        public string UserId { get; set; } = string.Empty;
        // Navigation Property
        public virtual User Worker { get; set; } = default!;

        // Foreign Key (Event.EventId)
        public int EventId { get; set; }
        // Navigation property
        public virtual Event Event { get; set; } = default!;
    }
}