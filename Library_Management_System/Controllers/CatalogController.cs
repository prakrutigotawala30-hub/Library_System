using LibraryManagementSystem.ClassLibrary.Data;
using Library_Management_System.ViewModels;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Controllers
{
    public class CatalogController : Controller
    {
        private readonly AppDbContext _context;

        public CatalogController(AppDbContext context)
        {
            _context = context;
        }

        // BROWSE BOOKS
        public async Task<IActionResult> Index(
            string? searchQuery,
            int? categoryId,
            int? authorId,
            string? sortBy,
            int pageNumber = 1)
        {
            const int pageSize = 8;

            var booksQuery = _context.Books
                .Include(b => b.Category)
                .Include(b => b.Author)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrEmpty(searchQuery))
            {
                booksQuery = booksQuery.Where(b =>
                    b.Title.Contains(searchQuery) ||
                    (b.Description != null && b.Description.Contains(searchQuery)));
            }

            // CATEGORY FILTER
            if (categoryId.HasValue)
            {
                booksQuery = booksQuery.Where(b => b.CategoryId == categoryId.Value);
            }

            // AUTHOR FILTER
            if (authorId.HasValue)
            {
                booksQuery = booksQuery.Where(b => b.AuthorId == authorId.Value);
            }

            // SORTING
            booksQuery = sortBy switch
            {
                "title"   => booksQuery.OrderBy(b => b.Title),
                "new"     => booksQuery.OrderByDescending(b => b.CreatedAt),
                "popular" => booksQuery.OrderByDescending(b => b.BorrowCount),
                _         => booksQuery.OrderByDescending(b => b.Id),
            };

            // PAGINATION
            int totalBooks = await booksQuery.CountAsync();

            var books = await booksQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new CatalogViewModel
            {
                SearchQuery = searchQuery,
                CategoryId = categoryId,
                AuthorId = authorId,
                SortBy = sortBy,
                PageNumber = pageNumber,
                TotalPages = (int)Math.Ceiling(totalBooks / (double)pageSize),
                PagedBooks = books,
                Categories = await _context.Categories.ToListAsync(),
                Authors = await _context.Authors.ToListAsync(),
            };

            return View(vm);
        }

        // BOOK DETAILS PAGE
        public async Task<IActionResult> Details(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound();

            var viewModel = new BookDetailsViewModel
            {
                Id = book.Id,
                Title = book.Title,
                AuthorName = book.Author != null ? book.Author.Name : "Unknown",
                CategoryName = book.Category != null ? book.Category.Name : "Uncategorized",
                Description = book.Description ?? string.Empty,
                CoverImageUrl = book.CoverImageUrl ?? string.Empty,
                IsAvailable = book.AvailableCopies > 0
            };

            return View(viewModel);
        }

        // ADD TO WISHLIST (basic version)
        [HttpPost]
        public IActionResult AddToWishlist(int bookId)
        {
            // TODO: replace with real user-based wishlist logic
            TempData["Success"] = "Book Added to wishList";
            return RedirectToAction("Details", new { id = bookId });
        }

        // RESERVE BOOK (basic version)
        [HttpPost]
        public async Task<IActionResult> Reserve(int bookId)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
                return NotFound();

            if (book.AvailableCopies <= 0)
            {
                TempData["Error"] = "Book is not available for reservation.";
                return RedirectToAction("Details", new { id = bookId });
            }

            // Mark reserved (simple logic — decrement copies)
            book.AvailableCopies--;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Book reserved successfully!";
            return RedirectToAction("Details", new { id = bookId });
        }

        // BY CATEGORY
        public async Task<IActionResult> ByCategory(int id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            var books = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Where(b => b.CategoryId == id)
                .ToListAsync();

            ViewBag.CategoryName = category.Name;
            return View(books);
        }

        // BY AUTHOR
        public async Task<IActionResult> ByAuthor(int id)
        {
            var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == id);

            if (author == null)
                return NotFound();

            var books = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Where(b => b.AuthorId == id)
                .ToListAsync();

            ViewBag.AuthorName = author.Name;
            return View(books);
        }
    }
}
