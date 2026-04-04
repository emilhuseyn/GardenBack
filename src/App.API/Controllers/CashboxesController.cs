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
    }
}
