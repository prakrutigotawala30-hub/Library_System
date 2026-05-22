using LibraryManagementSystem.ClassLibrary.Models;

namespace Library_Management_System.ViewModels
{
    public class HomePageViewModel
    {
        public List<Book> ContinueReadingBooks { get; set; } = new();

        public List<Book> NewArrivals { get; set; } = new();

        public List<CategoryWithCountViewModel> PopularCategories { get; set; } = new();

        public List<Event> Events { get; set; } = new();
    }

    public class CategoryWithCountViewModel
    {
        public string CategoryName { get; set; } = string.Empty;

        public int BookCount { get; set; }

        public string IconClass { get; set; } = "📚";
    }
}
