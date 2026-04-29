using App.Business.Services.Interfaces;
using App.Core.Common;
using App.Core.Entities;
using App.DAL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class SystemSettingsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public SystemSettingsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("messaging-status")]
        public async Task<IActionResult> GetMessagingStatus()
        {
            var setting = (await _unitOfWork.SystemSettings.FindAsync(s => s.Key == "MessagingEnabled")).FirstOrDefault();
            bool isEnabled = setting != null && setting.Value.ToLower() == "true";

            return Ok(ApiResponse<object>.SuccessResponse(new { isEnabled }));
        }

        [HttpPost("toggle-messaging")]
        public async Task<IActionResult> ToggleMessagingStatus([FromQuery] bool enable)
        {
            var setting = (await _unitOfWork.SystemSettings.FindAsync(s => s.Key == "MessagingEnabled")).FirstOrDefault();

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Key = "MessagingEnabled",
                    Value = enable.ToString().ToLower()
                };
                await _unitOfWork.SystemSettings.AddAsync(setting);
            }
            else
            {
                setting.Value = enable.ToString().ToLower();
                await _unitOfWork.SystemSettings.UpdateAsync(setting);
            }

            await _unitOfWork.SaveChangesAsync();

            var status = enable ? "aktiv" : "deaktiv";
            return Ok(ApiResponse<object>.SuccessResponse(new { isEnabled = enable }, $"Mesaj göndərmə statusu {status} edildi."));
        }
    }
}
