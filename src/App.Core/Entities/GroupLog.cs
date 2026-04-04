using App.Core.Entities.Commons;
using App.Core.Enums;

namespace App.Core.Entities
{
    public class GroupLog : BaseEntity
    {
        public int? GroupId { get; set; }
        public int? ChildId { get; set; }
        public GroupLogActionType ActionType { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    }
}
