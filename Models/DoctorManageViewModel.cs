using System.ComponentModel.DataAnnotations;

namespace MedicalAppointmentSystem.Models
{
    public class DoctorManageViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Specialty is required")]
        public string Specialty { get; set; }

        [Required(ErrorMessage = "Consultation fee is required")]
        [Range(0, 1000, ErrorMessage = "Fee must be between 0 and 1000")]
        public decimal ConsultationFee { get; set; }

        public string Bio { get; set; }
    }
}