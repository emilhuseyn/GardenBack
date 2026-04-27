using App.Core.Entities.Commons;

namespace App.Core.Entities
{
    public class HikvisionSyncLog : BaseEntity
    {
        public DateOnly SyncDate { get; set; }
        public DateTime SyncTime { get; set; }
        public int SyncedCount { get; set; }
        public int SkippedCount { get; set; }
        public bool IsManual { get; set; }
        public string? TriggeredBy { get; set; }
        public string? Details { get; set; }
    }
}
