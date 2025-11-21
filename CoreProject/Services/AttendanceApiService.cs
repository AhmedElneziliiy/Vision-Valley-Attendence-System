using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Services.IService;
using CoreProject.Utilities.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoreProject.Services
{
    /// <summary>
    /// Service implementation for mobile app attendance API
    /// Contains all business logic for check-in and check-out operations
    /// </summary>
    public class AttendanceApiService : IAttendanceApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITimezoneService _timezoneService;
        private readonly ILogger<AttendanceApiService> _logger;

        public AttendanceApiService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ITimezoneService timezoneService,
            ILogger<AttendanceApiService> logger)
        {
            _context = context;
            _userManager = userManager;
            _timezoneService = timezoneService;
            _logger = logger;
        }

        public async Task<AttendanceActionResponseDto> ProcessAttendanceActionAsync(AttendanceActionRequestDto request)
        {
            try
            {
                // 1. Find user by email with all related data
                var user = await _userManager.Users
                    .Include(u => u.Branch)
                        .ThenInclude(b => b.Organization)
                    .Include(u => u.Department)
                    .Include(u => u.Timetable)
                    .FirstOrDefaultAsync(u => u.Email == request.Username);

                if (user == null)
                {
                    _logger.LogWarning("Attendance action failed: User not found - {Email}", request.Username);
                    return new AttendanceActionResponseDto
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // 2. Check if user is active
                if (!user.IsActive)
                {
                    _logger.LogWarning("Attendance action failed: User inactive - {Email}", request.Username);
                    return new AttendanceActionResponseDto
                    {
                        Success = false,
                        Message = "Your account has been deactivated. Please contact your administrator."
                    };
                }

                // 3. Verify UDID matches
                if (string.IsNullOrEmpty(user.UDID) || user.UDID != request.UDID)
                {
                    _logger.LogWarning("Attendance action failed: UDID mismatch - User: {Email}, Expected: {Expected}, Got: {Actual}",
                        request.Username, user.UDID, request.UDID);
                    return new AttendanceActionResponseDto
                    {
                        Success = false,
                        Message = "Device not registered. Please login first to register your device."
                    };
                }

                // 4. Find device and validate
                var device = await _context.Devices
                    .Include(d => d.Branch)
                    .FirstOrDefaultAsync(d => d.DeviceID == request.DeviceID);

                if (device == null)
                {
                    _logger.LogWarning("Attendance action failed: Device not found - DeviceID: {DeviceID}", request.DeviceID);
                    return new AttendanceActionResponseDto
                    {
                        Success = false,
                        Message = "Device not found. Please contact your administrator."
                    };
                }

                if (!device.IsActive)
                {
                    _logger.LogWarning("Attendance action failed: Device inactive - DeviceID: {DeviceID}", request.DeviceID);
                    return new AttendanceActionResponseDto
                    {
                        Success = false,
                        Message = "This device is inactive. Please contact your administrator."
                    };
                }

                // 5. Verify user is assigned to the same branch as the device
                if (user.BranchID != device.BranchID)
                {
                    _logger.LogWarning("Attendance action failed: Branch mismatch - User Branch: {UserBranch}, Device Branch: {DeviceBranch}",
                        user.BranchID, device.BranchID);
                    return new AttendanceActionResponseDto
                    {
                        Success = false,
                        Message = $"You are not assigned to this branch. This device is for {device.Branch?.Name ?? "another branch"}."
                    };
                }

                // 6. Get branch timezone and calculate local time
                var branchTimezone = user.Branch?.TimeZone ?? 0;
                var branchNow = _timezoneService.GetBranchNow(branchTimezone);
                var today = branchNow.Date;
                var utcNow = DateTime.UtcNow;
                var localTime = _timezoneService.ConvertUtcTimeToLocal(utcNow.TimeOfDay, utcNow.Date, branchTimezone);

                // 7. Process action based on type
                if (request.ActionType == "CheckIn")
                {
                    return await ProcessCheckInAsync(user, device, today, localTime, utcNow, branchTimezone);
                }
                else if (request.ActionType == "CheckOut")
                {
                    return await ProcessCheckOutAsync(user, device, today, localTime, utcNow, branchTimezone);
                }
                else
                {
                    return new AttendanceActionResponseDto
                    {
                        Success = false,
                        Message = "Invalid action type. Must be 'CheckIn' or 'CheckOut'."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing attendance action for user: {Username}", request.Username);
                return new AttendanceActionResponseDto
                {
                    Success = false,
                    Message = "An error occurred while processing your request. Please try again later."
                };
            }
        }

        private async Task<AttendanceActionResponseDto> ProcessCheckInAsync(
            ApplicationUser user,
            Device device,
            DateTime today,
            TimeSpan localTime,
            DateTime utcNow,
            int branchTimezone)
        {
            try
            {
                // Get or create attendance record for today
                var attendance = await _context.Attendances
                    .Include(a => a.Records)
                    .FirstOrDefaultAsync(a => a.UserID == user.Id && a.Date == today);

                // Calculate attendance status based on timetable
                var (status, minutesLate) = CalculateAttendanceStatus(localTime, user.Timetable);

                if (attendance == null)
                {
                    // Create new attendance record
                    attendance = new Attendance
                    {
                        UserID = user.Id,
                        Date = today,
                        FirstCheckIn = localTime.ToString(@"hh\:mm"),
                        Status = status,
                        MinutesLate = minutesLate,
                        CreatedAt = utcNow
                    };

                    _context.Attendances.Add(attendance);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Update existing attendance status
                    attendance.Status = status;
                    attendance.MinutesLate = minutesLate;
                    attendance.UpdatedAt = utcNow;
                    await _context.SaveChangesAsync();
                }

                // Add check-in record
                var record = new AttendanceRecord
                {
                    AttendanceID = attendance.ID,
                    Time = localTime,
                    IsCheckIn = true,
                    IsAutomated = false
                };

                _context.AttendanceRecords.Add(record);
                await _context.SaveChangesAsync();

                // Set AccessControlState to 1 for the device
                device.AccessControlState = 1;
                _context.Entry(device).Property(d => d.AccessControlState).IsModified = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} ({Email}) checked in at {Time} via device {DeviceID}. AccessControlState set to 1.",
                    user.Id, user.Email, localTime, device.DeviceID);

                // Build response
                return new AttendanceActionResponseDto
                {
                    Success = true,
                    Message = "Checked in successfully",
                    Data = new AttendanceDataDto
                    {
                        AttendanceId = attendance.ID,
                        UserId = user.Id,
                        UserName = user.DisplayName,
                        ActionType = "CheckIn",
                        Date = today,
                        ActionTime = localTime,
                        ActionTimestamp = utcNow,
                        Status = status.ToString(),
                        CheckInTime = localTime,
                        CheckOutTime = null,
                        DurationMinutes = null,
                        DurationFormatted = null,
                        ExpectedCheckInTime = ParseTimeString(user.Timetable?.WorkingDayStartingHourMinimum),
                        ExpectedCheckOutTime = ParseTimeString(user.Timetable?.WorkingDayEndingHour),
                        DeviceId = device.DeviceID!,
                        BranchName = user.Branch?.Name ?? "N/A",
                        OrganizationName = user.Branch?.Organization?.Name ?? "N/A",
                        Timezone = GetTimezoneName(branchTimezone)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during check-in for user {UserId}", user.Id);
                return new AttendanceActionResponseDto
                {
                    Success = false,
                    Message = "Failed to check in. Please try again."
                };
            }
        }

        private async Task<AttendanceActionResponseDto> ProcessCheckOutAsync(
            ApplicationUser user,
            Device device,
            DateTime today,
            TimeSpan localTime,
            DateTime utcNow,
            int branchTimezone)
        {
            try
            {
                // Find today's attendance record
                var attendance = await _context.Attendances
                    .Include(a => a.Records)
                    .FirstOrDefaultAsync(a => a.UserID == user.Id && a.Date == today);

                if (attendance == null || string.IsNullOrEmpty(attendance.FirstCheckIn))
                {
                    _logger.LogWarning("Check-out failed: No check-in found for user {UserId} on {Date}", user.Id, today);
                    return new AttendanceActionResponseDto
                    {
                        Success = false,
                        Message = "No check-in found for today. Please check in first."
                    };
                }

                // Add check-out record
                var record = new AttendanceRecord
                {
                    AttendanceID = attendance.ID,
                    Time = localTime,
                    IsCheckIn = false,
                    IsAutomated = false
                };

                _context.AttendanceRecords.Add(record);
                await _context.SaveChangesAsync();

                // Reload attendance with all records
                var updatedAttendance = await _context.Attendances
                    .Include(a => a.Records)
                    .FirstOrDefaultAsync(a => a.ID == attendance.ID);

                if (updatedAttendance != null)
                {
                    // Calculate duration
                    var duration = CalculateDuration(updatedAttendance.Records.ToList());

                    // Update attendance fields
                    updatedAttendance.LastCheckOut = localTime.ToString(@"hh\:mm");
                    updatedAttendance.Duration = duration;
                    updatedAttendance.UpdatedAt = utcNow;

                    _context.Entry(updatedAttendance).Property(a => a.Duration).IsModified = true;
                    _context.Entry(updatedAttendance).Property(a => a.LastCheckOut).IsModified = true;
                    _context.Entry(updatedAttendance).Property(a => a.UpdatedAt).IsModified = true;

                    await _context.SaveChangesAsync();

                    // Set AccessControlState to 1 for the device
                    device.AccessControlState = 1;
                    _context.Entry(device).Property(d => d.AccessControlState).IsModified = true;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} ({Email}) checked out at {Time} via device {DeviceID}. Duration: {Duration} minutes. AccessControlState set to 1.",
                        user.Id, user.Email, localTime, device.DeviceID, duration);

                    // Build response
                    var checkInTime = ParseTimeString(attendance.FirstCheckIn);
                    var durationFormatted = FormatDuration(duration);

                    return new AttendanceActionResponseDto
                    {
                        Success = true,
                        Message = "Checked out successfully",
                        Data = new AttendanceDataDto
                        {
                            AttendanceId = updatedAttendance.ID,
                            UserId = user.Id,
                            UserName = user.DisplayName,
                            ActionType = "CheckOut",
                            Date = today,
                            ActionTime = localTime,
                            ActionTimestamp = utcNow,
                            Status = updatedAttendance.Status.ToString(),
                            CheckInTime = checkInTime,
                            CheckOutTime = localTime,
                            DurationMinutes = duration,
                            DurationFormatted = durationFormatted,
                            ExpectedCheckInTime = ParseTimeString(user.Timetable?.WorkingDayStartingHourMinimum),
                            ExpectedCheckOutTime = ParseTimeString(user.Timetable?.WorkingDayEndingHour),
                            DeviceId = device.DeviceID!,
                            BranchName = user.Branch?.Name ?? "N/A",
                            OrganizationName = user.Branch?.Organization?.Name ?? "N/A",
                            Timezone = GetTimezoneName(branchTimezone)
                        }
                    };
                }

                return new AttendanceActionResponseDto
                {
                    Success = false,
                    Message = "Failed to update attendance duration. Please try again."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during check-out for user {UserId}", user.Id);
                return new AttendanceActionResponseDto
                {
                    Success = false,
                    Message = "Failed to check out. Please try again."
                };
            }
        }

        #region Helper Methods

        /// <summary>
        /// Calculates attendance status (On Time, Late, Very Late, Early) based on check-in time and timetable
        /// </summary>
        private (AttendanceStatus status, int? minutesLate) CalculateAttendanceStatus(
            TimeSpan checkInTime,
            Timetable? timetable)
        {
            if (timetable == null)
            {
                return (AttendanceStatus.OnTime, null);
            }

            TimeSpan? startMin = ParseTimeString(timetable.WorkingDayStartingHourMinimum);
            TimeSpan? startMax = ParseTimeString(timetable.WorkingDayStartingHourMaximum);

            if (!startMin.HasValue && !startMax.HasValue)
            {
                return (AttendanceStatus.OnTime, null);
            }

            // Early: Before minimum start time
            if (startMin.HasValue && checkInTime < startMin.Value)
            {
                var minutesEarly = (int)(startMin.Value - checkInTime).TotalMinutes;
                return (AttendanceStatus.Early, -minutesEarly);
            }

            // On Time: Between minimum and maximum start time
            if (startMin.HasValue && startMax.HasValue)
            {
                if (checkInTime >= startMin.Value && checkInTime <= startMax.Value)
                {
                    return (AttendanceStatus.OnTime, 0);
                }
            }
            else if (startMin.HasValue && !startMax.HasValue)
            {
                return (AttendanceStatus.OnTime, 0);
            }

            // Late or Very Late: After maximum start time
            if (startMax.HasValue && checkInTime > startMax.Value)
            {
                var minutesLate = (int)(checkInTime - startMax.Value).TotalMinutes;

                if (minutesLate > 15)
                {
                    return (AttendanceStatus.VeryLate, minutesLate);
                }

                return (AttendanceStatus.Late, minutesLate);
            }

            return (AttendanceStatus.OnTime, 0);
        }

        /// <summary>
        /// Calculates total duration from attendance records (check-in/check-out pairs)
        /// </summary>
        private int CalculateDuration(List<AttendanceRecord> records)
        {
            if (!records.Any())
            {
                return 0;
            }

            int totalMinutes = 0;
            TimeSpan? lastCheckIn = null;

            var orderedRecords = records.OrderBy(r => r.Time).ToList();

            foreach (var record in orderedRecords)
            {
                if (record.IsCheckIn)
                {
                    lastCheckIn = record.Time;
                }
                else if (lastCheckIn.HasValue)
                {
                    var duration = record.Time - lastCheckIn.Value;
                    var minutes = (int)duration.TotalMinutes;
                    totalMinutes += minutes;
                    lastCheckIn = null;
                }
            }

            return totalMinutes;
        }

        /// <summary>
        /// Parses time string in format "HH:mm" to TimeSpan
        /// </summary>
        private TimeSpan? ParseTimeString(string? timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
            {
                return null;
            }

            if (TimeSpan.TryParse(timeString, out var result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Formats duration in minutes to "HH:mm" format
        /// </summary>
        private string FormatDuration(int minutes)
        {
            var hours = minutes / 60;
            var mins = minutes % 60;
            return $"{hours:D2}:{mins:D2}";
        }

        /// <summary>
        /// Gets timezone name based on offset
        /// </summary>
        private string GetTimezoneName(int offset)
        {
            return offset switch
            {
                2 => "Egypt (UTC+2)",
                4 => "Dubai (UTC+4)",
                3 => "Saudi Arabia (UTC+3)",
                0 => "UTC",
                _ => $"UTC+{offset}"
            };
        }

        #endregion

        #region Attendance Report

        /// <summary>
        /// Gets attendance report for the authenticated user
        /// Returns daily records with vacation days from branch settings
        /// </summary>
        public async Task<AttendanceReportResponseDto> GetUserAttendanceReportAsync(int userId, AttendanceReportRequestDto request)
        {
            try
            {
                // Get user with related data
                var user = await _userManager.Users
                    .Include(u => u.Branch)
                    .Include(u => u.Timetable)
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return new AttendanceReportResponseDto
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Set date range - default to current month if not specified
                var dateFrom = request.DateFrom ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var dateTo = request.DateTo ?? DateTime.Today;

                // Ensure dateTo is not in the future
                if (dateTo > DateTime.Today)
                    dateTo = DateTime.Today;

                // Ensure dateFrom is not after dateTo
                if (dateFrom > dateTo)
                    dateFrom = dateTo;

                // Get weekend days from branch (e.g., "Friday,Saturday")
                var weekendDays = user.Branch?.Weekend?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .ToList() ?? new List<string>();

                // Get national holidays from branch - support both JSON array and comma-separated formats
                var nationalHolidays = new List<DateTime>();
                if (!string.IsNullOrWhiteSpace(user.Branch?.NationalHolidays))
                {
                    try
                    {
                        var holidaysString = user.Branch.NationalHolidays.Trim();

                        // Try JSON array format first: ["2025-01-01","2025-02-15"]
                        if (holidaysString.StartsWith("["))
                        {
                            var jsonHolidays = JsonSerializer.Deserialize<List<string>>(holidaysString);
                            if (jsonHolidays != null)
                            {
                                nationalHolidays = jsonHolidays
                                    .Select(d => DateTime.Parse(d).Date)
                                    .ToList();
                            }
                        }
                        else
                        {
                            // Fallback to comma-separated format: "2025-01-25,2025-07-23"
                            nationalHolidays = holidaysString
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(d => DateTime.Parse(d.Trim()).Date)
                                .ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse national holidays for branch: {BranchId}. Value: {NationalHolidays}",
                            user.BranchID, user.Branch.NationalHolidays);
                        nationalHolidays = new List<DateTime>();
                    }
                }

                // Get all attendance records for the user in the date range
                var attendanceRecords = await _context.Attendances
                    .Where(a => a.UserID == userId && a.Date >= dateFrom && a.Date <= dateTo)
                    .OrderBy(a => a.Date)
                    .ToListAsync();

                var attendanceRecordsByDate = attendanceRecords.ToDictionary(a => a.Date.Date, a => a);

                // Get all attendance record details (check-ins/check-outs)
                var attendanceIds = attendanceRecords.Select(a => a.ID).ToList();
                var recordDetails = await _context.AttendanceRecords
                    .Where(r => attendanceIds.Contains(r.AttendanceID))
                    .OrderBy(r => r.Time)
                    .ToListAsync();

                var recordsByAttendanceId = recordDetails
                    .GroupBy(r => r.AttendanceID)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Build daily records
                var dailyRecords = new List<DailyAttendanceDto>();
                int totalPresentDays = 0;
                int totalAbsentDays = 0;
                int totalVacationDays = 0;
                int totalLateDays = 0;
                int totalEarlyDays = 0;

                for (var date = dateFrom; date <= dateTo; date = date.AddDays(1))
                {
                    var dayOfWeek = date.ToString("dddd");
                    var isWeekend = weekendDays.Contains(dayOfWeek);
                    var isNationalHoliday = nationalHolidays.Contains(date.Date);
                    var isVacation = isWeekend || isNationalHoliday;

                    var dailyRecord = new DailyAttendanceDto
                    {
                        Date = date,
                        DayOfWeek = dayOfWeek,
                        IsVacation = isVacation,
                        Status = "Absent"
                    };

                    if (isVacation)
                    {
                        dailyRecord.Status = "Vacation";
                        if (isWeekend)
                            dailyRecord.VacationName = "Weekend";
                        else if (isNationalHoliday)
                            dailyRecord.VacationName = "National Holiday";

                        totalVacationDays++;
                    }
                    else
                    {
                        // Check if user has attendance for this day
                        if (attendanceRecordsByDate.TryGetValue(date.Date, out var attendance))
                        {
                            if (recordsByAttendanceId.TryGetValue(attendance.ID, out var records))
                            {
                                // Get check-in and check-out times
                                var checkInRecord = records.FirstOrDefault(r => r.IsCheckIn);
                                var checkOutRecord = records.LastOrDefault(r => !r.IsCheckIn);

                                if (checkInRecord != null)
                                {
                                    dailyRecord.CheckInTime = checkInRecord.Time;
                                    totalPresentDays++;

                                    // Determine status based on attendance status
                                    dailyRecord.Status = attendance.Status.ToString();
                                    dailyRecord.MinutesLate = attendance.MinutesLate;

                                    if (attendance.Status == AttendanceStatus.Late || attendance.Status == AttendanceStatus.VeryLate)
                                        totalLateDays++;
                                    else if (attendance.Status == AttendanceStatus.Early)
                                        totalEarlyDays++;
                                }

                                if (checkOutRecord != null)
                                {
                                    dailyRecord.CheckOutTime = checkOutRecord.Time;

                                    // Calculate duration
                                    var duration = CalculateDuration(records);
                                    dailyRecord.DurationMinutes = duration;
                                    dailyRecord.DurationFormatted = FormatDuration(duration);
                                }
                            }
                            else
                            {
                                // Attendance exists but no records (shouldn't happen)
                                dailyRecord.Status = "Absent";
                                totalAbsentDays++;
                            }
                        }
                        else
                        {
                            // No attendance for working day
                            dailyRecord.Status = "Absent";
                            totalAbsentDays++;
                        }

                        // Add expected times from timetable
                        if (user.Timetable != null)
                        {
                            dailyRecord.ExpectedCheckInTime = ParseTimeString(user.Timetable.WorkingDayStartingHourMinimum);
                            dailyRecord.ExpectedCheckOutTime = ParseTimeString(user.Timetable.WorkingDayEndingHour);
                        }
                    }

                    dailyRecords.Add(dailyRecord);
                }

                var totalWorkingDays = (int)(dateTo - dateFrom).TotalDays + 1 - totalVacationDays;

                var reportData = new AttendanceReportDataDto
                {
                    UserId = user.Id,
                    UserName = user.DisplayName,
                    Email = user.Email!,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    TotalWorkingDays = totalWorkingDays,
                    TotalPresentDays = totalPresentDays,
                    TotalAbsentDays = totalAbsentDays,
                    TotalVacationDays = totalVacationDays,
                    TotalLateDays = totalLateDays,
                    TotalEarlyDays = totalEarlyDays,
                    DailyRecords = dailyRecords
                };

                return new AttendanceReportResponseDto
                {
                    Success = true,
                    Message = "Report generated successfully",
                    Data = reportData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attendance report for user: {UserId}", userId);
                return new AttendanceReportResponseDto
                {
                    Success = false,
                    Message = "An error occurred while generating the report"
                };
            }
        }

        #endregion

        #region Passthrough

        /// <summary>
        /// Processes passthrough access control - sets AccessControlState to 1 for user's branch device
        /// </summary>
        public async Task<PassthroughResponseDto> ProcessPassthroughAsync(PassthroughRequestDto request)
        {
            try
            {
                // 1. Find user by email
                var user = await _userManager.Users
                    .Include(u => u.Branch)
                    .FirstOrDefaultAsync(u => u.Email == request.Username);

                if (user == null)
                {
                    _logger.LogWarning("Passthrough failed: User not found - {Email}", request.Username);
                    return new PassthroughResponseDto
                    {
                        Success = false,
                        Message = "User not found",
                        AccessGranted = false
                    };
                }

                // 2. Verify user is active
                if (!user.IsActive)
                {
                    _logger.LogWarning("Passthrough failed: User inactive - {Email}", request.Username);
                    return new PassthroughResponseDto
                    {
                        Success = false,
                        Message = "User account is inactive",
                        AccessGranted = false
                    };
                }

                // 3. Verify UDID matches
                if (string.IsNullOrEmpty(user.UDID) || user.UDID != request.UDID)
                {
                    _logger.LogWarning("Passthrough failed: UDID mismatch - User: {Email}, Expected: {Expected}, Got: {Actual}",
                        request.Username, user.UDID, request.UDID);
                    return new PassthroughResponseDto
                    {
                        Success = false,
                        Message = "Device not registered",
                        AccessGranted = false
                    };
                }

                // 4. Find device by DeviceID
                var device = await _context.Devices
                    .Include(d => d.Branch)
                    .FirstOrDefaultAsync(d => d.DeviceID == request.DeviceID);

                if (device == null)
                {
                    _logger.LogWarning("Passthrough failed: Device not found - DeviceID: {DeviceID}", request.DeviceID);
                    return new PassthroughResponseDto
                    {
                        Success = false,
                        Message = "Device not found",
                        AccessGranted = false
                    };
                }

                // 5. Verify device is active
                if (!device.IsActive)
                {
                    _logger.LogWarning("Passthrough failed: Device inactive - DeviceID: {DeviceID}", request.DeviceID);
                    return new PassthroughResponseDto
                    {
                        Success = false,
                        Message = "Device is inactive",
                        AccessGranted = false
                    };
                }

                // 6. Verify user belongs to same branch as device
                if (user.BranchID != device.BranchID)
                {
                    _logger.LogWarning("Passthrough failed: Branch mismatch - User Branch: {UserBranch}, Device Branch: {DeviceBranch}",
                        user.BranchID, device.BranchID);
                    return new PassthroughResponseDto
                    {
                        Success = false,
                        Message = $"Access denied. Device belongs to {device.Branch?.Name ?? "another branch"}",
                        AccessGranted = false,
                        BranchName = device.Branch?.Name
                    };
                }

                // 7. Set AccessControlState to 1
                device.AccessControlState = 1;
                _context.Entry(device).Property(d => d.AccessControlState).IsModified = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Passthrough successful: User {UserId} ({Email}) granted access. Device {DeviceID} AccessControlState set to 1.",
                    user.Id, user.Email, device.DeviceID);

                return new PassthroughResponseDto
                {
                    Success = true,
                    Message = "Access granted",
                    AccessGranted = true,
                    AccessControlState = 1,
                    UserName = user.DisplayName,
                    BranchName = device.Branch?.Name
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing passthrough for user: {Username}", request.Username);
                return new PassthroughResponseDto
                {
                    Success = false,
                    Message = "An error occurred while processing your request",
                    AccessGranted = false
                };
            }
        }

        #endregion
    }
}
