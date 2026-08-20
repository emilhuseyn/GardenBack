using App.Business.DTOs.Children;
using App.Business.Services.Interfaces;
using App.Core.Common;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace App.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AllStaff")]
    public class ChildrenController : ControllerBase
    {
        private readonly IChildService _childService;
        private readonly IAgreementService _agreementService;

        public ChildrenController(IChildService childService, IAgreementService agreementService)
        {
            _childService = childService;
            _agreementService = agreementService;
        }

        // Administrator, Mühasib, Müəllim, Qəbul üzrə əməkdaş — hamısı görə bilər
        [HttpGet]
        public async Task<IActionResult> GetAllChildren([FromQuery] ChildFilterRequest filter)
        {
            var result = await _childService.GetAllChildrenAsync(filter);
            return Ok(ApiResponse<PagedResponse<ChildResponse>>.SuccessResponse(result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChildById(int id)
        {
            var result = await _childService.GetChildByIdAsync(id);
            return Ok(ApiResponse<ChildDetailResponse>.SuccessResponse(result));
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchChildren([FromQuery] string term)
        {
            var result = await _childService.SearchChildrenAsync(term);
            return Ok(ApiResponse<IEnumerable<ChildResponse>>.SuccessResponse(result));
        }

        // Administrator + Qəbul üzrə əməkdaş — əlavə, redaktə, status dəyişikliyi
        [HttpPost]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> CreateChild([FromBody] CreateChildRequest dto)
        {
            var result = await _childService.CreateChildAsync(dto);

            // Uşaq yaradılanda valideynə Kontrakt + Razılaşma sənədlərini WhatsApp-a göndər
            // (fon işi — cavabı gözlətmir, xəta uşağın yaradılmasını pozmur).
            BackgroundJob.Enqueue<INotificationService>(s => s.SendChildAgreementsAsync(result.Id));

            return CreatedAtAction(nameof(GetChildById), new { id = result.Id },
                ApiResponse<ChildResponse>.SuccessResponse(result, "Uşaq uğurla yaradıldı.", 201));
        }

        // Test / əl ilə: bir uşağın sənədlərini istənilən nömrəyə göndər
        // POST /api/childrens/{id}/send-agreements?phone=994503954614
        [HttpPost("{id}/send-agreements")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> SendAgreements(int id, [FromQuery] string? phone,
            [FromServices] INotificationService notificationService)
        {
            var r = await notificationService.SendChildAgreementsToAsync(id, phone);
            return Ok(ApiResponse<object>.SuccessResponse(
                new { r.Sent, r.Failed, r.Errors },
                r.Failed == 0 ? "Sənədlər göndərildi." : "Bəzi sənədlər göndərilmədi."));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> UpdateChild(int id, [FromBody] UpdateChildRequest dto)
        {
            var result = await _childService.UpdateChildAsync(id, dto);
            return Ok(ApiResponse<ChildResponse>.SuccessResponse(result, "Uşaq məlumatları yeniləndi."));
        }

        // Gövdə boş ola bilər — bu halda uşaq BUGÜNDƏN geri qayıtmış sayılır.
        [HttpPatch("{id}/activate")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> ActivateChild(int id,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ActivateChildRequest? request = null)
        {
            var result = await _childService.ActivateChildAsync(id, request?.ReturnDate);
            return Ok(ApiResponse<ReactivationResult>.SuccessResponse(result, "Uşaq aktivləşdirildi."));
        }

        // Gövdə boş ola bilər — bu halda köhnə davranış: bu gün deaktiv edilir.
        [HttpPatch("{id}/deactivate")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> DeactivateChild(int id,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DeactivateChildRequest? request = null)
        {
            var result = await _childService.DeactivateChildAsync(id, request?.EffectiveDate);
            return Ok(ApiResponse<DeactivationRecalcResult>.SuccessResponse(result, "Uşaq deaktiv edildi."));
        }

        // Yalnız Administrator — silmə
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteChild(int id)
        {
            await _childService.DeleteChildAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("Uşaq silindi."));
        }

        // Gövdə köhnə kimi sadəcə ID massividir, ona görə qayıdış tarixi opsional query parametridir:
        // ?returnDate=2026-08-19
        [HttpPost("activate-bulk")]
        public async Task<IActionResult> ActivateChildren([FromBody] List<int> ids,
            [FromQuery] DateTime? returnDate = null)
        {
            var results = await _childService.ActivateChildrenAsync(ids, returnDate);
            return Ok(new { message = "Uşaqlar aktiv edildi.", results });
        }

        // Gövdə köhnə kimi sadəcə ID massividir (JSON array obyektə bind oluna bilmir),
        // ona görə çıxış tarixi opsional query parametridir: ?effectiveDate=2026-07-20
        [HttpPost("deactivate-bulk")]
        public async Task<IActionResult> DeactivateChildren([FromBody] List<int> ids,
            [FromQuery] DateTime? effectiveDate = null)
        {
            var results = await _childService.DeactivateChildrenAsync(ids, effectiveDate);
            return Ok(new { message = "Uşaqlar deaktiv edildi.", results });
        }

        [HttpPost("delete-bulk")]
        public async Task<IActionResult> DeleteChildren([FromBody] List<int> ids)
        {
            await _childService.DeleteChildrenAsync(ids);
            return Ok(new { message = "Uşaqlar silindi." });
        }

        [HttpGet("{id}/agreement")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> DownloadAgreement(int id)
        {
            var (fileBytes, fileName) = await _agreementService.GenerateAgreementAsync(id);
            return File(fileBytes, "application/msword", fileName);
        }

        [HttpGet("{id}/contract")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> DownloadContract(int id)
        {
            var (fileBytes, fileName) = await _agreementService.GenerateContractAsync(id);
            return File(fileBytes, "application/msword", fileName);
        }
    }
}
