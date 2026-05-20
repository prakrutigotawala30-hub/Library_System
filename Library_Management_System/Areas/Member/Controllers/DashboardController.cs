
using Library_Management_System.ViewModels;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
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

            // MEMBER ID

            var memberId = await _context.Members
                .Where(m => m.ApplicationUserId == userId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();

            // CURRENT BORROWED BOOKS

            var borrowRecords = await _context.BorrowRecords
                .Include(b => b.Book)
                .ThenInclude(b => b.Author)
                .Where(b => b.MemberId == memberId && b.ReturnedOn == null)
                .ToListAsync();

            // MY BOOKS

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

            // RETURNED BOOKS COUNT

            var returnedBooks = await _context.BorrowRecords
                .CountAsync(x =>
                    x.MemberId == memberId &&
                    x.ReturnedOn != null);

            // RECENT ACTIVITY

            var recentActivities = await _context.BorrowRecords
                .Include(x => x.Book)
                .Where(x => x.MemberId == memberId)
                .OrderByDescending(x => x.IssuedOn)
                .Take(5)
                .Select(x => new RecentActivityViewModel
                {
                    Activity = x.ReturnedOn != null
                        ? "Returned \"" + x.Book.Title + "\""
                        : "Borrowed \"" + x.Book.Title + "\"",

                    ActivityDate = x.ReturnedOn ?? x.IssuedOn
                })
                .ToListAsync();

            // FINAL MODEL

            var model = new MemberDashboardViewModel
            {
                CurrentBorrows = borrowRecords.Count,

                Overdue = borrowRecords.Count(x =>
                    x.DueDate < DateTime.Now &&
                    x.ReturnedOn == null),

                FineDue = myBooks.Sum(x => x.FineAmount),

                WishListCount = 0,

                ReturnedBooks = returnedBooks,

                MyBooks = myBooks,

                RecentActivity = recentActivities
            };

            return View(model);
        }
    }
}

