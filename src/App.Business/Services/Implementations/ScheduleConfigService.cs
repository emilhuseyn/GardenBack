using App.Business.DTOs.Schedule;
using App.Business.Services.Interfaces;
using App.Core.Entities;
using App.Core.Exceptions.Commons;
using App.DAL.UnitOfWork;
using AutoMapper;
using System.Text.RegularExpressions;

namespace App.Business.Services.Implementations
{
    /// <summary>
    /// Handles schedule configuration CRUD.
    /// </summary>
    public class ScheduleConfigService : IScheduleConfigService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private static readonly Regex CodeRegex = new("^[A-Za-z][A-Za-z0-9_-]*$", RegexOptions.Compiled);

        public ScheduleConfigService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ScheduleConfigResponse>> GetAllConfigsAsync(bool includeInactive = false)
        {
            var configs = await _unitOfWork.ScheduleConfigs.GetAllAsync();
            var filtered = includeInactive ? configs : configs.Where(c => c.IsActive);
            return _mapper.Map<IEnumerable<ScheduleConfigResponse>>(filtered.OrderBy(c => c.Name));
        }

        public async Task<ScheduleConfigResponse> CreateScheduleAsync(CreateScheduleRequest dto, string userId)
        {
            ValidateCreate(dto);

            var existing = await _unitOfWork.ScheduleConfigs.GetByCodeAsync(dto.Code);
            if (existing != null)
                throw new Core.Exceptions.ValidationException($"\"{dto.Code}\" kodlu qrafik artıq mövcuddur.");

            var startTime = ParseTime(dto.StartTime, "Başlama vaxtı");
            var endTime = ParseTime(dto.EndTime, "Bitmə vaxtı");
            EnsureStartBeforeEnd(startTime, endTime);

            var config = new ScheduleConfig
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                StartTime = startTime,
                EndTime = endTime,
                IsActive = true,
                UpdatedById = userId
            };

            await _unitOfWork.ScheduleConfigs.AddAsync(config);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ScheduleConfigResponse>(config);
        }

        public async Task<ScheduleConfigResponse> UpdateScheduleAsync(int id, UpdateScheduleRequest dto, string userId)
        {
            var config = await _unitOfWork.ScheduleConfigs.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li qrafik konfiqurasiyası tapılmadı.");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                config.Name = dto.Name.Trim();

            if (!string.IsNullOrWhiteSpace(dto.StartTime))
                config.StartTime = ParseTime(dto.StartTime, "Başlama vaxtı");

            if (!string.IsNullOrWhiteSpace(dto.EndTime))
                config.EndTime = ParseTime(dto.EndTime, "Bitmə vaxtı");

            EnsureStartBeforeEnd(config.StartTime, config.EndTime);

            if (dto.IsActive.HasValue)
                config.IsActive = dto.IsActive.Value;

            config.UpdatedById = userId;

            await _unitOfWork.ScheduleConfigs.UpdateAsync(config);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ScheduleConfigResponse>(config);
        }

        /// <summary>
        /// Silmir — IsActive=false edir. Köhnə uşaqlar bu kodla qalsa belə,
        /// yeni seçim/filter siyahısında görsənmir.
        /// </summary>
        public async Task DeleteScheduleAsync(int id)
        {
            var config = await _unitOfWork.ScheduleConfigs.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li qrafik konfiqurasiyası tapılmadı.");

            // Hələ bu kodu istifadə edən uşaq varsa, hard-delete təhlükəlidir — sadəcə deaktiv et
            var inUse = (await _unitOfWork.Children.FindAsync(c => c.ScheduleType == config.Code)).Any();
            if (inUse)
            {
                config.IsActive = false;
                await _unitOfWork.ScheduleConfigs.UpdateAsync(config);
                await _unitOfWork.SaveChangesAsync();
                return;
            }

            await _unitOfWork.ScheduleConfigs.SoftDeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }

        // ── Köməkçilər ──────────────────────────────────────────────
        private static void ValidateCreate(CreateScheduleRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new Core.Exceptions.ValidationException("Kod boş ola bilməz.");
            if (!CodeRegex.IsMatch(dto.Code.Trim()))
                throw new Core.Exceptions.ValidationException("Kod yalnız latın hərfləri, rəqəm, '-' və '_' simvollarından ibarət ola bilər (məs. \"FullDay\", \"Evening_Group\").");
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Core.Exceptions.ValidationException("Ad boş ola bilməz.");
        }

        private static TimeOnly ParseTime(string value, string label)
        {
            if (!TimeOnly.TryParse(value, out var t))
                throw new Core.Exceptions.ValidationException($"{label} düzgün formatda deyil (HH:mm).");
            return t;
        }

        private static void EnsureStartBeforeEnd(TimeOnly start, TimeOnly end)
        {
            if (start >= end)
                throw new Core.Exceptions.ValidationException("Başlama vaxtı bitmə vaxtından əvvəl olmalıdır.");
        }
    }
}
