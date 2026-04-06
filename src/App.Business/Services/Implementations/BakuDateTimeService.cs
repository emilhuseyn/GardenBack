using App.Core.Services;
using System.Runtime.InteropServices;

namespace App.Business.Services.Implementations
{
    /// <summary>
    /// IDateTimeService-in Bakı vaxtı (UTC+4) implementasiyası.
    /// OS-dən asılı olmayaraq həmişə doğru vaxtı qaytarır.
    /// </summary>
    public class BakuDateTimeService : IDateTimeService
    {
        // Windows: "Azerbaijan Standard Time", Linux/macOS: "Asia/Baku"
        private static readonly TimeZoneInfo BakuZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "Azerbaijan Standard Time"
                    : "Asia/Baku");

        public DateTime Now =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BakuZone);

        public DateTime Today => Now.Date;

        /// <summary>
        /// Hangfire RecurringJob üçün timezone nesnəsi.
        /// Program.cs-dən istifadə edilir.
        /// </summary>
        public static TimeZoneInfo Zone => BakuZone;
    }
}
