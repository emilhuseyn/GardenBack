using App.Business.DTOs.Groups;
using App.Business.Services.Interfaces;
using App.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using App.Core.Entities.Identity;

namespace App.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly UserManager<User> _userManager;

        public GroupsController(IGroupService groupService, UserManager<User> userManager)
        {
            _groupService = groupService;
            _userManager  = userManager;
        }

        [HttpGet]
        [Authorize(Policy = "AllStaff")]
        public async Task<IActionResult> GetAllGroups()
        {
            var result = await _groupService.GetAllGroupsAsync();
            return Ok(ApiResponse<IEnumerable<GroupResponse>>.SuccessResponse(result));
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AllStaff")]
        public async Task<IActionResult> GetGroupById(int id)
        {
            var result = await _groupService.GetGroupByIdAsync(id);
            return Ok(ApiResponse<GroupDetailResponse>.SuccessResponse(result));
        }

        [HttpGet("division/{divisionId}")]
        [Authorize(Policy = "AllStaff")]
        public async Task<IActionResult> GetGroupsByDivision(int divisionId)
        {
            var result = await _groupService.GetGroupsByDivisionAsync(divisionId);
            return Ok(ApiResponse<IEnumerable<GroupResponse>>.SuccessResponse(result));
        }

        [HttpGet("{id}/logs")]
        [Authorize(Policy = "AllStaff")]
        public async Task<IActionResult> GetGroupLogs(int id)
        {
            var result = await _groupService.GetGroupLogsAsync(id);
            return Ok(ApiResponse<IEnumerable<GroupLogResponse>>.SuccessResponse(result));
        }

        [HttpPost]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest dto)
        {
            var result = await _groupService.CreateGroupAsync(dto);
            return CreatedAtAction(nameof(GetGroupById), new { id = result.Id },
                ApiResponse<GroupResponse>.SuccessResponse(result, "Qrup yaradıldı.", 201));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> UpdateGroup(int id, [FromBody] UpdateGroupRequest dto)
        {
            var result = await _groupService.UpdateGroupAsync(id, dto);
            return Ok(ApiResponse<GroupResponse>.SuccessResponse(result, "Qrup yeniləndi."));
        }

        [HttpPatch("{id}/assign-teacher")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> AssignTeacher(int id, [FromBody] AssignTeacherRequest dto)
        {
            await _groupService.AssignTeacherAsync(id, dto.TeacherId);
            return Ok(ApiResponse<string>.SuccessResponse("Müəllim təyin edildi."));
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            await _groupService.DeleteGroupAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("Qrup silindi."));
        }

        // ── Group Teachers ────────────────────────────────────────────────────

        [HttpGet("{id}/teachers")]
        [Authorize(Policy = "AllStaff")]
        public async Task<IActionResult> GetGroupTeachers(int id)
        {
            var result = await _groupService.GetGroupTeachersAsync(id);
            return Ok(ApiResponse<IEnumerable<GroupTeacherResponse>>.SuccessResponse(result));
        }

        [HttpPost("{id}/teachers")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> AddGroupTeacher(int id, [FromBody] AddGroupTeacherRequest dto)
        {
            await _groupService.AddGroupTeacherAsync(id, dto.UserId);
            return Ok(ApiResponse<string>.SuccessResponse("Tərbiyəçi qrupa əlavə edildi."));
        }

        [HttpDelete("{id}/teachers/{userId}")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> RemoveGroupTeacher(int id, string userId)
        {
            await _groupService.RemoveGroupTeacherAsync(id, userId);
            return Ok(ApiResponse<string>.SuccessResponse("Tərbiyəçi qrupdan çıxarıldı."));
        }

        [HttpPost("{id}/teachers/{userId}/move")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> MoveTeacher(int id, string userId, [FromBody] MoveTeacherRequest dto)
        {
            await _groupService.MoveTeacherAsync(id, userId, dto.ToGroupId);
            return Ok(ApiResponse<string>.SuccessResponse("Müəllim köçürüldü."));
        }

        [HttpPatch("{id}/teachers/{userId}/toggle-active")]
        [Authorize(Policy = "AdminOrAdmission")]
        public async Task<IActionResult> ToggleTeacherActive(int id, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new App.Core.Exceptions.Commons.EntityNotFoundException("İstifadəçi tapılmadı.");

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            var msg = user.IsActive ? "Müəllim aktivləşdirildi." : "Müəllim deaktivləşdirildi.";
            return Ok(ApiResponse<bool>.SuccessResponse(user.IsActive, msg));
        }
    }
}
