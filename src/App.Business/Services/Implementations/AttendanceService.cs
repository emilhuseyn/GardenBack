using App.Business.DTOs.Attendance;
using App.Business.Services.Interfaces;
using App.Core.Entities;
using App.Core.Enums;
using App.Core.Exceptions;
using App.Core.Exceptions.Commons;
using App.Core.Services;
using App.DAL.UnitOfWork;
using App.Shared.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace App.Business.Services.Implementations
{
    /// <summary>
    /// Handles attendance tracking operations.
    /// </summary>
    public class AttendanceService : IAttendanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDateTimeService _dt;
        private readonly IClaimService _claimService;

        public AttendanceService(IUnitOfWork unitOfWork, IMapper mapper, IDateTimeService dt, IClaimService claimService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _dt = dt;
            _claimService = claimService;
        }

        /// <summary>
        /// Marks attendance for a single child.
        /// </summary>
        public async Task<AttendanceResponse> MarkAttendanceAsync(MarkAttendanceRequest dto, string recordedById)
        {
            if (string.IsNullOrEmpty(recordedById))
                throw new Core.Exceptions.ValidationException(new[] { "Qeydiyyatçı ID mövcud deyil." });

            var child = await _unitOfWork.Children.GetByIdAsync(dto.ChildId)
                ?? throw new EntityNotFoundException($"{dto.ChildId} ID-li uşaq tapılmadı.");

            // Müəllim yalnız öz qrupunda davamiyyət yaza bilər
            var role = _claimService.GetUserRole();
            if (role == "Teacher")
            {
                var userId = _claimService.GetUserId();
                var group = await _unitOfWork.Groups.GetByIdAsync(child.GroupId)
                    ?? throw new EntityNotFoundException("Qrup tapılmadı.");

                var isPrimary  = group.TeacherId == userId;
                var isAssigned = await _unitOfWork.GroupTeachers.GetAsync(child.GroupId, userId) != null;

                if (!isPrimary && !isAssigned)
                    throw new UnauthorizedException("Bu qrupun davamiyyətini yazmaq üçün icazəniz yoxdur.");
            }

            _ = child; // already validated above

            var existing = (await _unitOfWork.Attendances
                .FindAsync(a => a.ChildId == dto.ChildId && a.Date == dto.Date))
                .FirstOrDefault();

            if (existing != null)
            {
                existing.Status = dto.Status;
                existing.ArrivalTime = dto.ArrivalTime;
                existing.DepartureTime = dto.DepartureTime;
                existing.Notes = dto.Notes;
                await _unitOfWork.Attendances.UpdateAsync(existing);
            }
            else
            {
                var attendance = _mapper.Map<Attendance>(dto);
                attendance.RecordedById = recordedById;
                await _unitOfWork.Attendances.AddAsync(attendance);
            }

            await _unitOfWork.SaveChangesAsync();

            var result = (await _unitOfWork.Attendances.FindAsync(
                a => a.ChildId == dto.ChildId && a.Date == dto.Date)).FirstOrDefault();

            if (result == null)
                throw new EntityNotFoundException($"Davamiyyət qeydi yaradıla bilmədi.");

            var response = await _unitOfWork.Attendances.GetAllAsync(
                a => a.Id == result.Id,
                a => a.Child,
                a => a.Child.Group);

            return _mapper.Map<AttendanceResponse>(response.FirstOrDefault());
        }

        /// <summary>
        /// Records arrival time for a child.
        /// </summary>
        public async Task<AttendanceResponse> MarkArrivalAsync(int attendanceId, TimeOnly arrivalTime)
        {
            var attendance = await _unitOfWork.Attendances.GetByIdAsync(
                a => a.Id == attendanceId,
                a => a.Child,
                a => a.Child.Group)
                ?? throw new EntityNotFoundException($"{attendanceId} ID-li davamiyyət qeydi tapılmadı.");

            attendance.ArrivalTime = arrivalTime;
            attendance.Status = AttendanceStatus.Present;
            await _unitOfWork.Attendances.UpdateAsync(attendance);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AttendanceResponse>(attendance);
        }

        /// <summary>
        /// Records departure time for a child.
        /// </summary>
        public async Task<AttendanceResponse> MarkDepartureAsync(int attendanceId, TimeOnly departureTime)
        {
            var attendance = await _unitOfWork.Attendances.GetByIdAsync(
                a => a.Id == attendanceId,
                a => a.Child,
                a => a.Child.Group)
                ?? throw new EntityNotFoundException($"{attendanceId} ID-li davamiyyət qeydi tapılmadı.");

            attendance.DepartureTime = departureTime;
            await _unitOfWork.Attendances.UpdateAsync(attendance);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AttendanceResponse>(attendance);
        }

        /// <summary>
        /// Gets the daily attendance report, optionally filtered by group.
        /// </summary>
        public async Task<DailyAttendanceReport> GetDailyReportAsync(DateOnly date, int? groupId)
        {
            var attendances = groupId.HasValue
                ? await _unitOfWork.Attendances.GetGroupAttendanceAsync(groupId.Value, date)
                : await _unitOfWork.Attendances.GetDailyAttendanceAsync(date);

            var entries = _mapper.Map<List<AttendanceResponse>>(attendances);

            return new DailyAttendanceReport
            {
                Date = date,
                TotalChildren = entries.Count,
                PresentCount = entries.Count(e => e.Status == AttendanceStatus.Present),
                AbsentCount = entries.Count(e => e.Status == AttendanceStatus.Absent),
                LateCount = entries.Count(e => e.IsLate),
                Entries = entries
            };
        }

        /// <summary>
        /// Gets the monthly attendance report, optionally filtered by group.
        /// </summary>
        public async Task<MonthlyAttendanceReport> GetMonthlyReportAsync(int month, int year, int? groupId)
        {
            var attendances = await _unitOfWork.Attendances.GetMonthlyAttendanceAsync(month, year);

            if (groupId.HasValue)
                attendances = attendances.Where(a => a.Child.GroupId == groupId.Value);

            var grouped = attendances.GroupBy(a => a.ChildId);
            var children = new List<ChildMonthlyAttendanceSummary>();

            foreach (var group in grouped)
            {
                var first = group.First();
                children.Add(new ChildMonthlyAttendanceSummary
                {
                    ChildId = group.Key,
                    ChildFullName = $"{first.Child.FirstName} {first.Child.LastName}",
                    GroupName = first.Child.Group?.Name ?? string.Empty,
                    PresentDays = group.Count(a => a.Status == AttendanceStatus.Present),
                    AbsentDays = group.Count(a => a.Status == AttendanceStatus.Absent),
                    LateDays = group.Count(a => a.IsLate),
                    EarlyLeaveDays = group.Count(a => a.IsEarlyLeave)
                });
            }

            return new MonthlyAttendanceReport
            {
                Month = month,
                Year = year,
                TotalWorkDays = attendances.Select(a => a.Date).Distinct().Count(),
                Children = children
            };
        }

        /// <summary>
        /// Gets attendance records for a specific child in a date range.
        /// </summary>
        public async Task<IEnumerable<AttendanceResponse>> GetChildAttendanceAsync(int childId, DateOnly from, DateOnly to)
        {
            var attendances = await _unitOfWork.Attendances.GetChildAttendanceAsync(childId, from, to);
            return _mapper.Map<IEnumerable<AttendanceResponse>>(attendances);
        }

        /// <summary>
        /// Auto-detects late arrivals and early leaves based on ScheduleConfig.
        /// </summary>
        public async Task AutoDetectLateAndEarlyLeave()
        {
            var today = DateOnly.FromDateTime(_dt.Now);
            var attendances = await _unitOfWork.Attendances.GetDailyAttendanceAsync(today);

            var fullDayConfig = await _unitOfWork.ScheduleConfigs.GetByScheduleTypeAsync(ScheduleType.FullDay);
            var halfDayConfig = await _unitOfWork.ScheduleConfigs.GetByScheduleTypeAsync(ScheduleType.HalfDay);

            foreach (var attendance in attendances.Where(a => a.Status == AttendanceStatus.Present))
            {
                var child = attendance.Child;
                var config = child.ScheduleType == ScheduleType.FullDay ? fullDayConfig : halfDayConfig;

                if (config == null) continue;

                if (attendance.ArrivalTime.HasValue && attendance.ArrivalTime.Value > config.StartTime)
                    attendance.IsLate = true;

                if (attendance.DepartureTime.HasValue && attendance.DepartureTime.Value < config.EndTime)
                    attendance.IsEarlyLeave = true;

                await _unitOfWork.Attendances.UpdateAsync(attendance);
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
