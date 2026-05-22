using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class MembershipPayment
    {
        public int Id { get; set; }

        public int MembershipId { get; set; }

        public Membership Membership { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        [Required]
        public string PaymentStatus { get; set; }

        public string TransactionId { get; set; }

        public DateTime PaymentDate { get; set; }
            = DateTime.Now;
    }
}
