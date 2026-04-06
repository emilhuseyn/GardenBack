using App.Core.Entities;

namespace App.DAL.Repositories.Interfaces
{
    public interface IGroupTeacherRepository
    {
        Task<IEnumerable<GroupTeacher>> GetByGroupAsync(int groupId);
        Task<GroupTeacher?> GetAsync(int groupId, string userId);
        Task AddAsync(GroupTeacher groupTeacher);
        Task RemoveAsync(GroupTeacher groupTeacher);
    }
}
