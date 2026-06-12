using LibraryManagementSystem.ClassLibrary.Models;

namespace Library_Management_System.ViewModels
{
    public class MembershipRazorPayViewModel
    {
        public string MembershipType { get; set; }

        public int DurationMonths { get; set; }

        public decimal Amount { get; set; }

        public string RazorpayKey { get; set; }

        public string RazorpayOrderId { get; set; }
    }
}
