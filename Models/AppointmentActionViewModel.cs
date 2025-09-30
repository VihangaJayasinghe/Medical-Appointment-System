namespace MedicalAppointmentSystem.Models
{
    public class AppointmentActionViewModel
    {
        public int AppointmentId { get; set; }
        public string Action { get; set; } // confirm, reschedule, cancel
        public DateTime? NewDate { get; set; }
        public TimeSpan? NewTime { get; set; }
        public string Notes { get; set; }
    }
}
