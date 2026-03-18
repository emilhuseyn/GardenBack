using App.Business.DTOs.Auth;
using App.Business.Services.Interfaces;
using App.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UsersController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _authService.GetAllUsersAsync();
            return Ok(ApiResponse<IEnumerable<UserResponse>>.SuccessResponse(result));
        }

        [HttpGet("by-role/{role}")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> GetUsersByRole(string role)
        {
            var result = await _authService.GetUsersByRoleAsync(role);
            return Ok(ApiResponse<IEnumerable<UserResponse>>.SuccessResponse(result));
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var result = await _authService.GetUserByIdAsync(id);
            return Ok(ApiResponse<UserResponse>.SuccessResponse(result));
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequest dto)
        {
            var result = await _authService.RegisterAsync(dto);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(result, "İstifadəçi yaradıldı."));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest dto)
        {
            await _authService.UpdateUserAsync(id, dto);
            return Ok(ApiResponse<string>.SuccessResponse("İstifadəçi yeniləndi."));
        }

        [HttpPatch("{id}/roles")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateUserRole(string id, [FromBody] UpdateUserRequest dto)
        {
            await _authService.UpdateUserAsync(id, new UpdateUserRequest { Role = dto.Role });
            return Ok(ApiResponse<string>.SuccessResponse("İstifadəçi rolu yeniləndi."));
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _authService.DeleteUserAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("İstifadəçi deaktiv edildi."));
        }

        [HttpDelete("{id}/remove")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> RemoveUser(string id)
        {
            await _authService.RemoveUserAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("İstifadəçi bazadan silindi."));
        }
    }
}
