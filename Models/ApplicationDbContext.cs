using Microsoft.EntityFrameworkCore;

namespace MedicalAppointmentSystem.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<DoctorAvailability> DoctorAvailabilities { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PatientNotes> PatientNotes { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User - Doctor (One-to-One)
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - Patient (One-to-One)
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Doctor - DoctorAvailability (One-to-Many)
            modelBuilder.Entity<DoctorAvailability>()
                .HasOne(da => da.Doctor)
                .WithMany(d => d.DoctorAvailabilities)
                .HasForeignKey(da => da.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Appointment - Patient relationship
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()  // Remove .WithMany(p => p.Appointments) if you have it
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment - Doctor relationship  
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()  // Remove .WithMany(d => d.Appointments) if you have it
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment - Payment (One-to-Many)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Appointment)
                .WithMany()
                .HasForeignKey(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Appointment - PatientNotes (One-to-One)
            modelBuilder.Entity<PatientNotes>()
                .HasOne(pn => pn.Appointment)
                .WithMany()
                .HasForeignKey(pn => pn.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Patient - Feedback (One-to-Many)
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Patient)
                .WithMany()
                .HasForeignKey(f => f.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Doctor - Feedback (One-to-Many)
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Doctor)
                .WithMany()
                .HasForeignKey(f => f.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}