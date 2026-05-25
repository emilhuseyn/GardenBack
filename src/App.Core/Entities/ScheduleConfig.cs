using App.Core.Entities.Commons;
using App.Core.Entities.Identity;

namespace App.Core.Entities
{
    public class ScheduleConfig : BaseEntity
    {
        /// <summary>
        /// Stabilliyi olan unikal kod (məs. "FullDay", "HalfDay", "Evening").
        /// Bu Child.ScheduleType sütununda saxlanır və filtrlərdə açar kimi istifadə olunur.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>UI-da göstəriləcək ad (məs. "Tam gün", "Yarım gün", "Axşam qrupu").</summary>
        public string Name { get; set; } = string.Empty;

        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        /// <summary>
        /// Soft-deactivation — keçmiş uşaqlar bu kodla qalır,
        /// amma yeni uşaq seçimi/filter siyahısında görsənmir.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public string UpdatedById { get; set; } = string.Empty;
        public User UpdatedBy { get; set; } = null!;
    }
}
