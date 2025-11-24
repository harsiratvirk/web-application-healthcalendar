using System.ComponentModel.DataAnnotations;
using HealthCalendar.Models;

namespace HealthCalendar.DTOs
{
    // DTO containing lists of AvailabilityIds generated for Event Update
    public class EventUpdateListsDTO
    {
        // AvailabilityIds for Schedules that need to be created
        [Required]
        public int[] ForCreateSchedules { get; set; } = [];

        // AvailabilityIds for Schedules that need to be deleted
        [Required]
        public int[] ForDeleteSchedules { get; set; } = [];

        // AvailabilityIds for Schedules that need to be updated
        [Required]
        public int[] ForUpdateSchedules { get; set; } = [];
    }
}