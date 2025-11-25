using System.ComponentModel.DataAnnotations;

namespace HealthCalendar.DTOs
{
    // Inherits all properties of EventDTO
    public class EventWithOwnerDTO : EventDTO
    {   
        [Required]
        public string OwnerName { get; set; } = string.Empty;
    }
}