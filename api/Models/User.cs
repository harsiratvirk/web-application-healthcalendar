using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using HealthCalendar.Shared;

namespace HealthCalendar.Models
{
    public class User : IdentityUser
    {
        /* 
            Inherits from IdentityUser,
            Properties from IdentityUser that User also uses are:
            Id (Primary key),
            Email,
            PasswordHash
        */
        public string Name { get; set; } = string.Empty;
        public Role Role { get; set; }
        
        // Foreign Key (User.Id)
        // For Patient, Points to related Worker
        string? WorkerId { get; set; } 
        // Navigation property
        [ForeignKey("WorkerId")]
        public virtual User? Worker { get; set; }
    }
}