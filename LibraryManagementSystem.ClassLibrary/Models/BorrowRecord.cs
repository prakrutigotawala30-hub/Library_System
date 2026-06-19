using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class BorrowRecord
    {
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public Book? Book { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        public string? ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; }

        [Required]
        public DateTime IssuedOn { get; set; } = DateTime.Now;

        [Required]
        public DateTime DueDate { get; set; }

        public int RenewCount { get; set; } 

        public DateTime? ReturnedOn { get; set; }

        // FINE MODULE

        public int DaysLate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinePerDay { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FineAmount { get; set; }

        public bool FinePaid { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } 

        public string? BookCondition { get; set; }





        [Column(TypeName = "decimal(18,2)")]
        public decimal BorrowFee { get; set; } = 50;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SecurityDeposit { get; set; } 

        public bool IsNonMemberBorrow { get; set; } = false;

        [StringLength(20)]
        public string ReturnStatus { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DamageCharge { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LostBookCharge { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExtraCharge { get; set; }

        [StringLength(20)]
        public string? ReturnCondition { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }

        public bool RefundProcessed { get; set; } = false;

        public DateTime? RefundDate { get; set; }

        public string? RazorpayPaymentId { get; set; }

        public string? RazorpayOrderId { get; set; }

        public string? RazorpayRefundId { get; set; }

    }
}
