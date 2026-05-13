using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ViewModels
{
    public class MembershipViewModel
    {
        [Required]
        public int MemberId { get; set; }

        public string? MemberName { get; set; }

        public decimal Fee { get; set; } = 1000;
    }
}