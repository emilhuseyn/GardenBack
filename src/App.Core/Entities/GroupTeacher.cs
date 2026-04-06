using App.Core.Entities.Identity;

namespace App.Core.Entities
{
    /// <summary>
    /// Junction table between Group and User (Teacher).
    /// One group can have multiple teachers.
    /// </summary>
    public class GroupTeacher
    {
        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public DateTime AssignedAt { get; set; }
    }
}
