namespace App.Business.DTOs.Cashboxes
{
    public class CreateCashboxRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? AccountNumber { get; set; }
    }

    public class UpdateCashboxRequest
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? AccountNumber { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CashboxResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? AccountNumber { get; set; }
        public bool IsActive { get; set; }
        public decimal Balance { get; set; }
    }
}
