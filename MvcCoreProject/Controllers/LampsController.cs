using CoreProject.Models;
using CoreProject.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

namespace MvcCoreProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LampsController : Controller
    {
        private readonly ILampService _lampService;

        public LampsController(ILampService lampService)
        {
            _lampService = lampService;
        }

        // GET: Lamps
        public async Task<IActionResult> Index()
        {
            var lamps = await _lampService.GetAllLampsAsync();
            return View(lamps);
        }

        // GET: Lamps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lamp = await _lampService.GetLampByIdAsync(id.Value);
            if (lamp == null)
            {
                return NotFound();
            }

            return View(lamp);
        }

        // GET: Lamps/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            return View();
        }

        // POST: Lamps/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DeviceID,Name,Description,BranchID,TimetableID")] Lamp lamp)
        {
            // Remove Branch and Timetable navigation property errors (they're null during binding)
            ModelState.Remove("Branch");
            ModelState.Remove("Timetable");

            if (!ModelState.IsValid)
            {
                // Add validation errors to TempData for debugging
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = string.Join("; ", errors);

                await PopulateDropdownsAsync(lamp.BranchID, lamp.TimetableID);
                return View(lamp);
            }

            var result = await _lampService.CreateLampAsync(lamp);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await PopulateDropdownsAsync(lamp.BranchID, lamp.TimetableID);
                return View(lamp);
            }
        }

        // GET: Lamps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lamp = await _lampService.GetLampByIdAsync(id.Value);
            if (lamp == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync(lamp.BranchID, lamp.TimetableID);
            return View(lamp);
        }

        // POST: Lamps/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,DeviceID,Name,Description,BranchID,TimetableID")] Lamp lamp)
        {
            if (id != lamp.ID)
            {
                return NotFound();
            }

            // Remove Branch and Timetable navigation property errors (they're null during binding)
            ModelState.Remove("Branch");
            ModelState.Remove("Timetable");

            if (!ModelState.IsValid)
            {
                // Add validation errors to TempData for debugging
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = string.Join("; ", errors);

                await PopulateDropdownsAsync(lamp.BranchID, lamp.TimetableID);
                return View(lamp);
            }

            var result = await _lampService.UpdateLampAsync(lamp);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await PopulateDropdownsAsync(lamp.BranchID, lamp.TimetableID);
                return View(lamp);
            }
        }

        // GET: Lamps/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lamp = await _lampService.GetLampByIdAsync(id.Value);
            if (lamp == null)
            {
                return NotFound();
            }

            return View(lamp);
        }

        // POST: Lamps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _lampService.DeleteLampAsync(id);

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

        // POST: Lamps/TurnOn/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TurnOn(int id)
        {
            var result = await _lampService.SendStateChangeAsync(id, turnOn: true);

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

        // POST: Lamps/TurnOff/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TurnOff(int id)
        {
            var result = await _lampService.SendStateChangeAsync(id, turnOn: false);

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

        // POST: Lamps/ToggleManualOverride/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleManualOverride(int id, bool enable, int? state)
        {
            var result = await _lampService.ToggleManualOverrideAsync(id, enable, state);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // API: Get timetables by branch (for dynamic dropdown)
        [HttpGet]
        public async Task<IActionResult> GetTimetablesByBranch(int branchId)
        {
            var timetables = await _lampService.GetTimetablesAsync();
            var filteredTimetables = timetables
                .Where(t => t.BranchID == branchId)
                .Select(t => new { id = t.ID, name = t.Name })
                .ToList();

            return Json(filteredTimetables);
        }

        private async Task PopulateDropdownsAsync(int? selectedBranchId = null, int? selectedTimetableId = null)
        {
            var branches = await _lampService.GetBranchesAsync();
            var timetables = await _lampService.GetTimetablesAsync();

            ViewBag.BranchID = new SelectList(branches, "ID", "Name", selectedBranchId);
            ViewBag.TimetableID = new SelectList(timetables, "ID", "Name", selectedTimetableId);
            ViewBag.AllTimetablesJson = System.Text.Json.JsonSerializer.Serialize(
                timetables.Select(t => new { id = t.ID, name = t.Name, branchId = t.BranchID }).ToList()
            );
        }
    }
}
