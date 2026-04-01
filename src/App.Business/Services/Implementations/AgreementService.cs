using App.Business.Services.Interfaces;
using App.Core.Exceptions.Commons;
using App.DAL.UnitOfWork;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Hosting;

namespace App.Business.Services.Implementations
{
    public class AgreementService : IAgreementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHostEnvironment _env;

        private static readonly string[] AzMonths =
        [
            "yanvar", "fevral", "mart", "aprel", "may", "iyun",
            "iyul", "avqust", "sentyabr", "oktyabr", "noyabr", "dekabr"
        ];

        private static readonly string[] EnMonths =
        [
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        ];

        public AgreementService(IUnitOfWork unitOfWork, IHostEnvironment env)
        {
            _unitOfWork = unitOfWork;
            _env = env;
        }

        public async Task<(byte[] FileBytes, string FileName)> GenerateAgreementAsync(int childId)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(
                c => c.Id == childId,
                c => c.Group)
                ?? throw new EntityNotFoundException($"{childId} ID-li uşaq tapılmadı.");

            var templatePath = Path.Combine(_env.ContentRootPath, "Templates", "Razilashma_Template.docx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("Razilaşma şablonu tapılmadı.", templatePath);

            var templateBytes = await File.ReadAllBytesAsync(templatePath);

            var date = child.RegistrationDate.ToLocalTime();
            var replacements = new Dictionary<string, string>
            {
                ["{AGREEMENT_NO}"]   = childId.ToString("D3"),
                ["{SIGN_DAY}"]       = date.Day.ToString("D2"),
                ["{SIGN_MONTH_AZ}"]  = AzMonths[date.Month - 1],
                ["{SIGN_MONTH_EN}"]  = EnMonths[date.Month - 1],
                ["{PARENT_NAME}"]    = child.ParentFullName,
                ["{CHILD_NAME}"]     = $"{child.FirstName} {child.LastName}",
                ["{AGE_GROUP}"]      = child.Group.AgeCategory,
                ["{MONTHLY_FEE}"]    = child.MonthlyFee.ToString("0.##"),
            };

            using var ms = new MemoryStream();
            ms.Write(templateBytes, 0, templateBytes.Length);

            using (var doc = WordprocessingDocument.Open(ms, true))
            {
                var body = doc.MainDocumentPart!.Document.Body!;
                ReplaceInBody(body, replacements);
                doc.MainDocumentPart.Document.Save();
            }

            var fileName = $"Razilashma_{child.FirstName}_{child.LastName}_{childId}.docx";
            return (ms.ToArray(), fileName);
        }

        private static void ReplaceInBody(Body body, Dictionary<string, string> replacements)
        {
            // Collect all paragraphs (including those inside table cells)
            foreach (var para in body.Descendants<Paragraph>())
            {
                // Merge all run texts into a single string for this paragraph
                var runs = para.Descendants<Run>().ToList();
                if (runs.Count == 0) continue;

                var fullText = string.Concat(runs.Select(r => r.InnerText));
                var replaced = fullText;

                foreach (var (placeholder, value) in replacements)
                    replaced = replaced.Replace(placeholder, value);

                if (replaced == fullText) continue;

                // Put the replaced text into the first run and remove the rest
                var firstRun = runs[0];
                var runProps = firstRun.RunProperties?.CloneNode(true);

                firstRun.RemoveAllChildren<Text>();
                firstRun.AppendChild(new Text(replaced) { Space = SpaceProcessingModeValues.Preserve });
                if (runProps != null && firstRun.RunProperties == null)
                    firstRun.PrependChild(runProps);

                for (int i = 1; i < runs.Count; i++)
                    runs[i].Remove();
            }
        }
    }
}
