using App.Business.DTOs.Groups;

namespace App.Business.Services.Interfaces
{
    /// <summary>
    /// Service for group management operations.
    /// </summary>
    public interface IGroupService
    {
        Task<GroupResponse> CreateGroupAsync(CreateGroupRequest dto);
        Task<GroupResponse> UpdateGroupAsync(int id, UpdateGroupRequest dto);
        Task<GroupDetailResponse> GetGroupByIdAsync(int id);
        Task<IEnumerable<GroupResponse>> GetAllGroupsAsync();
        Task<IEnumerable<GroupResponse>> GetGroupsByDivisionAsync(int divisionId);
        Task AssignTeacherAsync(int groupId, string teacherId);
        Task ChangeTeacherAsync(int groupId, string newTeacherId);
        Task DeleteGroupAsync(int id);
    }
}
