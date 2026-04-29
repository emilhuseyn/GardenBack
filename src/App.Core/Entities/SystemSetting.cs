using App.Core.Entities.Commons;

namespace App.Core.Entities
{
    public class SystemSetting : BaseEntity
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
