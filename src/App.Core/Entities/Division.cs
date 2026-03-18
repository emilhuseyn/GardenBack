using App.Core.Entities.Commons;

namespace App.Core.Entities
{
    public class Division : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<Group> Groups { get; set; } = new List<Group>();
    }
}
