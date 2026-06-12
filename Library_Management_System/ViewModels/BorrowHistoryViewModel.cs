namespace Library_Management_System.ViewModels
{
    public class BorrowHistoryViewModel
    {
        public int Id { get; set; }

        public string BookTitle { get; set; }

        public string Author { get; set; }

        public DateTime BorrowDate { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        public string Status { get; set; }

        // FINE MODULE

        public int DaysLate { get; set; }

        public decimal FinePerDay { get; set; }

        public decimal FineAmount { get; set; }

        public bool FinePaid { get; set; }

        public int BorrowCount { get; set; }

        public bool IsNonMemberBorrow { get; set; }

        public decimal BorrowFee { get; set; }

        public decimal SecurityDeposit { get; set; }

        public decimal RefundAmount { get; set; }

        public bool RefundProcessed { get; set; }

        public decimal DamageCharge { get; set; }

        public decimal LostBookCharge { get; set; }

        public string ReturnStatus { get; set; }
    }
}
