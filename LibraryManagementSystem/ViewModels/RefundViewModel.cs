namespace LibraryManagementSystem.ViewModels
{
    public class RefundViewModel
    {
        public int BorrowRecordId { get; set; }

        public string MemberName { get; set; } = string.Empty;

        public string BookTitle { get; set; } = string.Empty;

        public decimal SecurityDeposit { get; set; }

        public decimal FineAmount { get; set; }

        public decimal DamageCharge { get; set; }

        public decimal LostBookCharge { get; set; }

        public decimal RefundAmount { get; set; }

        public bool RefundProcessed { get; set; }

        public DateTime? RefundDate { get; set; }

        public string ReturnCondition { get; set; } = string.Empty;

        public DateTime BorrowDate { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }
    }
}
