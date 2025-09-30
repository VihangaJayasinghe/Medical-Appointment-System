using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalAppointmentSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MedicalAppointmentSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Login", "Account");

            return View();
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Login", "Account");

            try
            {
                var stats = new AdminDashboardViewModel
                {
                    TotalDoctors = await _context.Doctors.CountAsync(),
                    TotalPatients = await _context.Patients.CountAsync(),
                    TotalAppointments = await _context.Appointments.CountAsync(),
                    TotalRevenue = await _context.Payments.Where(p => p.Status == "paid").SumAsync(p => p.Amount) ?? 0,
                    RecentAppointments = await _context.Appointments
                        .Include(a => a.Doctor).ThenInclude(d => d.User)
                        .Include(a => a.Patient).ThenInclude(p => p.User)
                        .OrderByDescending(a => a.AppointmentDate)
                        .Take(10)
                        .ToListAsync()
                };

                return View(stats);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading dashboard statistics";
                return View(new AdminDashboardViewModel());
            }
        }

        // GET: /Admin/Doctors
        public async Task<IActionResult> Doctors()
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Login", "Account");

            var doctors = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.DoctorAvailabilities)
                .ToListAsync();

            return View(doctors);
        }

        // GET: /Admin/AddDoctor
        public IActionResult AddDoctor()
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Login", "Account");

            ViewBag.Specialties = GetSpecialties();
            return View();
        }

        // POST: /Admin/AddDoctor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctor(DoctorManageViewModel model)
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                try
                {
                    // Create user first
                    var user = new User
                    {
                        Email = model.Email,
                        PasswordHash = HashPassword("temp123"), // Default password
                        Role = "doctor",
                        Name = model.Name
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Create doctor profile
                    var doctor = new Doctor
                    {
                        UserId = user.Id,
                        Specialty = model.Specialty,
                        ConsultationFee = model.ConsultationFee,
                        Bio = model.Bio
                    };

                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Doctor added successfully!";
                    return RedirectToAction("Doctors");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error adding doctor. Please try again.");
                }
            }

            ViewBag.Specialties = GetSpecialties();
            return View(model);
        }

        // GET: /Admin/EditDoctor/{id}
        public async Task<IActionResult> EditDoctor(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Login", "Account");

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.DoctorAvailabilities)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("Doctors");
            }

            var model = new DoctorManageViewModel
            {
                Id = doctor.Id,
                Name = doctor.User?.Name,
                Email = doctor.User?.Email,
                Specialty = doctor.Specialty,
                ConsultationFee = doctor.ConsultationFee ?? 0,
                Bio = doctor.Bio
            };

            ViewBag.Specialties = GetSpecialties();
            ViewBag.Availability = doctor.DoctorAvailabilities?.ToList();
            return View(model);
        }

        // POST: /Admin/EditDoctor/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(int id, DoctorManageViewModel model)
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Login", "Account");

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("Doctors");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update doctor properties
                    doctor.Specialty = model.Specialty;
                    doctor.ConsultationFee = model.ConsultationFee;
                    doctor.Bio = model.Bio;

                    // Update user properties
                    if (doctor.User != null)
                    {
                        doctor.User.Name = model.Name;
                        doctor.User.Email = model.Email;
                    }

                    _context.Doctors.Update(doctor);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Doctor updated successfully!";
                    return RedirectToAction("Doctors");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating doctor. Please try again.");
                }
            }

            ViewBag.Specialties = GetSpecialties();
            return View(model);
        }

        // POST: /Admin/DeleteDoctor/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Login", "Account");

            try
            {
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (doctor != null)
                {
                    // Remove doctor's availabilities first
                    var availabilities = await _context.DoctorAvailabilities
                        .Where(da => da.DoctorId == id)
                        .ToListAsync();

                    _context.DoctorAvailabilities.RemoveRange(availabilities);

                    // Remove doctor
                    _context.Doctors.Remove(doctor);

                    // Remove user account
                    if (doctor.User != null)
                    {
                        _context.Users.Remove(doctor.User);
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Doctor deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Doctor not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting doctor. Please try again.";
            }

            return RedirectToAction("Doctors");
        }

        // GET: /Admin/ManageAvailability/{doctorId}
        public async Task<IActionResult> ManageAvailability(int doctorId)
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Login", "Account");

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.DoctorAvailabilities)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("Doctors");
            }

            ViewBag.Doctor = doctor;
            ViewBag.Days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            return View();
        }

        // POST: /Admin/AddAvailability
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAvailability(DoctorAvailability model)
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.DoctorAvailabilities.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Availability added successfully!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Error adding availability. Please try again.";
                }
            }

            return RedirectToAction("ManageAvailability", new { doctorId = model.DoctorId });
        }

        // POST: /Admin/DeleteAvailability/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvailability(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Login", "Account");

            try
            {
                var availability = await _context.DoctorAvailabilities.FindAsync(id);
                if (availability != null)
                {
                    _context.DoctorAvailabilities.Remove(availability);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Availability removed successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error removing availability. Please try again.";
            }

            return RedirectToAction("ManageAvailability", new { doctorId = Request.Form["doctorId"] });
        }

        // GET: /Admin/Patients
        public async Task<IActionResult> Patients()
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Login", "Account");

            var patients = await _context.Patients
                .Include(p => p.User)
                .ToListAsync();

            return View(patients);
        }

        // GET: /Admin/Reports
        public async Task<IActionResult> Reports()
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Login", "Account");

            var report = new AdminReportViewModel
            {
                TotalAppointments = await _context.Appointments.CountAsync(),
                CompletedAppointments = await _context.Appointments.CountAsync(a => a.Status == "completed"),
                CancelledAppointments = await _context.Appointments.CountAsync(a => a.Status == "cancelled"),
                TotalRevenue = await _context.Payments.Where(p => p.Status == "paid").SumAsync(p => p.Amount) ?? 0,
                PendingPayments = await _context.Payments.CountAsync(p => p.Status == "pending"),

                AppointmentStats = await _context.Appointments
                    .GroupBy(a => a.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.Status, g => g.Count),

                RevenueByDoctor = await _context.Payments
                    .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                    .ThenInclude(d => d.User)
                    .Where(p => p.Status == "paid")
                    .GroupBy(p => p.Appointment.Doctor.User.Name)
                    .Select(g => new { Doctor = g.Key, Revenue = g.Sum(p => p.Amount) })
                    .ToDictionaryAsync(g => g.Doctor, g => g.Revenue)
            };   

            return View(report);
        }

        // Helper method to hash passwords
        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Helper method to get specialties
        private List<string> GetSpecialties()
        {
            return new List<string>
            {
                "Cardiology",
                "Pediatrics",
                "Dermatology",
                "Neurology",
                "Orthopedics",
                "Gynecology",
                "Psychiatry",
                "Dentistry",
                "Ophthalmology",
                "Emergency Medicine"
            };
        }
    }
}