using CoreProject.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreProject.Services
{
    /// <summary>
    /// Service for generating PDF exports with formatted tables
    /// </summary>
    public class PdfExportService
    {
        public PdfExportService()
        {
            // Set QuestPDF license (Community license is free for open source projects)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Generates a PDF report for attendance data
        /// </summary>
        public byte[] GenerateAttendanceReportPdf(
            IEnumerable<AttendanceViewModel> attendances,
            DateTime startDate,
            DateTime endDate,
            string? userName = null,
            AttendanceSummaryViewModel? summary = null)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text("Attendance Report")
                            .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().PaddingTop(5).AlignCenter().Text($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}")
                            .FontSize(12).FontColor(Colors.Grey.Darken2);

                        if (!string.IsNullOrEmpty(userName))
                        {
                            column.Item().PaddingTop(3).AlignCenter().Text($"User: {userName}")
                                .FontSize(12).FontColor(Colors.Grey.Darken2);
                        }

                        column.Item().PaddingTop(5).AlignCenter().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(9).FontColor(Colors.Grey.Medium);

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    });

                    // Content
                    page.Content().PaddingTop(10).Column(contentColumn =>
                    {
                        contentColumn.Item().Table(table =>
                        {
                            // Define columns
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // User Name
                                columns.RelativeColumn(1.5f); // Date
                                columns.RelativeColumn(1); // Day
                                columns.RelativeColumn(1.2f); // Check In
                                columns.RelativeColumn(1.2f); // Check Out
                                columns.RelativeColumn(1); // Duration
                                columns.RelativeColumn(1.5f); // Status
                                columns.RelativeColumn(1); // Minutes Late
                            });

                            // Header row
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("User Name").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Date").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Day").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Check In").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Check Out").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Duration").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Status").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Late/Early").FontColor(Colors.White).Bold();
                            });

                            // Data rows
                            var rowIndex = 0;
                            foreach (var attendance in attendances)
                            {
                                var isEvenRow = rowIndex % 2 == 0;
                                var backgroundColor = isEvenRow ? Colors.Grey.Lighten3 : Colors.White;

                                var durationHours = attendance.Duration > 0 ? $"{attendance.Duration / 60.0:F2}h" : "-";
                                var minutesLate = attendance.MinutesLate.HasValue ? attendance.MinutesLate.Value.ToString() : "-";

                                // Get status color
                                var statusColor = GetStatusColor(attendance.Status);

                                table.Cell().Background(backgroundColor).Padding(6).Text(attendance.UserName ?? "Unknown");
                                table.Cell().Background(backgroundColor).Padding(6).Text(attendance.Date.ToString("yyyy-MM-dd"));
                                table.Cell().Background(backgroundColor).Padding(6).Text(attendance.Date.DayOfWeek.ToString().Substring(0, 3));
                                table.Cell().Background(backgroundColor).Padding(6).Text(attendance.FirstCheckIn ?? "-");
                                table.Cell().Background(backgroundColor).Padding(6).Text(attendance.LastCheckOut ?? "-");
                                table.Cell().Background(backgroundColor).Padding(6).Text(durationHours);
                                table.Cell().Background(backgroundColor).Padding(6).Text(attendance.Status).FontColor(statusColor).Bold();
                                table.Cell().Background(backgroundColor).Padding(6).Text(minutesLate);

                                rowIndex++;
                            }
                        });

                        // Add summary statistics if provided (for individual user reports)
                        if (summary != null && !string.IsNullOrEmpty(userName))
                        {
                            contentColumn.Item().PaddingTop(20).Column(summaryColumn =>
                            {
                                // Summary header
                                summaryColumn.Item().PaddingBottom(10).Text("Detailed Attendance Statistics")
                                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                                // Summary statistics table
                                summaryColumn.Item().Table(summaryTable =>
                                {
                                    summaryTable.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                    });

                                    // Row 1: Total Days and Working Days
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Total Days in Period:").Bold();
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text($"{summary.TotalDays} days");
                                    summaryTable.Cell().Background(Colors.Blue.Lighten4).Padding(8).Text("Working Days (Should be Present):").Bold().FontColor(Colors.Blue.Darken2);
                                    summaryTable.Cell().Background(Colors.Blue.Lighten4).Padding(8).Text($"{summary.WorkingDays} days").Bold().FontColor(Colors.Blue.Darken2);

                                    // Row 2: Present Days and Absent Days
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Days Actually Present:").Bold().FontColor(Colors.Green.Darken2);
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text($"{summary.PresentDays} days").Bold().FontColor(Colors.Green.Darken2);
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Days Absent:").Bold().FontColor(Colors.Red.Darken2);
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text($"{summary.AbsentDays} days").Bold().FontColor(Colors.Red.Darken2);

                                    // Row 3: Weekend Days and Holiday Days
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Weekend Days:");
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text($"{summary.WeekendDays} days");
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("National Holiday Days:");
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text($"{summary.HolidayDays} days");

                                    // Row 4: Total Hours and Attendance Rate
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Total Hours Worked:");
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text(summary.TotalHours);
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Actual Attendance Rate:").Bold();
                                    summaryTable.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text($"{summary.ActualAttendanceRate:F1}%").Bold()
                                        .FontColor(summary.ActualAttendanceRate >= 90 ? Colors.Green.Darken2 : summary.ActualAttendanceRate >= 75 ? Colors.Orange.Darken2 : Colors.Red.Darken2);
                                });

                                // Explanation note
                                summaryColumn.Item().PaddingTop(10).Text("Note: Attendance rate is calculated based on working days only (excluding weekends and national holidays).")
                                    .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                            });
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        text.Span(" of ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Generates a PDF report for users data
        /// </summary>
        public byte[] GenerateUsersReportPdf(IEnumerable<UserViewModel> users)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text("Users Report")
                            .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().PaddingTop(5).AlignCenter().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(9).FontColor(Colors.Grey.Medium);

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    });

                    // Content
                    page.Content().PaddingTop(10).Table(table =>
                    {
                        // Define columns
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Name
                            columns.RelativeColumn(2); // Email
                            columns.RelativeColumn(1.5f); // Mobile
                            columns.RelativeColumn(1.5f); // Branch
                            columns.RelativeColumn(1.5f); // Department
                            columns.RelativeColumn(1.5f); // Role
                            columns.RelativeColumn(1); // Status
                            columns.RelativeColumn(1); // Vacation Balance
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Full Name").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Email").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Mobile").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Branch").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Department").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Role").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Status").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Vacation").FontColor(Colors.White).Bold();
                        });

                        // Data rows
                        var rowIndex = 0;
                        foreach (var user in users)
                        {
                            var isEvenRow = rowIndex % 2 == 0;
                            var backgroundColor = isEvenRow ? Colors.Grey.Lighten3 : Colors.White;

                            var roleDisplay = user.Roles.Any() ? string.Join(", ", user.Roles) : "No Role";
                            var statusColor = user.IsActive ? Colors.Green.Darken2 : Colors.Red.Darken2;

                            table.Cell().Background(backgroundColor).Padding(6).Text(user.DisplayName);
                            table.Cell().Background(backgroundColor).Padding(6).Text(user.Email).FontSize(9);
                            table.Cell().Background(backgroundColor).Padding(6).Text(user.Mobile ?? "-");
                            table.Cell().Background(backgroundColor).Padding(6).Text(user.BranchName);
                            table.Cell().Background(backgroundColor).Padding(6).Text(user.DepartmentName);
                            table.Cell().Background(backgroundColor).Padding(6).Text(roleDisplay).FontSize(9);
                            table.Cell().Background(backgroundColor).Padding(6).Text(user.IsActive ? "Active" : "Inactive").FontColor(statusColor).Bold();
                            table.Cell().Background(backgroundColor).Padding(6).Text((user.VacationBalance ?? 0).ToString());

                            rowIndex++;
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        text.Span(" of ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Generates a PDF report for branch attendance data
        /// </summary>
        public byte[] GenerateBranchAttendanceReportPdf(
            ViewModels.BranchAttendanceViewModel model)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text("Branch Attendance Report")
                            .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().PaddingTop(5).AlignCenter().Text($"Period: {model.StartDate:yyyy-MM-dd} to {model.EndDate:yyyy-MM-dd}")
                            .FontSize(12).FontColor(Colors.Grey.Darken2);

                        column.Item().PaddingTop(3).AlignCenter().Text($"Total Branches: {model.Branches.Count}")
                            .FontSize(12).FontColor(Colors.Grey.Darken2);

                        column.Item().PaddingTop(5).AlignCenter().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(9).FontColor(Colors.Grey.Medium);

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    });

                    // Content
                    page.Content().PaddingTop(10).Column(column =>
                    {
                        foreach (var branch in model.Branches)
                        {
                            // Branch header
                            column.Item().PaddingBottom(10).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text(branch.BranchName)
                                        .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                                    col.Item().Text($"Attendance Rate: {branch.AttendanceRateDisplay} | Present: {branch.PresentUsers}/{branch.TotalUsers} | Absent: {branch.AbsentUsers}")
                                        .FontSize(10).FontColor(Colors.Grey.Darken2);
                                });
                            });

                            // Add Employee Summary Table for date ranges (not for today)
                            if (!model.IsToday && branch.Users.Any())
                            {
                                column.Item().PaddingBottom(10).Column(summaryCol =>
                                {
                                    summaryCol.Item().PaddingBottom(5).Text("Employee Attendance Summary")
                                        .FontSize(11).Bold().FontColor(Colors.Blue.Darken1);

                                    summaryCol.Item().Table(summaryTable =>
                                    {
                                        summaryTable.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2); // Employee
                                            columns.RelativeColumn(0.8f); // Total Days
                                            columns.RelativeColumn(1); // Working Days
                                            columns.RelativeColumn(0.8f); // Present
                                            columns.RelativeColumn(0.8f); // Absent
                                            columns.RelativeColumn(0.8f); // Weekends
                                            columns.RelativeColumn(0.8f); // Holidays
                                            columns.RelativeColumn(1); // Total Hours
                                            columns.RelativeColumn(0.8f); // Rate
                                        });

                                        // Header
                                        summaryTable.Header(header =>
                                        {
                                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Employee").FontColor(Colors.White).Bold().FontSize(8);
                                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Total").FontColor(Colors.White).Bold().FontSize(8);
                                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Working").FontColor(Colors.White).Bold().FontSize(8);
                                            header.Cell().Background(Colors.Green.Darken2).Padding(5).Text("Present").FontColor(Colors.White).Bold().FontSize(8);
                                            header.Cell().Background(Colors.Red.Darken2).Padding(5).Text("Absent").FontColor(Colors.White).Bold().FontSize(8);
                                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Weekends").FontColor(Colors.White).Bold().FontSize(8);
                                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Holidays").FontColor(Colors.White).Bold().FontSize(8);
                                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Hours").FontColor(Colors.White).Bold().FontSize(8);
                                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Rate").FontColor(Colors.White).Bold().FontSize(8);
                                        });

                                        // Calculate date range values
                                        var totalDays = (model.EndDate - model.StartDate).Days + 1;

                                        // Data rows
                                        var rowIdx = 0;
                                        foreach (var user in branch.Users)
                                        {
                                            var isEven = rowIdx % 2 == 0;
                                            var bgColor = isEven ? Colors.Grey.Lighten3 : Colors.White;

                                            // Parse present and absent days
                                            var presentStr = user.FirstCheckIn?.Replace(" days", "") ?? "0";
                                            var absentStr = user.LastCheckOut?.Replace(" days", "") ?? "0";
                                            var presentInt = int.TryParse(presentStr, out int p) ? p : 0;
                                            var absentInt = int.TryParse(absentStr, out int a) ? a : 0;
                                            var workingDays = presentInt + absentInt;

                                            // Calculate weekends
                                            var weekendDays = 0;
                                            for (var d = model.StartDate; d <= model.EndDate; d = d.AddDays(1))
                                            {
                                                if (d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday)
                                                    weekendDays++;
                                            }

                                            var holidayDays = totalDays - workingDays - weekendDays;
                                            var rate = workingDays > 0 ? (decimal)presentInt / workingDays * 100 : 0;
                                            var hours = user.Duration / 60;
                                            var minutes = user.Duration % 60;

                                            summaryTable.Cell().Background(bgColor).Padding(4).Text($"{user.UserName}\n{user.Department}").FontSize(7);
                                            summaryTable.Cell().Background(bgColor).Padding(4).Text(totalDays.ToString()).FontSize(8);
                                            summaryTable.Cell().Background(bgColor).Padding(4).Text(workingDays.ToString()).FontSize(8).Bold();
                                            summaryTable.Cell().Background(Colors.Green.Lighten4).Padding(4).Text(presentInt.ToString()).FontSize(8).Bold().FontColor(Colors.Green.Darken2);
                                            summaryTable.Cell().Background(Colors.Red.Lighten4).Padding(4).Text(absentInt.ToString()).FontSize(8).Bold().FontColor(Colors.Red.Darken2);
                                            summaryTable.Cell().Background(bgColor).Padding(4).Text(weekendDays.ToString()).FontSize(8);
                                            summaryTable.Cell().Background(bgColor).Padding(4).Text(holidayDays.ToString()).FontSize(8);
                                            summaryTable.Cell().Background(bgColor).Padding(4).Text($"{hours}h {minutes}m").FontSize(7);
                                            summaryTable.Cell().Background(bgColor).Padding(4).Text($"{rate:F1}%").FontSize(8).Bold()
                                                .FontColor(rate >= 90 ? Colors.Green.Darken2 : rate >= 75 ? Colors.Orange.Darken2 : Colors.Red.Darken2);

                                            rowIdx++;
                                        }
                                    });
                                });
                            }

                            // Users table for this branch
                            column.Item().PaddingBottom(20).Table(table =>
                            {
                                // Define columns
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2.5f); // User Name
                                    columns.RelativeColumn(2); // Email
                                    columns.RelativeColumn(1.5f); // Department

                                    if (model.IsToday)
                                    {
                                        columns.RelativeColumn(1.2f); // Check In
                                        columns.RelativeColumn(1.2f); // Check Out
                                        columns.RelativeColumn(1); // Duration
                                        columns.RelativeColumn(1.5f); // Status
                                    }
                                    else
                                    {
                                        columns.RelativeColumn(1.2f); // Days Present
                                        columns.RelativeColumn(1.2f); // Days Absent
                                        columns.RelativeColumn(1.2f); // Total Duration
                                    }
                                });

                                // Header row
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("User Name").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Email").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Department").FontColor(Colors.White).Bold().FontSize(9);

                                    if (model.IsToday)
                                    {
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Check In").FontColor(Colors.White).Bold().FontSize(9);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Check Out").FontColor(Colors.White).Bold().FontSize(9);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Duration").FontColor(Colors.White).Bold().FontSize(9);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Status").FontColor(Colors.White).Bold().FontSize(9);
                                    }
                                    else
                                    {
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Days Present").FontColor(Colors.White).Bold().FontSize(9);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Days Absent").FontColor(Colors.White).Bold().FontSize(9);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Total Duration").FontColor(Colors.White).Bold().FontSize(9);
                                    }
                                });

                                // Data rows
                                var rowIndex = 0;
                                foreach (var user in branch.Users)
                                {
                                    var isEvenRow = rowIndex % 2 == 0;
                                    var backgroundColor = isEvenRow ? Colors.Grey.Lighten3 : Colors.White;

                                    var durationHours = user.Duration > 0 ? $"{user.Duration / 60.0:F2}h" : "-";
                                    var statusColor = GetStatusColor(user.Status);

                                    table.Cell().Background(backgroundColor).Padding(5).Text(user.UserName).FontSize(9);
                                    table.Cell().Background(backgroundColor).Padding(5).Text(user.Email).FontSize(8);
                                    table.Cell().Background(backgroundColor).Padding(5).Text(user.Department).FontSize(9);

                                    if (model.IsToday)
                                    {
                                        table.Cell().Background(backgroundColor).Padding(5).Text(user.FirstCheckIn ?? "-").FontSize(9);
                                        table.Cell().Background(backgroundColor).Padding(5).Text(user.LastCheckOut ?? "-").FontSize(9);
                                        table.Cell().Background(backgroundColor).Padding(5).Text(durationHours).FontSize(9);
                                        table.Cell().Background(backgroundColor).Padding(5).Text(user.Status).FontColor(statusColor).Bold().FontSize(9);
                                    }
                                    else
                                    {
                                        table.Cell().Background(backgroundColor).Padding(5).Text(user.FirstCheckIn ?? "0 days").FontSize(9);
                                        table.Cell().Background(backgroundColor).Padding(5).Text(user.LastCheckOut ?? "0 days").FontSize(9);
                                        table.Cell().Background(backgroundColor).Padding(5).Text(durationHours).FontSize(9);
                                    }

                                    rowIndex++;
                                }
                            });
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        text.Span(" of ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Gets color based on attendance status
        /// </summary>
        private string GetStatusColor(string status)
        {
            return status switch
            {
                "On Time" => Colors.Green.Darken2,
                "Late" => Colors.Orange.Darken2,
                "Very Late" => Colors.Red.Darken2,
                "Absent" => Colors.Red.Darken3,
                "Early" => Colors.Blue.Darken2,
                "Present" => Colors.Green.Darken1,
                _ => Colors.Grey.Darken2
            };
        }
    }
}
