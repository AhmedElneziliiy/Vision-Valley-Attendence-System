using CoreProject.Models;
using CoreProject.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CoreProject.Context;
using System.Security.Claims;

namespace MvcCoreProject.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class ManualAttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IBranchService _branchService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ManualAttendanceController(
            IAttendanceService attendanceService,
            IBranchService branchService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _attendanceService = attendanceService;
            _branchService = branchService;
            _userManager = userManager;
            _context = context;
        }

        // GET: ManualAttendance
        public async Task<IActionResult> Index(int? branchId, int? userId, DateTime? date)
        {
            await PopulateBranchesAsync();
            await PopulateUsersAsync(branchId);

            var searchDate = date ?? DateTime.Today;
            ViewBag.SelectedDate = searchDate.ToString("yyyy-MM-dd");
            ViewBag.SelectedBranchId = branchId;
            ViewBag.SelectedUserId = userId;

            // Get attendance records for the selected date
            var report = await _attendanceService.GetAttendanceReportAsync(User, searchDate, searchDate, userId);

            return View(report.Attendances);
        }

        // GET: ManualAttendance/Create
        public async Task<IActionResult> Create()
        {
            await PopulateBranchesAsync();
            await PopulateUsersAsync(null);

            ViewBag.DefaultDate = DateTime.Today.ToString("yyyy-MM-dd");
            return View();
        }

        // POST: ManualAttendance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int userId, DateTime date, string? checkInTime, string? checkOutTime)
        {
            if (userId == 0)
            {
                TempData["ErrorMessage"] = "Please select a user.";
                await PopulateBranchesAsync();
                await PopulateUsersAsync(null);
                return View();
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _attendanceService.CreateManualAttendanceAsync(userId, date, checkInTime, checkOutTime, currentUserId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index), new { date = date.ToString("yyyy-MM-dd") });
            }

            TempData["ErrorMessage"] = result.Message;
            await PopulateBranchesAsync();
            await PopulateUsersAsync(null);
            return View();
        }

        // GET: ManualAttendance/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var attendance = await _attendanceService.GetAttendanceByIdAsync(id);
            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }

        // POST: ManualAttendance/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string? checkInTime, string? checkOutTime)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _attendanceService.UpdateManualAttendanceAsync(id, checkInTime, checkOutTime, currentUserId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = result.Message;
            var attendance = await _attendanceService.GetAttendanceByIdAsync(id);
            return View(attendance);
        }

        // POST: ManualAttendance/QuickCheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickCheckIn(int userId, DateTime date, string checkInTime)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _attendanceService.ManualCheckInAsync(userId, date, checkInTime, currentUserId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index), new { date = date.ToString("yyyy-MM-dd") });
        }

        // POST: ManualAttendance/QuickCheckOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickCheckOut(int attendanceId, string checkOutTime)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _attendanceService.ManualCheckOutAsync(attendanceId, checkOutTime, currentUserId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: ManualAttendance/GetUsersByBranch
        [HttpGet]
        public async Task<IActionResult> GetUsersByBranch(int branchId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isMainBranch = await _context.Branches
                .Where(b => b.ID == currentUser!.BranchID)
                .Select(b => b.IsMainBranch)
                .FirstOrDefaultAsync();

            var query = _context.Users
                .Where(u => u.IsActive);

            // HR in non-main branch can only see their branch users
            if (!isAdmin && !isMainBranch)
            {
                query = query.Where(u => u.BranchID == currentUser!.BranchID);
            }

            if (branchId > 0)
            {
                query = query.Where(u => u.BranchID == branchId);
            }

            var users = await query
                .OrderBy(u => u.DisplayName)
                .Select(u => new { id = u.Id, name = u.DisplayName })
                .ToListAsync();

            return Json(users);
        }

        #region Private Helpers

        private async Task PopulateBranchesAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isMainBranch = await _context.Branches
                .Where(b => b.ID == currentUser!.BranchID)
                .Select(b => b.IsMainBranch)
                .FirstOrDefaultAsync();

            IQueryable<Branch> query = _context.Branches.Where(b => b.IsActive);

            // HR in non-main branch can only see their branch
            if (!isAdmin && !isMainBranch)
            {
                query = query.Where(b => b.ID == currentUser!.BranchID);
            }

            var branches = await query.OrderBy(b => b.Name).ToListAsync();
            ViewBag.Branches = new SelectList(branches, "ID", "Name");
        }

        private async Task PopulateUsersAsync(int? branchId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isMainBranch = await _context.Branches
                .Where(b => b.ID == currentUser!.BranchID)
                .Select(b => b.IsMainBranch)
                .FirstOrDefaultAsync();

            var query = _context.Users.Where(u => u.IsActive);

            // HR in non-main branch can only see their branch users
            if (!isAdmin && !isMainBranch)
            {
                query = query.Where(u => u.BranchID == currentUser!.BranchID);
            }

            if (branchId.HasValue)
            {
                query = query.Where(u => u.BranchID == branchId.Value);
            }

            var users = await query.OrderBy(u => u.DisplayName).ToListAsync();
            ViewBag.Users = new SelectList(users, "Id", "DisplayName");
        }

        #endregion
    }
}
