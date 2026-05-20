namespace Library_Management_System.ViewModels
{
    public class MostPopularBookViewModel
    {
        public int BookId { get; set; }

        public string Title { get; set; }

        public string ISBN { get; set; }

        public string AuthorName { get; set; }

        public string CategoryName { get; set; }

        public string CoverImageUrl { get; set; }

        public int TotalBorrows { get; set; }
    }
}
