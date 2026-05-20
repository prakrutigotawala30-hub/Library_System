using Library_Management_System.Models;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Library_Management_System.Controllers
{
    // No class-level [Authorize] — HomeController serves the public site
    // (Home/Index, FAQ, About, Hours, Rules, Privacy, Contact, Error).
    // Adding [Authorize] here was the cause of "every menu click sends me to
    // Login" — clicking FAQ/About/etc. would trip the auth filter even though
    // those pages are meant to be browsable by anonymous visitors.
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

       

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Hours()
        {
            return View();
        }

        public IActionResult Rules()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
