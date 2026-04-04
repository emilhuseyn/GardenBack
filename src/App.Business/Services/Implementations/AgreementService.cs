using App.Business.Services.Interfaces;
using App.Core.Exceptions.Commons;
using App.DAL.UnitOfWork;
using Microsoft.Extensions.Hosting;
using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using System.Text.RegularExpressions;
using System.Text;

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

            var templatePath = Path.Combine(_env.ContentRootPath, "Templates", "Razilashma.doc");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("Razilaşma şablonu tapılmadı.", templatePath);

            var templateBytes = await File.ReadAllBytesAsync(templatePath);

            var date       = child.RegistrationDate.ToLocalTime();
            var day        = date.Day.ToString("D2");
            var monthAz    = AzMonths[date.Month - 1];
            var monthEn    = EnMonths[date.Month - 1];
            var year       = date.Year.ToString();
            var parentName  = child.ParentFullName;
            var childName   = $"{child.FirstName} {child.LastName}";
            var ageGroup    = child.Group.AgeCategory.Trim();
            var fee         = child.MonthlyFee.ToString("0.##");
            var parentNameEn = ToAscii(parentName);
            var childNameEn  = ToAscii(childName);

            using var input = new MemoryStream(templateBytes);
            using var doc   = new Document();
            doc.LoadFromStream(input, FileFormat.Doc);

            // ── Azərbaycanca hissə: sadə əvəzetmələr ───────────────────────
            ReplaceAll(doc, "«    »_________ 2026 il",    $"«{day}» {monthAz} {year} il");
            ReplaceAll(doc, "Tarixli _________N°",        "Tarixli _________N°");
            ReplaceAll(doc, "«           » 2026 - ci il", $"«{day}» {monthAz} {year} - ci il");
            ReplaceAll(doc, "(_______) manatı",           $"({fee}) manatı");

            // ── Valideyn adı (AZ): «Valideyn»-dən sonra gələn boş abzas ──
            // Şablonda blank paragraph yox; «Valideyn»-dan sonrakı abzas
            // "(Valideyninvə...)" dir. Onun əvvəlinə yeni abzas əlavə edirik.
            FillParentNameAz(doc, parentName);

            // ── Uşaq adı (AZ): "Arasında övladı"-dan sonra boş abzas ──────
            FillChildNameAz(doc, childName);

            // ── ogluna(...) yaş qrupu ────────────────────────────────────
            ReplaceAll(doc, "ogluna( qızına)          yaş", $"ogluna( qızına) {ageGroup} yaş");

            // ── İngilis hissəsi ───────────────────────────────────────────
            ReplaceAll(doc, "AGREEMENT No. _________",      "AGREEMENT No. _________");
            ReplaceAll(doc, "dated «      » _________",     $"dated «{day}» {monthEn}");
            ReplaceAll(doc, "APPENDIX No. 1 to the AGREEMENT No. dated ", $"APPENDIX No. 1 to the AGREEMENT No. dated {day}/{monthEn}/{year} ");
            FillDateBeforeYearInLine(doc, "Baku city", "/2026", $"{day}/{monthEn}");

            // Valideyn adı (EN): 35 alt xətt "in order to render..." əvvəlindən
            ReplaceAll(doc, "___________________________________ in order to render",
                $"{parentNameEn} in order to render");

            // Valideyn adı (EN): placeholder olan sətrlər
            ReplaceAll(doc, "_______________________", parentNameEn);
            FillLineBeforeAnchor(doc, "in order to render", parentNameEn);
            FillPreviousUnderscoreLine(doc, "in order to render", parentNameEn);
            TrimTextBeforeAnchor(doc, "in order to render", parentNameEn);

            // Uşaq adı (EN): 14 alt xətt + "  in  the age group of"
            ReplaceAll(doc, "______________  in  the age group of",
                $"{childNameEn}  in  the age group of");

            // Yaş qrupu (EN): "____" + "and based"  (ayrı abzaslarda ola bilər)
            ReplaceAll(doc, "____ and based", $"{ageGroup} and based");

            ReplaceAll(doc, "(_______) AZN", $"({fee}) AZN");

            using var output = new MemoryStream();
            doc.SaveToStream(output, FileFormat.Doc);

            var fileName = $"Razilashma_{child.FirstName}_{child.LastName}_{childId}.doc";
            return (output.ToArray(), fileName);
        }

        public async Task<(byte[] FileBytes, string FileName)> GenerateContractAsync(int childId)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(
                c => c.Id == childId,
                c => c.Group)
                ?? throw new EntityNotFoundException($"{childId} ID-li uşaq tapılmadı.");

            var templatePath = Path.Combine(_env.ContentRootPath, "Templates", "Kontrakt.doc");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("Kontrakt şablonu tapılmadı.", templatePath);

            var date = child.RegistrationDate.ToLocalTime();
            var day = date.Day.ToString("D2");
            var monthAz = AzMonths[date.Month - 1];
            var monthEn = EnMonths[date.Month - 1];
            var year = date.Year.ToString();
            var parentName = child.ParentFullName;
            var childName = $"{child.FirstName} {child.LastName}";
            var parentNameEn = ToAscii(parentName);
            var childNameEn = ToAscii(childName);

            var templateBytes = await File.ReadAllBytesAsync(templatePath);
            using var input = new MemoryStream(templateBytes);
            using var doc = new Document();
            doc.LoadFromStream(input, FileFormat.Doc);

            ReplaceAll(doc, "Bakı şəhəri                     “____”_ ___  2026-ci il", $"Bakı şəhəri                     “{day}” {monthAz}  {year}-ci il");
            ReplaceAll(doc, "Baku city           ___/____/  2026", $"Baku city           {day}/{monthEn}/2026");
            ReplaceAll(doc, "Baku city							/2026", $"Baku city							{day}/{monthEn}/2026");
            FillDateBeforeYearInLine(doc, "Baku city", "/2026", $"{day}/{monthEn}");

            ReplaceAll(doc, "<Valideynin soyad ad Ata adini yaz>", parentName);
            ReplaceAll(doc, "<Uşağın soyad adı>", childName);
            ReplaceAll(doc, "<Child full name>", childNameEn);

            ReplaceAll(doc,
                "__________________________________________________________ şəxsində valideyn və ya qanuni nümayəndə (bundan sonra “Valideyn”",
                $"{parentName} şəxsində valideyn və ya qanuni nümayəndə (bundan sonra “Valideyn”");

            ReplaceAll(doc,
                "__________________________________________________________ represented by parent or legal representative (hereinafter referred to as “Parent”",
                $"{parentNameEn} represented by parent or legal representative (hereinafter referred to as “Parent”");

            ReplaceAll(doc,
                "the parent or a legal representative ____________________________________ (hereinafter referred to as “Parent”)",
                $"the parent or a legal representative {parentNameEn} (hereinafter referred to as “Parent”)");

            ReplaceAll(doc,
                "________________________________________ məktəbəqədər təlim-tərbiyə xidmətləri göstərir, Valideyn isə göstərilən bu xidmətləri qəbul",
                $"{childName} məktəbəqədər təlim-tərbiyə xidmətləri göstərir, Valideyn isə göstərilən bu xidmətləri qəbul");

            ReplaceAll(doc,
                "state standards of preschool education for a child of a PARENT (or a child who is sponsored)_____________________________________ and the PARENT accepting",
                $"state standards of preschool education for a child of a PARENT (or a child who is sponsored){childNameEn} and the PARENT accepting");

            ReplaceAll(doc,
                "according to the state standards of preschool education for a child of a PARENT (or a child who is sponsored)_____________________________________ and the PARENT accepting these services, undertakes to pays to the KINDERGARTEN for",
                $"according to the state standards of preschool education for a child of a PARENT (or a child who is sponsored){childNameEn} and the PARENT accepting these services, undertakes to pays to the KINDERGARTEN for");

            FillParentNameAz(doc, parentName);
            FillChildNameAz(doc, childName);
            FillLineBeforeAnchor(doc, "şəxsində valideyn və ya qanuni nümayəndə", parentName);
            FillLineBeforeAnchor(doc, "represented by parent or legal representative", parentNameEn);
            FillLineBeforeAnchor(doc, "məktəbəqədər təlim-tərbiyə xidmətləri göstərir, Valideyn isə göstərilən bu xidmətləri qəbul", childName);
            FillLineBeforeAnchor(doc, "and the PARENT accepting", childNameEn);
            FillLineBeforeAnchor(doc, "and the PARENT accepting these services, undertakes to pays to the KINDERGARTEN for", childNameEn);
            FillPreviousUnderscoreLine(doc, "and the PARENT accepting these services, undertakes to pays to the KINDERGARTEN for", childNameEn);

            using var output = new MemoryStream();
            doc.SaveToStream(output, FileFormat.Doc);

            var fileName = $"Kontrakt_{child.FirstName}_{child.LastName}_{childId}.doc";
            return (output.ToArray(), fileName);
        }

        // ── Valideyn adını (AZ) "(Valideyninvə...)" abzasından əvvəl əlavə et ──
        private static void FillParentNameAz(Document doc, string parentName)
        {
            // "(Valideyninvə ya qanuni nümayəndənin S.A.A.)" olan abzası tap
            var selections = doc.FindAllString("(Valideyninvə ya qanuni", false, false);
            if (selections == null || selections.Length == 0)
                return;

            var range       = selections[0].GetAsOneRange();
            var anchorPara  = range.Owner as Paragraph;
            if (anchorPara == null) return;

            // Valideyn adının artıq yazılıb-yazılmadığını yoxla
            var parent = anchorPara.Owner; // TableCell və ya Body
            var idx    = IndexInParent(parent, anchorPara);
            if (idx <= 0) return;

            var prevObj = GetChildAt(parent, idx - 1);
            if (prevObj is Paragraph prevPara)
            {
                var prevText = prevPara.Text?.TrimEnd() ?? "";

                // Əvvəlki abzas «Valideyn» ilə bitir → valideyn adı hələ əlavə olunmayıb
                if (prevText.EndsWith("«Valideyn»"))
                {
                    var newPara = new Paragraph(doc);
                    newPara.AppendText(parentName);
                    InsertAt(parent, idx, newPara);
                }
                // Əvvəlki abzas artıq valideyn adıdır → skip
            }
        }

        private static void TrimTextBeforeAnchor(Document doc, string anchor, string value)
        {
            var selections = doc.FindAllString(anchor, false, false);
            if (selections == null || selections.Length == 0)
                return;

            foreach (var selection in selections)
            {
                var range = selection.GetAsOneRange();
                if (range.Owner is not Paragraph paragraph)
                    continue;

                var text = paragraph.Text ?? string.Empty;
                var idx = text.IndexOf(anchor, StringComparison.OrdinalIgnoreCase);
                if (idx <= 0)
                    continue;

                var updated = text[idx..].TrimStart();
                paragraph.ChildObjects.Clear();
                paragraph.AppendText(updated);
                ApplyParagraphFont(paragraph, "Times New Roman");
            }
        }

        // ── Uşaq adını (AZ) "Arasında övladı"-dan sonra boş abzasa yaz ──────
        private static void FillChildNameAz(Document doc, string childName)
        {
            var selections = doc.FindAllString("Arasında övladı", false, false);
            if (selections == null || selections.Length == 0)
                return;

            var range      = selections[0].GetAsOneRange();
            var anchorPara = range.Owner as Paragraph;
            if (anchorPara == null) return;

            var parent = anchorPara.Owner;
            var idx    = IndexInParent(parent, anchorPara);
            if (idx < 0) return;

            // Növbəti abzası tap
            var nextObj = GetChildAt(parent, idx + 1);
            if (nextObj is Paragraph nextPara)
            {
                if (string.IsNullOrWhiteSpace(nextPara.Text))
                {
                    // Boş abzas — uşaq adını yaz
                    nextPara.ChildObjects.Clear();
                    nextPara.AppendText(childName);
                }
                else if (!nextPara.Text.TrimStart().StartsWith("ogluna(", StringComparison.OrdinalIgnoreCase))
                {
                    // Hələ doldurulmayıb, boş abzas yox → yeni abzas əlavə et
                    var newPara = new Paragraph(doc);
                    newPara.AppendText(childName);
                    InsertAt(parent, idx + 1, newPara);
                }
            }
            else
            {
                // Sonrakı element paragraph deyil → insert et
                var newPara = new Paragraph(doc);
                newPara.AppendText(childName);
                InsertAt(parent, idx + 1, newPara);
            }
        }

        // ── Köməkçi metodlar ─────────────────────────────────────────────────

        private static void ReplaceAll(Document doc, string oldValue, string newValue)
        {
            // caseSensitive=false, wholeWord=false
            var selections = doc.FindAllString(oldValue, false, false);
            if (selections == null) return;
            foreach (TextSelection sel in selections)
            {
                var r = sel.GetAsOneRange();
                r.Text = newValue;
                r.CharacterFormat.FontName = "Times New Roman";
            }
        }

        /// <summary>Parent container-da (TableCell, Body, vs.) DocumentObject-in index-ini qaytarır.</summary>
        private static int IndexInParent(DocumentObject parent, DocumentObject child)
        {
            dynamic coll = ((dynamic)parent).ChildObjects;
            int count = coll.Count;
            for (int i = 0; i < count; i++)
                if (ReferenceEquals((object)coll[i], child)) return i;
            return -1;
        }

        private static DocumentObject? GetChildAt(DocumentObject parent, int index)
        {
            dynamic coll = ((dynamic)parent).ChildObjects;
            int count = coll.Count;
            return (index >= 0 && index < count) ? (DocumentObject)coll[index] : null;
        }

        private static void InsertAt(DocumentObject parent, int index, Paragraph para)
        {
            dynamic coll = ((dynamic)parent).ChildObjects;
            int count = coll.Count;
            if (index >= count)
                coll.Add(para);
            else
                coll.Insert(index, para);
        }

        private static void FillLineBeforeAnchor(Document doc, string anchor, string value)
        {
            var selections = doc.FindAllString(anchor, false, false);
            if (selections == null || selections.Length == 0)
                return;

            foreach (var selection in selections)
            {
                var range = selection.GetAsOneRange();
                if (range.Owner is not Paragraph paragraph)
                    continue;

                var text = paragraph.Text ?? string.Empty;
                if (!text.Contains("_") || text.Contains(value, StringComparison.OrdinalIgnoreCase))
                    continue;

                var start = text.IndexOf('_');
                var end = text.LastIndexOf('_');
                if (start < 0 || end < start)
                    continue;

                var updated = text[..start] + value + text[(end + 1)..];

                paragraph.ChildObjects.Clear();
                paragraph.AppendText(updated);
                ApplyParagraphFont(paragraph, "Times New Roman");
            }
        }

        private static void FillPreviousUnderscoreLine(Document doc, string anchor, string value)
        {
            var selections = doc.FindAllString(anchor, false, false);
            if (selections == null || selections.Length == 0)
                return;

            foreach (var selection in selections)
            {
                var range = selection.GetAsOneRange();
                if (range.Owner is not Paragraph paragraph)
                    continue;

                var parent = paragraph.Owner;
                var idx = IndexInParent(parent, paragraph);
                if (idx <= 0)
                    continue;

                var prevObj = GetChildAt(parent, idx - 1);
                if (prevObj is not Paragraph prevPara)
                    continue;

                var prevText = prevPara.Text ?? string.Empty;
                if (!prevText.Contains("_") || prevText.Contains(value, StringComparison.OrdinalIgnoreCase))
                    continue;

                var start = prevText.IndexOf('_');
                var end = prevText.LastIndexOf('_');
                if (start < 0 || end < start)
                    continue;

                var updated = prevText[..start] + value + prevText[(end + 1)..];

                prevPara.ChildObjects.Clear();
                prevPara.AppendText(updated);
                ApplyParagraphFont(prevPara, "Times New Roman");
            }
        }

        private static void ApplyParagraphFont(Paragraph paragraph, string fontName)
        {
            foreach (DocumentObject child in paragraph.ChildObjects)
            {
                if (child is TextRange tr)
                    tr.CharacterFormat.FontName = fontName;
            }
        }

        private static void FillDateBeforeYearInLine(Document doc, string startsWith, string yearPart, string dateValue)
        {
            foreach (Section section in doc.Sections)
            {
                foreach (DocumentObject obj in section.Body.ChildObjects)
                {
                    if (obj is not Paragraph paragraph)
                        continue;

                    var text = paragraph.Text ?? string.Empty;
                    if (!text.Contains(startsWith, StringComparison.OrdinalIgnoreCase) || !text.Contains(yearPart, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var updated = Regex.Replace(
                        text,
                        @"(Baku\s*city\s*)(?:[_\s\t]*)(/2026)",
                        m => $"{m.Groups[1].Value}{dateValue}{m.Groups[2].Value}",
                        RegexOptions.IgnoreCase);

                    if (updated == text)
                        continue;

                    paragraph.ChildObjects.Clear();
                    paragraph.AppendText(updated);
                    ApplyParagraphFont(paragraph, "Times New Roman");
                }
            }
        }

        private static string ToAscii(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            value = value
                .Replace('ə', 'e').Replace('Ə', 'E')
                .Replace('ş', 's').Replace('Ş', 'S')
                .Replace('ı', 'i').Replace('İ', 'I')
                .Replace('ğ', 'g').Replace('Ğ', 'G')
                .Replace('ö', 'o').Replace('Ö', 'O')
                .Replace('ü', 'u').Replace('Ü', 'U')
                .Replace('ç', 'c').Replace('Ç', 'C');
            var sb = new StringBuilder(value.Length);
            foreach (var c in value) if (c <= 127) sb.Append(c);
            return sb.ToString();
        }
    }
}
