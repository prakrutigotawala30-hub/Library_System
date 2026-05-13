using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using System.Globalization;

namespace LibraryManagementSystem.Controllers
{
    [Authorize]
    public class BooksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ExportService _exportService;

        public BooksController(AppDbContext context, ExportService exportService)
        {
            _context = context;
            _exportService = exportService;
        }

        // INDEX + SEARCH
        public async Task<IActionResult> Index(string? search)
        {

            var query = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Department)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b =>
                    b.Title.Contains(search) ||
                    b.ISBN.Contains(search) ||
                    b.Author!.Name.Contains(search) ||
                    b.Category!.Name.Contains(search)
                    );
            }

            var data = await query.ToListAsync();

            ViewBag.Search = search;
            return View(data);
        }

        // DETAILS 
        public async Task<IActionResult> Details(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Department)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound();

            var borrowHistory = await _context.BorrowRecords
                .Include(b => b.Member)
                .Where(b => b.BookId == id)
                .OrderByDescending(b => b.IssuedOn)
                .ToListAsync();
            var vm = new BookDetailsViewModel
            {
                Book = book,
                BorrowHistory = borrowHistory ?? new List<BorrowRecord>()
            };

            return View(vm);
        }

        // ADD BOOK
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.CategoryList = new SelectList(_context.Categories, "Id", "Name");

            ViewBag.AuthorList = new SelectList(_context.Authors, "Id", "Name");

            ViewBag.DepartmentList = new SelectList(_context.Departments, "Id", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book)
        {
            if (book.CategoryId == 0)
                ModelState.AddModelError("CategoryId", "Select Category");

            if (book.AuthorId == 0)
                ModelState.AddModelError("AuthorId", "Select Author");

            if (book.TotalCopies <= 0)
                ModelState.AddModelError("TotalCopies", "Enter valid total copies");

            book.AvailableCopies = book.TotalCopies;

            if (ModelState.IsValid)
            {
                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Book added successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryList = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);

            ViewBag.AuthorList = new SelectList(_context.Authors, "Id", "Name", book.AuthorId);

            ViewBag.DepartmentList = new SelectList(_context.Departments, "Id", "Name", book.DepartmentId);

            return View(book);
        }
        // EDIT BOOK

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _context.Books.FindAsync(id);

            if (book == null)
                return NotFound();

            ViewBag.CategoryList = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);

            ViewBag.AuthorList = new SelectList(_context.Authors, "Id", "Name", book.AuthorId);

            ViewBag.DepartmentList = new SelectList(_context.Departments, "Id", "Name", book.DepartmentId);

            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book)
        {
            if (id != book.Id)
                return NotFound();

            if (book.CategoryId == 0)
                ModelState.AddModelError("CategoryId", "Select Category");

            if (book.AuthorId == 0)
                ModelState.AddModelError("AuthorId", "Select Author");

            if (book.TotalCopies <= 0)
                ModelState.AddModelError("TotalCopies", "Enter valid total copies");

            if (ModelState.IsValid)
            {
                _context.Update(book);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Book updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryList = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);

            ViewBag.AuthorList = new SelectList(_context.Authors, "Id", "Name", book.AuthorId);

            ViewBag.DepartmentList = new SelectList(_context.Departments, "Id", "Name", book.DepartmentId);

            return View(book);
        }

        // DELETE BOOK
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Department)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound();

            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Book model)
        {
            var book = await _context.Books
                .Include(b => b.BorrowRecords)
                .FirstOrDefaultAsync(b => b.Id == model.Id);

            if (book == null)
                return NotFound();

            if (book.BorrowRecords != null && book.BorrowRecords.Any())
            {
                TempData["Error"] = "Cannot delete book with borrow history!";
                return RedirectToAction(nameof(Index));
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Book deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult MostPopularBooks()
        {
            var popularBooks = _context.BorrowRecords
                .GroupBy(x => new
                {
                    x.Book.Title,
                    x.Book.ISBN
                })
                .Select(g => new
                {
                    Title = g.Key.Title,
                    ISBN = g.Key.ISBN,
                    TotalBorrows = g.Count()
                })
                .OrderByDescending(x => x.TotalBorrows)
                .Take(10)
                .ToList();

            return View(popularBooks);
        }

        public async Task<IActionResult> ExportExcel()
        {
            var books = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .ToListAsync();

            var file = _exportService.ExportBooks(books);

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Books.xlsx"
            );
        }

        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(BookImportViewModel model)
        {
            try
            {
                if (model.CsvFile == null || model.CsvFile.Length == 0)
                {
                    TempData["Error"] = "Please upload a CSV file.";
                    return View(model);
                }

                var books = new List<Book>();

                using var stream = new StreamReader(model.CsvFile.OpenReadStream());

                using var csv = new CsvReader(stream, CultureInfo.InvariantCulture);

                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    try
                    {
                        int authorId = csv.GetField<int>("AuthorId");

                        int categoryId = csv.GetField<int>("CategoryId");

                        int? departmentId = null;

                        string? departmentText = null;

                        try
                        {
                            departmentText = csv.GetField("DepartmentId");
                        }
                        catch
                        {
                            departmentText = null;
                        }

                        if (!string.IsNullOrWhiteSpace(departmentText))
                        {
                            departmentId = int.Parse(departmentText);
                        }

                        // Validate Author
                        bool authorExists = await _context.Authors
                            .AnyAsync(a => a.Id == authorId);

                        // Validate Category
                        bool categoryExists = await _context.Categories
                            .AnyAsync(c => c.Id == categoryId);

                        // Validate Department
                        bool departmentExists = true;

                        if (departmentId.HasValue)
                        {
                            departmentExists = await _context.Departments
                                .AnyAsync(d => d.Id == departmentId.Value);
                        }

                        if (!authorExists ||
                            !categoryExists ||
                            !departmentExists)
                        {
                            continue;
                        }

                        string isbn = csv.GetField("ISBN");

                        // Duplicate ISBN Check
                        bool isbnExists = await _context.Books
                            .AnyAsync(b => b.ISBN == isbn);

                        if (isbnExists)
                        {
                            continue;
                        }

                        var book = new Book
                        {
                            Title = csv.GetField("Title"),
                            ISBN = isbn,
                            AuthorId = authorId,
                            CategoryId = categoryId,
                            DepartmentId = departmentId,
                            TotalCopies = csv.GetField<int>("TotalCopies"),
                            AvailableCopies = csv.GetField<int>("AvailableCopies"),
                            IsFeatured = csv.GetField<bool>("IsFeatured")
                        };

                        books.Add(book);
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (books.Any())
                {
                    await _context.Books.AddRangeAsync(books);

                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        $"{books.Count} books imported successfully!";
                }
                else
                {
                    TempData["Error"] =
                        "No valid records found in CSV.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return View(model);
            }
        }
    }


}