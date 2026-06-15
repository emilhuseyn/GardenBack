using System.IO;

namespace App.Business.Helpers
{
    /// <summary>
    /// Hazırlanmış PDF müqavilələrin müvəqqəti saxlanc yeri. NotificationService PDF-ləri
    /// burada yaradır, DocumentController isə WhatsApp çəkəndə birbaşa diskdən verir
    /// (çevrilmə fetch anında deyil, əvvəlcədən fon işində baş verir → sürətli, timeout-suz).
    /// Hər ikisi eyni prosesdədir, ona görə eyni qovluğu görür.
    /// </summary>
    public static class AgreementStorage
    {
        public static string Dir => Path.Combine(Path.GetTempPath(), "garden_agreements");

        public static string FilePath(int childId, string token, string kind)
            => Path.Combine(Dir, $"{childId}_{token}_{kind}.pdf");
    }
}
