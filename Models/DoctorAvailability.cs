using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalAppointmentSystem.Models
{
    public class DoctorAvailability
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Doctor")]
        public int DoctorId { get; set; }
        public virtual Doctor? Doctor { get; set; }

        public string? Day { get; set; } // "Monday", "Tuesday", etc.

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public string? Location { get; set; }
    }
}