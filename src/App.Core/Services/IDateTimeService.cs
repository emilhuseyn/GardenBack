namespace App.Core.Services
{
    /// <summary>
    /// Bütün tarix/saat əməliyyatları üçün mərkəzi servis.
    /// Həmişə Bakı vaxtını (UTC+4) qaytarır.
    /// </summary>
    public interface IDateTimeService
    {
        /// <summary>Bakı vaxtı ilə cari an.</summary>
        DateTime Now { get; }

        /// <summary>Bakı vaxtı ilə bu günün tarixi (saat 00:00:00).</summary>
        DateTime Today { get; }
    }
}
