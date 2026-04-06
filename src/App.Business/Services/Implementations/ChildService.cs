using App.Business.DTOs.Children;
using App.Business.Services.Interfaces;
using App.Core.Common;
using App.Core.Entities;
using App.Core.Enums;
using App.Core.Exceptions.Commons;
using App.Core.Services;
using App.DAL.UnitOfWork;
using AutoMapper;

namespace App.Business.Services.Implementations
{
    /// <summary>
    /// Handles child management operations.
    /// </summary>
    public class ChildService : IChildService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDateTimeService _dt;

        public ChildService(IUnitOfWork unitOfWork, IMapper mapper, IDateTimeService dt)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _dt = dt;
        }

        /// <summary>
        /// Creates a new child record.
        /// </summary>
        public async Task<ChildResponse> CreateChildAsync(CreateChildRequest dto)
        {
            var groupExists = await _unitOfWork.Groups.ExistsAsync(dto.GroupId);
            if (!groupExists)
                throw new EntityNotFoundException($"{dto.GroupId} ID-li qrup tapılmadı.");

            var child = _mapper.Map<Child>(dto);
            child.RegistrationDate = _dt.Now;
            child.Status = ChildStatus.Active;

            await _unitOfWork.Children.AddAsync(child);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.GroupLogs.AddAsync(new GroupLog
            {
                GroupId = child.GroupId,
                ChildId = child.Id,
                ActionType = GroupLogActionType.ChildAdded,
                Message = $"Uşaq əlavə olundu: {child.FirstName} {child.LastName}",
                ActionDate = _dt.Now
            });
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Children.GetByIdAsync(
                c => c.Id == child.Id,
                c => c.Group,
                c => c.Group.Division);

            return _mapper.Map<ChildResponse>(created);
        }

        /// <summary>
        /// Updates an existing child record.
        /// </summary>
        public async Task<ChildResponse> UpdateChildAsync(int id, UpdateChildRequest dto)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li uşaq tapılmadı.");

            if (dto.FirstName != null) child.FirstName = dto.FirstName;
            if (dto.LastName != null) child.LastName = dto.LastName;
            if (dto.DateOfBirth.HasValue) child.DateOfBirth = dto.DateOfBirth.Value;
            if (dto.GroupId.HasValue) child.GroupId = dto.GroupId.Value;
            if (dto.ScheduleType.HasValue) child.ScheduleType = dto.ScheduleType.Value;
            if (dto.MonthlyFee.HasValue) child.MonthlyFee = dto.MonthlyFee.Value;
            if (dto.PaymentDay.HasValue) child.PaymentDay = dto.PaymentDay.Value;
            if (dto.ParentFullName != null) child.ParentFullName = dto.ParentFullName;
            if (dto.SecondParentFullName != null) child.SecondParentFullName = dto.SecondParentFullName;
            if (dto.ParentPhone != null) child.ParentPhone = dto.ParentPhone;
            if (dto.SecondParentPhone != null) child.SecondParentPhone = dto.SecondParentPhone;
            if (dto.ParentEmail != null) child.ParentEmail = dto.ParentEmail;
            if (dto.FaceIdToken != null) child.FaceIdToken = dto.FaceIdToken;

            await _unitOfWork.Children.UpdateAsync(child);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.Children.GetByIdAsync(
                c => c.Id == id,
                c => c.Group,
                c => c.Group.Division);

            return _mapper.Map<ChildResponse>(updated);
        }

        /// <summary>
        /// Gets a child's full details including attendance and payment summaries.
        /// </summary>
        public async Task<ChildDetailResponse> GetChildByIdAsync(int id)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(
                c => c.Id == id,
                c => c.Group,
                c => c.Group.Division,
                c => c.Group.Teacher)
                ?? throw new EntityNotFoundException($"{id} ID-li uşaq tapılmadı.");

                            var response = _mapper.Map<ChildDetailResponse>(child);

            var now = DateOnly.FromDateTime(_dt.Now);
            var monthStart = new DateOnly(now.Year, now.Month, 1);
            var attendances = await _unitOfWork.Attendances.GetChildAttendanceAsync(id, monthStart, now);
            response.AttendanceDays = attendances.Count(a => a.Status == AttendanceStatus.Present);
            response.AbsentDays = attendances.Count(a => a.Status == AttendanceStatus.Absent);

            var payments = await _unitOfWork.Payments.GetPaymentsByChildAsync(id);
            response.TotalDebt = payments
                .Where(p => p.Status != PaymentStatus.Paid)
                .Sum(p => p.FinalAmount - p.PaidAmount);

            return response;
        }

        /// <summary>
        /// Gets all children with filtering and pagination.
        /// </summary>
        public async Task<PagedResponse<ChildResponse>> GetAllChildrenAsync(ChildFilterRequest filter)
        {
            var children = await _unitOfWork.Children.GetChildrenWithDetailsAsync();
            var query = children.AsQueryable().Where(x=>x.IsDeleted==false);

            if (filter.GroupId.HasValue)
                query = query.Where(c => c.GroupId == filter.GroupId.Value);
            if (filter.DivisionId.HasValue)
                query = query.Where(c => c.Group.DivisionId == filter.DivisionId.Value);
            if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<ChildStatus>(filter.Status, true, out var status))
                query = query.Where(c => c.Status == status);
            if (!string.IsNullOrEmpty(filter.ScheduleType) && Enum.TryParse<ScheduleType>(filter.ScheduleType, true, out var schedType))
                query = query.Where(c => c.ScheduleType == schedType);

            var totalCount = query.Count();

            // PageSize <= 0 göndərildikdə bütün nəticələr qaytar
            List<Child> items;
            int pageSize, page, totalPages;

            if (filter.PageSize <= 0)
            {
                items      = query.ToList();
                pageSize   = totalCount == 0 ? 1 : totalCount;
                page       = 1;
                totalPages = 1;
            }
            else
            {
                pageSize   = filter.PageSize;
                page       = Math.Max(filter.Page, 1);
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                items      = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            }

            return new PagedResponse<ChildResponse>
            {
                Items = _mapper.Map<IEnumerable<ChildResponse>>(items),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        /// <summary>
        /// Activates a child.
        /// </summary>
        public async Task ActivateChildAsync(int id)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li uşaq tapılmadı.");

            child.Status = ChildStatus.Active;

            await _unitOfWork.GroupLogs.AddAsync(new GroupLog
            {
                GroupId = child.GroupId,
                ChildId = child.Id,
                ActionType = GroupLogActionType.ChildReturned,
                Message = $"Uşaq qrupa geri qaytarıldı: {child.FirstName} {child.LastName}",
                ActionDate = _dt.Now
            });

            await _unitOfWork.Children.UpdateAsync(child);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Deactivates a child.
        /// </summary>
        public async Task DeactivateChildAsync(int id)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li uşaq tapılmadı.");

            child.Status = ChildStatus.Inactive;
            var actionDate = _dt.Now;

            await _unitOfWork.GroupLogs.AddAsync(new GroupLog
            {
                GroupId = child.GroupId,
                ChildId = child.Id,
                ActionType = GroupLogActionType.ChildRemoved,
                Message = $"Uşaq çıxarıldı: {child.FirstName} {child.LastName}",
                ActionDate = actionDate
            });

            await _unitOfWork.Children.UpdateAsync(child);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Soft-deletes a child.
        /// </summary>
        public async Task DeleteChildAsync(int id)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li uşaq tapılmadı.");

            await _unitOfWork.GroupLogs.AddAsync(new GroupLog
            {
                GroupId = child.GroupId,
                ChildId = child.Id,
                ActionType = GroupLogActionType.ChildRemoved,
                Message = $"Uşaq silindi: {child.FirstName} {child.LastName}",
                ActionDate = _dt.Now
            });

            await _unitOfWork.Children.SoftDeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Searches children by name, parent name, or phone.
        /// </summary>
        public async Task<IEnumerable<ChildResponse>> SearchChildrenAsync(string term)
        {
            var children = await _unitOfWork.Children.SearchChildrenAsync(term);
            return _mapper.Map<IEnumerable<ChildResponse>>(children);
        }

        /// <summary>
        /// Deactivates a list of children.
        /// </summary>
        public async Task DeactivateChildrenAsync(List<int> ids)
        {
            foreach (var id in ids)
            {
                var child = await _unitOfWork.Children.GetByIdAsync(id);
                if (child != null)
                {
                    child.Status = ChildStatus.Inactive;
                    var actionDate = _dt.Now;

                    await _unitOfWork.GroupLogs.AddAsync(new GroupLog
                    {
                        GroupId = child.GroupId,
                        ChildId = child.Id,
                        ActionType = GroupLogActionType.ChildRemoved,
                        Message = $"Uşaq çıxarıldı: {child.FirstName} {child.LastName}",
                        ActionDate = actionDate
                    });

                    await _unitOfWork.Children.UpdateAsync(child);
                }
            }
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Activates a list of children.
        /// </summary>
        public async Task ActivateChildrenAsync(List<int> ids)
        {
            foreach (var id in ids)
            {
                var child = await _unitOfWork.Children.GetByIdAsync(id);
                if (child != null)
                {
                    child.Status = ChildStatus.Active;

                    await _unitOfWork.GroupLogs.AddAsync(new GroupLog
                    {
                        GroupId = child.GroupId,
                        ChildId = child.Id,
                        ActionType = GroupLogActionType.ChildReturned,
                        Message = $"Uşaq qrupa geri qaytarıldı: {child.FirstName} {child.LastName}",
                        ActionDate = _dt.Now
                    });

                    await _unitOfWork.Children.UpdateAsync(child);
                }
            }
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Soft-deletes a list of children.
        /// </summary>
        public async Task DeleteChildrenAsync(List<int> ids)
        {
            foreach (var id in ids)
            {
                var child = await _unitOfWork.Children.GetByIdAsync(id);
                if (child != null)
                {
                    await _unitOfWork.GroupLogs.AddAsync(new GroupLog
                    {
                        GroupId = child.GroupId,
                        ChildId = child.Id,
                        ActionType = GroupLogActionType.ChildRemoved,
                        Message = $"Uşaq silindi: {child.FirstName} {child.LastName}",
                        ActionDate = _dt.Now
                    });
                }

                await _unitOfWork.Children.SoftDeleteAsync(id);
            }
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
