namespace Library_Management_System.ViewModels
{
    public class MemberDashboardViewModel
    {
        public int CurrentBorrows { get; set; }
        public int Overdue { get; set; }
        public decimal FineDue { get; set; }
        public int WishListCount { get; set; }

        public List<RecentActivityViewModel> RecentActivity { get; set; }
            = new List<RecentActivityViewModel>();

        public List<MyBookViewModel> MyBooks { get; set; }
            = new List<MyBookViewModel>();
    }

    public class MyBookViewModel
    {
        public int Id { get; set; }
        public string BookTitle { get; set; }
        public string Author { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsReturned { get; set; }
        public decimal FineAmount { get; set; }
    }

    public class RecentActivityViewModel
    {
        public string Activity { get; set; }
        public DateTime ActivityDate { get; set; }
    }
}