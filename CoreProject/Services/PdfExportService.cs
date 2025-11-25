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
            string? userName = null)
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
                    page.Content().PaddingTop(10).Table(table =>
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

                            // Users table for this branch
                            column.Item().PaddingBottom(20).Table(table =>
                            {
                                // Define columns
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2.5f); // User Name
                                    columns.RelativeColumn(2); // Email
                                    columns.RelativeColumn(1.5f); // Department
                                    columns.RelativeColumn(1.2f); // Check In
                                    columns.RelativeColumn(1.2f); // Check Out
                                    columns.RelativeColumn(1); // Duration
                                    columns.RelativeColumn(1.5f); // Status
                                });

                                // Header row
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("User Name").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Email").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Department").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Check In").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Check Out").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Duration").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Status").FontColor(Colors.White).Bold().FontSize(9);
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
                                    table.Cell().Background(backgroundColor).Padding(5).Text(user.FirstCheckIn ?? "-").FontSize(9);
                                    table.Cell().Background(backgroundColor).Padding(5).Text(user.LastCheckOut ?? "-").FontSize(9);
                                    table.Cell().Background(backgroundColor).Padding(5).Text(durationHours).FontSize(9);
                                    table.Cell().Background(backgroundColor).Padding(5).Text(user.Status).FontColor(statusColor).Bold().FontSize(9);

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
