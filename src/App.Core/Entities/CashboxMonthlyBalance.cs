using App.Core.Entities.Commons;

namespace App.Core.Entities
{
    /// <summary>
    /// Hər kassa üçün hər ayın açılış qalığı (əvvəlki aydan keçən məbləğ).
    /// </summary>
    public class CashboxMonthlyBalance : BaseEntity
    {
        public int CashboxId { get; set; }
        public Cashbox Cashbox { get; set; } = null!;

        public int Month { get; set; }
        public int Year { get; set; }

        /// <summary>Ayın əvvəlinə olan qalıq məbləğ (manuel daxil edilir).</summary>
        public decimal OpeningBalance { get; set; }
    }
}
