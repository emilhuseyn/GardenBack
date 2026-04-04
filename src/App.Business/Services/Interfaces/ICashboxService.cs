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
    }
}
