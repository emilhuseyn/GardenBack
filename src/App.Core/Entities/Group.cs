using App.Core.Entities.Commons;
using App.Core.Entities.Identity;

namespace App.Core.Entities
{
    public class Group : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public int DivisionId { get; set; }
        public string TeacherId { get; set; } = string.Empty;
        public int MaxChildCount { get; set; }
        public string AgeCategory { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;

        public Division Division { get; set; } = null!;
        public User Teacher { get; set; } = null!;
        public ICollection<Child> Children { get; set; } = new List<Child>();
    }
}
