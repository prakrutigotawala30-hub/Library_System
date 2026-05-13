using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly AppDbContext context;

        public HomeController(AppDbContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            var model = new DashboardViewModel
            {
                TotalBooks = await context.Books.CountAsync(),
                IssuedBooks = await context.BorrowRecords.CountAsync(b => b.ReturnedOn == null),
                OverdueBooks = await context.BorrowRecords.CountAsync(b => b.ReturnedOn == null && b.DueDate < now),
                TotalMembers = await context.Members.CountAsync(),

                ActiveMemberships = await context.Memberships
                    .CountAsync(m => m.IsActive && m.EndDate > now),

                ExpiredMemberships = await context.Memberships
                    .CountAsync(m => m.EndDate <= now || !m.IsActive)
            };

            var recentBorrows = await context.BorrowRecords
                .AsNoTracking()
                .Include(b => b.Book)
                .Include(b => b.Member)
                .OrderByDescending(b => b.IssuedOn)
                .Take(3)
                .ToListAsync();

            ViewBag.RecentBorrows = recentBorrows;

            ViewBag.FeaturedBooks = await context.Books
                .Where(b => b.IsFeatured)
                .Take(3)
                .ToListAsync();
            // BORROW CHART DATA

            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Now.Date.AddDays(-6 + i))
                .ToList();

            ViewBag.BorrowLabels = last7Days
                .Select(d => d.ToString("ddd"))
                .ToList();

            ViewBag.BorrowData = last7Days
                .Select(day => context.BorrowRecords
                    .Count(x => x.IssuedOn.Date == day))
                .ToList();

            // CATEGORY CHART DATA

            ViewBag.CategoryLabels = await context.Books
                .Include(x => x.Category)
                .GroupBy(x => x.Category!.Name)
                .Select(g => g.Key)
                .ToListAsync();

            ViewBag.CategoryData = await context.Books
                .Include(x => x.Category)
                .GroupBy(x => x.Category!.Name)
                .Select(g => g.Count())
                .ToListAsync();

            // FINE CHART DATA

            var months = Enumerable.Range(1, 12).ToList();

            ViewBag.FineLabels = months
                .Select(m => new DateTime(DateTime.Now.Year, m, 1).ToString("MMM"))
                .ToList();

            ViewBag.FineData = months
                .Select(month => context.BorrowRecords
                    .Where(x => x.IssuedOn.Month == month)
                    .Sum(x => (decimal?)x.FineAmount) ?? 0)
                .ToList();

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Error404()
        {
            return View("~/Views/Shared/Error404.cshtml");
        }

        public IActionResult Error500()
        {
            return View("~/Views/Shared/Error500.cshtml");
        }
    }
}