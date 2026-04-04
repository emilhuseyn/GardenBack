namespace App.Business.DTOs.Groups
{
    public class CreateGroupRequest
    {
        public string Name { get; set; } = string.Empty;
        public int DivisionId { get; set; }
        public string TeacherId { get; set; } = string.Empty;
        public int MaxChildCount { get; set; }
        public string AgeCategory { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }

    public class UpdateGroupRequest
    {
        public string? Name { get; set; }
        public int? DivisionId { get; set; }
        public string? TeacherId { get; set; }
        public int? MaxChildCount { get; set; }
        public string? AgeCategory { get; set; }
        public string? Language { get; set; }
    }

    public class GroupResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DivisionId { get; set; }
        public string DivisionName { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public int MaxChildCount { get; set; }
        public string AgeCategory { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public int CurrentChildCount { get; set; }
    }

    public class GroupDetailResponse : GroupResponse
    {
        public List<GroupChildItem> Children { get; set; } = new();
    }

    public class GroupChildItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ScheduleType { get; set; } = string.Empty;
    }

    public class AssignTeacherRequest
    {
        public string TeacherId { get; set; } = string.Empty;
    }

    public class GroupLogResponse
    {
        public int Id { get; set; }
        public int? GroupId { get; set; }
        public int? ChildId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; }
    }
}
