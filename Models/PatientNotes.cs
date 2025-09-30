using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalAppointmentSystem.Models
{
    public class PatientNotes
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Appointment")]
        public int AppointmentId { get; set; }
        public virtual Appointment? Appointment { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? Prescription { get; set; }
    }
}