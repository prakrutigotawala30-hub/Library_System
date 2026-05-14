namespace Library_Management_System.ViewModels
{
    public class BookDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public bool IsAvailable { get; set; }
    }
}