using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalAppointmentSystem.Models
{
    public class Doctor
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User? User { get; set; }

        public string? Specialty { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ConsultationFee { get; set; }

        public string? Bio { get; set; }

        public virtual ICollection<DoctorAvailability> DoctorAvailabilities { get; set; } = new List<DoctorAvailability>();
    }
}