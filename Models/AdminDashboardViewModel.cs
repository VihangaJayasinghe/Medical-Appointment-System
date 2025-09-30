using System.Collections.Generic;

namespace MedicalAppointmentSystem.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalDoctors { get; set; }
        public int TotalPatients { get; set; }
        public int TotalAppointments { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Appointment> RecentAppointments { get; set; } = new List<Appointment>();
    }
}