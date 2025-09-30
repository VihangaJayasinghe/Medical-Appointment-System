using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalAppointmentSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Security.Cryptography;
using MedicalAppointmentSystem.ViewModels;
using MedicalAppointmentSystem.Helpers;
using Microsoft.Data.SqlClient;

namespace MedicalAppointmentSystem.Controllers
{
    public class TestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: /Test/TestBooking
        public async Task<IActionResult> TestBooking()
        {
            try
            {
                // Create test booking data
                var testModel = new BookAppointmentViewModel
                {
                    DoctorId = 13, // Use an existing doctor ID
                    DoctorName = "Test Doctor",
                    ConsultationFee = 100.00m,
                    SelectedLocation = "Main Hospital",
                    AppointmentDate = DateTime.Today.AddDays(1),
                    StartTime = new TimeSpan(9, 0, 0)
                };

                // Store in session
                HttpContext.Session.SetString("BookingData", System.Text.Json.JsonSerializer.Serialize(testModel));

                return RedirectToAction("ConfirmAppointment");
            }
            catch (Exception ex)
            {
                return Content($"Test error: {ex.Message}");
            }
        }

        // GET: /Test/DebugAvailabilityCheck
        public async Task<IActionResult> DebugAvailabilityCheck(int doctorId = 13, string date = "2025-09-15", string time = "11:00:00", string location = "Heart Center")
        {
            try
            {
                var results = new System.Text.StringBuilder();
                var targetDate = DateTime.Parse(date);
                var targetTime = TimeSpan.Parse(time);

                results.AppendLine($"🔍 Debugging availability check for:");
                results.AppendLine($"- DoctorId: {doctorId}");
                results.AppendLine($"- Date: {targetDate:yyyy-MM-dd}");
                results.AppendLine($"- Time: {targetTime:hh\\:mm}");
                results.AppendLine($"- Location: {location}");
                results.AppendLine("");

                // 1. Check if appointments table is really empty
                var totalAppointments = await _context.Appointments.CountAsync();
                results.AppendLine($"Total appointments in database: {totalAppointments}");

                if (totalAppointments > 0)
                {
                    var recentAppointments = await _context.Appointments
                        .OrderByDescending(a => a.Id)
                        .Take(5)
                        .ToListAsync();

                    results.AppendLine("Recent appointments:");
                    foreach (var appt in recentAppointments)
                    {
                        results.AppendLine($"- {appt.AppointmentDate:yyyy-MM-dd} {appt.StartTime:hh\\:mm} with Dr.{appt.DoctorId}");
                    }
                }

                // 2. Run the exact SQL query that's being used
                results.AppendLine("");
                results.AppendLine("📊 Running availability check SQL:");

                var sql = @"
            SELECT COUNT(*)
            FROM Appointments
            WHERE DoctorId = @p0
            AND CAST(AppointmentDate AS DATE) = CAST(@p1 AS DATE)
            AND StartTime = @p2
            AND (Status IS NULL OR Status != 'cancelled')";

                var count = await _context.Database.ExecuteSqlRawAsync(sql, doctorId, targetDate, targetTime);
                results.AppendLine($"SQL result: {count} appointments found");

                // 3. Check if there are any appointments that might match
                var matchingAppointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId &&
                               a.AppointmentDate.Value.Date == targetDate.Date &&
                               a.StartTime == targetTime)
                    .ToListAsync();

                results.AppendLine($"");
                results.AppendLine($"Matching appointments (LINQ): {matchingAppointments.Count}");
                foreach (var appt in matchingAppointments)
                {
                    results.AppendLine($"- ID: {appt.Id}, Status: {appt.Status}, Patient: {appt.PatientId}");
                }

                // 4. Check the actual data in the appointments table
                results.AppendLine($"");
                results.AppendLine("All appointments in database:");
                var allAppointments = await _context.Appointments.ToListAsync();
                foreach (var appt in allAppointments)
                {
                    results.AppendLine($"- ID: {appt.Id}, Dr: {appt.DoctorId}, Date: {appt.AppointmentDate:yyyy-MM-dd}, Time: {appt.StartTime:hh\\:mm}, Status: {appt.Status}");
                }

                return Content(results.ToString());
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        // GET: /Test/CheckAppointmentsDirectly
        public async Task<IActionResult> CheckAppointmentsDirectly()
        {
            try
            {
                var results = new System.Text.StringBuilder();

                // Direct SQL query to see what's really in the table
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Appointments";

                using var reader = await command.ExecuteReaderAsync();

                results.AppendLine("📋 Direct SQL query results:");
                results.AppendLine("=============================");

                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        results.AppendLine($"ID: {reader["Id"]}, Doctor: {reader["DoctorId"]}, Date: {reader["AppointmentDate"]}, Time: {reader["StartTime"]}, Status: {reader["Status"]}");
                    }
                }
                else
                {
                    results.AppendLine("No appointments found in the database");
                }

                await connection.CloseAsync();
                return Content(results.ToString());
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }

