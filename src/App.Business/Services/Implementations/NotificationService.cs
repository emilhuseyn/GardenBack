using App.Business.Services.Interfaces;
using App.Core.Entities;
using App.Core.Enums;
using App.DAL.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace App.Business.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _whatsAppServiceUrl;

        public NotificationService(
            IUnitOfWork unitOfWork,
            ILogger<NotificationService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _unitOfWork         = unitOfWork;
            _logger             = logger;
            _httpClient         = httpClientFactory.CreateClient("WhatsApp");
            _whatsAppServiceUrl = configuration["WhatsApp:ServiceUrl"] ?? "http://localhost:3001";
        }

        /// <summary>Bir uşağın borcunu yoxlayır, gecikibsə WhatsApp mesajı göndərir.</summary>
        public async Task<SendResult> SendPaymentReminderAsync(int childId)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(childId);
            if (child == null) return new SendResult(0, 0, []);

            var debts = (await _unitOfWork.Payments.GetPaymentsByChildAsync(childId))
                .Where(p => p.Status != PaymentStatus.Paid).ToList();

            if (!debts.Any()) return new SendResult(0, 0, []);

            var totalDebt = debts.Sum(p => p.FinalAmount - p.PaidAmount);
            var monthDetails = debts.Select(d => (d.Month, d.Year, d.FinalAmount - d.PaidAmount)).ToList();
            var message = BuildDebtMessage(
                child.ParentFullName,
                $"{child.FirstName} {child.LastName}",
                totalDebt,
                monthDetails);

            var error = await SendWhatsAppAsync(child.ParentPhone, message, childId);
            return error == null ? new SendResult(1, 0, []) : new SendResult(0, 1, [error]);
        }

        /// <summary>Bütün borclu valideynlərə toplu xatırlatma göndərir.</summary>
        public async Task<SendResult> SendBulkRemindersToDebtorsAsync()
        {
            var debts   = await _unitOfWork.Payments.GetDebtorsAsync();
            var grouped = debts.GroupBy(p => p.ChildId).ToList();

            _logger.LogInformation("WhatsApp: {Count} borcluya mesaj gonderilir...", grouped.Count);

            int sent = 0, failed = 0;
            var errors = new List<string>();
            foreach (var group in grouped)
            {
                var r = await SendPaymentReminderAsync(group.Key);
                sent   += r.Sent;
                failed += r.Failed;
                errors.AddRange(r.Errors);
            }

            _logger.LogInformation("WhatsApp: Toplu gondermə tamamlandi. Sent={S} Failed={F}", sent, failed);
            return new SendResult(sent, failed, errors);
        }

        /// <summary>Yarın ödəniş günü olan uşaqların valideynlərinə xatırlatma göndərir.</summary>
        public async Task<SendResult> SendPaymentDueRemindersAsync()
        {
            var nowBaku = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetBakuTimeZone());
            var tomorrow = nowBaku.Date.AddDays(1);

            var activeChildren = await _unitOfWork.Children.GetActiveChildrenAsync();
            var childrenToRemind = activeChildren
                .Where(c => c.PaymentDay == tomorrow.Day)
                .ToList();

            _logger.LogInformation("Yarın ödəniş xatırlatması: {Count} uşaq (gün: {Day})", childrenToRemind.Count, tomorrow.Day);

            int sent = 0, failed = 0;
            var errors = new List<string>();

            foreach (var child in childrenToRemind)
            {
                try
                {
                    var payment = (await _unitOfWork.Payments
                        .FindAsync(p => p.ChildId == child.Id && p.Month == tomorrow.Month && p.Year == tomorrow.Year))
                        .FirstOrDefault();

                    if (payment?.Status == PaymentStatus.Paid)
                        continue;

                    var amount = payment == null
                        ? child.MonthlyFee
                        : Math.Max(0, payment.FinalAmount - payment.PaidAmount);

                    var message = BuildPaymentDueReminderMessage(
                        child.ParentFullName,
                        $"{child.FirstName} {child.LastName}",
                        amount,
                        tomorrow.Month,
                        tomorrow.Year);

                    var error = await SendWhatsAppAsync(child.ParentPhone, message, child.Id);
                    if (error == null)
                        sent++;
                    else
                    {
                        failed++;
                        errors.Add(error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ödəniş xatırlatması göndərilməsi uğursuz");
                    failed++;
                    errors.Add(ex.Message);
                }
            }

            _logger.LogInformation("Yarın ödəniş xatırlatması tamamlandi. Sent={S} Failed={F}", sent, failed);
            return new SendResult(sent, failed, errors);
        }

        /// <summary>Ödəniş edilərkən valideynə təsdiqləmə mesajı göndərir.</summary>
        public async Task<SendResult> SendPaymentConfirmationAsync(int paymentId)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(
                p => p.Id == paymentId,
                p => p.Child)
                ?? new Payment();

            if (payment.Child == null || payment.Id == 0)
                return new SendResult(0, 1, ["Ödəniş əylentisi tapılmadı."]);

            try
            {
                var child = payment.Child;
                var message = BuildPaymentConfirmationMessage(
                    child.ParentFullName,
                    $"{child.FirstName} {child.LastName}",
                    payment.PaidAmount,
                    payment.Month,
                    payment.Year);

                var error = await SendWhatsAppAsync(child.ParentPhone, message, child.Id);
                
                if (error == null)
                {
                    _logger.LogInformation("Ödəniş təsdiqləməsi göndərildi → {PaymentId} (Uşaq: {ChildId})", paymentId, child.Id);
                    return new SendResult(1, 0, []);
                }
                
                _logger.LogWarning("Ödəniş təsdiqləməsi göndərilməsi xətası → {PaymentId}: {Error}", paymentId, error);
                return new SendResult(0, 1, [error]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödəniş təsdiqləməsi prosesi xətaya düşdü → {PaymentId}", paymentId);
                return new SendResult(0, 1, [ex.Message]);
            }
        }

        // ──────────────────────────────────────────
        // Private helpers
        // ──────────────────────────────────────────

        // Returns null on success, error string on failure
        private async Task<string?> SendWhatsAppAsync(string toPhone, string message, int childId)
        {
            try
            {
                var body    = JsonSerializer.Serialize(new { phone = toPhone, message });
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_whatsAppServiceUrl}/send", content);
                var resBody  = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WhatsApp gondərildi → {Phone}", toPhone);
                    await LogNotificationAsync(toPhone, message, childId, isSuccessful: true);
                    return null;
                }

                var errMsg = $"{toPhone}: {resBody}";
                _logger.LogError("WhatsApp xeta: {Status} → {Body}", (int)response.StatusCode, resBody);
                await LogNotificationAsync(toPhone, message, childId, isSuccessful: false, error: resBody);
                return errMsg;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WhatsApp servisine qoşulmaq olmadi → {Phone}", toPhone);
                await LogNotificationAsync(toPhone, message, childId, isSuccessful: false, error: ex.Message);
                return $"{toPhone}: {ex.Message}";
            }
        }

        private async Task LogNotificationAsync(
            string phone, string message, int childId, bool isSuccessful, string? error = null)
        {
            var log = new SMSNotification
            {
                RecipientPhone = phone,
                Message        = isSuccessful ? message : $"[XETA: {error}] {message}",
                SentAt         = DateTime.UtcNow,
                IsSuccessful   = isSuccessful,
                ChildId        = childId
            };

            await _unitOfWork.Context.SMSNotifications.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }

        private static string BuildDebtMessage(
            string parentName, string childName, decimal debt, List<(int Month, int Year, decimal Amount)> monthDetails)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🌸 *Hörmətli {parentName}!*\n");
            sb.AppendLine($"Uşaq bağçamıza göstərdiyiniz etimada görə təşəkkür edirik.\n");
            sb.AppendLine($"Sizə xatırlatmaq istəyirik ki, *{childName}* adlı övladınızın aşağıdakı aylara aid ödənişləri hələ tamamlanmayıb:\n");
            sb.AppendLine($"📅 *Aylar və məbləğlər:*\n" + string.Join("\n", monthDetails.Select(x => $"- {MonthName(x.Month)} {x.Year}: {x.Amount:F2} AZN")));
            sb.AppendLine($"💰 *Ümumi borc:* {debt:F2} AZN\n");
            sb.AppendLine($"Zəhmət olmasa ödənişi ən qısa müddətdə həyata keçirməyinizi xahiş edirik.\n");
            sb.AppendLine($"Əlavə suallarınız olarsa, bağça rəhbərliyi ilə əlaqə saxlaya bilərsiniz.\n");
            sb.AppendLine($"Hörmətlə,\n*Uşaq Bağçası Administrasiyası* 🌸");
            return sb.ToString();
        }

        private static string BuildPaymentDueReminderMessage(
            string parentName, string childName, decimal amount, int month, int year)
            => $"🌸 *Hörmətli {parentName}!*\n\n" +
               $"Uşaq bağçamıza göstərdiyiniz etimada görə təşəkkür edirik.\n\n" +
               $"Sizə xatırlatmaq istərdik ki, sabah *{childName}* adlı övladınızın *{MonthName(month)} {year}* ayına aid ödəniş günüdür.\n\n" +
               $"💰 *Ödəniş məbləği:* {amount:F2} AZN\n\n" +
               $"Zəhmət olmasa ödənişi vaxtında etməyinizi xahiş edirik.\n\n" +
               $"Hörmətlə,\n*Uşaq Bağçası Administrasiyası* 🌸";

        private static string BuildPaymentConfirmationMessage(
            string parentName, string childName, decimal paidAmount, int month, int year)
            => $"🌸 *Hörmətli {parentName}!*\n\n" +
               $"Ödənişinizin qeydə alındığını bildiririk.\n\n" +
               $"*{childName}* adlı övladınızın *{MonthName(month)} {year}* ayına aid ödənişi uğurla qeydə alınmışdır.\n\n" +
               $"💰 *Ödədiyi məbləğ:* {paidAmount:F2} AZN\n\n" +
               $"Bağçamıza göstərdiyiniz etimada görə təşəkkür edirik! 💝\n\n" +
               $"Hörmətlə,\n*Uşaq Bağçası Administrasiyası* 🌸";

        private static string MonthName(int month) => month switch
        {
            1  => "Yanvar",
            2  => "Fevral",
            3  => "Mart",
            4  => "Aprel",
            5  => "May",
            6  => "İyun",
            7  => "İyul",
            8  => "Avqust",
            9  => "Sentyabr",
            10 => "Oktyabr",
            11 => "Noyabr",
            12 => "Dekabr",
            _  => month.ToString()
        };

        private static DateTime BuildDueDate(Payment payment)
        {
            var day = payment.Child?.PaymentDay ?? 1;
            var maxDay = DateTime.DaysInMonth(payment.Year, payment.Month);
            return new DateTime(payment.Year, payment.Month, Math.Min(day, maxDay));
        }

        private static TimeZoneInfo GetBakuTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Azerbaijan Standard Time"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Baku"); }
                catch { return TimeZoneInfo.Utc; }
            }
        }
    }
}
