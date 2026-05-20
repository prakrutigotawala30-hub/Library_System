using Library_Management_System.Models;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

        // INDEX
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

        // ADD TO WISHLIST
        public async Task<IActionResult> Add(int bookId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // CHECK EXIST
            var exists = await _context.Wishlists
                .AnyAsync(x =>
                    x.MemberId == userId &&
                    x.BookId == bookId);

            if (!exists)
            {
                var wishlist = new Wishlist
                {
                    MemberId = userId,
                    BookId = bookId
                };

                _context.Wishlists.Add(wishlist);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // REMOVE
        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var wishlist = await _context.Wishlists.FindAsync(id);

            if (wishlist != null)
            {
                _context.Wishlists.Remove(wishlist);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}