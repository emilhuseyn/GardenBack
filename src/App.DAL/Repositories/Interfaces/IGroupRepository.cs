using App.Core.Entities;

namespace App.DAL.Repositories.Interfaces
{
    public interface IGroupRepository : IRepository<Group>
    {
        Task<IEnumerable<Group>> GetGroupsWithDetailsAsync();
        Task<IEnumerable<Group>> GetGroupsByDivisionAsync(int divisionId);
        Task<IEnumerable<Group>> GetGroupsByTeacherIdAsync(string teacherId);
    }
}
