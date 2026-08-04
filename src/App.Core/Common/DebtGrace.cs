namespace App.Core.Common
{
    /// <summary>
    /// Borc güzəşti (grace) — ödəniş gününə hələ vaxt varkən valideynə GECİKMƏ MESAJI göndərilmir.
    /// Qayda: uşağın PaymentDay + GraceDays gününə qədər CARİ ayın sətri hələ gecikmiş sayılmır.
    /// Keçmiş ayların ödənilməmiş sətri həmişə gecikmişdir — onlara güzəşt yoxdur.
    ///
    /// C1 (ştabın qərarı): güzəşt ARTIQ BORCU GİZLƏTMİR. Borclu siyahısı (PaymentRepository.GetDebtorsAsync)
    /// və uşaq kartının TotalDebt-i (ChildService.GetChildByIdAsync) sətri yarandığı andan göstərir —
    /// əvvəl ayın əvvəlində "Borclular" tamamilə boş görünürdü və ştab sistemi sınıq sanırdı.
    /// Bu sinfin YEGANƏ istifadəçisi indi gündəlik WhatsApp işidir:
    /// NotificationService.SendPaymentOverdueRemindersAsync (sətir-sətir IsWithinGrace).
    ///
    /// Arifmetika DƏYİŞMƏYİB — sərhəd əvvəlki kimi sübut olunmuş haldadır.
    /// </summary>
    public static class DebtGrace
    {
        /// <summary>Ödəniş günündən sonra verilən əlavə gün sayı. Tək mənbə — heç yerdə təkrar yazılmır.</summary>
        public const int GraceDays = 4;

        /// <summary>
        /// Güzəştin SON günü — həm gizlətmə, həm də bildiriş bu YEGANƏ sərhəddən oxuyur (D-C).
        /// Sərhəd ayın uzunluğuna sıxılır: PaymentDay 27/28 üçün 31/32 heç bir təqvim günündə
        /// keçilmir, bu isə həmin uşaqların bildirişini əbədi susdurardı. Ayın son günü
        /// (DaysInMonth - 1 sərhədi sayəsində) həmişə gecikmə günüdür.
        /// İki funksiya ayrı arifmetika işlətdikdə valideyn borc göstərilməyən gündə xəbərdarlıq alırdı.
        /// </summary>
        public static int Deadline(DateTime asOf, int paymentDay) =>
            Math.Min(paymentDay + GraceDays, DateTime.DaysInMonth(asOf.Year, asOf.Month) - 1);

        /// <summary>
        /// Verilən ay/il sətri <paramref name="asOf"/> tarixinə görə hələ güzəşt içindədirsə true.
        /// Məs. PaymentDay=1 → ayın 1-5-i gizli, 6-dan görünür; PaymentDay=6 → 1-10 gizli, 11-dən görünür.
        /// Keçmiş ayın ödənilməmiş sətri heç vaxt güzəştdə deyil — həmişə borcdur.
        /// </summary>
        public static bool IsWithinGrace(DateTime asOf, int paymentDay, int paymentMonth, int paymentYear)
        {
            if (paymentYear != asOf.Year || paymentMonth != asOf.Month) return false;
            return asOf.Day <= Deadline(asOf, paymentDay);
        }

        /// <summary>
        /// Gecikmə bildirişi üçün: güzəşt pəncərəsi tamamilə bitibsə true.
        /// <see cref="IsWithinGrace"/> ilə eyni sərhəddin TAM əksidir — aralarında nə boşluq,
        /// nə də üst-üstə düşmə ola bilməz.
        /// QEYD: hazırda HEÇ BİR YERDƏN ÇAĞIRILMIR (bildiriş işi ay/il də yoxlamalı olduğu üçün
        /// <see cref="IsWithinGrace"/> işlədir). Sərhədin ikinci, oxunaqlı ifadəsi kimi saxlanılır —
        /// silinsə, gələcəkdə kimsə eyni arifmetikanı yenidən, çox güman fərqli yazacaq.
        /// </summary>
        public static bool IsOverdue(DateTime asOf, int paymentDay)
        {
            return asOf.Day > Deadline(asOf, paymentDay);
        }
    }
}
