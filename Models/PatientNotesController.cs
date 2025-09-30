using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointmentSystem.Models
{
    public class PatientNotesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientNotesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PatientNotes
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.PatientNotes.Include(p => p.Appointment);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: PatientNotes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patientNotes = await _context.PatientNotes
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (patientNotes == null)
            {
                return NotFound();
            }

            return View(patientNotes);
        }

        // GET: PatientNotes/Create
        public IActionResult Create()
        {
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "Id", "Id");
            return View();
        }

        // POST: PatientNotes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,AppointmentId,Notes,CreatedDate,Prescription")] PatientNotes patientNotes)
        {
            if (ModelState.IsValid)
            {
                _context.Add(patientNotes);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "Id", "Id", patientNotes.AppointmentId);
            return View(patientNotes);
        }

        // GET: PatientNotes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patientNotes = await _context.PatientNotes.FindAsync(id);
            if (patientNotes == null)
            {
                return NotFound();
            }
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "Id", "Id", patientNotes.AppointmentId);
            return View(patientNotes);
        }

        // POST: PatientNotes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AppointmentId,Notes,CreatedDate,Prescription")] PatientNotes patientNotes)
        {
            if (id != patientNotes.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patientNotes);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientNotesExists(patientNotes.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "Id", "Id", patientNotes.AppointmentId);
            return View(patientNotes);
        }

        // GET: PatientNotes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patientNotes = await _context.PatientNotes
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (patientNotes == null)
            {
                return NotFound();
            }

            return View(patientNotes);
        }

        // POST: PatientNotes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patientNotes = await _context.PatientNotes.FindAsync(id);
            if (patientNotes != null)
            {
                _context.PatientNotes.Remove(patientNotes);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PatientNotesExists(int id)
        {
            return _context.PatientNotes.Any(e => e.Id == id);
        }
    }
}
