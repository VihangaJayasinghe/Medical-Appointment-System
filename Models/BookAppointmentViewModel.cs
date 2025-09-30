using System;
using System.ComponentModel.DataAnnotations;
using MedicalAppointmentSystem.Models;

namespace MedicalAppointmentSystem.ViewModels
{
    public class BookAppointmentViewModel
    {
        // Step 1: Search criteria
        public string? Specialty { get; set; }
        public string? Location { get; set; }
        public string? Day { get; set; }

        // Step 2: Doctor selection
        public int? DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public decimal ConsultationFee { get; set; }

        // Step 3: Date and time selection
        [Required(ErrorMessage = "Please select a date")]
        [DataType(DataType.Date)]
        public DateTime? AppointmentDate { get; set; }

        [Required(ErrorMessage = "Please select a time")]
        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }

        public string? SelectedLocation { get; set; }
    }

    public class DoctorSearchViewModel
    {
        public string Specialty { get; set; }
        public string Location { get; set; }
        public string Day { get; set; }
        public List<Doctor> Doctors { get; set; } = new List<Doctor>();
    }
}