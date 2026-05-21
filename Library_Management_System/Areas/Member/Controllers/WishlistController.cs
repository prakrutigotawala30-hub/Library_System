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
    public class WishlistController : Controller
    {
        private readonly AppDbContext _context;

        public WishlistController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Wishlist
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var wishlist = await _context.Wishlists
                .Include(w => w.Book)
                    .ThenInclude(b => b.Author)
                .Include(w => w.Book)
                    .ThenInclude(b => b.Category)
                .Where(w => w.MemberId == userId)
                .ToListAsync();

            return View(wishlist);
        }

        // AJAX TOGGLE (IMPORTANT FIX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int bookId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            var existing = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.BookId == bookId && w.MemberId == userId);

            bool added;

            if (existing == null)
            {
                _context.Wishlists.Add(new Wishlist
                {
                    BookId = bookId,
                    MemberId = userId
                });

                added = true;
            }
            else
            {
                _context.Wishlists.Remove(existing);
                added = false;
            }

            await _context.SaveChangesAsync();

            return Json(new { added });
        }
    }
}
