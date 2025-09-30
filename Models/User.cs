using System.ComponentModel.DataAnnotations;

namespace MedicalAppointmentSystem.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Role { get; set; } // "admin", "doctor", "patient"

        public string? Name { get; set; }
    }
}