using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace HealthCalendar.Models
{
    public class User : IdentityUser
    {
        /* 
            Inherits from IdentityUser,
            Properties from IdentityUser that User also uses are:
            Id (Primary key),
            Username (Works as email, since Username is non-nullable),
            PasswordHash
        */
        public string Name { get; set; } = string.Empty;
        // Role can be "Patient", "Worker" or "Admin"
        public string Role { get; set; } = string.Empty;
        
        // Foreign Key (User.Id)
        // For Patient, Points to related Worker
        public string? WorkerId { get; set; } 
        // Navigation property
        [ForeignKey("WorkerId")]
        public virtual User? Worker { get; set; }
    }
}