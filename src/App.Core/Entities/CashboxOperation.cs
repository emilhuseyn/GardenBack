using App.Core.Entities.Commons;
using App.Core.Enums;

namespace App.Core.Entities
{
    public class CashboxOperation : BaseEntity
    {
        public int CashboxId { get; set; }
        public CashboxOperationType Type { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public DateTime OperationDate { get; set; } = DateTime.UtcNow;

        public Cashbox Cashbox { get; set; } = null!;
    }
}
