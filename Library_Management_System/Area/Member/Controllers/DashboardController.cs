using Library_Management_System.ViewModels;
using Library_Management_System.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            // STEP 1: Get MemberId safely
            var memberId = await _context.Members
                .Where(m => m.ApplicationUserId == userId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();

            // STEP 2: Get borrow records
            var borrowRecords = await _context.BorrowRecords
                .Include(b => b.Book)
                .Where(b => b.MemberId == memberId && b.ReturnedOn == null)
                .ToListAsync();

            // STEP 3: Map to ViewModel
            var myBooks = borrowRecords.Select(b => new MyBookViewModel
            {
                Id = b.Id,
                BookTitle = b.Book.Title,
                Author = b.Book.Author.Name,
                IssueDate = b.IssuedOn,
                DueDate = b.DueDate,
                IsReturned = b.ReturnedOn != null,

                FineAmount = b.DueDate < DateTime.Now
                    ? (DateTime.Now - b.DueDate).Days *
                      (b.FinePerDay > 0 ? b.FinePerDay : 5)
                    : 0
            }).ToList();

            // STEP 4: Build dashboard model
            var model = new MemberDashboardViewModel
            {
                CurrentBorrows = borrowRecords.Count,
                Overdue = borrowRecords.Count(x =>
                    x.DueDate < DateTime.Now && x.ReturnedOn == null),

                FineDue = myBooks.Sum(x => x.FineAmount),

                WishListCount = 0,

                MyBooks = myBooks,

                RecentActivity = new List<RecentActivityViewModel>()
            };

            return View(model);
        }
    }
}