namespace Library_Management_System.ViewModels
{
    public class ReturnChargeSummaryVM
    {
        public decimal BorrowFee { get; set; }
        public decimal SecurityDeposit { get; set; }
        public decimal LateFine { get; set; }
        public decimal DamageCharge { get; set; }
        public decimal LostBookCharge { get; set; }
        public decimal AdditionalCharge { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal TotalPayable { get; set; }
    }
}
