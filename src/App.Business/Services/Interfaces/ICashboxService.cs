using App.Business.DTOs.Cashboxes;

namespace App.Business.Services.Interfaces
{
    public interface ICashboxService
    {
        Task<CashboxResponse> CreateCashboxAsync(CreateCashboxRequest dto);
        Task<CashboxResponse> UpdateCashboxAsync(int id, UpdateCashboxRequest dto);
        Task<IEnumerable<CashboxResponse>> GetAllCashboxesAsync(bool onlyActive = false);
        Task<CashboxResponse> GetCashboxByIdAsync(int id);
        Task DeactivateCashboxAsync(int id);
        Task<CashboxMonthlyBalanceResponse> SetOpeningBalanceAsync(int cashboxId, SetOpeningBalanceRequest dto);
        Task<CashboxMonthlyBalanceResponse> GetMonthlyBalanceAsync(int cashboxId, int month, int year);
        Task<IEnumerable<CashboxMonthlyBalanceResponse>> GetAllMonthlyBalancesAsync(int cashboxId);
        Task<CashboxOperationResponse> AddIncomeAsync(int cashboxId, CashboxOperationRequest dto);
        Task<CashboxOperationResponse> AddExpenseAsync(int cashboxId, CashboxOperationRequest dto);
        Task<IEnumerable<CashboxOperationResponse>> GetOperationsAsync(int cashboxId, int? month = null, int? year = null);
        Task<CashboxTransferResponse> TransferAsync(CashboxTransferRequest dto);
        Task<IEnumerable<CashboxTransferHistoryResponse>> GetTransferHistoryAsync(int? cashboxId = null);
    }
}
