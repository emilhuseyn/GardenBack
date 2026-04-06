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
        Task<IEnumerable<GroupLogResponse>> GetGroupLogsAsync(int groupId);
        Task AssignTeacherAsync(int groupId, string teacherId);
        Task ChangeTeacherAsync(int groupId, string newTeacherId);
        Task DeleteGroupAsync(int id);
        Task<IEnumerable<GroupTeacherResponse>> GetGroupTeachersAsync(int groupId);
        Task AddGroupTeacherAsync(int groupId, string userId);
        Task RemoveGroupTeacherAsync(int groupId, string userId);
    }
}
