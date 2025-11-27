using System.ComponentModel.DataAnnotations;
using HealthCalendar.Models;

namespace HealthCalendar.DTOs
{
    // DTO representing Availability model
    public class AvailabilityDTO
    {
        // Primary Key
        public int AvailabilityId { get; set; }
        [Required]
        public TimeOnly From { get; set; }
        [Required]
        public TimeOnly To { get; set; }

        // Specifies Day of Week 
        [Required]
        [Range(0,6)]
        public DayOfWeek DayOfWeek { get; set; }
        
        // Specifies sepcific date, overrides overlapping Availability where Date is null
        public DateOnly? Date { get; set; }

        // Foreign Key (User.Id)
        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}