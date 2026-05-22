using Library_Management_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member")]
    public class BorrowHistoryController : Controller
    {
        private readonly AppDbContext _context;

        public BorrowHistoryController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            // MEMBER ID

            var memberId = await _context.Members
                .Where(x => x.ApplicationUserId == userId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            // QUERY

            var query = _context.BorrowRecords
                .Include(x => x.Book)
                .ThenInclude(x => x.Author)
                .Where(x => x.MemberId == memberId)
                .AsQueryable();

            // FILTERS

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                {
                    query = query.Where(x => x.ReturnedOn == null);
                }
                else if (status == "returned")
                {
                    query = query.Where(x => x.ReturnedOn != null);
                }
                else if (status == "overdue")
                {
                    query = query.Where(x =>
                        x.ReturnedOn == null &&
                        x.DueDate < DateTime.Now);
                }
            }

            // DATA

            var history = await query
                .OrderByDescending(x => x.IssuedOn)
                .Select(x => new BorrowHistoryViewModel
                {
                    Id = x.Id,

                    BookTitle = x.Book.Title,

                    Author = x.Book.Author.Name,

                    BorrowDate = x.IssuedOn,

                    DueDate = x.DueDate,

                    ReturnDate = x.ReturnedOn,

                    FineAmount = x.ReturnedOn == null &&
                                 x.DueDate < DateTime.Now
                        ? (DateTime.Now - x.DueDate).Days *
                          (x.FinePerDay > 0 ? x.FinePerDay : 5)
                        : 0,

                    Status = x.ReturnedOn != null
                        ? "Returned"
                        : x.DueDate < DateTime.Now
                            ? "Overdue"
                            : "Active"
                })
                .ToListAsync();

            ViewBag.CurrentStatus = status;

            return View(history);
        }
    }
}
