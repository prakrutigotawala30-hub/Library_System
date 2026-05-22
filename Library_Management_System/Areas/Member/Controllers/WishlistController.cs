using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    // Intentionally NO [Authorize] — the heart icon lives on the PUBLIC
    // catalog so anonymous visitors can click it. Each action checks
    // `user == null` and returns JSON { success:false, message:"Login required" }
    // (or RedirectToAction("Login") for non-AJAX actions). The JS then shows
    // an alert. Adding [Authorize] here makes ASP.NET return a 302/HTML
    // redirect that JS can't parse → "error" popup on every heart click for
    // logged-out users.
    public class WishlistController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(AppDbContext context,
                                  UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================
        // VIEW WISHLIST (BOOK ONLY)
        // =========================
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var wishlistItems = await _context.Wishlists
                .Include(w => w.Book)
                .Where(w => w.MemberId == user.Id && w.BookId != null)
                .OrderByDescending(w => w.AddedOn)
                .ToListAsync();

            return View(wishlistItems);
        }

        // =========================
        // TOGGLE WISHLIST (BOOK ONLY)
        // =========================
        [HttpPost]
        public async Task<IActionResult> Toggle(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Login required"
                });
            }

            var existing = await _context.Wishlists
                .FirstOrDefaultAsync(x =>
                    x.MemberId == user.Id &&
                    x.BookId == bookId);

            bool added;

            if (existing == null)
            {
                _context.Wishlists.Add(new Wishlist
                {
                    MemberId = user.Id,
                    BookId = bookId,
                    AddedOn = DateTime.Now
                });

                added = true;
            }
            else
            {
                _context.Wishlists.Remove(existing);
                added = false;
            }

            await _context.SaveChangesAsync();

            var wishlistIds = await _context.Wishlists
                .Where(x => x.MemberId == user.Id && x.BookId != null)
                .Select(x => x.BookId.Value)
                .ToListAsync();

            return Json(new
            {
                success = true,
                added,
                wishlistIds
            });
        }

        // =========================
        // REMOVE BOOK
        // =========================
        [HttpPost]
        public async Task<IActionResult> Remove(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var item = await _context.Wishlists
                .FirstOrDefaultAsync(w =>
                    w.MemberId == user.Id &&
                    w.BookId == bookId);

            if (item != null)
            {
                _context.Wishlists.Remove(item);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Book removed from wishlist.";

            return RedirectToAction("Index");
        }
    }
}
