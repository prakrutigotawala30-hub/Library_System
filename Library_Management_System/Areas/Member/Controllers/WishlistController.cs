using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LibraryManagementSystem.ClassLibrary.Models;
using LibraryManagementSystem.ClassLibrary.Data;

public class WishlistController : Controller
{
    private readonly AppDbContext _context;

    public WishlistController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> ToggleWishlist(int bookId)
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Json(new
            {
                added = false
            });
        }

        var existing =
            await _context.Wishlists
            .FirstOrDefaultAsync(x =>
                x.BookId == bookId &&
                x.MemberId == userId);

        if (existing != null)
        {
            _context.Wishlists.Remove(existing);

            await _context.SaveChangesAsync();

            return Json(new
            {
                added = false
            });
        }

        Wishlist wishlist = new Wishlist
        {
            BookId = bookId,
            MemberId = userId
        };

        _context.Wishlists.Add(wishlist);

        await _context.SaveChangesAsync();

        return Json(new
        {
            added = true
        });
    }
}
