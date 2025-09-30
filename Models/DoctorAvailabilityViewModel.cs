namespace MedicalAppointmentSystem.Models
{
    public class DoctorAvailabilityViewModel
    {
        public int? Id { get; set; }


        public string Day { get; set; }


        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string Location { get; set; }
    }
}
