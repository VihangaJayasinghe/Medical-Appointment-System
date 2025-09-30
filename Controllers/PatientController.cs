using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalAppointmentSystem.Models;
using MedicalAppointmentSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MedicalAppointmentSystem.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedicalAppointmentSystem.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<Patient> GetCurrentPatientAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                return null;

            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userIdInt);
        }

        // GET: /Patient
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            return RedirectToAction("Dashboard");
        }

        // GET: /Patient/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var patient = await GetCurrentPatientAsync();
            if (patient == null)
                return RedirectToAction("Login", "Account");

            try
            {
                // Get patient statistics safely
                var appointments = await _context.Appointments
                    .Where(a => a.PatientId == patient.Id)
                    .ToListAsync();

                var payments = await _context.Payments
                    .Include(p => p.Appointment)
                    .Where(p => p.Appointment != null && p.Appointment.PatientId == patient.Id)
                    .ToListAsync();

                var recentAppointments = await _context.Appointments
                    .Where(a => a.PatientId == patient.Id)
                    .Include(a => a.Doctor)
                        .ThenInclude(d => d.User)
                    .OrderByDescending(a => a.AppointmentDate)
                    .Take(5)
                    .ToListAsync();

                var viewModel = new PatientDashboardViewModel
                {
                    Patient = patient,
                    TotalAppointments = appointments.Count,
                    UpcomingAppointments = appointments.Count(a =>
                        a.AppointmentDate >= DateTime.Today &&
                        a.Status != "cancelled" &&
                        a.Status != "completed"),
                    PendingPayments = payments.Count(p => p.Status == "pending"),
                    RecentAppointments = recentAppointments
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log error and show simple dashboard
                ViewBag.Error = "Error loading dashboard data";
                return View(new PatientDashboardViewModel
                {
                    Patient = patient,
                    TotalAppointments = 0,
                    UpcomingAppointments = 0,
                    PendingPayments = 0,
                    RecentAppointments = new List<Appointment>()
                });
            }
        }

        // GET: /Patient/SearchDoctors
        public async Task<IActionResult> SearchDoctors(string specialty, string location, string day)
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var viewModel = new DoctorSearchViewModel
            { Specialty = specialty, Location = location, Day = day};

            try{
                var allDoctors = await _context.Doctors
                    .Include(d => d.User)
                    .Include(d => d.DoctorAvailabilities)
                    .ToListAsync();


                IEnumerable<Doctor> filteredDoctors = allDoctors;

                if (!string.IsNullOrEmpty(specialty) && specialty != "All Specialties")
                {
                    filteredDoctors = filteredDoctors.Where(d =>
                        d.Specialty != null &&
                        d.Specialty.Equals(specialty, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(location) && location != "All Locations")
                {
                    filteredDoctors = filteredDoctors.Where(d =>
                        d.DoctorAvailabilities.Any(da =>
                            da.Location != null &&
                            da.Location.Equals(location, StringComparison.OrdinalIgnoreCase)));
                }

                if (!string.IsNullOrEmpty(day) && day != "Any Day")
                {
                    filteredDoctors = filteredDoctors.Where(d =>
                        d.DoctorAvailabilities.Any(da =>
                            da.Day != null &&
                            da.Day.Trim().Equals(day.Trim(), StringComparison.OrdinalIgnoreCase)));
                }

                viewModel.Doctors = filteredDoctors.ToList();


                ViewBag.TotalDoctors = allDoctors.Count;
                ViewBag.FilteredDoctors = viewModel.Doctors.Count;
                ViewBag.SearchCriteria = $"Specialty: {specialty}, Location: {location}, Day: {day}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search error: {ex.Message}");
                viewModel.Doctors = new List<Doctor>();
            }

            ViewBag.Specialties = await _context.Doctors
                .Where(d => d.Specialty != null)
                .Select(d => d.Specialty)
                .Distinct()
                .ToListAsync();

            ViewBag.Locations = await _context.DoctorAvailabilities
                .Where(da => da.Location != null)
                .Select(da => da.Location)
                .Distinct()
                .ToListAsync();

            ViewBag.Days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

            return View(viewModel);
        }


        // GET: /Patient/SelectDoctor/{id}
        public async Task<IActionResult> SelectDoctor(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.DoctorAvailabilities)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("SearchDoctors");
            }


            var viewModel = new BookAppointmentViewModel
            {
                DoctorId = doctor.Id,
                DoctorName = doctor.User?.Name ?? "Unknown Doctor",
                ConsultationFee = doctor.ConsultationFee ?? 0,
                Specialty = doctor.Specialty
            };

            ViewBag.Doctor = doctor;
            return View(viewModel);
        }

        // GET: /Patient/SelectDateTime/{doctorId}
        public async Task<IActionResult> SelectDateTime(int doctorId, string location)
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.DoctorAvailabilities)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("SearchDoctors");
            }

            var viewModel = new BookAppointmentViewModel
            {
                DoctorId = doctorId,
                DoctorName = doctor.User?.Name ?? "Unknown Doctor",
                ConsultationFee = doctor.ConsultationFee ?? 0,
                SelectedLocation = location
            };

            ViewBag.Doctor = doctor;

            var availability = doctor.DoctorAvailabilities?
                .Where(da => da.Location == location || string.IsNullOrEmpty(location))
                .ToList();

            ViewBag.Availability = availability;

            return View(viewModel);
        }
        // GET: /Patient/ViewNotes

        public IActionResult ViewNotes()
        {

            return View();
        }
        // POST: /Patient/SelectDateTime
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectDateTime(BookAppointmentViewModel model)
        {
            Console.WriteLine("🔍 SelectDateTime POST - Start");
            Console.WriteLine($"DoctorId: {model.DoctorId}");
            Console.WriteLine($"AppointmentDate: {model.AppointmentDate}");
            Console.WriteLine($"StartTime: {model.StartTime}");
            Console.WriteLine($"SelectedLocation: {model.SelectedLocation}");

            if (HttpContext.Session.GetString("UserRole") != "patient")
            {
                Console.WriteLine("❌ Not logged in");
                return RedirectToAction("Login", "Account");
            }


            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.DoctorAvailabilities)
                .FirstOrDefaultAsync(d => d.Id == model.DoctorId);

            if (doctor == null)
            {
                Console.WriteLine("❌ Doctor not found");
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("SearchDoctors");
            }

            ViewBag.Doctor = doctor;
            ViewBag.Availability = doctor.DoctorAvailabilities?
                .Where(da => da.Location == model.SelectedLocation || string.IsNullOrEmpty(model.SelectedLocation))
                .ToList();


            if (model.AppointmentDate.HasValue && !model.StartTime.HasValue)
            {
                Console.WriteLine("⚠️ Only date selected, showing time slots");
                return View(model);
            }

            if (!model.DoctorId.HasValue || !model.AppointmentDate.HasValue || !model.StartTime.HasValue || string.IsNullOrEmpty(model.SelectedLocation))
            {
                Console.WriteLine("❌ Missing required fields");
                ModelState.AddModelError("", "Please complete all required fields.");
                return View(model);
            }


            var isAvailable = await TimeSlotHelper.IsTimeSlotAvailableAsync(
                model.AppointmentDate.Value,
                model.StartTime.Value,
                model.DoctorId.Value,
                _context
            );

            Console.WriteLine($"✅ Availability check: {isAvailable}");

            if (!isAvailable)
            {
                Console.WriteLine("❌ Time slot not available");
                ModelState.AddModelError("", "This time slot is no longer available. Please choose another time.");
                return View(model);
            }

            var bookingData = System.Text.Json.JsonSerializer.Serialize(model);
            TempData["BookingData"] = bookingData;

            Console.WriteLine("✅ Booking data stored in TempData:");
            Console.WriteLine(bookingData);
            Console.WriteLine("Redirecting to ConfirmAppointment...");

            return RedirectToAction("ConfirmAppointment", "Patient");
        }

        // GET: /Patient/ConfirmAppointment
        public IActionResult ConfirmAppointment()
        {
            Console.WriteLine("🔍 ConfirmAppointment GET - Start");

            if (HttpContext.Session.GetString("UserRole") != "patient")
            {
                Console.WriteLine("❌ Not logged in");
                return RedirectToAction("Login", "Account");
            }


            var bookingData = TempData["BookingData"]?.ToString();
            Console.WriteLine($"TempData contains BookingData: {!string.IsNullOrEmpty(bookingData)}");

            if (string.IsNullOrEmpty(bookingData))
            {
                Console.WriteLine("❌ No booking data found in TempData");
                TempData["ErrorMessage"] = "Booking data not found. Please start over.";
                return RedirectToAction("SearchDoctors", "Patient");
            }

            try
            {
                var model = System.Text.Json.JsonSerializer.Deserialize<BookAppointmentViewModel>(bookingData);
                Console.WriteLine($"✅ Successfully deserialized booking data:");
                Console.WriteLine($"   DoctorId: {model.DoctorId}");
                Console.WriteLine($"   DoctorName: {model.DoctorName}");
                Console.WriteLine($"   AppointmentDate: {model.AppointmentDate}");
                Console.WriteLine($"   StartTime: {model.StartTime}");
                Console.WriteLine($"   SelectedLocation: {model.SelectedLocation}");
                Console.WriteLine($"   ConsultationFee: {model.ConsultationFee}");


                TempData.Keep("BookingData");

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Deserialization error: {ex.Message}");
                TempData["ErrorMessage"] = "Invalid booking data. Please start over.";
                return RedirectToAction("SearchDoctors", "Patient");
            }
        }


        // GET: /Patient/TestTempData
        public IActionResult TestTempData()
        {
            var bookingData = TempData["BookingData"]?.ToString();
            return Content($"TempData BookingData: {(string.IsNullOrEmpty(bookingData) ? "EMPTY" : bookingData)}");
        }

        // POST: /Patient/DebugConfirm
        [HttpPost]
        public IActionResult DebugConfirm()
        {
            Console.WriteLine("🔍 DebugConfirm - Form data received:");

            foreach (var key in Request.Form.Keys)
            {
                Console.WriteLine($"{key}: {Request.Form[key]}");
            }

            return Content($"Form fields received: {string.Join(", ", Request.Form.Keys)}");
        }
        // POST: /Patient/ConfirmAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAppointment(BookAppointmentViewModel model)
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var patient = await GetCurrentPatientAsync();
            if (patient == null || !model.DoctorId.HasValue || !model.AppointmentDate.HasValue || !model.StartTime.HasValue)
                return RedirectToAction("Login", "Account");

            try
            {

                var isAvailable = await TimeSlotHelper.IsTimeSlotAvailableAsync(
                    model.AppointmentDate.Value,
                    model.StartTime.Value,
                    model.DoctorId.Value,
                    _context
                );

                if (!isAvailable)
                {
                    TempData["ErrorMessage"] = "This time slot is no longer available. Please choose another time.";
                    return RedirectToAction("SelectDateTime", new { doctorId = model.DoctorId, location = model.SelectedLocation });
                }

                var appointment = new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = model.DoctorId.Value,
                    AppointmentDate = model.AppointmentDate.Value.Date,
                    StartTime = model.StartTime.Value,
                    EndTime = model.StartTime.Value.Add(new TimeSpan(0, 30, 0)),
                    Location = model.SelectedLocation,
                    Status = "booked"
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                var doctor = await _context.Doctors.FindAsync(model.DoctorId);
                if (doctor != null)
                {
                    var payment = new Payment
                    {
                        AppointmentId = appointment.Id,
                        Amount = doctor.ConsultationFee ?? 0,
                        Status = "pending"
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();
                }


                TempData.Remove("BookingData");

                TempData["SuccessMessage"] = $"Appointment booked successfully with Dr. {model.DoctorName} on {model.AppointmentDate.Value:MMMM dd, yyyy} at {model.StartTime.Value:hh\\:mm}";
                return RedirectToAction("Appointments");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ConfirmAppointment: {ex.Message}");
                TempData["ErrorMessage"] = "Error booking appointment. Please try again.";
                return RedirectToAction("SelectDateTime", new { doctorId = model.DoctorId, location = model.SelectedLocation });
            }
        }

        // GET: /Patient/Appointments
        public async Task<IActionResult> Appointments(string filter = null)
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var patient = await GetCurrentPatientAsync();
            if (patient == null)
                return RedirectToAction("Login", "Account");

            try
            {
                var query = _context.Appointments
                    .Where(a => a.PatientId == patient.Id)
                    .Include(a => a.Doctor)
                        .ThenInclude(d => d.User)
                    .AsQueryable();

                // Apply filters
                switch (filter?.ToLower())
                {
                    case "upcoming":
                        query = query.Where(a => a.AppointmentDate >= DateTime.Today && a.Status != "cancelled");
                        break;
                    case "past":
                        query = query.Where(a => a.AppointmentDate < DateTime.Today || a.Status == "completed");
                        break;
                    case "cancelled":
                        query = query.Where(a => a.Status == "cancelled");
                        break;
                }

                var appointments = await query
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenByDescending(a => a.StartTime)
                    .ToListAsync();

                ViewBag.Filter = filter;
                return View(appointments ?? new List<Appointment>());
            }
            catch (Exception)
            {
                ViewBag.Filter = filter;
                return View(new List<Appointment>());
            }
        }

        // POST: /Patient/CancelAppointment/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var patient = await GetCurrentPatientAsync();
            if (patient == null)
                return RedirectToAction("Login", "Account");

            try
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == patient.Id);

                if (appointment != null)
                {
                    appointment.Status = "cancelled";
                    _context.Appointments.Update(appointment);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Appointment cancelled successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Appointment not found or you don't have permission to cancel it.";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error cancelling appointment. Please try again.";
            }

            return RedirectToAction("Appointments");
        }

        // GET: /Patient/Payments
        public async Task<IActionResult> Payments()
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var patient = await GetCurrentPatientAsync();
            if (patient == null)
                return RedirectToAction("Login", "Account");

            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Appointment)
                        .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.User)
                    .Where(p => p.Appointment != null && p.Appointment.PatientId == patient.Id)
                    .OrderByDescending(p => p.Appointment.AppointmentDate)
                    .ToListAsync();

                return View(payments ?? new List<Payment>());
            }
            catch (Exception)
            {
                return View(new List<Payment>());
            }
        }

        // POST: /Patient/MakePayment/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakePayment(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var patient = await GetCurrentPatientAsync();
            if (patient == null)
                return RedirectToAction("Login", "Account");

            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Appointment)
                    .FirstOrDefaultAsync(p => p.Id == id && p.Appointment != null && p.Appointment.PatientId == patient.Id);

                if (payment != null)
                {
                    payment.Status = "paid";
                    _context.Payments.Update(payment);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Payment completed successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Payment not found or you don't have permission to pay it.";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error processing payment. Please try again.";
            }

            return RedirectToAction("Payments");
        }

        // GET: /Patient/MedicalRecords
        public async Task<IActionResult> MedicalRecords()
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var patient = await GetCurrentPatientAsync();
            if (patient == null)
                return RedirectToAction("Login", "Account");

            try
            {
                var records = await _context.PatientNotes
                    .Include(pn => pn.Appointment)
                        .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.User)
                    .Where(pn => pn.Appointment != null && pn.Appointment.PatientId == patient.Id)
                    .OrderByDescending(pn => pn.Appointment.AppointmentDate)
                    .ToListAsync();

                return View(records ?? new List<PatientNotes>());
            }
            catch (Exception)
            {
                return View(new List<PatientNotes>());
            }
        }

        // GET: /Patient/Profile
        public async Task<IActionResult> Profile()
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var patient = await GetCurrentPatientAsync();
            if (patient == null)
                return RedirectToAction("Login", "Account");

            return View(patient);
        }

        // POST: /Patient/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Patient model)
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var patient = await GetCurrentPatientAsync();
            if (patient == null)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                try
                {
                    // Update patient properties
                    patient.Age = model.Age;
                    patient.Phone = model.Phone;

                    // Update user properties if user exists
                    if (patient.User != null)
                    {
                        patient.User.Name = model.User?.Name ?? patient.User.Name;
                    }

                    _context.Patients.Update(patient);
                    await _context.SaveChangesAsync();

                    // Update session name if changed
                    if (patient.User != null && !string.IsNullOrEmpty(patient.User.Name))
                    {
                        HttpContext.Session.SetString("UserName", patient.User.Name);
                    }

                    TempData["SuccessMessage"] = "Profile updated successfully!";
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "Error updating profile. Please try again.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Please correct the validation errors.";
            }

            return RedirectToAction("Profile");
        }
        // GET: /Patient/Feedback
        public async Task<IActionResult> Feedback()
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var patient = await GetCurrentPatientAsync();
            if (patient == null)
                return RedirectToAction("Login", "Account");

            var viewModel = new FeedbackViewModel();
            await PopulateDoctorsDropdown(viewModel);

            return View(viewModel);
        }

        // POST: /Patient/Feedback
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Feedback(FeedbackViewModel model)
        {
            if (HttpContext.Session.GetString("UserRole") != "patient")
                return RedirectToAction("Login", "Account");

            var patient = await GetCurrentPatientAsync();
            if (patient == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                await PopulateDoctorsDropdown(model);
                return View(model);
            }

            try
            {
                // Validate doctor selection (if provided)
                if (model.SelectedDoctorId.HasValue)
                {
                    var doctorExists = await _context.Doctors
                        .AnyAsync(d => d.Id == model.SelectedDoctorId.Value);

                    if (!doctorExists)
                    {
                        ModelState.AddModelError("SelectedDoctorId", "Selected doctor does not exist");
                        await PopulateDoctorsDropdown(model);
                        return View(model);
                    }
                }

                var feedback = new Feedback
                {
                    PatientId = patient.Id,
                    DoctorId = model.SelectedDoctorId,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    CreatedAt = DateTime.Now
                };

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thank you for your feedback!";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving feedback: {ex.Message}");
                TempData["ErrorMessage"] = "Error submitting feedback. Please try again.";
                await PopulateDoctorsDropdown(model);
                return View(model);
            }
        }

        // Helper method to populate doctors dropdown
        private async Task PopulateDoctorsDropdown(FeedbackViewModel model)
        {
            var doctors = await _context.Doctors
                .Include(d => d.User)
                .OrderBy(d => d.User.Name)
                .ToListAsync();

            model.Doctors = doctors.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = $"Dr. {d.User?.Name} - {d.Specialty}"
            }).ToList();

            // Add empty option
            model.Doctors.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Select a doctor (optional)"
            });
        }
    }
}