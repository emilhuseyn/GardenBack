using App.Business.DTOs.Cashboxes;
using App.Business.Services.Interfaces;
using App.Core.Entities;
using App.Core.Enums;
using App.Core.Services;
using App.Core.Exceptions;
using App.Core.Exceptions.Commons;
using App.DAL.UnitOfWork;
using AutoMapper;

namespace App.Business.Services.Implementations
{
    public class CashboxService : ICashboxService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CashboxService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CashboxResponse> CreateCashboxAsync(CreateCashboxRequest dto)
        {
            if (!Enum.TryParse<CashboxType>(dto.Type, true, out var type))
                throw new ValidationException("Kassa tipi yanlışdır.");

            var exists = (await _unitOfWork.Cashboxes.FindAsync(x => x.Name == dto.Name)).Any();
            if (exists)
                throw new ConflictException("Bu adda kassa artıq mövcuddur.");

            var cashbox = new Cashbox
            {
                Name = dto.Name,
                Type = type,
                AccountNumber = dto.AccountNumber,
                IsActive = true
            };

            await _unitOfWork.Cashboxes.AddAsync(cashbox);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashboxResponse>(cashbox);
        }

        public async Task<CashboxResponse> UpdateCashboxAsync(int id, UpdateCashboxRequest dto)
        {
            var cashbox = await _unitOfWork.Cashboxes.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li kassa tapılmadı.");

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                var duplicate = (await _unitOfWork.Cashboxes.FindAsync(x => x.Name == dto.Name && x.Id != id)).Any();
                if (duplicate)
                    throw new ConflictException("Bu adda başqa kassa mövcuddur.");

                cashbox.Name = dto.Name;
            }

            if (!string.IsNullOrWhiteSpace(dto.Type))
            {
                if (!Enum.TryParse<CashboxType>(dto.Type, true, out var type))
                    throw new ValidationException("Kassa tipi yanlışdır.");
                cashbox.Type = type;
            }

            if (dto.AccountNumber != null)
                cashbox.AccountNumber = dto.AccountNumber;

            if (dto.IsActive.HasValue)
                cashbox.IsActive = dto.IsActive.Value;

            await _unitOfWork.Cashboxes.UpdateAsync(cashbox);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashboxResponse>(cashbox);
        }

        public async Task<IEnumerable<CashboxResponse>> GetAllCashboxesAsync(bool onlyActive = false)
        {
            var cashboxes = await _unitOfWork.Cashboxes.GetAllWithPaymentsAsync();

            if (onlyActive)
                cashboxes = cashboxes.Where(x => x.IsActive);

            return cashboxes.Select(c =>
            {
                var dto = _mapper.Map<CashboxResponse>(c);
                dto.Balance = c.Payments.Sum(p => p.PaidAmount);
                return dto;
            });
        }

        public async Task<CashboxResponse> GetCashboxByIdAsync(int id)
        {
            var cashbox = await _unitOfWork.Cashboxes.GetByIdWithPaymentsAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li kassa tapılmadı.");

            var dto = _mapper.Map<CashboxResponse>(cashbox);
            dto.Balance = cashbox.Payments.Sum(p => p.PaidAmount);
            return dto;
        }

        public async Task DeactivateCashboxAsync(int id)
        {
            var cashbox = await _unitOfWork.Cashboxes.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li kassa tapılmadı.");

            cashbox.IsActive = false;
            await _unitOfWork.Cashboxes.UpdateAsync(cashbox);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<CashboxMonthlyBalanceResponse> SetOpeningBalanceAsync(int cashboxId, SetOpeningBalanceRequest dto)
        {
            var cashbox = await _unitOfWork.Cashboxes.GetByIdWithPaymentsAsync(cashboxId)
                ?? throw new EntityNotFoundException($"{cashboxId} ID-li kassa tapılmadı.");

            var existing = await _unitOfWork.CashboxBalances.GetAsync(cashboxId, dto.Month, dto.Year);

            if (existing != null)
            {
                existing.OpeningBalance = dto.OpeningBalance;
                await _unitOfWork.CashboxBalances.UpdateAsync(existing);
            }
            else
            {
                await _unitOfWork.CashboxBalances.AddAsync(new CashboxMonthlyBalance
                {
                    CashboxId      = cashboxId,
                    Month          = dto.Month,
                    Year           = dto.Year,
                    OpeningBalance = dto.OpeningBalance
                });
            }

            await _unitOfWork.SaveChangesAsync();

            var monthlyIncome = cashbox.Payments
                .Where(p => p.CreatedAt.Month == dto.Month && p.CreatedAt.Year == dto.Year)
                .Sum(p => p.PaidAmount);

            return new CashboxMonthlyBalanceResponse
            {
                CashboxId     = cashboxId,
                CashboxName   = cashbox.Name,
                Month         = dto.Month,
                Year          = dto.Year,
                OpeningBalance = dto.OpeningBalance,
                MonthlyIncome  = monthlyIncome,
                TotalBalance   = dto.OpeningBalance + monthlyIncome
            };
        }

        public async Task<CashboxMonthlyBalanceResponse> GetMonthlyBalanceAsync(int cashboxId, int month, int year)
        {
            var cashbox = await _unitOfWork.Cashboxes.GetByIdWithPaymentsAsync(cashboxId)
                ?? throw new EntityNotFoundException($"{cashboxId} ID-li kassa tapılmadı.");

            var balance = await _unitOfWork.CashboxBalances.GetAsync(cashboxId, month, year);
            var openingBalance = balance?.OpeningBalance ?? 0;

            var monthlyIncome = cashbox.Payments
                .Where(p => p.CreatedAt.Month == month && p.CreatedAt.Year == year)
                .Sum(p => p.PaidAmount);

            return new CashboxMonthlyBalanceResponse
            {
                CashboxId      = cashboxId,
                CashboxName    = cashbox.Name,
                Month          = month,
                Year           = year,
                OpeningBalance = openingBalance,
                MonthlyIncome  = monthlyIncome,
                TotalBalance   = openingBalance + monthlyIncome
            };
        }

        public async Task<IEnumerable<CashboxMonthlyBalanceResponse>> GetAllMonthlyBalancesAsync(int cashboxId)
        {
            var cashbox = await _unitOfWork.Cashboxes.GetByIdWithPaymentsAsync(cashboxId)
                ?? throw new EntityNotFoundException($"{cashboxId} ID-li kassa tapılmadı.");

            var balances = await _unitOfWork.CashboxBalances.GetByCashboxAsync(cashboxId);
            var balanceDict = balances.ToDictionary(b => (b.Month, b.Year));

            // Ödəniş olan aylar + açılış qalığı yazılmış ayları birləşdir
            var paymentMonths = cashbox.Payments
                .Select(p => (p.CreatedAt.Month, p.CreatedAt.Year))
                .Distinct();

            var allMonths = paymentMonths
                .Union(balanceDict.Keys)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month);

            return allMonths.Select(key =>
            {
                var openingBalance = balanceDict.TryGetValue(key, out var b) ? b.OpeningBalance : 0;
                var monthlyIncome  = cashbox.Payments
                    .Where(p => p.CreatedAt.Month == key.Month && p.CreatedAt.Year == key.Year)
                    .Sum(p => p.PaidAmount);

                return new CashboxMonthlyBalanceResponse
                {
                    CashboxId      = cashboxId,
                    CashboxName    = cashbox.Name,
                    Month          = key.Month,
                    Year           = key.Year,
                    OpeningBalance = openingBalance,
                    MonthlyIncome  = monthlyIncome,
                    TotalBalance   = openingBalance + monthlyIncome
                };
            });
        }
    }
}
