namespace LibraryManagementSystem.ViewModels
{
    public class BookCsvModel
    {
        public string Title { get; set; } = "";
        public string ISBN { get; set; } = "";
        public string Author { get; set; } = "";
        public string Category { get; set; } = "";
        public int TotalCopies { get; set; }
    }
}