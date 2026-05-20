using LibraryManagementSystem.ClassLibrary.Data;
using Library_Management_System.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Controllers
{
    public class BooksController : Controller
    {
        private readonly AppDbContext _context;

        public BooksController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> MostPopularBooks()
        {
            var books = await _context.BorrowRecords
                .Include(x => x.Book)
                .ThenInclude(b => b.Author)

                .Include(x => x.Book)
                .ThenInclude(b => b.Category)

                .GroupBy(x => x.BookId)

                .Select(g => new MostPopularBookViewModel
                {
                    BookId = g.Key,

                    Title = g.First().Book.Title,

                    ISBN = g.First().Book.ISBN,

                    AuthorName = g.First().Book.Author.Name,

                    CategoryName = g.First().Book.Category.Name,

                    CoverImageUrl = g.First().Book.CoverImageUrl,

                    TotalBorrows = g.Count()
                })

                .OrderByDescending(x => x.TotalBorrows)

                .Take(10)

                .ToListAsync();

            return View(books);
        }
    }
}
