using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Library_Management_System.Models;
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

        // INDEX
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var wishlistBooks = await _context.Wishlists
                .Include(x => x.Book)
                .ThenInclude(x => x.Author)
                .Where(x => x.MemberId == userId)
                .OrderByDescending(x => x.AddedOn)
                .ToListAsync();

            return View(wishlistBooks);
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

            return RedirectToAction("Index");
        }

        // REMOVE

        public async Task<IActionResult> Remove(int bookId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(x =>
                    x.MemberId == userId &&
                    x.BookId == bookId);

            if (wishlistItem != null)
            {
                _context.Wishlists.Remove(wishlistItem);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}