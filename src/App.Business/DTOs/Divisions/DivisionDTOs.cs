namespace App.Business.DTOs.Divisions
{
    public class CreateDivisionRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateDivisionRequest
    {
        public string? Name { get; set; }
        public string? Language { get; set; }
        public string? Description { get; set; }
    }

    public class DivisionResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int GroupCount { get; set; }
    }
}
