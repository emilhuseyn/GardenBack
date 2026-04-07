using App.Business.DTOs.Groups;
using App.Business.Services.Interfaces;
using App.Core.Entities;
using App.Core.Enums;
using App.Core.Exceptions.Commons;
using App.Core.Services;
using App.DAL.UnitOfWork;
using App.Shared.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Business.Services.Implementations
{
    /// <summary>
    /// Handles group management operations.
    /// </summary>
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDateTimeService _dt;
        private readonly UserManager<App.Core.Entities.Identity.User> _userManager;
        private readonly IClaimService _claimService;

        public GroupService(IUnitOfWork unitOfWork, IMapper mapper, IDateTimeService dt,
            UserManager<App.Core.Entities.Identity.User> userManager, IClaimService claimService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _dt = dt;
            _userManager = userManager;
            _claimService = claimService;
        }

        /// <summary>
        /// Creates a new group.
        /// </summary>
        public async Task<GroupResponse> CreateGroupAsync(CreateGroupRequest dto)
        {
            if (!await _unitOfWork.Divisions.ExistsAsync(dto.DivisionId))
                throw new EntityNotFoundException($"{dto.DivisionId} ID-li bölmə tapılmadı.");

            var group = _mapper.Map<Group>(dto);
            await _unitOfWork.Groups.AddAsync(group);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Groups.GetByIdAsync(
                g => g.Id == group.Id,
                g => g.Division,
                g => g.Teacher);

            return _mapper.Map<GroupResponse>(created);
        }

        /// <summary>
        /// Updates an existing group.
        /// </summary>
        public async Task<GroupResponse> UpdateGroupAsync(int id, UpdateGroupRequest dto)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li qrup tapılmadı.");

            if (dto.Name != null) group.Name = dto.Name;
            if (dto.DivisionId.HasValue) group.DivisionId = dto.DivisionId.Value;
            if (dto.TeacherId != null) group.TeacherId = dto.TeacherId;
            if (dto.MaxChildCount.HasValue) group.MaxChildCount = dto.MaxChildCount.Value;
            if (dto.AgeCategory != null) group.AgeCategory = dto.AgeCategory;
            if (dto.Language != null) group.Language = dto.Language;

            await _unitOfWork.GroupLogs.AddAsync(new GroupLog
            {
                GroupId = group.Id,
                ActionType = GroupLogActionType.GroupUpdated,
                Message = $"Qrup redaktə edildi: {group.Name}",
                ActionDate = _dt.Now
            });

            await _unitOfWork.Groups.UpdateAsync(group);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.Groups.GetByIdAsync(
                g => g.Id == id,
                g => g.Division,
                g => g.Teacher);

            return _mapper.Map<GroupResponse>(updated);
        }

        /// <summary>
        /// Gets a group's full details including children list.
        /// </summary>
        public async Task<GroupDetailResponse> GetGroupByIdAsync(int id)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(
                g => g.Id == id,
                g => g.Division,
                g => g.Teacher,
                g => g.Children)
                ?? throw new EntityNotFoundException($"{id} ID-li qrup tapılmadı.");

            // Müəllim yalnız öz qrupuna girə bilər
            var role = _claimService.GetUserRole();
            if (role == "Teacher")
            {
                var userId = _claimService.GetUserId();
                var isPrimary = group.TeacherId == userId;
                var isAssigned = await _unitOfWork.GroupTeachers.GetAsync(id, userId) != null;

                if (!isPrimary && !isAssigned)
                    throw new Core.Exceptions.UnauthorizedException("Bu qrupa giriş icazəniz yoxdur.");
            }

            var response = _mapper.Map<GroupDetailResponse>(group);

            var groupTeachers = await _unitOfWork.GroupTeachers.GetByGroupAsync(id);
            response.Teachers = groupTeachers.Select(gt => new GroupTeacherResponse
            {
                UserId    = gt.UserId,
                FullName  = $"{gt.User.FirstName} {gt.User.LastName}".Trim(),
                Email     = gt.User.Email ?? string.Empty,
                AssignedAt = gt.AssignedAt
            }).ToList();

            return response;
        }

        /// <summary>
        /// Gets all groups with details.
        /// </summary>
        public async Task<IEnumerable<GroupResponse>> GetAllGroupsAsync()
        {
            var groups = await _unitOfWork.Groups.GetGroupsWithDetailsAsync();

            // Yalnız Teacher filtrə edilər — Admin, Admission, Accountant bütün qrupları görür
            var role = _claimService.GetUserRole();
            if (role == "Teacher")
            {
                var userId = _claimService.GetUserId();

                var assignedGroupIds = await _unitOfWork.Context.GroupTeachers
                    .Where(gt => gt.UserId == userId)
                    .Select(gt => gt.GroupId)
                    .ToListAsync();

                groups = groups.Where(g => g.TeacherId == userId || assignedGroupIds.Contains(g.Id));
            }

            return _mapper.Map<IEnumerable<GroupResponse>>(groups);
        }

        /// <summary>
        /// Gets groups filtered by division.
        /// </summary>
        public async Task<IEnumerable<GroupResponse>> GetGroupsByDivisionAsync(int divisionId)
        {
            var groups = await _unitOfWork.Groups.GetGroupsByDivisionAsync(divisionId);
            return _mapper.Map<IEnumerable<GroupResponse>>(groups);
        }

        /// <summary>
        /// Gets activity logs for a specific group.
        /// </summary>
        public async Task<IEnumerable<GroupLogResponse>> GetGroupLogsAsync(int groupId)
        {
            if (!await _unitOfWork.Groups.ExistsAsync(groupId))
                throw new EntityNotFoundException($"{groupId} ID-li qrup tapılmadı.");

            var logs = await _unitOfWork.GroupLogs.FindAsync(x => x.GroupId == groupId);

            return logs
                .OrderByDescending(x => x.ActionDate)
                .Select(x => new GroupLogResponse
                {
                    Id = x.Id,
                    GroupId = x.GroupId,
                    ChildId = x.ChildId,
                    ActionType = x.ActionType.ToString(),
                    Message = x.Message,
                    ActionDate = x.ActionDate
                });
        }

        /// <summary>
        /// Assigns a teacher to a group.
        /// </summary>
        public async Task AssignTeacherAsync(int groupId, string teacherId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId)
                ?? throw new EntityNotFoundException($"{groupId} ID-li qrup tapılmadı.");

            group.TeacherId = teacherId;
            await _unitOfWork.Groups.UpdateAsync(group);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Allows admin to change the teacher of a group.
        /// </summary>
        public async Task ChangeTeacherAsync(int groupId, string newTeacherId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId)
                ?? throw new EntityNotFoundException($"{groupId} ID-li qrup tapılmadı.");
            group.TeacherId = newTeacherId;
            await _unitOfWork.Groups.UpdateAsync(group);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Soft-deletes a group.
        /// </summary>
        public async Task DeleteGroupAsync(int id)
        {
            await _unitOfWork.Groups.SoftDeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Returns all teachers assigned to a group.
        /// </summary>
        public async Task<IEnumerable<GroupTeacherResponse>> GetGroupTeachersAsync(int groupId)
        {
            if (!await _unitOfWork.Groups.ExistsAsync(groupId))
                throw new EntityNotFoundException($"{groupId} ID-li qrup tapılmadı.");

            var groupTeachers = await _unitOfWork.GroupTeachers.GetByGroupAsync(groupId);
            return groupTeachers.Select(gt => new GroupTeacherResponse
            {
                UserId     = gt.UserId,
                FullName   = $"{gt.User.FirstName} {gt.User.LastName}".Trim(),
                Email      = gt.User.Email ?? string.Empty,
                IsActive   = gt.User.IsActive,
                AssignedAt = gt.AssignedAt
            });
        }

        /// <summary>
        /// Adds a teacher to a group.
        /// </summary>
        public async Task AddGroupTeacherAsync(int groupId, string userId)
        {
            if (!await _unitOfWork.Groups.ExistsAsync(groupId))
                throw new EntityNotFoundException($"{groupId} ID-li qrup tapılmadı.");

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new EntityNotFoundException($"{userId} ID-li istifadəçi tapılmadı.");

            var existing = await _unitOfWork.GroupTeachers.GetAsync(groupId, userId);
            if (existing != null)
                throw new InvalidOperationException("Bu tərbiyəçi artıq bu qrupa təyin edilib.");

            await _unitOfWork.GroupTeachers.AddAsync(new GroupTeacher
            {
                GroupId    = groupId,
                UserId     = userId,
                AssignedAt = _dt.Now
            });

            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Removes a teacher from a group.
        /// </summary>
        public async Task RemoveGroupTeacherAsync(int groupId, string userId)
        {
            var record = await _unitOfWork.GroupTeachers.GetAsync(groupId, userId)
                ?? throw new EntityNotFoundException("Bu tərbiyəçi bu qrupda tapılmadı.");

            await _unitOfWork.GroupTeachers.RemoveAsync(record);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Moves a teacher from one group to another.
        /// </summary>
        public async Task MoveTeacherAsync(int fromGroupId, string userId, int toGroupId)
        {
            if (fromGroupId == toGroupId)
                throw new App.Core.Exceptions.ValidationException("Müəllim artıq bu qrupdadır.");

            if (!await _unitOfWork.Groups.ExistsAsync(toGroupId))
                throw new EntityNotFoundException($"{toGroupId} ID-li qrup tapılmadı.");

            var record = await _unitOfWork.GroupTeachers.GetAsync(fromGroupId, userId)
                ?? throw new EntityNotFoundException("Bu tərbiyəçi bu qrupda tapılmadı.");

            var alreadyInTarget = await _unitOfWork.GroupTeachers.GetAsync(toGroupId, userId);
            if (alreadyInTarget != null)
                throw new App.Core.Exceptions.ValidationException("Bu müəllim hədəf qrupa artıq təyin edilib.");

            await _unitOfWork.GroupTeachers.RemoveAsync(record);
            await _unitOfWork.GroupTeachers.AddAsync(new GroupTeacher
            {
                GroupId    = toGroupId,
                UserId     = userId,
                AssignedAt = _dt.Now
            });

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
