using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalAppointmentSystem.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User? User { get; set; }

        public int? Age { get; set; }

        public string? Phone { get; set; }

        // Navigation properties
        //public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}