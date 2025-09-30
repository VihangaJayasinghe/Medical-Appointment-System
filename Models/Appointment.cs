using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalAppointmentSystem.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Patient")]
        public int PatientId { get; set; }
        public virtual Patient? Patient { get; set; }

        [ForeignKey("Doctor")]
        public int DoctorId { get; set; }
        public virtual Doctor? Doctor { get; set; }

        public DateTime? AppointmentDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public string? Location { get; set; }

        public string? Status { get; set; } // "booked", "confirmed", "completed", "cancelled"

        //public virtual ICollection<Payment> Payments { get; set; }
    }
}