        // GET: /Test/SimpleAppointmentTest
        public async Task<IActionResult> SimpleAppointmentTest()
        {
            try
            {
                var results = new System.Text.StringBuilder();

                // Create a simple appointment without any complex relationships
                var appointment = new Appointment
                {
                    PatientId = 1,  // Hardcoded for testing
                    DoctorId = 1,   // Hardcoded for testing
                    AppointmentDate = DateTime.Today,
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    Location = "Test Location",
                    Status = "booked"
                };

                results.AppendLine("📝 Attempting to create appointment:");
                results.AppendLine($"- PatientId: {appointment.PatientId}");
                results.AppendLine($"- DoctorId: {appointment.DoctorId}");
                results.AppendLine($"- Date: {appointment.AppointmentDate}");
                results.AppendLine($"- Time: {appointment.StartTime}");

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                results.AppendLine("✅ Appointment created successfully!");
                results.AppendLine($"Appointment ID: {appointment.Id}");

                return Content(results.ToString());
            }
            catch (Exception ex)
            {
                return Content($"❌ Error: {ex.Message}\n\n{ex.StackTrace}");
            }
        }
        // GET: /Test/TestAppointmentSchema
        public async Task<IActionResult> TestAppointmentSchema()
        {
            try
            {
                // Test if we can query appointments without errors
                var appointmentCount = await _context.Appointments.CountAsync();
                var result = $"✅ Appointment table works! Found {appointmentCount} appointments.";

                // Test the availability check with raw SQL
                var testDate = DateTime.Today;
                var testTime = new TimeSpan(10, 0, 0);
                var testDoctorId = 13;

                var sql = @"
            SELECT COUNT(*)
            FROM Appointments
            WHERE DoctorId = @p0
            AND CAST(AppointmentDate AS DATE) = CAST(@p1 AS DATE)
            AND StartTime = @p2";

                var sqlResult = await _context.Database.ExecuteSqlRawAsync(sql, testDoctorId, testDate, testTime);
                result += $"\n✅ Raw SQL query works! Result: {sqlResult}";

                return Content(result);
            }
            catch (Exception ex)
            {
                return Content($"❌ Error: {ex.Message}\n\n{ex.InnerException?.Message}");
            }
        }
        // GET: /Test/TestTempData
        public IActionResult TestTempData()
        {
            try
            {
                // Test TempData
                TempData["TestValue"] = "Hello from TempData";
                Console.WriteLine("✅ TempData test value set");

                // Test redirect
                return RedirectToAction("TestTempDataResult");
            }
            catch (Exception ex)
            {
                return Content($"❌ TempData test failed: {ex.Message}");
            }
        }

        // GET: /Test/TestTempDataResult
        public IActionResult TestTempDataResult()
        {
            var testValue = TempData["TestValue"]?.ToString();
            Console.WriteLine($"✅ TempData test value retrieved: {testValue}");

            return Content($"TempData test: {testValue ?? "NULL"}");
        }
        // GET: /Test/TestTimeSlotHelper
        public async Task<IActionResult> TestTimeSlotHelper()
        {
            var results = new System.Text.StringBuilder();

            results.AppendLine("🧪 Testing TimeSlotHelper");
            results.AppendLine("==========================");

            // Test with known data
            var testDate = DateTime.Today.AddDays(1);
            var testTime = new TimeSpan(11, 0, 0);
            var doctorId = 13; // Use your actual doctor ID

            results.AppendLine($"Testing date: {testDate:yyyy-MM-dd}");
            results.AppendLine($"Testing time: {testTime:hh\\:mm}");
            results.AppendLine($"Doctor ID: {doctorId}");

            try
            {
                var isAvailable = await TimeSlotHelper.IsTimeSlotAvailableAsync(
                    testDate, testTime, doctorId, _context);

                results.AppendLine($"Time slot available: {isAvailable}");
            }
            catch (Exception ex)
            {
                results.AppendLine($"❌ TimeSlotHelper error: {ex.Message}");
            }

            return Content(results.ToString());
        }
        // GET: /Test/CheckAppointmentsTable
        public async Task<IActionResult> CheckAppointmentsTable()
        {
            try
            {
                var results = new System.Text.StringBuilder();

                // Check if appointments table exists and is accessible
                results.AppendLine("📊 Appointments Table Check");
                results.AppendLine("===========================");

                // Method 1: Raw SQL count
                try
                {
                    var count = await _context.Database.ExecuteSqlRawAsync("SELECT COUNT(*) FROM Appointments");
                    results.AppendLine($"Total appointments: {count}");
                }
                catch (Exception ex)
                {
                    results.AppendLine($"❌ Count failed: {ex.Message}");
                }

                // Method 2: EF query
                try
                {
                    var appointments = await _context.Appointments.Take(5).ToListAsync();
                    results.AppendLine($"EF query found: {appointments.Count} appointments");

                    foreach (var appt in appointments)
                    {
                        results.AppendLine($"- ID: {appt.Id}, Dr: {appt.DoctorId}, Date: {appt.AppointmentDate}, Time: {appt.StartTime}");
                    }
                }
                catch (Exception ex)
                {
                    results.AppendLine($"❌ EF query failed: {ex.Message}");
                }

                // Method 3: Check table structure
                try
                {
                    var columns = await _context.Database.SqlQueryRaw<string>(
                        "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Appointments' ORDER BY ORDINAL_POSITION"
                    ).ToListAsync();

                    results.AppendLine($"Table columns: {string.Join(", ", columns)}");
                }
                catch (Exception ex)
                {
                    results.AppendLine($"❌ Column check failed: {ex.Message}");
                }

                return Content(results.ToString());
            }
            catch (Exception ex)
            {
                return Content($"Error checking appointments table: {ex.Message}");
            }
        }

