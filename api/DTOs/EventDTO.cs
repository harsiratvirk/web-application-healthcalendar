using System.ComponentModel.DataAnnotations;

namespace HealthCalendar.DTOs
{
    public class EventDTO
    {
        // Primary Key
        public int EventId { get; set; }
        
        [Required]
        public TimeOnly From { get; set; }

        [Required]
        public TimeOnly To { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        [StringLength(30,MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(30,MinimumLength = 1)]
        public string Location { get; set; } = string.Empty;

        // Foreign Key (User.Id)
        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}