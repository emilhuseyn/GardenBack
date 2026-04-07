namespace App.Core.Entities
{
    public class CashboxTransfer
    {
        public int Id { get; set; }
        public int FromCashboxId { get; set; }
        public int ToCashboxId { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public DateTime TransferDate { get; set; }

        public Cashbox FromCashbox { get; set; } = null!;
        public Cashbox ToCashbox { get; set; } = null!;
    }
}
