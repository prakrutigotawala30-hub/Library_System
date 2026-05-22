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
    public class WishlistController : Controller
    {
        private readonly AppDbContext _context;

        public WishlistController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Member/Wishlist  — list current user's saved books
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var items = await _context.Wishlists
                .Include(w => w.Book)
                    .ThenInclude(b => b.Author)
                .Where(w => w.MemberId == userId)
                .OrderByDescending(w => w.AddedOn)
                .Select(w => new WishlistViewModel
                {
                    WishlistId = w.Id,
                    BookId     = w.BookId,
                    Title      = w.Book.Title,
                    Author     = w.Book.Author != null ? w.Book.Author.Name : "",
                    ImageUrl   = w.Book.CoverImageUrl ?? ""
                })
                .ToListAsync();

            return View(items);
        }

        // POST: /Member/Wishlist/ToggleWishlist  — add if missing, remove if present
        // Returns JSON { added: true|false } for Wishlist.js to update the heart icon
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleWishlist(int bookId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Json(new { added = false });

            var existing = await _context.Wishlists
                .FirstOrDefaultAsync(x =>
                    x.BookId == bookId &&
                    x.MemberId == userId);

            if (existing != null)
            {
                _context.Wishlists.Remove(existing);
                await _context.SaveChangesAsync();
                return Json(new { added = false });
            }

            _context.Wishlists.Add(new Wishlist
            {
                BookId   = bookId,
                MemberId = userId
            });
            await _context.SaveChangesAsync();
            return Json(new { added = true });
        }

        // POST: /Member/Wishlist/Remove/{id} — used by the Index page's remove buttons
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var item = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.Id == id && w.MemberId == userId);

            if (item != null)
            {
                _context.Wishlists.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
