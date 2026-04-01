using App.Core.Entities.Identity;
using App.DAL.Presistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public AdminController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// bagca (1).xlsx faylındakı uşaq məlumatlarını DB-yə yükləyir.
        /// Mövcud qeydlər saxlanılır (dublikat yoxlanılır).
        /// Həmçinin hər qrupa aid tərbiyəçilər yaradılır.
        /// </summary>
        [HttpPost("seed-excel")]
        public async Task<IActionResult> SeedExcelData()
        {
            await ExcelDataSeeder.SeedAsync(_context, _userManager);
            return Ok(new { message = "Excel məlumatları uğurla yükləndi." });
        }
    }
}
