using System;
using System.Security.Cryptography;
using System.Text;

namespace App.Business.Helpers
{
    /// <summary>
    /// Public sənəd linkləri üçün təxmin edilə bilməyən token yaradır/yoxlayır (HMAC-SHA256).
    /// Eyni secret ilə NotificationService link qurur, DocumentController isə yoxlayır —
    /// belə ki, link yalnız WhatsApp/WABA üçün işləyir, kənar şəxs başqa uşağın
    /// müqaviləsini yükləyə bilmir.
    /// </summary>
    public static class DocumentTokens
    {
        public static string Create(int childId, string secret)
        {
            using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? string.Empty));
            var hash = h.ComputeHash(Encoding.UTF8.GetBytes($"agreement:{childId}"));
            return Convert.ToHexString(hash).Substring(0, 32).ToLowerInvariant();
        }

        public static bool Validate(int childId, string token, string secret)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            var expected = Create(childId, secret);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(token.ToLowerInvariant()));
        }
    }
}
