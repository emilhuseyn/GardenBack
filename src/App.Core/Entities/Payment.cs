using App.Core.Entities.Commons;
using App.Core.Enums;

namespace App.Core.Entities
{
    public class Payment : BaseEntity
    {
        public int ChildId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal OriginalAmount { get; set; }
        public DiscountType DiscountType { get; set; } = DiscountType.None;
        public decimal DiscountValue { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal? LastPaymentAmount { get; set; }
        public int? CashboxId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Debt;
        public string? Notes { get; set; }

        /// <summary>Çoxaylı vahid çek üçün paket ID-si — eyni kütləvi ödənişin bütün sətirlərində eynidir</summary>
        public Guid? PaymentBatchId { get; set; }

        /// <summary>
        /// Hesablanan dövrün ay daxilindəki BAŞLANĞIC günü (həmin gün də daxildir).
        /// Null — tam ay. Bu sütun "Dövr:" qeydini ƏVƏZ EDİR: hesab məntiqi yalnız buradan oxuyur.
        /// </summary>
        public int? PeriodStartDay { get; set; }

        /// <summary>
        /// Hesablanan dövrün ay daxilindəki BİTİŞ günü — SON HESABLANAN gün (həmin gün də daxildir).
        /// Null — tam ay. Gün sayı həmişə max(0, PeriodEndDay - PeriodStartDay + 1)-dir.
        /// C2: uşağın çıxış tarixi "artıq gəlmədiyi İLK gün"dür (eksklüziv), ona görə çıxış ayında
        /// bu sütun çıxış günündən BİR AZ ƏVVƏLdir (çıxış günü - 1). Uşaq həmin ay ümumiyyətlə
        /// gəlməyibsə aralıq BOŞ olur və sütunlar bunu açıq şəkildə göstərir: bitiş = başlanğıc - 1
        /// (məs. 1/0). Belə sətri 1 günlük saymaq OLMAZ.
        /// </summary>
        public int? PeriodEndDay { get; set; }

        /// <summary>
        /// Sətir HAZIRDA hansı çıxış tarixinə görə sıfırlanıb. Yalnız sıfırlanmış sətirdə doludur;
        /// bərpa olunanda və ya reaktivasiyada yekunlaşdırılanda null-a qayıdır.
        /// Bərpanın YEGANƏ açarı budur — qeyd mətni artıq oxunmur.
        /// </summary>
        public DateTime? ZeroedByExitDate { get; set; }

        /// <summary>
        /// Sətir BAĞLANMIŞ qeydiyyat epizoduna aiddir və bir daha AVTOMATİK yenidən yazılmır (A2/G1).
        /// Uşaq geri qayıdanda true olur və iki halı əhatə edir:
        ///  (a) çıxışdan sonrakı sıfırlanmış aylar — uşaq həmin aylarda həqiqətən gəlməyib,
        ///  (b) həmin epizodun ÇIXIŞ ayı — gün-gün bölünmüş məbləği YEKUNDUR.
        /// ZeroedByExitDate boş olduğu üçün belə sətir "heç vaxt sıfırlanmamış sətir"dən seçilmirdi;
        /// bu bayraq onları AYIRIR. True olan sətir nə sıfırlanır (ZeroedByExitDate üstündən
        /// yazılmır), nə də sonrakı çıxış düzəlişi ilə tam aya qaytarılır.
        /// Ad tarixidir — məna "təsdiqlənmiş yoxluq"dan "bağlanmış epizodun yekun sətri"nə genişlənib.
        /// </summary>
        public bool AbsenceConfirmed { get; set; }

        /// <summary>Audit sahəsi — FK deyil, yalnız user ID saxlanılır</summary>
        public string? RecordedById { get; set; }

        public Child Child { get; set; } = null!;
        public Cashbox? Cashbox { get; set; }
    }
}
