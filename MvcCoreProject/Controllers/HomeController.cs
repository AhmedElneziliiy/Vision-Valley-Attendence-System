using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcCoreProject.Models;
using System.Diagnostics;

namespace MvcCoreProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // If user is authenticated, redirect to Dashboard
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            // Otherwise show the home/landing page
            return View();
        }
        
        [Authorize]
        public IActionResult Privacy()
        {
            ViewBag.Message = "You are authorized!";
            ViewBag.User = User.Identity?.Name;
            ViewBag.BranchID = User.FindFirst("BranchID")?.Value;
            Console.WriteLine($"User: {ViewBag.User}, BranchID: {ViewBag.BranchID}");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
