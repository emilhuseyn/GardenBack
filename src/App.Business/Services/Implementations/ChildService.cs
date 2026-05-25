using App.Business.DTOs.Children;
using App.Business.Services.Interfaces;
using App.Core.Common;
using App.Core.Entities;
using App.Core.Enums;
using App.Core.Exceptions;
using App.Core.Exceptions.Commons;
using App.Core.Services;
using App.DAL.UnitOfWork;
using App.Shared.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
        private readonly IClaimService _claimService;

        public ChildService(IUnitOfWork unitOfWork, IMapper mapper, IDateTimeService dt, IClaimService claimService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _dt = dt;
            _claimService = claimService;
        }

        /// <summary>
        /// Müəllim yalnız öz qrupuna aid uşağa girə bilər.
        /// </summary>
        private async Task EnsureTeacherGroupAccessAsync(int childGroupId)
        {
            var role = _claimService.GetUserRole();
            if (role != "Teacher") return;

            var userId = _claimService.GetUserId();
            var group = await _unitOfWork.Groups.GetByIdAsync(childGroupId)
                ?? throw new EntityNotFoundException("Qrup tapılmadı.");

            var isPrimary  = group.TeacherId == userId;
            var isAssigned = await _unitOfWork.GroupTeachers.GetAsync(childGroupId, userId) != null;

            if (!isPrimary && !isAssigned)
                throw new UnauthorizedException("Bu uşağa giriş icazəniz yoxdur.");
        }

        /// <summary>
        /// Creates a new child record.
        /// </summary>
        public async Task<ChildResponse> CreateChildAsync(CreateChildRequest dto)
        {
            var groupExists = await _unitOfWork.Groups.ExistsAsync(dto.GroupId);
            if (!groupExists)
                throw new EntityNotFoundException($"{dto.GroupId} ID-li qrup tapılmadı.");

            // Check PersonId uniqueness BEFORE creating
            if (dto.PersonId.HasValue && dto.PersonId.Value > 0)
            {
                var existingPerson = (await _unitOfWork.Children.FindAsync(c => c.PersonId == dto.PersonId.Value)).FirstOrDefault();
                if (existingPerson != null)
                {
                    if (existingPerson.IsDeleted)
                    {
                        // Transfer PersonId from the deleted child to the new one
                        existingPerson.PersonId = null;
                        await _unitOfWork.Children.UpdateAsync(existingPerson);
                    }
                    else
                    {
                        throw new Core.Exceptions.ValidationException(new[] { $"Bu İVMS ID artıq {existingPerson.FirstName} {existingPerson.LastName} üçün istifadə olunur ({dto.PersonId.Value})" });
                    }
                }
            }

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

            await EnsureTeacherGroupAccessAsync(child.GroupId);

            // Capture pre-change fee/discount so we can resync existing unpaid bills if either of them moves.
            var oldMonthlyFee  = child.MonthlyFee;
            var oldDiscountPct = child.DiscountPercentage ?? 0;

            if (dto.FirstName != null) child.FirstName = dto.FirstName;
            if (dto.LastName != null) child.LastName = dto.LastName;
            if (dto.DateOfBirth.HasValue) child.DateOfBirth = dto.DateOfBirth.Value;
            if (dto.GroupId.HasValue) child.GroupId = dto.GroupId.Value;
            if (!string.IsNullOrWhiteSpace(dto.ScheduleType)) child.ScheduleType = dto.ScheduleType.Trim();
            if (dto.MonthlyFee.HasValue) child.MonthlyFee = dto.MonthlyFee.Value;
            if (dto.DiscountPercentage.HasValue) child.DiscountPercentage = dto.DiscountPercentage.Value;
            if (dto.PaymentDay.HasValue) child.PaymentDay = dto.PaymentDay.Value;
            if (dto.RegistrationDate.HasValue) child.RegistrationDate = dto.RegistrationDate.Value;
            if (dto.DeactivationDate.HasValue) child.DeactivationDate = dto.DeactivationDate.Value;
            if (dto.ParentFullName != null) child.ParentFullName = dto.ParentFullName;
            if (dto.SecondParentFullName != null) child.SecondParentFullName = dto.SecondParentFullName;
            if (dto.ParentPhone != null) child.ParentPhone = dto.ParentPhone;
            if (dto.SecondParentPhone != null) child.SecondParentPhone = dto.SecondParentPhone;
            if (dto.ParentEmail != null) child.ParentEmail = dto.ParentEmail;

            // Check PersonId uniqueness BEFORE updating
            var newPersonId = dto.PersonId.HasValue && dto.PersonId.Value > 0 ? dto.PersonId.Value : (int?)null;

            if (newPersonId.HasValue)
            {
                var existingPerson = (await _unitOfWork.Children.FindAsync(c => c.PersonId == newPersonId.Value && c.Id != id)).FirstOrDefault();
                if (existingPerson != null)
                {
                    if (existingPerson.IsDeleted)
                    {
                        // Transfer PersonId from the deleted child to the new one
                        existingPerson.PersonId = null;
                        await _unitOfWork.Children.UpdateAsync(existingPerson);
                    }
                    else
                    {
                        throw new Core.Exceptions.ValidationException(new[] { $"Bu İVMS ID artıq {existingPerson.FirstName} {existingPerson.LastName} üçün istifadə olunur ({newPersonId.Value})" });
                    }
                }
            }

            child.PersonId = newPersonId;
            if (dto.FaceIdToken != null) child.FaceIdToken = dto.FaceIdToken;

            await _unitOfWork.Children.UpdateAsync(child);

            // If MonthlyFee or DiscountPercentage moved, retroactively fix every still-open bill for
            // this child so the parent isn't shown as a debtor for the gap (or as overpaid).
            var newDiscountPct = child.DiscountPercentage ?? 0;
            if (child.MonthlyFee != oldMonthlyFee || newDiscountPct != oldDiscountPct)
            {
                await ResyncUnpaidPaymentsAfterFeeChangeAsync(child);
            }

            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.Children.GetByIdAsync(
                c => c.Id == id,
                c => c.Group,
                c => c.Group.Division);

            return _mapper.Map<ChildResponse>(updated);
        }

        /// <summary>
        /// Re-bills every still-open payment for this child after their MonthlyFee or DiscountPercentage
        /// changed. Skips already-Paid rows, preserves pro-rated entry/exit periods (parsed from the Notes),
        /// reapplies the discount, and re-evaluates Status against PaidAmount.
        /// </summary>
        private async Task ResyncUnpaidPaymentsAfterFeeChangeAsync(Child child)
        {
            var openPayments = (await _unitOfWork.Payments
                .FindAsync(p => p.ChildId == child.Id && p.Status != PaymentStatus.Paid))
                .ToList();

            if (openPayments.Count == 0) return;

            var discountPercent = child.DiscountPercentage ?? 0;
            var hasDiscount = discountPercent > 0;
            var periodRegex = new Regex(@"Dövr:\s*\d+\s*-\s*\d+\s*\(\s*(\d+)\s*gün\s*\)", RegexOptions.IgnoreCase);

            foreach (var payment in openPayments)
            {
                // Preserve any partial-month period stored in Notes ("Dövr: 5-25 (21 gün)")
                var daysInMonth = DateTime.DaysInMonth(payment.Year, payment.Month);
                var daysActive = daysInMonth;
                if (!string.IsNullOrEmpty(payment.Notes))
                {
                    var match = periodRegex.Match(payment.Notes);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var parsedDays))
                    {
                        daysActive = parsedDays;
                    }
                }

                var newOriginal = daysActive == daysInMonth
                    ? child.MonthlyFee
                    : Math.Round(child.MonthlyFee * daysActive / daysInMonth, 0, MidpointRounding.AwayFromZero);

                var rawFinal = hasDiscount
                    ? newOriginal - (newOriginal * discountPercent / 100m)
                    : newOriginal;
                var newFinal = Math.Round(rawFinal, 0, MidpointRounding.AwayFromZero);

                if (payment.OriginalAmount == newOriginal && payment.FinalAmount == newFinal) continue;

                var oldFinal = payment.FinalAmount;

                payment.OriginalAmount = newOriginal;
                payment.FinalAmount = newFinal;
                payment.DiscountType = hasDiscount ? DiscountType.Percentage : DiscountType.None;
                payment.DiscountValue = hasDiscount ? discountPercent : 0;

                // Audit trail in the Notes column
                var resyncNote = $"Aylıq qiymət yeniləndi: {oldFinal:F0} → {newFinal:F0} ₼";
                payment.Notes = string.IsNullOrWhiteSpace(payment.Notes)
                    ? resyncNote
                    : $"{payment.Notes} | {resyncNote}";

                // Recompute Status against the new FinalAmount
                if (payment.PaidAmount >= payment.FinalAmount)
                    payment.Status = PaymentStatus.Paid;
                else if (payment.PaidAmount > 0)
                    payment.Status = PaymentStatus.PartiallyPaid;
                else
                    payment.Status = PaymentStatus.Debt;

                await _unitOfWork.Payments.UpdateAsync(payment);
            }
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

            await EnsureTeacherGroupAccessAsync(child.GroupId);

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
            var query = children.AsQueryable().Where(x => x.IsDeleted == false);

            // Müəllim yalnız öz qruplarının uşaqlarını görə bilər
            var role = _claimService.GetUserRole();
            if (role == "Teacher")
            {
                var userId = _claimService.GetUserId();
                var teacherGroupIds = (await _unitOfWork.GroupTeachers.GetByGroupAsync(0))
                    .Select(gt => gt.GroupId).ToList();

                // primary teacher olduğu qrupları da əlavə et
                var allGroups = await _unitOfWork.Groups.FindAsync(g => g.TeacherId == userId);
                var primaryGroupIds = allGroups.Select(g => g.Id).ToList();

                var assignedGroupIds = (await _unitOfWork.Context.GroupTeachers
                    .Where(gt => gt.UserId == userId)
                    .Select(gt => gt.GroupId)
                    .ToListAsync());

                var allowedGroupIds = primaryGroupIds.Union(assignedGroupIds).ToHashSet();
                query = query.Where(c => allowedGroupIds.Contains(c.GroupId));
            }

            if (filter.GroupId.HasValue)
                query = query.Where(c => c.GroupId == filter.GroupId.Value);
            if (filter.DivisionId.HasValue)
                query = query.Where(c => c.Group.DivisionId == filter.DivisionId.Value);
            if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<ChildStatus>(filter.Status, true, out var status))
                query = query.Where(c => c.Status == status);
            if (!string.IsNullOrEmpty(filter.ScheduleType))
                query = query.Where(c => c.ScheduleType == filter.ScheduleType);

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
            child.DeactivationDate = null;

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
        /// Creates or adjusts the pro-rated payment for a child leaving mid-month.
        /// Accounts for both the registration day (if same month) and the exit day,
        /// so a child who arrives on the 5th and leaves on the 25th is billed for 21 days, not 25.
        /// </summary>
        private async Task ApplyExitMonthPaymentAsync(Child child, DateTime exitDate)
        {
            var month = exitDate.Month;
            var year = exitDate.Year;
            var exitDay = exitDate.Day;
            var daysInMonth = DateTime.DaysInMonth(year, month);

            // If the child registered in the same month, count from registration day; otherwise from day 1.
            var startDay = (child.RegistrationDate.Year == year && child.RegistrationDate.Month == month)
                ? child.RegistrationDate.Day
                : 1;
            var daysActive = Math.Max(0, exitDay - startDay + 1);

            // Bill in whole manats — no qəpik fractions in the bill
            var proratedBase = Math.Round(child.MonthlyFee * daysActive / daysInMonth, 0, MidpointRounding.AwayFromZero);
            var discountPercent = child.DiscountPercentage ?? 0;
            var hasDiscount = discountPercent > 0;
            var rawFinal = hasDiscount
                ? proratedBase * (1 - discountPercent / 100)
                : proratedBase;
            var finalAmount = Math.Round(rawFinal, 0, MidpointRounding.AwayFromZero);

            var periodNote = $"Dövr: {startDay}-{exitDay} ({daysActive} gün)";

            var existing = (await _unitOfWork.Payments
                .FindAsync(p => p.ChildId == child.Id && p.Month == month && p.Year == year))
                .FirstOrDefault();

            if (existing != null)
            {
                if (existing.Status == PaymentStatus.Paid) return; // already fully paid – leave as is

                existing.OriginalAmount = proratedBase;
                existing.FinalAmount = finalAmount;
                existing.Notes = periodNote;

                if (existing.PaidAmount >= finalAmount)
                    existing.Status = PaymentStatus.Paid;
                else if (existing.PaidAmount > 0)
                    existing.Status = PaymentStatus.PartiallyPaid;
                else
                    existing.Status = PaymentStatus.Debt;

                await _unitOfWork.Payments.UpdateAsync(existing);
            }
            else
            {
                var payment = new Payment
                {
                    ChildId = child.Id,
                    Month = month,
                    Year = year,
                    OriginalAmount = proratedBase,
                    FinalAmount = finalAmount,
                    PaidAmount = 0,
                    LastPaymentAmount = null,
                    Status = PaymentStatus.Debt,
                    DiscountType = hasDiscount ? DiscountType.Percentage : DiscountType.None,
                    DiscountValue = hasDiscount ? discountPercent : 0,
                    Notes = periodNote,
                    RecordedById = "system"
                };
                await _unitOfWork.Payments.AddAsync(payment);
            }
        }

        /// <summary>
        /// Deactivates a child.
        /// </summary>
        public async Task DeactivateChildAsync(int id)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li uşaq tapılmadı.");

            var now = _dt.Now;
            child.Status = ChildStatus.Inactive;
            child.DeactivationDate = now;

            await _unitOfWork.GroupLogs.AddAsync(new GroupLog
            {
                GroupId = child.GroupId,
                ChildId = child.Id,
                ActionType = GroupLogActionType.ChildRemoved,
                Message = $"Uşaq çıxarıldı: {child.FirstName} {child.LastName}",
                ActionDate = now
            });

            await _unitOfWork.Children.UpdateAsync(child);

            // Adjust this month's payment to cover only the days the child attended
            await ApplyExitMonthPaymentAsync(child, now);

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
            var now = _dt.Now;
            foreach (var id in ids)
            {
                var child = await _unitOfWork.Children.GetByIdAsync(id);
                if (child != null)
                {
                    child.Status = ChildStatus.Inactive;
                    child.DeactivationDate = now;

                    await _unitOfWork.GroupLogs.AddAsync(new GroupLog
                    {
                        GroupId = child.GroupId,
                        ChildId = child.Id,
                        ActionType = GroupLogActionType.ChildRemoved,
                        Message = $"Uşaq çıxarıldı: {child.FirstName} {child.LastName}",
                        ActionDate = now
                    });

                    await _unitOfWork.Children.UpdateAsync(child);
                    await ApplyExitMonthPaymentAsync(child, now);
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
                    child.DeactivationDate = null;

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
