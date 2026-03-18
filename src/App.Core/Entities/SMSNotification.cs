using App.Core.Entities.Commons;

namespace App.Core.Entities
{
    public class SMSNotification : BaseEntity
    {
        public string RecipientPhone { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsSuccessful { get; set; }
        public int? ChildId { get; set; }

        public Child? Child { get; set; }
    }
}
