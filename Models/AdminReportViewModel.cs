using System.Collections.Generic;

namespace MedicalAppointmentSystem.Models
{
    public class AdminReportViewModel
    {
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingPayments { get; set; }

        public Dictionary<string, int> AppointmentStats { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, decimal?> RevenueByDoctor { get; set; } = new Dictionary<string, decimal?>();
    }
}