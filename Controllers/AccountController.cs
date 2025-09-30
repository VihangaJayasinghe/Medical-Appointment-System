using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalAppointmentSystem.Models;
using MedicalAppointmentSystem.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace MedicalAppointmentSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    return View(model);
                }

                // Create user
                var user = new User
                {
                    Email = model.Email,
                    PasswordHash = HashPassword(model.Password),
                    Role = "patient", // Only patients can register
                    Name = model.Name
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create patient
                var patient = new Patient
                {
                    UserId = user.Id,
                    Age = model.Age,
                    Phone = model.Phone
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                // Set session and redirect
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("UserName", user.Name ?? "");

                return RedirectToAction("Index", "Patient");
            }

            return View(model);
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var hashedPassword = HashPassword(model.Password);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordHash == hashedPassword);

                if (user != null)
                {
                    // Set session
                    HttpContext.Session.SetString("UserId", user.Id.ToString());
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserRole", user.Role);
                    HttpContext.Session.SetString("UserName", user.Name ?? "");

                    // Redirect based on role
                    return user.Role.ToLower() switch
                    {
                        "admin" => RedirectToAction("Index", "Admin"),
                        "doctor" => RedirectToAction("Index", "Doctor"),
                        "patient" => RedirectToAction("Index", "Patient"),
                        _ => RedirectToAction("Index", "Home")
                    };
                }

                ModelState.AddModelError("", "Invalid login attempt.");
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Helper method to hash passwords
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}