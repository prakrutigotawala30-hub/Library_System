using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Library_Management_System.ViewModels;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member,User")]
    public class BorrowHistoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public BorrowHistoryController(AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var memberId = await _context.Members
                .Where(x => x.ApplicationUserId == userId)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            var borrowRecords = await _context.BorrowRecords
                .Include(x => x.Book)
                    .ThenInclude(x => x.Author)
                .Where(x =>
                    x.ApplicationUserId == userId ||
                    (memberId.HasValue && x.MemberId == memberId.Value))
                .OrderByDescending(x => x.IssuedOn)
                .ToListAsync();

            // CURRENT fine rate from admin Settings page — overrides any
            // stale per-record value so the page always reflects what the
            // admin configured today. Falls back to 10 if no settings row.
            var settings = await _context.LibrarySettings.FirstOrDefaultAsync();
            var currentFinePerDay = settings != null && settings.FinePerDay > 0
                ? settings.FinePerDay
                : 10m;

            var query = borrowRecords
                .GroupBy(x => x.BookId)
                .Select(g => new
                {
                    Borrow = g.First(),
                    BorrowCount = g.Count()
                })
                .AsQueryable();

            // =========================
            // FILTERS
            // =========================

            if (!string.IsNullOrEmpty(status))
            {
                if (status.ToLower() == "active")
                {
                    query = query.Where(x =>
                        x.Borrow.ReturnedOn == null &&
                        x.Borrow.DueDate >= DateTime.Now);
                }
                else if (status.ToLower() == "returned")
                {
                    query = query.Where(x =>
                        x.Borrow.ReturnedOn != null);
                }
                else if (status.ToLower() == "overdue")
                {
                    query = query.Where(x =>
                        x.Borrow.ReturnedOn == null &&
                        x.Borrow.DueDate < DateTime.Now);
                }
            }

            var history = query
                .OrderByDescending(x => x.Borrow.IssuedOn)
                .Select(x => new BorrowHistoryViewModel
                {
                    Id = x.Borrow.Id,

                    BookTitle =x.Borrow.Book != null
                            ? x.Borrow.Book.Title
                            : "",

                    Author =x.Borrow.Book != null &&
                            x.Borrow.Book.Author != null
                                ? x.Borrow.Book.Author.Name
                                : "",

                    BorrowDate =
                        x.Borrow.IssuedOn,

                    DueDate =
                        x.Borrow.DueDate,

                    ReturnDate =
                        x.Borrow.ReturnedOn,

                    DaysLate =
                        x.Borrow.DaysLate,

                    // Show the CURRENT admin-configured fine rate (not the
                    // stale value frozen on the borrow row at issue time).
                    FinePerDay = currentFinePerDay,

                    FineAmount =
                        x.Borrow.FineAmount,

                    FinePaid =
                        x.Borrow.FinePaid,

                    BorrowCount =
                        x.BorrowCount,

                    // Non Member Fields

                    IsNonMemberBorrow =
                        x.Borrow.IsNonMemberBorrow,

                    BorrowFee =
                        x.Borrow.BorrowFee,

                    SecurityDeposit =
                        x.Borrow.SecurityDeposit,

                    RefundAmount =
                        x.Borrow.RefundAmount,

                    DamageCharge =
                        x.Borrow.DamageCharge,

                    LostBookCharge =
                        x.Borrow.LostBookCharge,

                    ExtraCharge =
                        x.Borrow.ExtraCharge,

                    RefundProcessed =
                        x.Borrow.RefundProcessed,

                    ReturnCondition =
                        x.Borrow.ReturnCondition,

                    ReturnStatus =
                        x.Borrow.Status,

                    Status =
                        x.Borrow.ReturnedOn != null
                            ? "Returned"
                            : x.Borrow.DueDate < DateTime.Now
                                ? "Overdue"
                                : "Active"
                })
                .ToList();

            ViewBag.CurrentStatus = status;

            ViewBag.TotalBooks =
                history.Sum(x => x.BorrowCount);

            ViewBag.ActiveBooks =
                history.Count(x => x.Status == "Active");

            ViewBag.OverdueBooks =
                history.Count(x => x.Status == "Overdue");

            ViewBag.ReturnedBooks =
                history.Count(x => x.Status == "Returned");

            return View(history);
        }
    }
}
