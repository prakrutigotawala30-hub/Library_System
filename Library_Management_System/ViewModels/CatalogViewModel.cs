using LibraryManagementSystem.Models;

namespace Library_Management_System.ViewModels
{
    public class CatalogViewModel
    {
        public string? SearchQuery { get; set; }

        public int? CategoryId { get; set; }

        public int? AuthorId { get; set; }

        public string? Availability { get; set; }

        public string? SortBy { get; set; }

        public int PageNumber { get; set; } = 1;

        public int TotalPages { get; set; }

        public List<BookDto> PagedBooks { get; set; } = new();

        public List<Category> Categories { get; set; } = new();

        public List<Author> Authors { get; set; } = new();
    }
}
