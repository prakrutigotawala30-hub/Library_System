namespace LibraryManagementSystem.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int IssuedBooks { get; set; }
        public int OverdueBooks { get; set; }
        public int TotalMembers { get; set; }
        public int ActiveMemberships { get; set; }
        public int ExpiredMemberships { get; set; }
    }
}