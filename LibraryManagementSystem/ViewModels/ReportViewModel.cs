namespace LibraryManagementSystem.ViewModels
{
    public class ReportViewModel
    {
        public List<OverdueBookVM> overdueBookVMs { get; set; } = new();

        public List<TopBorrowerVM> topBorrowerVMs { get; set; } = new();    

    }

    public class OverdueBookVM
    {
        public string BookTitle { get; set; }
        public string MemberName { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysOverdue { get; set; }
    }

    public class TopBorrowerVM
    {
        public string MemberName { get; set; }
        public int TotalBorrowed { get; set; }
    }
}
