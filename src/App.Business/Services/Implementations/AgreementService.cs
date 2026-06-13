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
            // Şablon artıq "yaş" / "age group" əlavə etdiyi üçün sadəcə rəqəm diapazonunu götür
            var ageGroup    = NormalizeAgeGroup(child.Group.AgeCategory);
            var fee         = child.MonthlyFee.ToString("0.##");
            var parentNameEn = ToAscii(parentName);
            var childNameEn  = ToAscii(childName);
            // Unikal müqavilə nömrəsi: prefix + qeydiyyat ili + uşaq ID-si.
            // Razılaşma əsas Müqavilənin "1 saylı Əlavə"sidir, ona görə ikisi eyni nömrəni paylaşır.
            var contractNumber = $"KG-{year}/{child.Id}";

            using var input = new MemoryStream(templateBytes);
            using var doc   = new Document();
            doc.LoadFromStream(input, FileFormat.Doc);

            // ─────────────────────────────────────────────────────────────────────
            // 2026 şablonu — bütün Razılaşma mətni 2 sütunlu cədvəlin içindədir
            // (sol xana = AZ, sağ xana = EN). Yer-tutucular regex ilə YERİNDƏ
            // (doc.Replace) əvəz olunur ki, abzas daxili sətir keçidləri (manual line
            // break) və formatlaşma pozulmasın. FreeSpire Replace(Regex,string) $-qrup
            // referansını dəstəkləmir → əvəzləmələr sabit mətndir (anchor təkrar yazılır).
            // ─────────────────────────────────────────────────────────────────────

            // ── AZ ──────────────────────────────────────────────────────────────
            // Yuxarı tarix:  «  »_______ 2026 il
            RegexReplace(doc, @"«\s*»_{3,}\s*202\d\s*il", $"«{day}» {monthAz} {year} il");
            // Müqavilə №:  "Tarixli _______ N°- li Müqaviləyə 1 saylı Əlavə"
            RegexReplace(doc, @"Tarixli\s+_{3,}\s*N°", $"Tarixli {contractNumber} N°", RegexOptions.IgnoreCase);
            // Şəhər tarixi:  «          » 2026 - ci il
            RegexReplace(doc, @"«\s+»\s*202\d\s*-\s*ci\s+il", $"«{day}» {monthAz} {year} - ci il");
            // Aylıq haqq:  (_______) manat təşkil edir
            RegexReplace(doc, @"\(\s*_{3,}\s*\)\s*manat\s+təşkilı?\s*edir", $"({fee}) manat təşkil edir", RegexOptions.IgnoreCase);
            // Ödəniş tarixi:  "____"________202___ tarixinə qədər
            RegexReplace(doc, "[“\"]_{2,}[”\"][_\\s]*202_{1,4}\\s*tarixinə\\s+qədər",
                $"“{day}” {monthAz} {year} tarixinə qədər", RegexOptions.IgnoreCase);
            // Yaş qrupu:  ogluna( qızına)      yaş
            RegexReplace(doc, @"ogluna\(\s*qızına\)\s+yaş", $"ogluna( qızına) {ageGroup} yaş", RegexOptions.IgnoreCase);
            // Direktor (AZ): köhnə → yeni (imza hissəsi artıq A.M.Mahmudova-dır)
            RegexReplace(doc, @"Əliyeva Aytən Hafiz qızının", "Mahmudova Aysel Mehman qızının", RegexOptions.IgnoreCase);
            // Valideyn adı (AZ): «Valideyn»-dən sonrakı abzasa
            FillParentNameAz(doc, parentName);
            // Uşaq adı (AZ): "Arasında övladı"-dan sonrakı boş abzasa
            FillChildNameAz(doc, childName);

            // ── EN ──────────────────────────────────────────────────────────────
            // Direktor (EN): köhnə → yeni
            RegexReplace(doc, "its Director Aytan Hafiz Aliyeva", "its Director Aysel Mehman Mahmudova", RegexOptions.IgnoreCase);
            // "Dated: “” __________ 2026"
            RegexReplace(doc, "Dated:\\s*[“\"][”\"]\\s*_{3,}\\s*202\\d", $"Dated: “{day}” {monthEn} {year}", RegexOptions.IgnoreCase);
            // Appendix №+tarix:  "Agreement No. ______ dated “” __________ 2026"
            RegexReplace(doc, "Agreement No\\.\\s+_{3,}\\s+dated\\s+[“\"][”\"]\\s*_{3,}\\s*202\\d",
                $"Agreement No. {contractNumber} dated “{day}” {monthEn} {year}", RegexOptions.IgnoreCase);
            // Şəhər tarixi:  "___" __________ 2026
            RegexReplace(doc, "[“\"]_{2,}[”\"]\\s*_{3,}\\s*202\\d", $"“{day}” {monthEn} {year}", RegexOptions.IgnoreCase);
            // Yaş qrupu (EN):  (son/daughter) in the ______ age group
            RegexReplace(doc, @"\(son/daughter\)\s+in\s+the\s+_{2,}\s+age\s+group", $"(son/daughter) in the {ageGroup} age group", RegexOptions.IgnoreCase);
            // Aylıq haqq (EN):  is (___) AZN
            RegexReplace(doc, @"is\s+\(\s*_{3,}\s*\)\s+AZN", $"is ({fee}) AZN", RegexOptions.IgnoreCase);
            // Ödəniş tarixi (EN):  must be paid in full by “_” __________ 202__
            RegexReplace(doc, "must be paid in full by [“\"]_{1,}[”\"][_\\s]+202_{1,4}",
                $"must be paid in full by “{day}” {monthEn} {year}", RegexOptions.IgnoreCase);
            // Valideyn adı (EN): "(Full name of the parent...)" əvvəlinə
            FillNameBeforeAnchor(doc, "(Full name of the parent or legal representative)", parentNameEn, underline: true);
            // Uşaq adı (EN): "...to their child:" sonrakı boş abzasa
            FillNameAfterAnchor(doc, "preschool educational services to their child", childNameEn, underline: true);

            // ── İmza/rekvizit hissəsi: valideynin ad-soyadı ─────────────────────
            // İmza blokunda «Bağça» tərəfində direktorun adı (A.M.Mahmudova) var;
            // «Valideyn» tərəfində isə yalnız yer-tutucu vardı — faktiki adı yazırıq.
            // AZ: «Valideyn» altındakı "A.S.A." (Ad Soyad Ata adı) → faktiki ad
            RegexReplace(doc, @"A\.S\.A\.", parentName);
            // EN: "Parent" altındakı "Full Name" → faktiki ad. Case-sensitive ki,
            // mətndəki "(Full name of the parent...)" başlığına toxunmasın.
            RegexReplace(doc, "Full Name", parentNameEn);

            // ── EN — köhnə "COVENANT" remnant (Word render etmir) təhlükəsiz doldurulur ──
            RegexReplace(doc, "Aytan Aliyeva Hafiz", "Aysel Mehman Mahmudova", RegexOptions.IgnoreCase);
            RegexReplace(doc, @"_{5,}\s+in order to render", $"{parentNameEn} in order to render", RegexOptions.IgnoreCase);
            RegexReplace(doc, @"child,\s+_{5,}\s+in\s+the\s+age\s+group\s+of", $"child, {childNameEn}  in  the age group of", RegexOptions.IgnoreCase);
            RegexReplace(doc, @"age group of\s+_{2,}\s+and based", $"age group of {ageGroup} and based", RegexOptions.IgnoreCase);
            RegexReplace(doc, @"\(\s*_{3,}\s*\)\s+AZN\s+for\s+each\s+month", $"({fee}) AZN for each month", RegexOptions.IgnoreCase);

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
            // Unikal müqavilə nömrəsi (Razılaşma ilə eyni format)
            var contractNumber = $"KG-{year}/{child.Id}";

            var templateBytes = await File.ReadAllBytesAsync(templatePath);
            using var input = new MemoryStream(templateBytes);
            using var doc = new Document();
            doc.LoadFromStream(input, FileFormat.Doc);

            // ─────────────────────────────────────────────────────────────────────
            // 2026 şablonu — bütün müqavilə mətni 2 sütunlu cədvəlin içindədir
            // (sol xana = AZ, sağ xana = EN). Yer-tutucular regex ilə YERİNDƏ
            // (doc.Replace) əvəz olunur ki, paraqraf daxilindəki sətir keçidləri
            // (manual line break) və formatlaşma pozulmasın.
            // Qeyd: FreeSpire Replace(Regex,string) $-qrup referansını dəstəkləmir,
            // ona görə əvəzləmələr sabit mətndir (anchor sözü təkrar yazılır).
            // ─────────────────────────────────────────────────────────────────────

            // ── EN tarix ƏVVƏL doldurulur (AZ tarix regexi onu səhvən tutmasın deyə) ──
            // "Baku city  «____» ______ 2026"
            RegexReplace(doc, "Baku city\\s+[“\"]_{2,}[”\"][_\\s]+202\\d",
                $"Baku city “{day}” {monthEn} {year}", RegexOptions.IgnoreCase);

            // ── AZ ──────────────────────────────────────────────────────────────
            // Müqavilə №  →  "M Ü Q A V I L Ə  №  _______"
            RegexReplace(doc, @"№\s*_{3,}", $"№  {contractNumber}");
            // Tarix  →  "Bakı şəhəri  «____»_ ___  2026-ci il"
            // "-ci il" şərtdir ki, EN tarixini ("...2026" sonu fərqli) səhvən tutmasın.
            RegexReplace(doc, "[“\"]_{2,}[”\"][_\\s]+202\\d\\s*-\\s*ci\\s+il", $"“{day}” {monthAz} {year}-ci il");
            // Valideyn adı  →  "____ şəxsində valideyn və ya qanuni nümayəndə"
            RegexReplace(doc, @"_{5,}\s*şəxsində valideyn və ya qanuni nümayəndə",
                $"{parentName} şəxsində valideyn və ya qanuni nümayəndə");
            // Uşaq adı  →  "____ məktəbəqədər təlim-tərbiyə xidmətləri göstərir"
            RegexReplace(doc, @"_{5,}\s*məktəbəqədər təlim-tərbiyə xidmətləri göstərir",
                $"{childName} məktəbəqədər təlim-tərbiyə xidmətləri göstərir");

            // ── EN ──────────────────────────────────────────────────────────────
            // Agreement No.  →  "...SERVICES No. _______"  (köhnə "AGREEMENT NO. ___" da daxil)
            RegexReplace(doc, @"No\.\s+_{3,}", $"No. {contractNumber}", RegexOptions.IgnoreCase);
            // Valideyn adı  →  "____ acting as a parent or legal representative"
            RegexReplace(doc, @"_{5,}\s*acting as a parent or legal representative",
                $"{parentNameEn} acting as a parent or legal representative", RegexOptions.IgnoreCase);
            // Uşaq adı (EN) — "...child of the Parent (or a child under their guardianship):"
            // abzasından sonrakı boş abzasa yazılır.
            FillNameAfterAnchor(doc, "a child under their guardianship", childNameEn);

            // ── EN — köhnə şablondan qalan təkrar (gizli) paraqraflar varsa təhlükəsiz doldurulur ──
            // Köhnə tarix:  "Baku city  ___/____/  2026"
            RegexReplace(doc, @"Baku city\s+_{2,}\s*/\s*_{2,}\s*/\s*202\d",
                $"Baku city           {day}/{monthEn}/{year}", RegexOptions.IgnoreCase);
            // Köhnə valideyn:  "... ____ (hereinafter referred to as “Parent”)"
            RegexReplace(doc, "_{5,}\\s*\\(hereinafter referred to as [“\"]?Parent",
                $"{parentNameEn} (hereinafter referred to as “Parent", RegexOptions.IgnoreCase);
            // Köhnə uşaq:  "(or a child who is sponsored)____"
            RegexReplace(doc, @"\(or a child who is sponsored\)_{5,}",
                $"(or a child who is sponsored){childNameEn}", RegexOptions.IgnoreCase);

            using var output = new MemoryStream();
            doc.SaveToStream(output, FileFormat.Doc);

            var fileName = $"Kontrakt_{child.FirstName}_{child.LastName}_{childId}.doc";
            return (output.ToArray(), fileName);
        }

        /// <summary>
        /// Sənəddəki BÜTÜN paragraph-ları (səksiya body + cədvəl xanaları) gəzir və
        /// regex əsaslı text replace edir. Run boundary-ləri görmür, çünki paragraph.Text
        /// bütün run-ları birləşdirir. FindAllString-dən fərqli olaraq anchor-a güvənmir.
        /// </summary>
        private static int ReplaceInAllParagraphs(Document doc, string pattern, string replacement, RegexOptions options = RegexOptions.None)
        {
            int count = 0;
            var rx = new Regex(pattern, options);
            foreach (Section section in doc.Sections)
            {
                count += ReplaceInChildObjects(section.Body.ChildObjects, rx, replacement);
            }
            return count;
        }

        private static int ReplaceInChildObjects(dynamic objects, Regex rx, string replacement)
        {
            int count = 0;
            foreach (DocumentObject obj in objects)
            {
                if (obj is Paragraph p)
                {
                    var text = p.Text ?? string.Empty;
                    if (string.IsNullOrEmpty(text)) continue;
                    var updated = rx.Replace(text, replacement);
                    if (updated == text) continue;

                    p.ChildObjects.Clear();
                    p.AppendText(updated);
                    ApplyParagraphFont(p, "Times New Roman");
                    count++;
                }
                else if (obj is Table t)
                {
                    foreach (TableRow row in t.Rows)
                    {
                        foreach (TableCell cell in row.Cells)
                        {
                            count += ReplaceInChildObjects(cell.ChildObjects, rx, replacement);
                        }
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Anchor mətnini ehtiva edən hər bir abzasda regex əsaslı text replace edir.
        /// Run boundary-lərini görmür çünki paragraph.Text bütün run-ları birləşdirir.
        /// Yer-tutucu (underscore/space sequence) və xüsusi formatlı mətnlər üçün ideal.
        /// </summary>
        private static int ReplaceInParagraphRegex(Document doc, string anchor, string pattern, string replacement, RegexOptions options = RegexOptions.None)
        {
            var selections = doc.FindAllString(anchor, false, false);
            if (selections == null || selections.Length == 0) return 0;

            var processed = new HashSet<Paragraph>();
            int count = 0;
            foreach (TextSelection sel in selections)
            {
                var range = sel.GetAsOneRange();
                if (range.Owner is not Paragraph paragraph) continue;
                if (!processed.Add(paragraph)) continue;

                var text = paragraph.Text ?? string.Empty;
                var updated = Regex.Replace(text, pattern, replacement, options);
                if (updated == text) continue;

                paragraph.ChildObjects.Clear();
                paragraph.AppendText(updated);
                ApplyParagraphFont(paragraph, "Times New Roman");
                count++;
            }
            return count;
        }

        /// <summary>
        /// Anchor abzasından ƏVVƏL yeni abzas kimi value əlavə edir.
        /// İdempotentdir — əgər əvvəlki abzas artıq value-i ehtiva edirsə, atlayır.
        /// Yeni şablon yapısı ilə "(Full name of the parent...)" kimi xətdən əvvəl
        /// valideyn adını yazmaq üçün istifadə olunur.
        /// </summary>
        private static void FillNameBeforeAnchor(Document doc, string anchor, string value, bool underline = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var selections = doc.FindAllString(anchor, false, false);
            if (selections == null || selections.Length == 0) return;

            var range = selections[0].GetAsOneRange();
            var anchorPara = range.Owner as Paragraph;
            if (anchorPara == null) return;

            var parent = anchorPara.Owner;
            var idx = IndexInParent(parent, anchorPara);
            if (idx < 0) return;

            // İdempotentlik: əvvəlki abzas artıq value-i ehtiva edirsə skip
            if (idx > 0)
            {
                var prevObj = GetChildAt(parent, idx - 1);
                if (prevObj is Paragraph prevPara)
                {
                    var prevText = prevPara.Text?.Trim() ?? string.Empty;
                    if (prevText.Equals(value, StringComparison.OrdinalIgnoreCase) ||
                        prevText.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                        return;
                }
            }

            var newPara = new Paragraph(doc);
            newPara.AppendText(value);
            ApplyParagraphFont(newPara, "Times New Roman");
            if (underline) AddUnderline(newPara);
            InsertAt(parent, idx, newPara);
        }

        /// <summary>Abzasın altına nazik üfüqi xətt (forma kimi "doldurulmuş blank") əlavə edir.</summary>
        private static void AddUnderline(Paragraph p)
        {
            p.Format.Borders.Bottom.BorderType = Spire.Doc.Documents.BorderStyle.Single;
            p.Format.Borders.Bottom.LineWidth = 1.0f;
            p.Format.Borders.Bottom.Space = 1.0f;
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
                    // Ad forma kimi öz xəttinin üzərində otursun (altında üfüqi xətt).
                    var newPara = new Paragraph(doc);
                    newPara.AppendText(parentName);
                    ApplyParagraphFont(newPara, "Times New Roman");
                    AddUnderline(newPara);
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
                    // Boş abzas — uşaq adını yaz (forma kimi altında xətt)
                    nextPara.ChildObjects.Clear();
                    nextPara.AppendText(childName);
                    ApplyParagraphFont(nextPara, "Times New Roman");
                    AddUnderline(nextPara);
                }
                else if (!nextPara.Text.TrimStart().StartsWith("ogluna(", StringComparison.OrdinalIgnoreCase))
                {
                    // Hələ doldurulmayıb, boş abzas yox → yeni abzas əlavə et
                    var newPara = new Paragraph(doc);
                    newPara.AppendText(childName);
                    ApplyParagraphFont(newPara, "Times New Roman");
                    AddUnderline(newPara);
                    InsertAt(parent, idx + 1, newPara);
                }
            }
            else
            {
                // Sonrakı element paragraph deyil → insert et
                var newPara = new Paragraph(doc);
                newPara.AppendText(childName);
                ApplyParagraphFont(newPara, "Times New Roman");
                AddUnderline(newPara);
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

        /// <summary>
        /// Bütün sənəd boyu (body + cədvəl xanaları) regex uyğunluğunu YERİNDƏ əvəz edir.
        /// Yalnız uyğun gələn alt mətn dəyişir — ətrafdakı run-lar və paraqraf daxili sətir
        /// keçidləri (manual line break) qorunur. FreeSpire `Replace(Regex,string)` $-qrup
        /// referansını dəstəkləmədiyi üçün <paramref name="replacement"/> sabit mətn olmalıdır.
        /// </summary>
        private static void RegexReplace(Document doc, string pattern, string replacement, RegexOptions options = RegexOptions.None)
        {
            doc.Replace(new Regex(pattern, options), replacement);
        }

        /// <summary>
        /// Anchor abzasından SONRAKI abzası value ilə doldurur:
        /// boşdursa onu yazır, deyilsə yeni abzas əlavə edir. İdempotentdir.
        /// </summary>
        private static void FillNameAfterAnchor(Document doc, string anchor, string value, bool underline = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var selections = doc.FindAllString(anchor, false, false);
            if (selections == null || selections.Length == 0) return;

            var range = selections[0].GetAsOneRange();
            if (range.Owner is not Paragraph anchorPara) return;

            var parent = anchorPara.Owner;
            var idx = IndexInParent(parent, anchorPara);
            if (idx < 0) return;

            var nextObj = GetChildAt(parent, idx + 1);
            if (nextObj is Paragraph nextPara)
            {
                var nextText = nextPara.Text?.Trim() ?? string.Empty;
                if (nextText.Equals(value, StringComparison.OrdinalIgnoreCase)) return; // artıq yazılıb
                if (string.IsNullOrWhiteSpace(nextText))
                {
                    nextPara.ChildObjects.Clear();
                    nextPara.AppendText(value);
                    ApplyParagraphFont(nextPara, "Times New Roman");
                    if (underline) AddUnderline(nextPara);
                    return;
                }
            }

            var newPara = new Paragraph(doc);
            newPara.AppendText(value);
            ApplyParagraphFont(newPara, "Times New Roman");
            if (underline) AddUnderline(newPara);
            InsertAt(parent, idx + 1, newPara);
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

        /// <summary>
        /// "Label ______" formatında yalnız həmin label-dan sonrakı alt xətti value ilə doldurur.
        /// Digər sahələrə (I.D, Address və s.) toxunmur.
        /// </summary>
        private static void ReplaceFieldValue(Document doc, string label, string value, bool withColon = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            var selections = doc.FindAllString(label, false, false);
            if (selections == null || selections.Length == 0)
                return;

            var processed = new HashSet<Paragraph>();
            foreach (TextSelection selection in selections)
            {
                var range = selection.GetAsOneRange();
                if (range.Owner is not Paragraph paragraph) continue;
                if (!processed.Add(paragraph)) continue;

                var text = paragraph.Text ?? string.Empty;
                if (!text.Contains(label, StringComparison.OrdinalIgnoreCase)) continue;
                if (text.Contains(value, StringComparison.OrdinalIgnoreCase)) continue;

                var pattern = $@"({Regex.Escape(label)}\s*)(_+)";
                var replacementLabel = withColon ? $"{label}: " : "$1";
                var updated = Regex.Replace(text, pattern, withColon ? $"{replacementLabel}{value}" : $"$1{value}", RegexOptions.IgnoreCase);

                if (updated == text)
                {
                    // Bəzi şablonlarda alt xətlər arasında boşluqlar olur
                    pattern = $@"({Regex.Escape(label)}\s*)([_\s]{{3,}})";
                    updated = Regex.Replace(text, pattern, withColon ? $"{replacementLabel}{value}" : $"$1{value}", RegexOptions.IgnoreCase);
                }

                if (updated == text) continue;

                paragraph.ChildObjects.Clear();
                paragraph.AppendText(updated);
                ApplyParagraphFont(paragraph, "Times New Roman");
            }
        }

        /// <summary>
        /// Group.AgeCategory dəyərini şablon üçün təmizləyir.
        /// "5-6 yas" / "5-6 yaş" / "3-4 yaşlı" → "5-6" və ya "3-4"
        /// Şablonda artıq "yaş" və "age group" statik mətni olduğu üçün
        /// AgeCategory-ni rəqəm diapazonuna qədər kəsirik.
        /// </summary>
        private static string NormalizeAgeGroup(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var s = value.Trim();
            // Tail-də olan "yaşlı / yaş / yas / years / year / lik / lı" suffix-lərini sil
            string[] suffixes =
            {
                " yaşlı", " yaslı", " yaşlilar", " yaslilar",
                " yaş", " yas",
                " years old", " year old",
                " years", " year"
            };
            bool removed;
            do
            {
                removed = false;
                foreach (var suf in suffixes)
                {
                    if (s.EndsWith(suf, StringComparison.OrdinalIgnoreCase))
                    {
                        s = s[..^suf.Length].TrimEnd();
                        removed = true;
                    }
                }
            } while (removed);
            return s;
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