        // GET: /Test/TestTimeSlotDirectly
        public async Task<IActionResult> TestTimeSlotDirectly()
        {
            try
            {
                var results = new System.Text.StringBuilder();

                results.AppendLine("🧪 Direct Time Slot Test");
                results.AppendLine("========================");

                // Test parameters
                var testDate = DateTime.Today.AddDays(1);
                var testTime = new TimeSpan(11, 0, 0);
                var doctorId = 13;

                results.AppendLine($"Testing: Date={testDate:yyyy-MM-dd}, Time={testTime:hh\\:mm}, Doctor={doctorId}");

                // Test direct SQL
                var sql = @"
            SELECT COUNT(*) 
            FROM Appointments 
            WHERE DoctorId = @doctorId 
            AND CAST(AppointmentDate AS DATE) = @date
            AND StartTime = @time";

                var parameters = new[]
                {
            new SqlParameter("@doctorId", doctorId),
            new SqlParameter("@date", testDate.Date),
            new SqlParameter("@time", testTime)
        };

                var count = await _context.Database.ExecuteSqlRawAsync(sql, parameters);
                results.AppendLine($"Direct SQL count: {count}");

                // Test with cancelled status
                var sqlWithStatus = @"
            SELECT COUNT(*) 
            FROM Appointments 
            WHERE DoctorId = @doctorId 
            AND CAST(AppointmentDate AS DATE) = @date
            AND StartTime = @time
            AND (Status IS NULL OR Status != 'cancelled')";

                var countWithStatus = await _context.Database.ExecuteSqlRawAsync(sqlWithStatus, parameters);
                results.AppendLine($"With status check: {countWithStatus}");

                return Content(results.ToString());
            }
            catch (Exception ex)
            {
                return Content($"Direct test error: {ex.Message}");
            }
        }
        // GET: /Test/TestCompleteBooking
        public async Task<IActionResult> TestCompleteBooking()
        {
            try
            {
                // Create test booking data
                var testModel = new BookAppointmentViewModel
                {
                    DoctorId = 13,
                    DoctorName = "Dr. John Smith",
                    ConsultationFee = 150.00m,
                    SelectedLocation = "Main Hospital",
                    AppointmentDate = DateTime.Today.AddDays(1),
                    StartTime = new TimeSpan(11, 0, 0)
                };

                // Test availability check
                var isAvailable = await TimeSlotHelper.IsTimeSlotAvailableAsync(
                    testModel.AppointmentDate.Value,
                    testModel.StartTime.Value,
                    testModel.DoctorId.Value,
                    _context
                );

                var results = new System.Text.StringBuilder();
                results.AppendLine("✅ Complete Booking Test");
                results.AppendLine("========================");
                results.AppendLine($"Time slot available: {isAvailable}");
                results.AppendLine($"Doctor: Dr. {testModel.DoctorName}");
                results.AppendLine($"Date: {testModel.AppointmentDate.Value:yyyy-MM-dd}");
                results.AppendLine($"Time: {testModel.StartTime.Value:hh\\:mm}");
                results.AppendLine($"Location: {testModel.SelectedLocation}");

                return Content(results.ToString());
            }
            catch (Exception ex)
            {
                return Content($"Test error: {ex.Message}");
            }
        }
        // GET: /Test/DebugModelValidation
        public IActionResult DebugModelValidation()
        {
            var results = new System.Text.StringBuilder();

            // Test creating a model and checking validation
            var testModel = new BookAppointmentViewModel
            {
                DoctorId = 13,
                DoctorName = "Test Doctor",
                ConsultationFee = 150.00m,
                SelectedLocation = "Main Hospital",
                AppointmentDate = DateTime.Today.AddDays(1),
                StartTime = new TimeSpan(11, 0, 0)
            };

            results.AppendLine("🧪 Testing BookAppointmentViewModel Validation");
            results.AppendLine("==============================================");

            results.AppendLine($"DoctorId: {testModel.DoctorId}");
            results.AppendLine($"DoctorName: {testModel.DoctorName}");
            results.AppendLine($"ConsultationFee: {testModel.ConsultationFee}");
            results.AppendLine($"SelectedLocation: {testModel.SelectedLocation}");
            results.AppendLine($"AppointmentDate: {testModel.AppointmentDate}");
            results.AppendLine($"StartTime: {testModel.StartTime}");

            // Test required fields
            results.AppendLine("");
            results.AppendLine("✅ Required Fields Check:");
            results.AppendLine($"DoctorId has value: {testModel.DoctorId.HasValue}");
            results.AppendLine($"AppointmentDate has value: {testModel.AppointmentDate.HasValue}");
            results.AppendLine($"StartTime has value: {testModel.StartTime.HasValue}");
            results.AppendLine($"SelectedLocation not empty: {!string.IsNullOrEmpty(testModel.SelectedLocation)}");

            return Content(results.ToString());
        }
        // GET: /Test/SessionDebug
        public IActionResult SessionDebug()
        {
            var results = new System.Text.StringBuilder();

            results.AppendLine("🔍 Session Debug Information");
            results.AppendLine("============================");

            // Check if session is working
            results.AppendLine($"Session ID: {HttpContext.Session.Id}");
            results.AppendLine($"Session IsAvailable: {HttpContext.Session.IsAvailable}");

            // Check booking data
            var bookingData = HttpContext.Session.GetString("BookingData");
            results.AppendLine($"Booking Data in Session: {!string.IsNullOrEmpty(bookingData)}");

            if (!string.IsNullOrEmpty(bookingData))
            {
                try
                {
                    var model = System.Text.Json.JsonSerializer.Deserialize<BookAppointmentViewModel>(bookingData);
                    results.AppendLine($"- DoctorId: {model.DoctorId}");
                    results.AppendLine($"- DoctorName: {model.DoctorName}");
                    results.AppendLine($"- Date: {model.AppointmentDate}");
                    results.AppendLine($"- Time: {model.StartTime}");
                    results.AppendLine($"- Location: {model.SelectedLocation}");
                }
                catch (Exception ex)
                {
                    results.AppendLine($"- Deserialization Error: {ex.Message}");
                }
            }

            // Check TempData
            var tempData = TempData["BookingData"]?.ToString();
            results.AppendLine($"Booking Data in TempData: {!string.IsNullOrEmpty(tempData)}");

            return Content(results.ToString());
        }
        // GET: /Test/DebugBookingFlow
        public async Task<IActionResult> DebugBookingFlow()
        {
            try
            {
                var results = new System.Text.StringBuilder();

                // Check session data
                var bookingData = HttpContext.Session.GetString("BookingData");
                results.AppendLine("📋 Session Booking Data:");
                results.AppendLine($"- Has data: {!string.IsNullOrEmpty(bookingData)}");

                if (!string.IsNullOrEmpty(bookingData))
                {
                    var model = System.Text.Json.JsonSerializer.Deserialize<BookAppointmentViewModel>(bookingData);
                    results.AppendLine($"- DoctorId: {model.DoctorId}");
                    results.AppendLine($"- Date: {model.AppointmentDate}");
                    results.AppendLine($"- Time: {model.StartTime}");
                    results.AppendLine($"- Location: {model.SelectedLocation}");
                }

                // Check model binding
                results.AppendLine("\n🔍 Model Binding Test:");
                results.AppendLine("Try submitting with these values:");

                var doctor = await _context.Doctors.FirstOrDefaultAsync();
                if (doctor != null)
                {
                    results.AppendLine($"- DoctorId: {doctor.Id}");
                    results.AppendLine($"- Date: {DateTime.Today.AddDays(1):yyyy-MM-dd}");
                    results.AppendLine($"- Time: 09:00:00");
                    results.AppendLine($"- Location: Main Hospital");
                }

                return Content(results.ToString());
            }
            catch (Exception ex)
            {
                return Content($"Debug error: {ex.Message}");
            }
        }
        // GET: /Test/DetailedSchemaCheck
        public async Task<IActionResult> DetailedSchemaCheck()
        {
            try
            {
                var schemaInfo = new System.Text.StringBuilder();

                // Check what columns actually exist in the database
                schemaInfo.AppendLine("Actual Database Columns in DoctorAvailabilities:");
                try
                {
                    var connection = _context.Database.GetDbConnection();
                    await connection.OpenAsync();

                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'DoctorAvailabilities' 
                ORDER BY ORDINAL_POSITION";

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader.GetString(0);
                        var dataType = reader.GetString(1);
                        var nullable = reader.GetString(2);
                        schemaInfo.AppendLine($"- {columnName} ({dataType}, Nullable: {nullable})");
                    }

                    await connection.CloseAsync();
                }
                catch (Exception ex)
                {
                    schemaInfo.AppendLine($"Error reading columns: {ex.Message}");
                }

                // Check what Entity Framework thinks the columns should be
                schemaInfo.AppendLine("\nEntity Framework Expected Columns:");
                try
                {
                    var entityType = _context.Model.FindEntityType(typeof(DoctorAvailability));
                    foreach (var property in entityType.GetProperties())
                    {
                        schemaInfo.AppendLine($"- {property.Name} ({property.ClrType.Name})");
                    }
                }
                catch (Exception ex)
                {
                    schemaInfo.AppendLine($"Error reading EF model: {ex.Message}");
                }

                // Check relationships
                schemaInfo.AppendLine("\nEntity Framework Relationships:");
                try
                {
                    var entityType = _context.Model.FindEntityType(typeof(DoctorAvailability));
                    var foreignKeys = entityType.GetForeignKeys();
                    foreach (var fk in foreignKeys)
                    {
                        schemaInfo.AppendLine($"- Foreign Key: {fk.PrincipalEntityType.Name}");
                        foreach (var property in fk.Properties)
                        {
                            schemaInfo.AppendLine($"  - Property: {property.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    schemaInfo.AppendLine($"Error reading relationships: {ex.Message}");
                }

                return Content(schemaInfo.ToString());
            }
            catch (Exception ex)
            {
                return Content($"Error in detailed schema check: {ex.Message}");
            }
        }

        // GET: /Test/AddDoctorAvailabilities
        public async Task<IActionResult> AddDoctorAvailabilities()
        {
            try
            {
                var results = new System.Text.StringBuilder();

                // Get the existing doctors
                var doctors = await _context.Doctors
                    .Include(d => d.User)
                    .ToListAsync();

                if (!doctors.Any())
                {
                    return Content("❌ No doctors found. Please create doctors first.");
                }

                // Define availability patterns for each specialty
                var availabilityData = new[]
                {
            // Cardiology - Dr. John Smith (ID: 13)
            new { DoctorId = 13, Days = new[] { "Monday", "Wednesday", "Friday" },
                  StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0),
                  Location = "Heart Center" },
            
            // Pediatrics - Dr. Sarah Johnson (ID: 14)
            new { DoctorId = 14, Days = new[] { "Tuesday", "Thursday", "Saturday" },
                  StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0),
                  Location = "Children's Hospital" },
            
            // Neurology - Dr. Michael Brown (ID: 15)
            new { DoctorId = 15, Days = new[] { "Monday", "Wednesday", "Friday" },
                  StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(18, 0, 0),
                  Location = "Neuro Center" },
            
            // Dermatology - Dr. Emily Davis (ID: 16)
            new { DoctorId = 16, Days = new[] { "Tuesday", "Thursday", "Saturday" },
                  StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0),
                  Location = "Skin Care Clinic" },
            
            // Orthopedics - Dr. Robert Wilson (ID: 17)
            new { DoctorId = 17, Days = new[] { "Monday", "Wednesday", "Friday" },
                  StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0),
                  Location = "Sports Medicine Center" }
        };

