using App.Core.Entities;
using App.DAL.Presistence;
using App.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Repositories.Abstractions
{
    public class GroupTeacherRepository : IGroupTeacherRepository
    {
        private readonly AppDbContext _context;

        public GroupTeacherRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<GroupTeacher>> GetByGroupAsync(int groupId)
        {
            return await _context.GroupTeachers
                .Include(gt => gt.User)
                .Where(gt => gt.GroupId == groupId)
                .OrderBy(gt => gt.AssignedAt)
                .ToListAsync();
        }

        public async Task<GroupTeacher?> GetAsync(int groupId, string userId)
        {
            return await _context.GroupTeachers
                .FirstOrDefaultAsync(gt => gt.GroupId == groupId && gt.UserId == userId);
        }

        public async Task AddAsync(GroupTeacher groupTeacher)
        {
            await _context.GroupTeachers.AddAsync(groupTeacher);
        }

        public Task RemoveAsync(GroupTeacher groupTeacher)
        {
            _context.GroupTeachers.Remove(groupTeacher);
            return Task.CompletedTask;
        }
    }
}
