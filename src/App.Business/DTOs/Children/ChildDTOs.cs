namespace App.Business.DTOs.Children
{
    public class CreateChildRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int GroupId { get; set; }
        /// <summary>ScheduleConfig.Code dəyəri (məs. "FullDay", "HalfDay")</summary>
        public string ScheduleType { get; set; } = "FullDay";
        public decimal MonthlyFee { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public int PaymentDay { get; set; } = 1;
        public string ParentFullName { get; set; } = string.Empty;
        public string? SecondParentFullName { get; set; }
        public string ParentPhone { get; set; } = string.Empty;
        public string? SecondParentPhone { get; set; }
        public string? ParentEmail { get; set; }
        public int? PersonId { get; set; }
        public string? FaceIdToken { get; set; }
    }

    public class UpdateChildRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? GroupId { get; set; }
        public string? ScheduleType { get; set; }
        public decimal? MonthlyFee { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public int? PaymentDay { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public DateTime? DeactivationDate { get; set; }
        public string? ParentFullName { get; set; }
        public string? SecondParentFullName { get; set; }
        public string? ParentPhone { get; set; }
        public string? SecondParentPhone { get; set; }
        public string? ParentEmail { get; set; }
        public int? PersonId { get; set; }
        public string? FaceIdToken { get; set; }
    }

    /// <summary>
    /// Deaktivləşdirmə sorğusu. Gövdə boş/göndərilməmiş ola bilər — bu halda bugünkü tarix götürülür.
    /// </summary>
    public class DeactivateChildRequest
    {
        /// <summary>
        /// Uşağın ARTIQ GƏLMƏDİYİ İLK gün — həmin gün HESABLANMIR (C2, eksklüziv).
        /// Məs. 01.08 verilsə avqust ayı 0 gün (0 ₼) olur.
        /// Boşdursa bugün. Qəbul tarixindən əvvəl və ya sabahdan gec ola bilməz.
        /// </summary>
        public DateTime? EffectiveDate { get; set; }
    }

    /// <summary>
    /// Uşağı geri qaytarma sorğusu.
    /// </summary>
    public class ActivateChildRequest
    {
        /// <summary>
        /// Uşağın YENİDƏN GƏLDİYİ İLK gün — həmin gün HESABLANIR (İNKLÜZİV, qeydiyyat tarixi ilə eyni məntiq).
        /// Məs. 19.08 verilsə avqust 19-31 hesablanır. Boşdursa bugün.
        /// Qəbul tarixindən əvvəl və ya sabahdan gec ola bilməz.
        /// </summary>
        public DateTime? ReturnDate { get; set; }
    }

    /// <summary>
    /// Deaktivləşdirmədən sonra hesabların yenidən qurulmasının nəticəsi.
    /// </summary>
    public class DeactivationRecalcResult
    {
        public int ChildId { get; set; }
        public string ChildFullName { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        /// <summary>Çıxış ayından SONRAKI sıfırlanmış sətirlərin sayı.</summary>
        public int ZeroedMonths { get; set; }
        /// <summary>
        /// Real pul ödənildiyi üçün toxunulmayan sətirlər — geri qaytarma əl ilə edilməlidir.
        /// </summary>
        public List<SkippedPaidMonth> SkippedPaidMonths { get; set; } = new();
        /// <summary>
        /// BAĞLANMIŞ qeydiyyat epizodunda yekunlaşdırıldığı üçün (AbsenceConfirmed) sıfırlanmayan
        /// aylar (G2). Əvvəllər belə aylar heç bir siyahıya düşmürdü və ştab onların yenidən
        /// hesablandığını güman edirdi. Sətir QƏSDƏN toxunulmaz qalır — yalnız BİLDİRİLİR.
        /// </summary>
        public List<SkippedConfirmedMonth> SkippedConfirmedMonths { get; set; } = new();
        /// <summary>
        /// Əvvəlki (daha erkən) çıxış tarixi ilə sıfırlanmış, indi yenidən hesablanan aylar.
        /// Çıxış tarixi düzəldildikdə hesabların geri qayıtdığını ştaba göstərir.
        /// </summary>
        public List<RestoredMonth> RestoredMonths { get; set; } = new();
        /// <summary>Bərpa olunan ayların sayı (UI üçün qısa yol).</summary>
        public int RestoredMonthsCount => RestoredMonths.Count;
        /// <summary>
        /// Çıxış ayı yenidən bölündükdə ödənilmiş məbləğ yeni məbləğdən çox qalıbsa — artıq ödəniş.
        /// Null deyilsə geri qaytarma əl ilə edilməlidir.
        /// </summary>
        public ExitMonthOverpayment? ExitMonthOverpayment { get; set; }
        /// <summary>
        /// Çıxış ayı İRƏLİ sürüşdükdə yeni hesab ödənilmiş məbləğdən ÇOX olarsa — az hesablanma (F5).
        /// Tam ödənilmiş sətrin məbləğinə toxunmuruq, ona görə fərq ştaba BİLDİRİLİR.
        /// </summary>
        public ExitMonthUnderpayment? ExitMonthUnderpayment { get; set; }
        /// <summary>
        /// Bərpa pəncərəsində HESABI ÜMUMİYYƏTLƏ OLMAYAN aylar (F1). Sətir avtomatik YARADILMIR:
        /// sxemdə "bu ay həqiqətən gəlməyib" ilə "bu aya sətir yazılmayıb" fərqlənmir, ona görə
        /// avtomatik yaratmaq real valideynə uydurma borc yazardı. Ştab siyahını görür və lazım
        /// olsa ayı əl ilə (ödəniş qeyd edərək və ya aylıq generasiya ilə) yaradır.
        /// </summary>
        public List<MissingMonth> MissingMonths { get; set; } = new();
        /// <summary>
        /// Çıxış ayının ÖZÜNÜN nəticəsi (F3): sətir yaradıldı / yenidən hesablandı / toxunulmadı.
        /// Əməliyyatın toxunduğu HƏR ay hesabatda görünməlidir.
        /// </summary>
        public ExitMonthOutcome? ExitMonth { get; set; }
    }

    /// <summary>
    /// Bərpa pəncərəsində sətri olmayan ay (F1) — yaradılmır, yalnız BİLDİRİLİR.
    /// </summary>
    public class MissingMonth
    {
        public int Month { get; set; }
        public int Year { get; set; }
    }

    /// <summary>Çıxış ayının yekun vəziyyəti (F3) — hər deaktivasiyada dolur.</summary>
    public class ExitMonthOutcome
    {
        /// <summary>Sətir bu əməliyyatda yaradılıbsa da real ID-dir (SaveChanges transaksiyanın içindədir).</summary>
        public int PaymentId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        /// <summary>Əməliyyatdan SONRAKI yekun məbləğ.</summary>
        public decimal FinalAmount { get; set; }
        /// <summary>Sətirdəki real ödəniş (heç vaxt dəyişdirilmir).</summary>
        public decimal PaidAmount { get; set; }
        /// <summary>Hesablanan dövrün başlanğıc günü (daxil).</summary>
        public int PeriodStartDay { get; set; }
        /// <summary>
        /// Hesablanan dövrün SON HESABLANAN günü (daxil) — çıxış tarixinin bir gün əvvəli.
        /// Uşaq həmin ay heç gəlməyibsə başlanğıcdan kiçik olur (boş aralıq, məs. 1/0).
        /// </summary>
        public int PeriodEndDay { get; set; }
        /// <summary>Sətir bu əməliyyatla YARADILDI (əvvəllər mövcud deyildi).</summary>
        public bool Created { get; set; }
        /// <summary>Sətrin məbləğinə toxunulmadı — ştab əl ilə yoxlamalıdır.</summary>
        public bool NeedsManualReview { get; set; }
        /// <summary>Toxunulmama səbəbi (ştaba göstərilir). Yalnız NeedsManualReview olanda dolur.</summary>
        public string? Reason { get; set; }
    }

    /// <summary>Sıfırlanmayıb keçilən (ödənişi olan) ay.</summary>
    public class SkippedPaidMonth
    {
        public int PaymentId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal PaidAmount { get; set; }
    }

    /// <summary>
    /// Bağlanmış epizodda yekunlaşdırıldığı üçün sıfırlanmayan ay (G2) — məbləği olduğu kimi qalır.
    /// </summary>
    public class SkippedConfirmedMonth
    {
        public int PaymentId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        /// <summary>Toxunulmayan yekun məbləğ (0 ola bilər — "gəlmədiyi ay").</summary>
        public decimal FinalAmount { get; set; }
        /// <summary>Sətirdəki real ödəniş (heç vaxt dəyişdirilmir).</summary>
        public decimal PaidAmount { get; set; }
    }

    /// <summary>Əvvəlki səhv çıxış tarixi ilə sıfırlanmış, indi yenidən hesablanan ay.</summary>
    public class RestoredMonth
    {
        public int PaymentId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        /// <summary>Bərpadan sonrakı ödəniləcək məbləğ.</summary>
        public decimal FinalAmount { get; set; }
        /// <summary>
        /// Bərpa anında sətirdə olan REAL ödəniş. 0-dan böyükdürsə bərpa ödənişi olan ayın
        /// üstünə düşüb (məs. 100 ₼ ödənilmiş çıxış ayı 300 ₼-a qayıdır → PartiallyPaid):
        /// pul itmir, amma ştab fərqi görməlidir.
        /// </summary>
        public decimal PaidAmount { get; set; }
    }

    /// <summary>Çıxış ayının yenidən bölünməsi nəticəsində yaranan artıq ödəniş.</summary>
    public class ExitMonthOverpayment
    {
        public int PaymentId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        /// <summary>Artıq ödənilmiş məbləğ (dəyişmir).</summary>
        public decimal PaidAmount { get; set; }
        /// <summary>Yenidən bölünmədən sonrakı məbləğ.</summary>
        public decimal NewFinalAmount { get; set; }
        /// <summary>Valideynə qaytarılmalı fərq.</summary>
        public decimal Difference { get; set; }
    }

    /// <summary>
    /// Çıxış tarixi irəli düzəldildikdə yaranan AZ HESABLANMA (F5): sətir tam ödənilmiş olduğu üçün
    /// məbləği yenidən yazılmır, amma yeni hesab ödənilmişdən çoxdur — fərq əl ilə alınmalıdır.
    /// </summary>
    public class ExitMonthUnderpayment
    {
        public int PaymentId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        /// <summary>Ödənilmiş məbləğ (dəyişmir).</summary>
        public decimal PaidAmount { get; set; }
        /// <summary>Yeni (düzgün) yekun məbləğ.</summary>
        public decimal NewFinalAmount { get; set; }
        /// <summary>Valideyndən əlavə alınmalı fərq.</summary>
        public decimal Difference { get; set; }
    }

    /// <summary>
    /// Uşaq geri qayıtdıqdan sonra hesabların vəziyyəti (H1). Deaktivasiya nəticəsi ilə simmetrikdir:
    /// qayıdış ayı yenidən hesablanır, ondan ƏVVƏLKİ sıfırlanmış aylar "gəlmədiyi ay" kimi
    /// yekunlaşdırılır, SONRAKI sıfırlanmış aylar isə tam aya bərpa olunur.
    /// </summary>
    public class ReactivationResult
    {
        public int ChildId { get; set; }
        public string ChildFullName { get; set; } = string.Empty;
        /// <summary>Uşağın yenidən gəldiyi İLK gün (daxil).</summary>
        public DateTime ReturnDate { get; set; }
        /// <summary>
        /// Qayıdış ayının nəticəsi: sətir yaradıldı / yenidən hesablandı / toxunulmadı.
        /// Null yalnız qayıdış ayı üçün heç bir iş görülməyəndə olur.
        /// </summary>
        public ReturnMonthOutcome? ReturnMonth { get; set; }
        /// <summary>
        /// Qayıdışdan ƏVVƏLKİ, "uşaq bu ay həqiqətən gəlmədi" kimi yekunlaşdırılan aylar (A2).
        /// Bu sətirlər bir daha bərpa olunmur.
        /// </summary>
        public List<ConfirmedAbsenceMonth> ConfirmedMonths { get; set; } = new();
        /// <summary>Qayıdış ayından SONRAKI, sıfırlanmış vəziyyətdən tam aya qaytarılan aylar.</summary>
        public List<RestoredMonth> RestoredMonths { get; set; } = new();
    }

    /// <summary>Geri qayıtma ayının yekun vəziyyəti.</summary>
    public class ReturnMonthOutcome
    {
        public int PaymentId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        /// <summary>Əməliyyatdan SONRAKI yekun məbləğ.</summary>
        public decimal FinalAmount { get; set; }
        /// <summary>Sətirdəki real ödəniş (heç vaxt dəyişdirilmir).</summary>
        public decimal PaidAmount { get; set; }
        /// <summary>Hesablanan dövrün başlanğıc günü (daxil).</summary>
        public int PeriodStartDay { get; set; }
        /// <summary>Hesablanan dövrün son günü (daxil).</summary>
        public int PeriodEndDay { get; set; }
        /// <summary>Hesablanan gün sayı — dövr iki hissəyə bölünübsə cəmi göstərir.</summary>
        public int BilledDays { get; set; }
        /// <summary>Sətir bu əməliyyatla YARADILDI (əvvəllər mövcud deyildi).</summary>
        public bool Created { get; set; }
        /// <summary>Sətrin məbləğinə toxunulmadı — ştab əl ilə yoxlamalıdır.</summary>
        public bool NeedsManualReview { get; set; }
        /// <summary>Toxunulmama səbəbi (ştaba göstərilir). Yalnız NeedsManualReview olanda dolur.</summary>
        public string? Reason { get; set; }
    }

    /// <summary>Uşaq geri qayıdanda "həqiqətən gəlmədiyi ay" kimi yekunlaşdırılan sətir (A2).</summary>
    public class ConfirmedAbsenceMonth
    {
        public int PaymentId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }

    public class ChildResponse
    {
        public int Id { get; set; }
        public int? PersonId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string DivisionName { get; set; } = string.Empty;
        public string ScheduleType { get; set; } = string.Empty;
        public decimal MonthlyFee { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public int PaymentDay { get; set; }
        public DateTime? DeactivationDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ParentFullName { get; set; } = string.Empty;
        public string? SecondParentFullName { get; set; }
        public string ParentPhone { get; set; } = string.Empty;
        public string? SecondParentPhone { get; set; }
        public string? ParentEmail { get; set; }
        public DateTime RegistrationDate { get; set; }
        /// <summary>
        /// Yalnız redaktə cavabında dolur: çıxış tarixi dəyişdirildikdə hesabların yenidən
        /// qurulmasının nəticəsi (D4). Digər hallarda null qalır.
        /// </summary>
        public DeactivationRecalcResult? Recalculation { get; set; }
    }

    public class ChildDetailResponse : ChildResponse
    {
        public string? FaceIdToken { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int AttendanceDays { get; set; }
        public int AbsentDays { get; set; }
        public decimal TotalDebt { get; set; }
    }

    public class ChildFilterRequest
    {
        public int? GroupId { get; set; }
        public int? DivisionId { get; set; }
        public string? Status { get; set; }
        public string? ScheduleType { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 0;
    }
}
