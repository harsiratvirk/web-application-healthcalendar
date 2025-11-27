using System.ComponentModel.DataAnnotations;

namespace HealthCalendar.DTOs
{
    public class UserDTO
    {
        // Primary Key
        public string Id {get; set;} = string.Empty;
        
        // Works as email, since Username is non-nullable for IdentityUser
        [Required]
        [EmailAddress]
        public string UserName {get; set;} = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;
        
        // Role can be "Patient", "Worker" or "Usermanager"
        [Required]
        public string Role { get; set; } = string.Empty;
        
        // Foreign Key (User.Id)
        // For Patient, Points to related Worker
        public string? WorkerId { get; set; } 
    }
}