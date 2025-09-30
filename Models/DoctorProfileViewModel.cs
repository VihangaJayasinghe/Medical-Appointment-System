// Add to MedicalAppointmentSystem.ViewModels
namespace MedicalAppointmentSystem.ViewModels
{
    public class DoctorProfileViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }


        public string Name { get; set; }


        public string Specialty { get; set; }

        public decimal ConsultationFee { get; set; }

        public string Bio { get; set; }
    }
}