                // Add multiple locations for each doctor
                var additionalLocations = new[]
                {
            new { DoctorId = 13, Days = new[] { "Tuesday" },
                  StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(19, 0, 0),
                  Location = "Main Hospital" },

            new { DoctorId = 14, Days = new[] { "Monday" },
                  StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(16, 0, 0),
                  Location = "Pediatric Clinic" },

            new { DoctorId = 15, Days = new[] { "Thursday" },
                  StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(14, 0, 0),
                  Location = "Research Institute" },

            new { DoctorId = 16, Days = new[] { "Wednesday" },
                  StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(19, 0, 0),
                  Location = "Cosmetic Center" },

            new { DoctorId = 17, Days = new[] { "Thursday" },
                  StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0),
                  Location = "Orthopedic Surgery Unit" }
        };

                var allAvailabilities = new List<DoctorAvailability>();
                var addedCount = 0;

                // Add primary availabilities
                foreach (var data in availabilityData)
                {
                    var doctor = doctors.FirstOrDefault(d => d.Id == data.DoctorId);
                    if (doctor == null)
                    {
                        results.AppendLine($"❌ Doctor with ID {data.DoctorId} not found");
                        continue;
                    }

                    foreach (var day in data.Days)
                    {
                        var availability = new DoctorAvailability
                        {
                            DoctorId = data.DoctorId,
                            Day = day,
                            StartTime = data.StartTime,
                            EndTime = data.EndTime,
                            Location = data.Location
                        };

                        allAvailabilities.Add(availability);
                        addedCount++;
                    }

                    results.AppendLine($"✅ Added {data.Days.Length} availabilities for Dr. {doctor.User?.Name} ({doctor.Specialty})");
                }

                // Add additional locations
                foreach (var data in additionalLocations)
                {
                    var doctor = doctors.FirstOrDefault(d => d.Id == data.DoctorId);
                    if (doctor == null) continue;

                    foreach (var day in data.Days)
                    {
                        var availability = new DoctorAvailability
                        {
                            DoctorId = data.DoctorId,
                            Day = day,
                            StartTime = data.StartTime,
                            EndTime = data.EndTime,
                            Location = data.Location
                        };

                        allAvailabilities.Add(availability);
                        addedCount++;
                    }

                    results.AppendLine($"✅ Added additional location for Dr. {doctor.User?.Name}");
                }

                // Add to database
                _context.DoctorAvailabilities.AddRange(allAvailabilities);
                await _context.SaveChangesAsync();

                results.AppendLine($"\n🎉 Successfully added {addedCount} availability records!");
                results.AppendLine("\n📋 Summary of availabilities:");

                // Display summary
                foreach (var doctor in doctors.OrderBy(d => d.Specialty))
                {
                    var doctorAvailabilities = allAvailabilities
                        .Where(a => a.DoctorId == doctor.Id)
                        .ToList();

                    results.AppendLine($"\nDr. {doctor.User?.Name} ({doctor.Specialty}):");
                    foreach (var avail in doctorAvailabilities)
                    {
                        results.AppendLine($"  - {avail.Day}: {avail.StartTime:hh\\:mm} to {avail.EndTime:hh\\:mm} at {avail.Location}");
                    }
                }

                return Content(results.ToString());
            }
            catch (Exception ex)
            {
                return Content($"❌ Error adding availabilities: {ex.Message}\n\n{ex.InnerException?.Message}");
            }
        }
        // GET: /Test/CheckSchema
        public async Task<IActionResult> CheckSchema()
        {
            try
            {
                var schemaInfo = new System.Text.StringBuilder();

                // Check DoctorAvailabilities table columns
                schemaInfo.AppendLine("DoctorAvailabilities Table Columns:");
                try
                {
                    var columns = await _context.DoctorAvailabilities
                        .FromSqlRaw("SELECT TOP 0 * FROM DoctorAvailabilities")
                        .ToArrayAsync();

                    // Get column names using reflection (this is a workaround)
                    var entityType = _context.Model.FindEntityType(typeof(DoctorAvailability));
                    var properties = entityType.GetProperties();

                    foreach (var property in properties)
                    {
                        schemaInfo.AppendLine($"- {property.Name} ({property.ClrType.Name})");
                    }
                }
                catch (Exception ex)
                {
                    schemaInfo.AppendLine($"Error reading schema: {ex.Message}");
                }

                // Check if DoctorId1 column exists
                schemaInfo.AppendLine("\nChecking for DoctorId1 column:");
                try
                {
                    var hasDoctorId1 = await _context.Database.ExecuteSqlRawAsync(
                        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DoctorAvailabilities' AND COLUMN_NAME = 'DoctorId1'"
                    ) > 0;

                    schemaInfo.AppendLine($"DoctorId1 column exists: {hasDoctorId1}");
                }
                catch (Exception ex)
                {
                    schemaInfo.AppendLine($"Error checking for DoctorId1: {ex.Message}");
                }

                return Content(schemaInfo.ToString());
            }
            catch (Exception ex)
            {
                return Content($"Error checking schema: {ex.Message}");
            }
        }
        // GET: /Test/SeedData
        public async Task<IActionResult> SeedData()
        {
            try
            {
                // Clear existing data (optional - be careful in production!)
                _context.Users.RemoveRange(_context.Users);
                await _context.SaveChangesAsync();

                // Create users
                var adminUser = new User { Email = "admin@clinic.com", PasswordHash = "admin123", Role = "admin", Name = "Admin User" };
                var doctorUser = new User { Email = "dr.smith@clinic.com", PasswordHash = "doctor123", Role = "doctor", Name = "Dr. John Smith" };
                var patientUser = new User { Email = "patient@example.com", PasswordHash = "patient123", Role = "patient", Name = "John Doe" };

                _context.Users.AddRange(adminUser, doctorUser, patientUser);
                await _context.SaveChangesAsync();

                // Create doctor
                var doctor = new Doctor { UserId = doctorUser.Id, Specialty = "Cardiology", ConsultationFee = 100.00m, Bio = "Experienced cardiologist with 10 years of experience." };
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                // Create patient
                var patient = new Patient { UserId = patientUser.Id, Age = 30, Phone = "123-456-7890" };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                // Create doctor availability
                var availability = new DoctorAvailability
                {
                    DoctorId = doctor.Id,
                    Day = "Monday",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    Location = "New York Clinic"
                };
                _context.DoctorAvailabilities.Add(availability);
                await _context.SaveChangesAsync();

                // Create appointment
                var appointment = new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    AppointmentDate = DateTime.Today.AddDays(1),
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    Location = "New York Clinic",
                    Status = "booked"
                };
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                // Create payment
                var payment = new Payment
                {
                    AppointmentId = appointment.Id,
                    Amount = doctor.ConsultationFee,
                    Status = "pending"
                };
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Create patient notes
                var patientNotes = new PatientNotes
                {
                    AppointmentId = appointment.Id,
                    Notes = "Patient complained of chest pain. Recommended further tests.",
                    Prescription = "Aspirin 100mg daily for 7 days"
                };
                _context.PatientNotes.Add(patientNotes);
                await _context.SaveChangesAsync();

                // Create feedback
                var feedback = new Feedback
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    //AppointmentId = appointment.Id,
                    Rating = 5,
                    Comment = "Excellent doctor! Very professional and helpful.",
                    CreatedAt = DateTime.Now
                };
                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                return Content("✅ Test data seeded successfully! \n\n" +
                             $"Admin: {adminUser.Email} \n" +
                             $"Doctor: {doctorUser.Name} \n" +
                             $"Patient: {patientUser.Name} \n" +
                             $"Appointment: {appointment.AppointmentDate:yyyy-MM-dd} at {appointment.StartTime}");
            }
            catch (Exception ex)
            {
                return Content($"❌ Error seeding data: {ex.Message}\n\n{ex.InnerException?.Message}");
            }
        }

        // GET: /Test/CheckData
        public async Task<IActionResult> CheckData()
        {
            try
            {
                var data = new
                {
                    UserCount = await _context.Users.CountAsync(),
                    DoctorCount = await _context.Doctors.CountAsync(),
                    PatientCount = await _context.Patients.CountAsync(),
                    AppointmentCount = await _context.Appointments.CountAsync(),
                    AvailabilityCount = await _context.DoctorAvailabilities.CountAsync(),
                    PaymentCount = await _context.Payments.CountAsync(),
                    FeedbackCount = await _context.Feedbacks.CountAsync(),

                    Users = await _context.Users.ToListAsync(),
                    Doctors = await _context.Doctors.Include(d => d.User).ToListAsync(),
                    Patients = await _context.Patients.Include(p => p.User).ToListAsync(),
                    Appointments = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.Patient)
                        .ToListAsync()
                };

                return Json(data);
            }
            catch (Exception ex)
            {
                return Content($"❌ Error checking data: {ex.Message}");
            }
        }

        // GET: /Test/TestRelationships
        public async Task<IActionResult> TestRelationships()
        {
            try
            {
                var appointmentWithDetails = await _context.Appointments
                    .Include(a => a.Doctor)
                        .ThenInclude(d => d.User)
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    //.Include(a => a.Payments)
                    .FirstOrDefaultAsync();

                if (appointmentWithDetails == null)
                    return Content("No appointments found. Run /Test/SeedData first.");

                return Json(new
                {
                    Appointment = appointmentWithDetails,
                    DoctorName = appointmentWithDetails.Doctor?.User?.Name,
                    PatientName = appointmentWithDetails.Patient?.User?.Name,
                    //PaymentStatus = appointmentWithDetails.Payments?.FirstOrDefault()?.Status
                });
            }
            catch (Exception ex)
            {
                return Content($"❌ Error testing relationships: {ex.Message}");
            }
        }

        // GET: /Test/CreateTestDoctors
        public async Task<IActionResult> CreateTestDoctors()
        {
            try
            {
                // First, check if we already have test doctors to avoid duplicates
                var existingDoctors = await _context.Doctors
                    .Include(d => d.User)
                    .Where(d => d.User.Email.Contains("@clinic.com"))
                    .ToListAsync();

                if (existingDoctors.Any())
                {
                    return Content("✅ Test doctors already exist!\n\n" +
                                 string.Join("\n", existingDoctors.Select(d => $"{d.User.Name} - {d.Specialty}")));
                }

                // Create doctor users
                var doctorUsers = new List<User>
        {
            new User { Email = "doctor1@clinic.com", PasswordHash = HashPassword("doctor1"), Role = "doctor", Name = "Dr. John Smith" },
            new User { Email = "doctor2@clinic.com", PasswordHash = HashPassword("doctor2"), Role = "doctor", Name = "Dr. Sarah Johnson" },
            new User { Email = "doctor3@clinic.com", PasswordHash = HashPassword("doctor3"), Role = "doctor", Name = "Dr. Michael Brown" },
            new User { Email = "doctor4@clinic.com", PasswordHash = HashPassword("doctor4"), Role = "doctor", Name = "Dr. Emily Davis" },
            new User { Email = "doctor5@clinic.com", PasswordHash = HashPassword("doctor5"), Role = "doctor", Name = "Dr. Robert Wilson" }
        };

                _context.Users.AddRange(doctorUsers);
                await _context.SaveChangesAsync();

                // Create doctors with specialties
                var doctors = new List<Doctor>
        {
            new Doctor { UserId = doctorUsers[0].Id, Specialty = "Cardiology", ConsultationFee = 150.00m, Bio = "Board-certified cardiologist with 12 years of experience. Specializes in heart disease prevention and treatment." },
            new Doctor { UserId = doctorUsers[1].Id, Specialty = "Pediatrics", ConsultationFee = 120.00m, Bio = "Pediatric specialist with a gentle approach to child healthcare. Loves working with children and families." },
            new Doctor { UserId = doctorUsers[2].Id, Specialty = "Neurology", ConsultationFee = 180.00m, Bio = "Neurology expert with extensive research background in neurodegenerative diseases. Published multiple research papers." },
            new Doctor { UserId = doctorUsers[3].Id, Specialty = "Dermatology", ConsultationFee = 130.00m, Bio = "Dermatologist specializing in skin cancer prevention and cosmetic dermatology. Certified by the American Board of Dermatology." },
            new Doctor { UserId = doctorUsers[4].Id, Specialty = "Orthopedics", ConsultationFee = 160.00m, Bio = "Orthopedic surgeon specializing in sports injuries and joint replacements. Former team doctor for college athletics." }
        };

                _context.Doctors.AddRange(doctors);
                await _context.SaveChangesAsync();

                // Create availability for doctors
                var availabilities = new List<DoctorAvailability>
        {
            // Doctor 1 - Cardiologist
            new DoctorAvailability { DoctorId = doctors[0].Id, Day = "Monday", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), Location = "Main Hospital" },
            new DoctorAvailability { DoctorId = doctors[0].Id, Day = "Wednesday", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(16, 0, 0), Location = "Heart Center" },
            new DoctorAvailability { DoctorId = doctors[0].Id, Day = "Friday", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(14, 0, 0), Location = "Main Hospital" },
            
            // Doctor 2 - Pediatrician
            new DoctorAvailability { DoctorId = doctors[1].Id, Day = "Tuesday", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(15, 0, 0), Location = "Children's Hospital" },
            new DoctorAvailability { DoctorId = doctors[1].Id, Day = "Thursday", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), Location = "Main Hospital" },
            new DoctorAvailability { DoctorId = doctors[1].Id, Day = "Saturday", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(14, 0, 0), Location = "Pediatric Clinic" },
            
            // Doctor 3 - Neurologist
            new DoctorAvailability { DoctorId = doctors[2].Id, Day = "Monday", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(12, 0, 0), Location = "Neuro Center" },
            new DoctorAvailability { DoctorId = doctors[2].Id, Day = "Wednesday", StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(17, 0, 0), Location = "Main Hospital" },
            new DoctorAvailability { DoctorId = doctors[2].Id, Day = "Friday", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(16, 0, 0), Location = "Research Institute" },
            
            // Doctor 4 - Dermatologist
            new DoctorAvailability { DoctorId = doctors[3].Id, Day = "Tuesday", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(18, 0, 0), Location = "Skin Care Center" },
            new DoctorAvailability { DoctorId = doctors[3].Id, Day = "Thursday", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0), Location = "Main Hospital" },
            new DoctorAvailability { DoctorId = doctors[3].Id, Day = "Saturday", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(13, 0, 0), Location = "Cosmetic Clinic" },
            
            // Doctor 5 - Orthopedist
            new DoctorAvailability { DoctorId = doctors[4].Id, Day = "Monday", StartTime = new TimeSpan(7, 0, 0), EndTime = new TimeSpan(15, 0, 0), Location = "Sports Medicine Center" },
            new DoctorAvailability { DoctorId = doctors[4].Id, Day = "Wednesday", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0), Location = "Main Hospital" },
            new DoctorAvailability { DoctorId = doctors[4].Id, Day = "Friday", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), Location = "Orthopedic Surgery Unit" }
        };


                return Content("✅ Test doctors created successfully!\n\n" +
                             "Doctors created:\n" +
                             "1. Dr. John Smith - Cardiology ($150.00)\n" +
                             "2. Dr. Sarah Johnson - Pediatrics ($120.00)\n" +
                             "3. Dr. Michael Brown - Neurology ($180.00)\n" +
                             "4. Dr. Emily Davis - Dermatology ($130.00)\n" +
                             "5. Dr. Robert Wilson - Orthopedics ($160.00)\n\n" +
                             "Each doctor has multiple availability slots across different locations.\n" +
                             "Sample appointments also created to demonstrate booking system.");
            }
            catch (Exception ex)
            {
                // Detailed error information
                var errorMessage = $"❌ Error creating test doctors: {ex.Message}";

                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";

                    if (ex.InnerException.InnerException != null)
                    {
                        errorMessage += $"\n\nInner Inner Exception: {ex.InnerException.InnerException.Message}";
                    }
                }

                errorMessage += $"\n\nStack Trace: {ex.StackTrace}";

                return Content(errorMessage);
            }
        }
        // GET: /Test/AddAdmin
        public async Task<IActionResult> AddAdmin()
        {
            try
            {
                // Check if admin already exists
                var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@clinic.com");
                if (existingAdmin != null)
                {
                    return Content("❌ Admin user already exists!\n\n" +
                                 $"Email: admin@clinic.com\n" +
                                 $"Password: admin123");
                }

                // Create admin user
                var adminUser = new User
                {
                    Email = "admin@clinic.com",
                    PasswordHash = HashPassword("admin123"),
                    Role = "admin",
                    Name = "System Administrator"
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                return Content("✅ Admin user created successfully!\n\n" +
                             $"Email: admin@clinic.com\n" +
                             $"Password: admin123\n" +
                             $"Role: admin\n\n" +
                             $"You can now login at: /Account/Login");
            }
            catch (Exception ex)
            {
                return Content($"❌ Error creating admin user: {ex.Message}\n\n{ex.InnerException?.Message}");
            }
        }
        // GET: /Test/CreateTestUsers
        public async Task<IActionResult> CreateTestUsers()
        {
            try
            {
                // Create admin user
                var adminUser = new User
                {
                    Email = "admin1@clinic.com",
                    PasswordHash = HashPassword("admin1"),
                    Role = "admin",
                    Name = "Admin User"
                };

                // Create first doctor user
                var doctorUser1 = new User
                {
                    Email = "doctor1@clinic.com",
                    PasswordHash = HashPassword("doctor1"),
                    Role = "doctor",
                    Name = "Dr. John Smith"
                };

                // Create second doctor user
                var doctorUser2 = new User
                {
                    Email = "doctor2@clinic.com",
                    PasswordHash = HashPassword("doctor2"),
                    Role = "doctor",
                    Name = "Dr. Sarah Johnson"
                };

                _context.Users.AddRange(adminUser, doctorUser1, doctorUser2);
                await _context.SaveChangesAsync();

                // Create doctor profiles
                var doctor1 = new Doctor
                {
                    UserId = doctorUser1.Id,
                    Specialty = "Cardiology",
                    ConsultationFee = 100.00m,
                    Bio = "Experienced cardiologist with 10 years of experience."
                };

                var doctor2 = new Doctor
                {
                    UserId = doctorUser2.Id,
                    Specialty = "Pediatrics",
                    ConsultationFee = 80.00m,
                    Bio = "Pediatric specialist with focus on child healthcare."
                };

                _context.Doctors.AddRange(doctor1, doctor2);
                await _context.SaveChangesAsync();

                return Content("✅ Test users created successfully!\n\n" +
                             $"Admin: admin1@clinic.com / admin1\n" +
                             $"Doctor 1: doctor1@clinic.com / doctor1 (Cardiologist)\n" +
                             $"Doctor 2: doctor2@clinic.com / doctor2 (Pediatrician)");
            }
            catch (Exception ex)
            {
                return Content($"❌ Error creating test users: {ex.Message}\n\n{ex.InnerException?.Message}");
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
        // GET: /Test/ClearData (Use with caution!)
        public async Task<IActionResult> ClearData()
        {
            try
            {
                _context.Feedbacks.RemoveRange(_context.Feedbacks);
                _context.PatientNotes.RemoveRange(_context.PatientNotes);
                _context.Payments.RemoveRange(_context.Payments);
                _context.Appointments.RemoveRange(_context.Appointments);
                _context.DoctorAvailabilities.RemoveRange(_context.DoctorAvailabilities);
                _context.Patients.RemoveRange(_context.Patients);
                _context.Doctors.RemoveRange(_context.Doctors);
                _context.Users.RemoveRange(_context.Users);

                await _context.SaveChangesAsync();

                return Content("✅ All data cleared successfully!");
            }
            catch (Exception ex)
            {
                return Content($"❌ Error clearing data: {ex.Message}");
            }
        }
    }
}