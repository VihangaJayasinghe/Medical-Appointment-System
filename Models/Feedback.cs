// Update your Feedback model to make DoctorId nullable
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MedicalAppointmentSystem.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Patient")]
        public int PatientId { get; set; }
        public virtual Patient? Patient { get; set; }

        [ForeignKey("Doctor")]
        public int? DoctorId { get; set; } // Changed to nullable
        public virtual Doctor? Doctor { get; set; }

        public int Rating { get; set; } // Changed to non-nullable (required)

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } // Changed to non-nullable
    }
}