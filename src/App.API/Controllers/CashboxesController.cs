using App.Business.DTOs.Cashboxes;
using App.Business.Services.Interfaces;
using App.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOrAccountant")]
    public class CashboxesController : ControllerBase
    {
        private readonly ICashboxService _cashboxService;

        public CashboxesController(ICashboxService cashboxService)
        {
            _cashboxService = cashboxService;
        }

        [HttpGet]
        [Authorize(Policy = "PaymentView")]
        public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = false)
        {
            var result = await _cashboxService.GetAllCashboxesAsync(onlyActive);
            return Ok(ApiResponse<IEnumerable<CashboxResponse>>.SuccessResponse(result));
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "PaymentView")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _cashboxService.GetCashboxByIdAsync(id);
            return Ok(ApiResponse<CashboxResponse>.SuccessResponse(result));
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create([FromBody] CreateCashboxRequest dto)
        {
            var result = await _cashboxService.CreateCashboxAsync(dto);
            return Ok(ApiResponse<CashboxResponse>.SuccessResponse(result, "Kassa yaradıldı."));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCashboxRequest dto)
        {
            var result = await _cashboxService.UpdateCashboxAsync(id, dto);
            return Ok(ApiResponse<CashboxResponse>.SuccessResponse(result, "Kassa yeniləndi."));
        }

        [HttpPatch("{id}/deactivate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Deactivate(int id)
        {
            await _cashboxService.DeactivateCashboxAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("Kassa deaktiv edildi."));
        }

        // ── Aylıq qalıq məbləğ ──────────────────────────────────────────

        /// <summary>Kassa üçün ayin açılış qalığını əlavə et / yenilə.</summary>
        [HttpPut("{id}/balance")]
        [Authorize(Policy = "AdminOrAccountant")]
        public async Task<IActionResult> SetOpeningBalance(int id, [FromBody] SetOpeningBalanceRequest dto)
        {
            var result = await _cashboxService.SetOpeningBalanceAsync(id, dto);
            return Ok(ApiResponse<CashboxMonthlyBalanceResponse>.SuccessResponse(result, "Açılış qalığı saxlanıldı."));
        }

        /// <summary>Kassa üçün müəyyən ay/ilin balansını gətir.</summary>
        [HttpGet("{id}/balance")]
        [Authorize(Policy = "PaymentView")]
        public async Task<IActionResult> GetMonthlyBalance(int id, [FromQuery] int month, [FromQuery] int year)
        {
            var result = await _cashboxService.GetMonthlyBalanceAsync(id, month, year);
            return Ok(ApiResponse<CashboxMonthlyBalanceResponse>.SuccessResponse(result));
        }

        /// <summary>Kassanın bütün aylıq balans tarixçəsini gətir.</summary>
        [HttpGet("{id}/balance/history")]
        [Authorize(Policy = "PaymentView")]
        public async Task<IActionResult> GetBalanceHistory(int id)
        {
            var result = await _cashboxService.GetAllMonthlyBalancesAsync(id);
            return Ok(ApiResponse<IEnumerable<CashboxMonthlyBalanceResponse>>.SuccessResponse(result));
        }
    }
}
