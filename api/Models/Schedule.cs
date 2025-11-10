
namespace HealthCalendar.Models
{
    public class Schedule
    {
        // Primary Key
        public int ScheduleId { get; set; }

        // Foreign Key (Availability.AvailabilityId)
        public int AvailabilityId { get; set; }
        // Navigation property
        public virtual Availability Availability { get; set; } = default!;

        // Foreign Key (Event.EventId)
        public int EventId { get; set; }
        // Navigation property
        public virtual Event Event { get; set; } = default!;
    }
}