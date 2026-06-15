using App.Business.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace App.API.Controllers
{
    // PluralizedRouteConvention bu controller-i "api/documents" route-una çevirir.
    // Auth-suzdur ki, WhatsApp/WABA serveri sənədi public linkdən çəkə bilsin.
    // Link təxmin edilə bilməyən HMAC token ilə qorunur (DocumentTokens).
    // PDF əvvəlcədən (NotificationService fon işində) hazırlanıb diskə yazılır;
    // burada yalnız sürətlə verilir (fetch anında çevrilmə yoxdur → timeout riski yoxdur).
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class DocumentController : ControllerBase
    {
        private readonly IConfiguration _config;

        public DocumentController(IConfiguration config)
        {
            _config = config;
        }

        // GET /api/documents/{childId}/{token}/contract.pdf   (kind = "contract")
        // GET /api/documents/{childId}/{token}/agreement.pdf  (kind = "agreement")
        [HttpGet("{childId:int}/{token}/{kind}.pdf")]
        public IActionResult Get(int childId, string token, string kind)
        {
            var secret = _config["Waba:Token"] ?? string.Empty;
            if (!DocumentTokens.Validate(childId, token, secret))
                return NotFound();

            if (kind != "contract" && kind != "agreement")
                return NotFound();

            var path = AgreementStorage.FilePath(childId, token, kind);
            if (!System.IO.File.Exists(path))
                return NotFound();

            var bytes = System.IO.File.ReadAllBytes(path);
            var name = (kind == "contract" ? "Kontrakt" : "Razilashma") + $"_{childId}.pdf";
            return File(bytes, "application/pdf", name);
        }
    }
}
