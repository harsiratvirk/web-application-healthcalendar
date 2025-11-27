using System.ComponentModel.DataAnnotations;

namespace HealthCalendar.DTOs
{
    // DTO used when a user is registered
    public class RegisterDTO
    {
        [Required]
        [StringLength(100,MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}