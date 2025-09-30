using System.ComponentModel.DataAnnotations;

namespace MedicalAppointmentSystem.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        public string Name { get; set; }

        public int? Age { get; set; }

        [Phone]
        public string Phone { get; set; }
    }
}