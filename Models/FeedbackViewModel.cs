// Add to MedicalAppointmentSystem.ViewModels
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MedicalAppointmentSystem.ViewModels
{
    public class FeedbackViewModel
    {
        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        public string? Comment { get; set; }

        public int? SelectedDoctorId { get; set; }

        // For dropdown lists
        public List<SelectListItem> Doctors { get; set; } = new List<SelectListItem>();
    }
}