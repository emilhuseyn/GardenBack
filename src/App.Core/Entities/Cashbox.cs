using App.Core.Entities.Commons;
using App.Core.Enums;

namespace App.Core.Entities
{
    public class Cashbox : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public CashboxType Type { get; set; }
        public string? AccountNumber { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<CashboxOperation> Operations { get; set; } = new List<CashboxOperation>();
    }
}
