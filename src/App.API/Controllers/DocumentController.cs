using App.Business.Helpers;
using App.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace App.API.Controllers
{
    // PluralizedRouteConvention bu controller-i "api/documents" route-una çevirir.
    // Auth-suzdur ki, WhatsApp/WABA serveri sənədi public linkdən çəkə bilsin.
    // Link təxmin edilə bilməyən HMAC token ilə qorunur (DocumentTokens).
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class DocumentController : ControllerBase
    {
        private readonly IAgreementService _agreement;
        private readonly IConfiguration _config;

        public DocumentController(IAgreementService agreement, IConfiguration config)
        {
            _agreement = agreement;
            _config = config;
        }

        // GET /api/documents/{childId}/{token}/contract.doc   (kind = "contract")
        // GET /api/documents/{childId}/{token}/agreement.doc  (kind = "agreement")
        [HttpGet("{childId:int}/{token}/{kind}.doc")]
        public async Task<IActionResult> Get(int childId, string token, string kind)
        {
            var secret = _config["Waba:Token"] ?? string.Empty;
            if (!DocumentTokens.Validate(childId, token, secret))
                return NotFound();

            (byte[] FileBytes, string FileName) doc;
            if (string.Equals(kind, "contract", StringComparison.OrdinalIgnoreCase))
                doc = await _agreement.GenerateContractAsync(childId);
            else if (string.Equals(kind, "agreement", StringComparison.OrdinalIgnoreCase))
                doc = await _agreement.GenerateAgreementAsync(childId);
            else
                return NotFound();

            return File(doc.FileBytes, "application/msword", doc.FileName);
        }
    }
}
