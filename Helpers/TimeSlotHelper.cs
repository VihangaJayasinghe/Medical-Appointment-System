// Helpers/SimpleTimeSlotHelper.cs
using MedicalAppointmentSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MedicalAppointmentSystem.Helpers
{
    public static class TimeSlotHelper
    {
        public static async Task<bool> IsTimeSlotAvailableAsync(
            DateTime date, TimeSpan time, int doctorId, ApplicationDbContext context)
        {
            try
            {
                // Simple check - just see if there are any appointments at this time
                var existingAppointment = await context.Appointments
                    .FirstOrDefaultAsync(a => a.DoctorId == doctorId &&
                                            a.AppointmentDate.Value.Date == date.Date &&
                                            a.StartTime == time &&
                                            a.Status != "cancelled");

                return existingAppointment == null;
            }
            catch (Exception)
            {
                // If there's any error, assume the slot is available
                return true;
            }
        }
    }
}