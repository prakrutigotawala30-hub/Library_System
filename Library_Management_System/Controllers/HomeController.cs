// using Library_Management_System.Models;
// using LibraryManagementSystem.ClassLibrary.Models;
// using Microsoft.AspNetCore.Mvc;
// using System.Diagnostics;

// namespace Library_Management_System.Controllers
// {
//     // No class-level [Authorize] — HomeController serves the public site
//     // (Home/Index, FAQ, About, Hours, Rules, Privacy, Contact, Error).
//     // Adding [Authorize] here was the cause of "every menu click sends me to
//     // Login" — clicking FAQ/About/etc. would trip the auth filter even though
//     // those pages are meant to be browsable by anonymous visitors.
//     public class HomeController : Controller
//     {
//         private readonly ILogger<HomeController> _logger;

//         public HomeController(ILogger<HomeController> logger)
//         {
//             _logger = logger;
//         }

//         public IActionResult Index()
//         {
//             return View();
//         }



//         [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//         public IActionResult Error()
//         {
//             return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
//         }

//         public IActionResult FAQ()
//         {
//             return View();
//         }

//         public IActionResult About()
//         {
//             return View();
//         }

//         public IActionResult Hours()
//         {
//             return View();
//         }

//         public IActionResult Rules()
//         {
//             return View();
//         }

//         public IActionResult Privacy()
//         {
//             return View();
//         }
//     }
// }


using Library_Management_System.Models;
using Library_Management_System.ViewModels;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Library_Management_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomePageViewModel();

            // CONTINUE READING

            model.ContinueReadingBooks = await _context.Books
                .Include(x => x.Author)
                .OrderByDescending(x => x.CreatedAt)
                .Take(8)
                .ToListAsync();

            // POPULAR CATEGORIES

            model.PopularCategories = await _context.Categories
                .Select(x => new CategoryWithCountViewModel
                {
                    CategoryName = x.Name,
                    BookCount = x.Books.Count(),

                    IconClass =
                        x.Name.Contains("Technology") ? "💻" :
                        x.Name.Contains("Science") ? "🧪" :
                        x.Name.Contains("Business") ? "💼" :
                        x.Name.Contains("History") ? "🏛️" :
                        x.Name.Contains("Programming") ? "🧑‍💻" :
                        "📚"
                })
                .OrderByDescending(x => x.BookCount)
                .Take(6)
                .ToListAsync();

            // NEW ARRIVALS -> LAST 30 DAYS

            var last30Days = DateTime.UtcNow.AddDays(-30);

            model.NewArrivals = await _context.Books
                .Include(x => x.Author)
                .Where(x => x.CreatedAt >= last30Days)
                .OrderByDescending(x => x.CreatedAt)
                .Take(12)
                .ToListAsync();

            return View(model);
        }

        [ResponseCache(Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]

        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                Activity.Current?.Id ??
                HttpContext.TraceIdentifier
            });
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
