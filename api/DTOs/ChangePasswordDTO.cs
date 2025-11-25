using System.ComponentModel.DataAnnotations;

namespace HealthCalendar.DTOs
{
    // DTO used when a user tries to change password
    public class ChangePasswordDTO
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
        
        [Required]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        public string NewPasswordRepeated { get; set; } = string.Empty;
    }
}