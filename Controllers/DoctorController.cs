

namespace MedicalAppointmentSystem.Controllers
{
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<Doctor> GetCurrentDoctorAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            Console.WriteLine($"Session UserId: {userId}");

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                Console.WriteLine("Invalid or missing UserId in session");
                return null;
            }

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.UserId == userIdInt);

            Console.WriteLine($"Found doctor: {doctor != null}, Doctor ID: {doctor?.Id}");

            return doctor;
        }

        // GET: /Doctor
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserRole") != "doctor")
                return RedirectToAction("Login", "Account");

            return RedirectToAction("Dashboard");
        }

        // GET: /Doctor/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (HttpContext.Session.GetString("UserRole") != "doctor")
                return RedirectToAction("Login", "Account");

            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
                return RedirectToAction("Login", "Account");

            try
            {
                var appointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id)
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .ToListAsync();

                var today = DateTime.Today;
                var upcomingAppointments = appointments
                    .Where(a => a.AppointmentDate >= today && a.Status != "cancelled" && a.Status != "completed")
                    .ToList();

                var todayAppointments = appointments
                    .Where(a => a.AppointmentDate == today && a.Status != "cancelled" && a.Status != "completed")
                    .ToList();

                var recentAppointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id)
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenByDescending(a => a.StartTime)
                    .Take(5)
                    .ToListAsync();

                var viewModel = new DoctorDashboardViewModel
                {
                    Doctor = doctor,
                    TotalAppointments = appointments.Count,
                    UpcomingAppointments = upcomingAppointments.Count,
                    TodayAppointments = todayAppointments.Count,
                    RecentAppointments = recentAppointments
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading dashboard data";
                return View(new DoctorDashboardViewModel
                {
                    Doctor = doctor,
                    TotalAppointments = 0,
                    UpcomingAppointments = 0,
                    TodayAppointments = 0,
                    RecentAppointments = new List<Appointment>()
                });
            }
        }
        // GET: /Doctor/Profile
        public async Task<IActionResult> Profile()
        {
            if (HttpContext.Session.GetString("UserRole") != "doctor")
                return RedirectToAction("Login", "Account");

            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
                return RedirectToAction("Login", "Account");

            // Get fresh data from database
            var currentDoctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == doctor.Id);

            if (currentDoctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("Dashboard");
            }

            // Map to ViewModel
            var viewModel = new DoctorProfileViewModel
            {
                Id = currentDoctor.Id,
                UserId = currentDoctor.UserId,
                Name = currentDoctor.User?.Name,
                Specialty = currentDoctor.Specialty,
                ConsultationFee = currentDoctor.ConsultationFee ?? 0,
                Bio = currentDoctor.Bio
            };

            return View(viewModel);
        }

        // POST: /Doctor/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(DoctorProfileViewModel model)
        {
            if (HttpContext.Session.GetString("UserRole") != "doctor")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                // Return to form with validation errors
                TempData["ErrorMessage"] = "Please correct the validation errors.";
                return View("Profile", model);
            }

            try
            {
                var doctor = await GetCurrentDoctorAsync();
                if (doctor == null)
                    return RedirectToAction("Login", "Account");

                // Get the current doctor from database with tracking
                var currentDoctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.Id == doctor.Id);

                if (currentDoctor == null)
                {
                    TempData["ErrorMessage"] = "Doctor not found.";
                    return RedirectToAction("Profile");
                }

                // Update doctor properties
                currentDoctor.Specialty = model.Specialty;
                currentDoctor.ConsultationFee = model.ConsultationFee;
                currentDoctor.Bio = model.Bio;

                // Update user name
                if (currentDoctor.User != null)
                {
                    currentDoctor.User.Name = model.Name;
                }

                _context.Doctors.Update(currentDoctor);
                await _context.SaveChangesAsync();

                // Update session
                HttpContext.Session.SetString("UserName", model.Name);

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile: {ex.Message}");
                TempData["ErrorMessage"] = "Error updating profile. Please try again.";
                return View("Profile", model);
            }
        }

        // GET: /Doctor/Availability
        public async Task<IActionResult> Availability()
        {
            if (HttpContext.Session.GetString("UserRole") != "doctor")
                return RedirectToAction("Login", "Account");

            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
                return RedirectToAction("Login", "Account");

            var availabilities = await _context.DoctorAvailabilities
                .Where(da => da.DoctorId == doctor.Id)
                .OrderBy(da => da.Day)
                .ThenBy(da => da.StartTime)
                .ToListAsync();

            ViewBag.Days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            return View(availabilities);
        }

        // POST: /Doctor/AddAvailability
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAvailability(DoctorAvailabilityViewModel model)
        {
            if (HttpContext.Session.GetString("UserRole") != "doctor")
                return RedirectToAction("Login", "Account");

            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                try
                {
                    var availability = new DoctorAvailability
                    {
                        DoctorId = doctor.Id,
                        Day = model.Day,
                        StartTime = model.StartTime,
                        EndTime = model.EndTime,
                        Location = model.Location
                    };

                    _context.DoctorAvailabilities.Add(availability);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Availability added successfully!";
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "Error adding availability. Please try again.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Please correct the validation errors.";
            }

            return RedirectToAction("Availability");
        }

        // POST: /Doctor/DeleteAvailability/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvailability(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "doctor")
                return RedirectToAction("Login", "Account");

            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
                return RedirectToAction("Login", "Account");

            try
            {
                var availability = await _context.DoctorAvailabilities
                    .FirstOrDefaultAsync(da => da.Id == id && da.DoctorId == doctor.Id);

                if (availability != null)
                {
                    _context.DoctorAvailabilities.Remove(availability);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Availability removed successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Availability not found.";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error removing availability. Please try again.";
            }

            return RedirectToAction("Availability");
        }

        // GET: /Doctor/Appointments
        public async Task<IActionResult> Appointments(string filter = null)
        {
            if (HttpContext.Session.GetString("UserRole") != "doctor")
                return RedirectToAction("Login", "Account");

            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
                return RedirectToAction("Login", "Account");

            try
            {
                var query = _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id)
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .AsQueryable();

                // Apply filters
                switch (filter?.ToLower())
                {
                    case "upcoming":
                        query = query.Where(a => a.AppointmentDate >= DateTime.Today && a.Status != "cancelled");
                        break;
                    case "today":
                        query = query.Where(a => a.AppointmentDate == DateTime.Today && a.Status != "cancelled");
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

        // POST: /Doctor/UpdateAppointmentStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAppointmentStatus(AppointmentActionViewModel model)
        {
            if (HttpContext.Session.GetString("UserRole") != "doctor")
                return RedirectToAction("Login", "Account");

            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
                return RedirectToAction("Login", "Account");

            try
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == model.AppointmentId && a.DoctorId == doctor.Id);

                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Appointment not found.";
                    return RedirectToAction("Appointments");
                }

                switch (model.Action.ToLower())
                {
                    case "confirm":
                        appointment.Status = "confirmed";
                        break;
                    case "cancel":
                        appointment.Status = "cancelled";
                        break;
                    case "complete":
                        appointment.Status = "completed";
                        break;
                    case "reschedule":
                        if (model.NewDate.HasValue && model.NewTime.HasValue)
                        {
                            appointment.AppointmentDate = model.NewDate.Value;
                            appointment.StartTime = model.NewTime.Value;
                            appointment.EndTime = model.NewTime.Value.Add(new TimeSpan(0, 30, 0));
                            appointment.Status = "rescheduled";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "New date and time are required for rescheduling.";
                            return RedirectToAction("Appointments");
                        }
                        break;
                    default:
                        TempData["ErrorMessage"] = "Invalid action.";
                        return RedirectToAction("Appointments");
                }

                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Appointment {model.Action} successfully!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error updating appointment. Please try again.";
            }

            return RedirectToAction("Appointments");
        }

        // GET: /Doctor/AddNote

        public IActionResult AddNote()
        {

            return View();
        }

        // GET: /Doctor/PatientNotes/{appointmentId}
        public async Task<IActionResult> PatientNotes(int appointmentId)
        {
            if (HttpContext.Session.GetString("UserRole") != "doctor")
                return RedirectToAction("Login", "Account");

            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
                return RedirectToAction("Login", "Account");

            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId && a.DoctorId == doctor.Id);

                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Appointment not found.";
                    return RedirectToAction("Appointments");
                }

                var existingNotes = await _context.PatientNotes
                    .FirstOrDefaultAsync(pn => pn.AppointmentId == appointmentId);

                var viewModel = new PatientNotesViewModel
                {
                    AppointmentId = appointmentId
                };

                if (existingNotes != null)
                {
                    viewModel.Notes = existingNotes.Notes;
                    viewModel.Prescription = existingNotes.Prescription;
                }

                ViewBag.Appointment = appointment;
                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading patient notes.";
                return RedirectToAction("Appointments");
            }
        }

        // POST: /Doctor/SavePatientNotes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePatientNotes(PatientNotesViewModel model)
        {
            if (HttpContext.Session.GetString("UserRole") != "doctor")
                return RedirectToAction("Login", "Account");

            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide consultation notes.";
                return RedirectToAction("PatientNotes", new { appointmentId = model.AppointmentId });
            }

            try
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == model.AppointmentId && a.DoctorId == doctor.Id);

                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Appointment not found.";
                    return RedirectToAction("Appointments");
                }

                var existingNotes = await _context.PatientNotes
                    .FirstOrDefaultAsync(pn => pn.AppointmentId == model.AppointmentId);

                if (existingNotes != null)
                {
                    existingNotes.Notes = model.Notes;
                    existingNotes.Prescription = model.Prescription;
                    _context.PatientNotes.Update(existingNotes);
                }
                else
                {
                    var patientNotes = new PatientNotes
                    {
                        AppointmentId = model.AppointmentId,
                        Notes = model.Notes,
                        Prescription = model.Prescription,
                        CreatedDate = DateTime.Now
                    };
                    _context.PatientNotes.Add(patientNotes);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Patient notes saved successfully!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error saving patient notes. Please try again.";
            }

            return RedirectToAction("Appointments");
        }
    }
}