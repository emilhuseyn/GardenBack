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
    }
}
