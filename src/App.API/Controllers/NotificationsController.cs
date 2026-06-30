using App.Business.Services.Interfaces;
using App.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly string _wabaApiUrl;
        private readonly string _wabaToken;

        public NotificationsController(
            INotificationService notificationService,
            IConfiguration configuration)
        {
            _notificationService = notificationService;
            _wabaApiUrl = configuration["Waba:ApiUrl"] ?? string.Empty;
            _wabaToken = configuration["Waba:Token"] ?? string.Empty;
        }

        /// <summary>Soft10 WABA konfiqurasiya vəziyyətini qaytarır.</summary>
        [HttpGet("waba/status")]
        [HttpGet("whatsapp/status")]
        public IActionResult GetWabaStatus()
        {
            var hasApiUrl = !string.IsNullOrWhiteSpace(_wabaApiUrl);
            var hasToken = !string.IsNullOrWhiteSpace(_wabaToken);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                provider = "soft10-waba",
                connected = hasApiUrl && hasToken,
                hasQR = false,
                running = true,
                message = hasApiUrl && hasToken
                    ? "Soft10 WABA konfiqurasiyası hazırdır."
                    : "Soft10 WABA konfiqurasiyası natamamdır (ApiUrl/Token)."
            }));
        }

        /// <summary>Sabah ödəniş günü olanlara mesaj göndərir.</summary>
        [HttpPost("send-due-alerts")]
        public async Task<IActionResult> SendDueAlerts()
        {
            var result = await _notificationService.SendPaymentDueRemindersAsync();
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                sent = result.Sent,
                failed = result.Failed,
                errors = result.Errors,
                message = $"{result.Sent} mesaj göndərildi, {result.Failed} uğursuz."
            }));
        }

        /// <summary>Bütün borclu valideynlərə WhatsApp xatırlatması göndərir.</summary>
        [HttpPost("send-reminders")]
        public async Task<IActionResult> SendBulkReminders()
        {
            var result = await _notificationService.SendBulkRemindersToDebtorsAsync();
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                sent   = result.Sent,
                failed = result.Failed,
                errors = result.Errors,
                message = $"{result.Sent} mesaj göndərildi, {result.Failed} uğursuz."
            }));
        }

        /// <summary>Konkret uşağın valideyninə xatırlatma göndərir.</summary>
        [HttpPost("send-reminder/{childId}")]
        public async Task<IActionResult> SendReminderToChild(int childId)
        {
            var result = await _notificationService.SendPaymentReminderAsync(childId);
            if (result.Failed > 0)
                return StatusCode(502, ApiResponse<object>.ErrorResponse(
                    $"Mesaj göndərilə bilmədi: {string.Join("; ", result.Errors)}"));

            return Ok(ApiResponse<string>.SuccessResponse("Xatırlatma göndərildi."));
        }

        /// <summary>Konkret ödəniş üçün təsdiq mesajını təkrar göndərir (uğursuz qalan ödənişlər üçün).</summary>
        [HttpPost("resend-confirmation/{paymentId:int}")]
        public async Task<IActionResult> ResendConfirmation(int paymentId)
        {
            var result = await _notificationService.SendPaymentConfirmationAsync(paymentId);
            if (result.Failed > 0)
                return StatusCode(502, ApiResponse<object>.ErrorResponse(
                    $"Göndərilə bilmədi: {string.Join("; ", result.Errors)}"));

            return Ok(ApiResponse<string>.SuccessResponse("Təsdiq mesajı göndərildi."));
        }

        /// <summary>
        /// Verilən tarixdə (default: bu gün) qeydə alınmış BÜTÜN ödənişlərin təsdiq mesajını təkrar göndərir.
        /// Məs: WABA sıradan çıxan gün uğursuz qalan təsdiqləri bir əmrlə bərpa etmək üçün.
        /// İstifadə: POST /api/notifications/resend-confirmations?date=2026-06-30
        /// </summary>
        [HttpPost("resend-confirmations")]
        public async Task<IActionResult> ResendConfirmations([FromQuery] DateTime? date)
        {
            var target = date ?? DateTime.Now;
            var result = await _notificationService.ResendConfirmationsForDateAsync(target);
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                sent   = result.Sent,
                failed = result.Failed,
                errors = result.Errors,
                message = $"{target:dd.MM.yyyy}: {result.Sent} təsdiq göndərildi, {result.Failed} uğursuz."
            }));
        }

        /// <summary>Ödəniş günü keçmiş, hələ ödəməyən valideynlərə gecikmə xəbərdarlığını əl ilə göndərir.</summary>
        [HttpPost("send-overdue-alerts")]
        public async Task<IActionResult> SendOverdueAlerts()
        {
            var result = await _notificationService.SendPaymentOverdueRemindersAsync();
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                sent   = result.Sent,
                failed = result.Failed,
                errors = result.Errors,
                message = $"{result.Sent} mesaj göndərildi, {result.Failed} uğursuz."
            }));
        }
    }
}
