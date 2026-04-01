using App.Business.Services.Interfaces;
using App.Core.Exceptions.Commons;
using App.DAL.UnitOfWork;
using Microsoft.Extensions.Hosting;
using Spire.Doc;
using Spire.Doc.Documents;
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
            var agreementNo = childId.ToString("D3");
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
            ReplaceAll(doc, "Tarixli _________N°",        $"Tarixli {agreementNo} N°");
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
            ReplaceAll(doc, "AGREEMENT No. _________",      $"AGREEMENT No. {agreementNo}");
            ReplaceAll(doc, "dated «      » _________",     $"dated «{day}» {monthEn}");

            // Valideyn adı (EN): 35 alt xətt "in order to render..." əvvəlindən
            ReplaceAll(doc, "___________________________________ in order to render",
                $"{parentNameEn} in order to render");

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
