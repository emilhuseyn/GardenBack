using System;
using System.Diagnostics;
using System.IO;

namespace App.Business.Helpers
{
    /// <summary>
    /// Word (.doc) → PDF çevrilməsi LibreOffice headless (soffice) ilə.
    /// WhatsApp Word faylını çatdırmadığı üçün sənədlər göndərilməzdən əvvəl PDF-ə çevrilir.
    /// LibreOffice server (Session 0 / headless) üçün təhlükəsizdir — Word COM-dan fərqli olaraq.
    /// </summary>
    public static class DocToPdfConverter
    {
        public static byte[] Convert(byte[] docBytes, string sofficePath)
        {
            if (docBytes == null || docBytes.Length == 0)
                throw new ArgumentException("Boş sənəd.", nameof(docBytes));
            if (string.IsNullOrWhiteSpace(sofficePath) || !File.Exists(sofficePath))
                throw new FileNotFoundException("LibreOffice (soffice.exe) tapılmadı. App:SofficePath yoxlayın.", sofficePath);

            var work = Path.Combine(Path.GetTempPath(), "garden_pdf", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(work);
            var profile = Path.Combine(work, "loprofile");
            var docPath = Path.Combine(work, "doc.doc");
            var pdfPath = Path.Combine(work, "doc.pdf");
            try
            {
                File.WriteAllBytes(docPath, docBytes);
                var profileUri = new Uri(profile).AbsoluteUri; // file:///C:/...

                var psi = new ProcessStartInfo
                {
                    FileName = sofficePath,
                    Arguments = $"--headless --norestore --nolockcheck -env:UserInstallation={profileUri} " +
                                $"--convert-to pdf --outdir \"{work}\" \"{docPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var p = Process.Start(psi)
                    ?? throw new InvalidOperationException("soffice prosesi başladıla bilmədi.");

                // Deadlock olmasın deyə stream-ləri asinxron oxu, sonra gözlə
                var outTask = p.StandardOutput.ReadToEndAsync();
                var errTask = p.StandardError.ReadToEndAsync();

                if (!p.WaitForExit(120000))
                {
                    try { p.Kill(true); } catch { }
                    throw new TimeoutException("LibreOffice çevrilməsi vaxtı keçdi (120 san).");
                }

                if (!File.Exists(pdfPath))
                    throw new InvalidOperationException(
                        $"PDF yaranmadı (exit={p.ExitCode}). out={outTask.Result} err={errTask.Result}");

                return File.ReadAllBytes(pdfPath);
            }
            finally
            {
                try { Directory.Delete(work, true); } catch { }
            }
        }
    }
}
