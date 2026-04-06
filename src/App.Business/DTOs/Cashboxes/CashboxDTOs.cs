namespace App.Business.DTOs.Cashboxes
{
    public class SetOpeningBalanceRequest
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal OpeningBalance { get; set; }
    }

    public class CashboxMonthlyBalanceResponse
    {
        public int CashboxId { get; set; }
        public string CashboxName { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpense { get; set; }
        public decimal TotalBalance { get; set; }
    }

    public class CashboxOperationRequest
    {
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public DateTime? OperationDate { get; set; }
    }

    public class CashboxOperationResponse
    {
        public int Id { get; set; }
        public int CashboxId { get; set; }
        public string CashboxName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public DateTime OperationDate { get; set; }
    }


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
