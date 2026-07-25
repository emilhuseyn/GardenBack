using App.Business.Helpers;
using App.Business.Services.Interfaces;
using App.Core.Entities;
using App.Core.Enums;
using App.Core.Services;
using App.DAL.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Text.Json;

namespace App.Business.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IDateTimeService _dt;
        private readonly IAgreementService _agreementService;
        private readonly string _wabaApiUrl;
        private readonly string _wabaToken;
        private readonly string _docTemplate;
        private readonly string _publicBaseUrl;
        private readonly string _sofficePath;

        // WABA template names
        private const string TplReminder  = "odenis_xatirlatma_2"; // {{1}} = ödəniş tarixi
        private const string TplPayment   = "odenis_olduqda_1";    // {{1}} = tarix, {{2}} = məbləğ, {{3}} = qalıq borc
        private const string TplOverdue   = "odenis_gecikdikde";   // parametr yoxdur

        public NotificationService(
            IUnitOfWork unitOfWork,
            ILogger<NotificationService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IDateTimeService dt,
            IAgreementService agreementService)
        {
            _unitOfWork  = unitOfWork;
            _logger      = logger;
            _httpClient  = httpClientFactory.CreateClient();
            _dt          = dt;
            _agreementService = agreementService;
            _wabaApiUrl  = configuration["Waba:ApiUrl"]  ?? "https://api.soft10.io/v1/waba/message/send";
            _wabaToken   = configuration["Waba:Token"]   ?? string.Empty;
            _docTemplate = configuration["Waba:DocumentTemplate"] ?? string.Empty;
            _publicBaseUrl = (configuration["App:PublicBaseUrl"] ?? "https://bagca.site").TrimEnd('/');
            _sofficePath = configuration["App:SofficePath"] ?? @"C:\Program Files\LibreOffice\program\soffice.exe";
        }

        private async Task<bool> IsMessagingEnabledAsync()
        {
            var setting = (await _unitOfWork.SystemSettings.FindAsync(s => s.Key == "MessagingEnabled")).FirstOrDefault();
            if (setting == null)
            {
                return false; // Default false (messaging is disabled)
            }
            return setting.Value.ToLower() == "true";
        }

        // ────────────────────────────────────────────────────────────────
        // 1. Ödəniş günündən 1 gün əvvəl saat 10:00 — xatirlatma_wp
        // ────────────────────────────────────────────────────────────────
        public async Task<SendResult> SendPaymentDueRemindersAsync()
        {
            if (!await IsMessagingEnabledAsync())
                return DisabledResult();

            var tomorrow = _dt.Now.Date.AddDays(1);
            var activeChildren = await _unitOfWork.Children.GetActiveChildrenAsync();
            var toRemind = activeChildren.Where(c => c.PaymentDay == tomorrow.Day).ToList();

            _logger.LogInformation("WABA xatırlatma: {Count} uşaq (gün: {Day})", toRemind.Count, tomorrow.Day);

            int sent = 0, failed = 0;
            var errors = new List<string>();

            foreach (var child in toRemind)
            {
                try
                {
                    var payment = (await _unitOfWork.Payments
                        .FindAsync(p => p.ChildId == child.Id && p.Month == tomorrow.Month && p.Year == tomorrow.Year))
                        .FirstOrDefault();

                    if (payment?.Status == PaymentStatus.Paid) continue;

                    // {{1}} = ödəniş tarixi  DD.MM.YYYY
                    var payDate = $"{tomorrow.Day:D2}.{tomorrow.Month:D2}.{tomorrow.Year}";
                    var error = await SendWabaAsync(child.ParentPhone, TplReminder, [payDate], child.Id);

                    if (error == null) sent++; else { failed++; errors.Add(error); }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Xatırlatma göndərilmədi (childId={Id})", child.Id);
                    failed++; errors.Add(ex.Message);
                }
            }

            _logger.LogInformation("WABA xatırlatma tamamlandı. Sent={S} Failed={F}", sent, failed);
            return new SendResult(sent, failed, errors);
        }

        // ────────────────────────────────────────────────────────────────
        // 2. Ödəniş qeydə alınanda — odenish_wp
        // ────────────────────────────────────────────────────────────────
        public async Task<SendResult> SendPaymentConfirmationAsync(int paymentId)
        {
            if (!await IsMessagingEnabledAsync())
                return DisabledResult();

            var payment = await _unitOfWork.Payments.GetByIdAsync(p => p.Id == paymentId, p => p.Child)
                          ?? new Payment();

            if (payment.Child == null || payment.Id == 0)
                return new SendResult(0, 1, ["Ödəniş tapılmadı."]);

            try
            {
                var child      = payment.Child;
                var payDate    = payment.PaymentDate.HasValue
                    ? $"{payment.PaymentDate.Value:dd.MM.yyyy}"
                    : $"{_dt.Now:dd.MM.yyyy}";
                var paidAmount    = payment.PaidAmount.ToString("F0");
                var remaining     = Math.Max(0, payment.FinalAmount - payment.PaidAmount).ToString("F0");

                // {{1}} = tarix, {{2}} = ödənilən məbləğ, {{3}} = qalıq borc
                var error = await SendWabaAsync(child.ParentPhone, TplPayment,
                    [payDate, paidAmount, remaining], child.Id);

                if (error == null)
                {
                    _logger.LogInformation("WABA ödəniş təsdiqi göndərildi → paymentId={Id}", paymentId);
                    return new SendResult(1, 0, []);
                }
                _logger.LogWarning("WABA ödəniş təsdiqi xəta → {Error}", error);
                return new SendResult(0, 1, [error]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendPaymentConfirmationAsync xəta → paymentId={Id}", paymentId);
                return new SendResult(0, 1, [ex.Message]);
            }
        }

        // ────────────────────────────────────────────────────────────────
        // 2b. Verilən tarixdə qeydə alınmış ödənişlərin təsdiqini TƏKRAR göndər.
        //     (məs: WABA token sıradan çıxdığı gün uğursuz qalan təsdiqləri əl ilə bərpa etmək üçün)
        // ────────────────────────────────────────────────────────────────
        public async Task<SendResult> ResendConfirmationsForDateAsync(DateTime date)
        {
            if (!await IsMessagingEnabledAsync())
                return DisabledResult();

            var dayStart = date.Date;
            var dayEnd   = dayStart.AddDays(1);

            // O gün faktiki ödəniş alınmış (PaidAmount>0) bütün ödənişlər
            var payments = (await _unitOfWork.Payments
                .FindAsync(p => p.PaymentDate.HasValue
                                && p.PaymentDate.Value >= dayStart
                                && p.PaymentDate.Value < dayEnd
                                && p.PaidAmount > 0))
                .ToList();

            _logger.LogInformation("WABA təkrar təsdiq: {Count} ödəniş ({Date:dd.MM.yyyy})", payments.Count, dayStart);

            int sent = 0, failed = 0;
            var errors = new List<string>();
            foreach (var p in payments)
            {
                var r = await SendPaymentConfirmationAsync(p.Id);
                sent += r.Sent; failed += r.Failed; errors.AddRange(r.Errors);
            }

            _logger.LogInformation("WABA təkrar təsdiq tamamlandı. Sent={S} Failed={F}", sent, failed);
            return new SendResult(sent, failed, errors);
        }

        // ────────────────────────────────────────────────────────────────
        // 3. Ödəniş günü keçmiş VƏ ödənilməmiş bütün uşaqlara — HƏR GÜN gecikme_wp
        //    Ödəniş edilənə qədər (və ya ay bitənə qədər) hər gün təkrarlanır.
        // ────────────────────────────────────────────────────────────────
        public async Task<SendResult> SendPaymentOverdueRemindersAsync()
        {
            if (!await IsMessagingEnabledAsync())
                return DisabledResult();

            var today = _dt.Now.Date;

            var activeChildren = await _unitOfWork.Children.GetActiveChildrenAsync();

            // Bu ayda ödəniş günü artıq KEÇMİŞ bütün uşaqlar
            // (məs: today=10, PaymentDay=4 → gecikib; PaymentDay=15 → hələ vaxtı çatmayıb)
            var overdueChildren = activeChildren
                .Where(c => c.PaymentDay < today.Day)
                .ToList();

            _logger.LogInformation("WABA gecikme: {Count} potensial gecikmiş uşaq yoxlanılır (Bu gün: {Day})",
                overdueChildren.Count, today.Day);

            int sent = 0, skipped = 0, failed = 0;
            var errors = new List<string>();

            foreach (var child in overdueChildren)
            {
                try
                {
                    var payment = (await _unitOfWork.Payments
                        .FindAsync(p => p.ChildId == child.Id && p.Month == today.Month && p.Year == today.Year))
                        .FirstOrDefault();

                    // Ödəniş edilibsə keç (ödənilənə qədər hər gün, ödənildikdə dayan)
                    if (payment?.Status == PaymentStatus.Paid) { skipped++; continue; }

                    // gecikme_wp — parametrsiz template, hər gün təkrar
                    var error = await SendWabaAsync(child.ParentPhone, TplOverdue, [], child.Id);

                    if (error == null) sent++; else { failed++; errors.Add(error); }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gecikme mesajı göndərilmədi (childId={Id})", child.Id);
                    failed++; errors.Add(ex.Message);
                }
            }

            _logger.LogInformation("WABA gecikme tamamlandı. Sent={S} Skipped={K} Failed={F}", sent, skipped, failed);
            return new SendResult(sent, failed, errors);
        }

        // ────────────────────────────────────────────────────────────────
        // Köhnə metodlar (saxlanılır, lazım olsa istifadə olunur)
        // ────────────────────────────────────────────────────────────────
        public async Task<SendResult> SendPaymentReminderAsync(int childId)
        {
            if (!await IsMessagingEnabledAsync())
                return DisabledResult();

            var child = await _unitOfWork.Children.GetByIdAsync(childId);
            if (child == null) return new SendResult(0, 0, []);

            var debts = (await _unitOfWork.Payments.GetPaymentsByChildAsync(childId))
                .Where(p => p.Status != PaymentStatus.Paid).ToList();

            if (!debts.Any()) return new SendResult(0, 0, []);

            // Ən yaxın ödəniş tarixini göndər
            var payDate = $"{_dt.Now.Day:D2}.{_dt.Now.Month:D2}.{_dt.Now.Year}";
            var error = await SendWabaAsync(child.ParentPhone, TplReminder, [payDate], childId);
            return error == null ? new SendResult(1, 0, []) : new SendResult(0, 1, [error]);
        }

        public async Task<SendResult> SendBulkRemindersToDebtorsAsync()
        {
            if (!await IsMessagingEnabledAsync())
                return DisabledResult();

            var debts   = await _unitOfWork.Payments.GetDebtorsAsync();
            var grouped = debts.GroupBy(p => p.ChildId).ToList();
            _logger.LogInformation("WABA toplu xatırlatma: {Count} borclu", grouped.Count);

            int sent = 0, failed = 0;
            var errors = new List<string>();
            foreach (var group in grouped)
            {
                var r = await SendPaymentReminderAsync(group.Key);
                sent += r.Sent; failed += r.Failed; errors.AddRange(r.Errors);
            }
            return new SendResult(sent, failed, errors);
        }

        // ────────────────────────────────────────────────────────────────
        // 4. Uşaq yaradılanda — Kontrakt + Razılaşma sənədlərini göndər
        // ────────────────────────────────────────────────────────────────
        public Task<SendResult> SendChildAgreementsAsync(int childId)
            => SendChildAgreementsToAsync(childId, null);

        public async Task<SendResult> SendChildAgreementsToAsync(int childId, string? phoneOverride)
        {
            if (!await IsMessagingEnabledAsync())
                return DisabledResult();

            if (string.IsNullOrWhiteSpace(_docTemplate))
            {
                _logger.LogWarning("WABA sənəd template-i təyin olunmayıb (Waba:DocumentTemplate).");
                return new SendResult(0, 1, ["Sənəd template-i konfiqurasiyada təyin olunmayıb (Waba:DocumentTemplate)."]);
            }

            var child = await _unitOfWork.Children.GetByIdAsync(childId);
            if (child == null)
                return new SendResult(0, 1, [$"{childId} ID-li uşaq tapılmadı."]);

            var phone = string.IsNullOrWhiteSpace(phoneOverride) ? child.ParentPhone : phoneOverride;
            if (string.IsNullOrWhiteSpace(phone))
                return new SendResult(0, 1, ["Valideyn telefon nömrəsi yoxdur."]);

            var token    = DocumentTokens.Create(childId, _wabaToken);
            var baseName = $"{child.FirstName}_{child.LastName}";

            // WhatsApp Word faylını çatdırmır → Word-də hazırlanan sənədləri əvvəlcədən
            // PDF-ə çevirib diskə yazırıq; WhatsApp çəkəndə DocumentController birbaşa
            // PDF-i verir (fetch anında çevrilmə yoxdur → timeout riski yoxdur).
            try
            {
                Directory.CreateDirectory(AgreementStorage.Dir);

                var contractDoc  = await _agreementService.GenerateContractAsync(childId);
                var agreementDoc = await _agreementService.GenerateAgreementAsync(childId);

                var contractPdf  = DocToPdfConverter.Convert(contractDoc.FileBytes,  _sofficePath);
                var agreementPdf = DocToPdfConverter.Convert(agreementDoc.FileBytes, _sofficePath);

                await File.WriteAllBytesAsync(AgreementStorage.FilePath(childId, token, "contract"),  contractPdf);
                await File.WriteAllBytesAsync(AgreementStorage.FilePath(childId, token, "agreement"), agreementPdf);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sənəd PDF hazırlanmadı (childId={Id})", childId);
                return new SendResult(0, 1, [$"PDF hazırlanmadı: {ex.Message}"]);
            }

            var contractUrl  = $"{_publicBaseUrl}/api/documents/{childId}/{token}/contract.pdf";
            var agreementUrl = $"{_publicBaseUrl}/api/documents/{childId}/{token}/agreement.pdf";

            int sent = 0, failed = 0;
            var errors = new List<string>();

            var e1 = await SendDocumentAsync(phone, _docTemplate, contractUrl,  $"Kontrakt_{baseName}.pdf",   childId);
            if (e1 == null) sent++; else { failed++; errors.Add(e1); }

            var e2 = await SendDocumentAsync(phone, _docTemplate, agreementUrl, $"Razilashma_{baseName}.pdf", childId);
            if (e2 == null) sent++; else { failed++; errors.Add(e2); }

            _logger.LogInformation("WABA sənədlər (PDF) → childId={Id} Sent={S} Failed={F}", childId, sent, failed);
            return new SendResult(sent, failed, errors);
        }

        // Sənəd başlıqlı template mesajı: header = document(link + filename), body yoxdur.
        private async Task<string?> SendDocumentAsync(string toPhone, string template, string link, string filename, int childId)
        {
            var phone = new string(toPhone.Where(char.IsDigit).ToArray());

            object payload = new
            {
                to = phone,
                type = "template",
                template = new
                {
                    name = template,
                    language = "az",
                    components = new object[]
                    {
                        new
                        {
                            type = "header",
                            parameters = new object[]
                            {
                                new { type = "document", document = new { link, filename } }
                            }
                        }
                    }
                }
            };

            var logMsg = $"[WABA] to={phone} type=template template={template} document={filename} link={link}";

            try
            {
                var body    = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, _wabaApiUrl);
                request.Headers.Add("Authorization", $"Bearer {_wabaToken}");
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var resBody  = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WABA sənəd göndərildi → {Phone} [{File}]", phone, filename);
                    await LogNotificationAsync(toPhone, logMsg, childId, isSuccessful: true);
                    return null;
                }

                _logger.LogError("WABA sənəd xəta: {Status} → {Body}", (int)response.StatusCode, resBody);
                await LogNotificationAsync(toPhone, logMsg, childId, isSuccessful: false, error: resBody);
                return $"{phone}: HTTP {(int)response.StatusCode} — {resBody}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WABA sənəd servisinə qoşulmaq olmadı → {Phone}", phone);
                await LogNotificationAsync(toPhone, logMsg, childId, isSuccessful: false, error: ex.Message);
                return $"{phone}: {ex.Message}";
            }
        }

        // ────────────────────────────────────────────────────────────────
        // WABA API çağırışı
        // ────────────────────────────────────────────────────────────────
        private async Task<string?> SendWabaAsync(string toPhone, string template, string[] parameters, int childId)
        {
            var phone = new string(toPhone.Where(char.IsDigit).ToArray());

            object payload;
            if (parameters.Length > 0)
            {
                payload = new
                {
                    to = phone,
                    type = "template",
                    template = new
                    {
                        name = template,
                        language = "az",
                        components = new[]
                        {
                            new
                            {
                                type = "body",
                                parameters = parameters.Select(p => new { type = "text", text = p }).ToArray()
                            }
                        }
                    }
                };
            }
            else
            {
                payload = new
                {
                    to = phone,
                    type = "template",
                    template = new
                    {
                        name = template,
                        language = "az"
                    }
                };
            }

            var logMsg = $"[WABA] to={phone} type=template template={template} params=[{string.Join(",", parameters)}]";

            try
            {
                var body    = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, _wabaApiUrl);
                request.Headers.Add("Authorization", $"Bearer {_wabaToken}");
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var resBody  = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WABA göndərildi → {Phone} [{Template}]", phone, template);
                    await LogNotificationAsync(toPhone, logMsg, childId, isSuccessful: true);
                    return null;
                }

                var errMsg = $"{phone}: HTTP {(int)response.StatusCode} — {resBody}";
                _logger.LogError("WABA xəta: {Status} → {Body}", (int)response.StatusCode, resBody);
                await LogNotificationAsync(toPhone, logMsg, childId, isSuccessful: false, error: resBody);
                return errMsg;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WABA servisinə qoşulmaq olmadı → {Phone}", phone);
                await LogNotificationAsync(toPhone, logMsg, childId, isSuccessful: false, error: ex.Message);
                return $"{phone}: {ex.Message}";
            }
        }

        private async Task LogNotificationAsync(
            string phone, string message, int childId, bool isSuccessful, string? error = null)
        {
            var log = new SMSNotification
            {
                RecipientPhone = phone,
                Message        = isSuccessful ? message : $"[XETA: {error}] {message}",
                SentAt         = _dt.Now,
                IsSuccessful   = isSuccessful,
                ChildId        = childId
            };
            await _unitOfWork.Context.SMSNotifications.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }

        private SendResult DisabledResult()
        {
            _logger.LogInformation("Mesaj göndərmə müvəqqəti deaktivdir.");
            return new SendResult(0, 1, ["Mesaj göndərmə müvəqqəti deaktiv edilib."]);
        }
    }
}
