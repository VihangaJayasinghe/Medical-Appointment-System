// Add to MedicalAppointmentSystem.ViewModels
using MedicalAppointmentSystem.Models;

namespace MedicalAppointmentSystem.ViewModels
{
    public class DoctorDashboardViewModel
    {
        public Doctor Doctor { get; set; }
        public int TotalAppointments { get; set; }
        public int UpcomingAppointments { get; set; }
        public int TodayAppointments { get; set; }
        public List<Appointment> RecentAppointments { get; set; } = new List<Appointment>();
    }
}