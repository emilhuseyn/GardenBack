using App.Business.DTOs.Schedule;
using App.Business.Services.Interfaces;
using App.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace App.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AllStaff")]
    public class ScheduleController : ControllerBase
    {
        private readonly IScheduleConfigService _scheduleService;

        public ScheduleController(IScheduleConfigService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllConfigs([FromQuery] bool includeInactive = false)
        {
            var result = await _scheduleService.GetAllConfigsAsync(includeInactive);
            return Ok(ApiResponse<IEnumerable<ScheduleConfigResponse>>.SuccessResponse(result));
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleRequest dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _scheduleService.CreateScheduleAsync(dto, userId);
            return Ok(ApiResponse<ScheduleConfigResponse>.SuccessResponse(result, "Qrafik əlavə edildi."));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] UpdateScheduleRequest dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _scheduleService.UpdateScheduleAsync(id, dto, userId);
            return Ok(ApiResponse<ScheduleConfigResponse>.SuccessResponse(result, "Qrafik yeniləndi."));
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            await _scheduleService.DeleteScheduleAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("Qrafik silindi (və ya istifadədə olduğu üçün deaktiv edildi)."));
        }
    }
}
