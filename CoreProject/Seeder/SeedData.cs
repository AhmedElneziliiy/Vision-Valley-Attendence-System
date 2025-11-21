using CoreProject.Context;
using CoreProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreProject.Seeder
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            // 1. Migrate DB
            await context.Database.MigrateAsync();

            // 2. Seed Roles
            await SeedRoles(roleManager);

            // 3. Seed Core Data (Org, Branch, Dept, Timetable)
            await SeedOrganizationAndBranch(context);
            await SeedDepartments(context);
            await SeedTimetables(context);
            await SeedDevices(context);
            await SeedAttendanceActionTypes(context);

            // 4. Seed Users (one per role)
            await SeedUsers(userManager, context);
            await SeedAttendance(context, userManager);
            await SeedBranchesAndUsers(context, userManager);

            await context.SaveChangesAsync();
        }
        private static async Task SeedBranchesAndUsers(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.Branches.Any()) return;

            var org = await context.Organizations.FirstOrDefaultAsync();
            if (org == null)
            {
                org = new Organization
                {
                    Name = "Vision Valley",
                    LogoUrl = "https://visionvalley.com/logo.png",
                    PassThrough = false,
                    StartTime = TimeSpan.FromHours(9),
                    EndTime = TimeSpan.FromHours(17)
                };
                context.Organizations.Add(org);
                await context.SaveChangesAsync();
            }

            var departments = new[]
            {
        new Department { Name = "IT", IsActive = true, CreatedAt = DateTime.UtcNow },
        new Department { Name = "HR", IsActive = true, CreatedAt = DateTime.UtcNow },
        new Department { Name = "Finance", IsActive = true, CreatedAt = DateTime.UtcNow },
        new Department { Name = "Operations", IsActive = true, CreatedAt = DateTime.UtcNow }
    };

            var timetable = new Timetable
            {
                Name = "Standard 9-5",
                WorkingDayStartingHourMinimum = "08:00",
                WorkingDayStartingHourMaximum = "09:30",
                WorkingDayEndingHour = "17:00",
                AverageWorkingHours = 8.0f,
                IsWorkingDayEndingHourEnable = true,
                IsActive = true
            };

            // === BRANCH 1: Dubai HQ ===
            var dubai = new Branch
            {
                Name = "Dubai HQ",
                OrganizationID = org.ID,
                TimeZone = 4,
                Weekend = "Friday,Saturday",
                NationalHolidays = "[\"2025-01-01\",\"2025-12-02\"]",
                IsMainBranch = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Branches.Add(dubai);

            // === BRANCH 2: Cairo ===
            var cairo = new Branch
            {
                Name = "Cairo",
                OrganizationID = org.ID,
                TimeZone = 2,
                Weekend = "Friday,Saturday",
                NationalHolidays = "[\"2025-01-01\",\"2025-10-06\"]",
                IsMainBranch = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Branches.Add(cairo);

            // === BRANCH 3: Alexandria ===
            var alex = new Branch
            {
                Name = "Alexandria",
                OrganizationID = org.ID,
                TimeZone = 2,
                Weekend = "Friday,Saturday",
                NationalHolidays = "[\"2025-01-01\",\"2025-10-06\"]",
                IsMainBranch = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Branches.Add(alex);

            await context.SaveChangesAsync();

            // Assign departments & timetable to each branch
            var branches = new[] { dubai, cairo, alex };
            foreach (var branch in branches)
            {
                foreach (var dept in departments)
                {
                    dept.BranchID = branch.ID;
                    context.Departments.Add(dept);
                }
                timetable.BranchID = branch.ID;
                context.Timetables.Add(timetable);
                await context.SaveChangesAsync();
            }

            // Get fresh IDs
            var itDeptId = await context.Departments.Where(d => d.Name == "IT").Select(d => d.ID).FirstAsync();
            var hrDeptId = await context.Departments.Where(d => d.Name == "HR").Select(d => d.ID).FirstAsync();
            var opsDeptId = await context.Departments.Where(d => d.Name == "Operations").Select(d => d.ID).FirstAsync();
            var financeDeptId = await context.Departments.Where(d => d.Name == "Finance").Select(d => d.ID).FirstAsync();
            var timetableId = await context.Timetables.Select(t => t.ID).FirstAsync();

            // === USERS PER BRANCH ===
            var branchUsers = new[]
            {
        // Dubai HQ
        ("Dubai", dubai.ID, new[]
        {
            ("hr.dubai@visionvalley.com", "HR Dubai", "HR", hrDeptId, "+971501111111"),
            ("manager.dubai@visionvalley.com", "Manager Dubai", "Manager", opsDeptId, "+971502222222"),
            ("emp1.dubai@visionvalley.com", "Emp1 Dubai", "Employee", financeDeptId, "+971503333333"),
            ("emp2.dubai@visionvalley.com", "Emp2 Dubai", "Employee", itDeptId, "+971504444444"),
            ("emp3.dubai@visionvalley.com", "Emp3 Dubai", "Employee", opsDeptId, "+971505555555")
        }),
        // Cairo
        ("Cairo", cairo.ID, new[]
        {
            ("hr.cairo@visionvalley.com", "HR Cairo", "HR", hrDeptId, "+201001111111"),
            ("manager.cairo@visionvalley.com", "Manager Cairo", "Manager", opsDeptId, "+201002222222"),
            ("emp1.cairo@visionvalley.com", "Emp1 Cairo", "Employee", financeDeptId, "+201003333333"),
            ("emp2.cairo@visionvalley.com", "Emp2 Cairo", "Employee", itDeptId, "+201004444444"),
            ("emp3.cairo@visionvalley.com", "Emp3 Cairo", "Employee", opsDeptId, "+201005555555")
        }),
        // Alexandria
        ("Alexandria", alex.ID, new[]
        {
            ("hr.alex@visionvalley.com", "HR Alex", "HR", hrDeptId, "+203001111111"),
            ("manager.alex@visionvalley.com", "Manager Alex", "Manager", opsDeptId, "+203002222222"),
            ("emp1.alex@visionvalley.com", "Emp1 Alex", "Employee", financeDeptId, "+203003333333"),
            ("emp2.alex@visionvalley.com", "Emp2 Alex", "Employee", itDeptId, "+203004444444"),
            ("emp3.alex@visionvalley.com", "Emp3 Alex", "Employee", opsDeptId, "+203005555555")
        })
    };

            foreach (var (branchName, branchId, users) in branchUsers)
            {
                foreach (var (email, name, role, deptId, mobile) in users)
                {
                    var user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            DisplayName = name,
                            Mobile = mobile,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            BranchID = branchId,
                            DepartmentID = deptId,
                            TimetableID = timetableId,
                            Gender = 'M',
                            VacationBalance = 21,
                            Address = $"{branchName}, Egypt"
                        };

                        var result = await userManager.CreateAsync(user, "Pass@123");
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, role);
                        }
                    }
                }
            }
        }
        private static async Task SeedRoles(RoleManager<IdentityRole<int>> roleManager)
        {
            string[] roles = { "Admin", "HR", "Manager", "Employee" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(role));
                }
            }
        }

        private static async Task SeedOrganizationAndBranch(ApplicationDbContext context)
        {
            if (context.Organizations.Any()) return;

            var org = new Organization
            {
                Name = "Vision Valley",
                LogoUrl = "https://visionvalley.com/logo.png",
                PassThrough = false,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(17)
            };
            context.Organizations.Add(org);
            await context.SaveChangesAsync();

            var mainBranch = new Branch
            {
                Name = "Dubai HQ",
                OrganizationID = org.ID,
                TimeZone = 4,
                Weekend = "Friday,Saturday",
                NationalHolidays = "[\"2025-01-01\",\"2025-12-02\"]",
                IsMainBranch = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Branches.Add(mainBranch);
            await context.SaveChangesAsync();
        }

        private static async Task SeedDepartments(ApplicationDbContext context)
        {
            if (context.Departments.Any()) return;

            var mainBranch = await context.Branches.FirstAsync(b => b.IsMainBranch);

            var depts = new[]
            {
                new Department { Name = "IT", BranchID = mainBranch.ID, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Department { Name = "HR", BranchID = mainBranch.ID, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Department { Name = "Finance", BranchID = mainBranch.ID, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Department { Name = "Operations", BranchID = mainBranch.ID, IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            context.Departments.AddRange(depts);
            await context.SaveChangesAsync();
        }

        private static async Task SeedTimetables(ApplicationDbContext context)
        {
            if (context.Timetables.Any()) return;

            var mainBranch = await context.Branches.FirstAsync(b => b.IsMainBranch);

            var timetable = new Timetable
            {
                Name = "Standard 9-5",
                WorkingDayStartingHourMinimum = "08:00",
                WorkingDayStartingHourMaximum = "09:30",
                WorkingDayEndingHour = "17:00",
                AverageWorkingHours = 8.0f,
                IsWorkingDayEndingHourEnable = true,
                BranchID = mainBranch.ID,
                IsActive = true
            };
            context.Timetables.Add(timetable);
            await context.SaveChangesAsync();
        }
        private static async Task SeedAttendance(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.Attendances.Any()) return;

            var users = await userManager.Users
                .Include(u => u.Department)
                .ToListAsync();

            var random = new Random();
            var startDate = DateTime.Today.AddMonths(-5).Date;
            var endDate = DateTime.Today;

            var attendances = new List<Attendance>();

            foreach (var user in users)
            {
                var current = startDate;
                while (current <= endDate)
                {
                    // Skip weekends (Friday & Saturday in UAE)
                    var dayName = current.DayOfWeek.ToString();
                    if (dayName == "Friday" || dayName == "Saturday")
                    {
                        current = current.AddDays(1);
                        continue;
                    }

                    // 90% chance of attendance
                    if (random.NextDouble() < 0.9)
                    {
                        var checkIn = current.AddHours(8).AddMinutes(random.Next(0, 90)); // 8:00 - 9:30
                        var checkOut = checkIn.AddHours(8).AddMinutes(random.Next(-30, 30)); // ~8 hours

                        attendances.Add(new Attendance
                        {
                            UserID = user.Id,
                            Date = current,
                            FirstCheckIn = checkIn.ToString(),
                            LastCheckOut = checkOut.ToString(),
                            HRPosted = random.NextDouble() < 0.8, // 80% approved
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    current = current.AddDays(1);
                }
            }

            context.Attendances.AddRange(attendances);
            await context.SaveChangesAsync();
        }
        private static async Task SeedDevices(ApplicationDbContext context)
        {
            if (context.Devices.Any()) return;

            var mainBranch = await context.Branches.FirstAsync(b => b.IsMainBranch);

            var devices = new[]
            {
                new Device { DeviceType = 'F', CoverageArea = 50, BranchID = mainBranch.ID, IsPassThrough = true, Description = "Main Entrance" },
                new Device { DeviceType = 'F', CoverageArea = 30, BranchID = mainBranch.ID, IsPassThrough = false, Description = "HR Office" }
            };

            context.Devices.AddRange(devices);
        }

        private static async Task SeedAttendanceActionTypes(ApplicationDbContext context)
        {
            if (context.AttendanceActionTypes.Any()) return;

            var mainBranch = await context.Branches.FirstAsync(b => b.IsMainBranch);

            var types = new[]
            {
                new AttendanceActionType { Name = "CheckIn", DisplayName_En = "Check In", DisplayName_Ar = "تسجيل الدخول", BranchID = mainBranch.ID },
                new AttendanceActionType { Name = "CheckOut", DisplayName_En = "Check Out", DisplayName_Ar = "تسجيل الخروج", BranchID = mainBranch.ID },
                new AttendanceActionType { Name = "BreakStart", DisplayName_En = "Break Start", DisplayName_Ar = "بدء الاستراحة", BranchID = mainBranch.ID },
                new AttendanceActionType { Name = "BreakEnd", DisplayName_En = "Break End", DisplayName_Ar = "نهاية الاستراحة", BranchID = mainBranch.ID }
            };

            context.AttendanceActionTypes.AddRange(types);
        }

        // NEW: Seed one user per role
        private static async Task SeedUsers(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            var mainBranch = await context.Branches.FirstAsync(b => b.IsMainBranch);
            var itDept = await context.Departments.FirstAsync(d => d.Name == "IT");
            var hrDept = await context.Departments.FirstAsync(d => d.Name == "HR");
            var financeDept = await context.Departments.FirstAsync(d => d.Name == "Finance");
            var opsDept = await context.Departments.FirstAsync(d => d.Name == "Operations");
            var timetable = await context.Timetables.FirstAsync();

            var users = new[]
            {
                new { Email = "admin@visionvalley.com",  Name = "Admin User",     Role = "Admin",     Dept = itDept,       Mobile = "+971501234567" },
                new { Email = "hr@visionvalley.com",     Name = "HR Manager",    Role = "HR",        Dept = hrDept,       Mobile = "+971502345678" },
                new { Email = "manager@visionvalley.com",Name = "Team Manager",  Role = "Manager",   Dept = opsDept,      Mobile = "+971503456789" },
                new { Email = "emp@visionvalley.com",    Name = "John Employee", Role = "Employee",  Dept = financeDept,  Mobile = "+971504567890" }
            };

            foreach (var u in users)
            {
                var user = await userManager.FindByEmailAsync(u.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = u.Email,
                        Email = u.Email,
                        DisplayName = u.Name,
                        Mobile = u.Mobile,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        BranchID = mainBranch.ID,
                        DepartmentID = u.Dept.ID,
                        TimetableID = timetable.ID,
                        Gender = 'M',
                        VacationBalance = 21,
                        Address = "Dubai, UAE"
                    };

                    var result = await userManager.CreateAsync(user, "Pass@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, u.Role);
                    }
                }
            }
        }
    }
}