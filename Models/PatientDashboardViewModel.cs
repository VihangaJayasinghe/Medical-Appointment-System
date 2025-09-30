using MedicalAppointmentSystem.Models;
using System.Collections.Generic;

namespace MedicalAppointmentSystem.ViewModels
{
    public class PatientDashboardViewModel
    {
        public Patient Patient { get; set; }
        public int TotalAppointments { get; set; }
        public int UpcomingAppointments { get; set; }
        public int PendingPayments { get; set; }
        public List<Appointment> RecentAppointments { get; set; }
        public bool ShowFeedbackButton { get; set; } = true;
    }
}