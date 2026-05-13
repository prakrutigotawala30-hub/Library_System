using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.ViewModels
{
    public class CategoryDetailsViewModel
    {
        public Category Category { get; set; }

        public List<Book> Books { get; set; } = new List<Book>();
    }
}