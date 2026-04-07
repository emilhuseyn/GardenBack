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
        private readonly IDateTimeService _dt;

        public CashboxService(IUnitOfWork unitOfWork, IMapper mapper, IDateTimeService dt)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _dt = dt;
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

            var list = cashboxes.ToList();
            var result = new List<CashboxResponse>();

            foreach (var c in list)
            {
                var operations = await _unitOfWork.CashboxOperations.GetByCashboxAsync(c.Id);
                var dto = _mapper.Map<CashboxResponse>(c);
                dto.Balance = CalculateCashboxBalance(c.Payments, operations);
                result.Add(dto);
            }

            return result;
        }

        public async Task<CashboxResponse> GetCashboxByIdAsync(int id)
        {
            var cashbox = await _unitOfWork.Cashboxes.GetByIdWithPaymentsAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li kassa tapılmadı.");

            var operations = await _unitOfWork.CashboxOperations.GetByCashboxAsync(id);
            var dto = _mapper.Map<CashboxResponse>(cashbox);
            dto.Balance = CalculateCashboxBalance(cashbox.Payments, operations);
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

            var monthlyOperations = await _unitOfWork.CashboxOperations.GetByCashboxAndMonthAsync(cashboxId, dto.Month, dto.Year);
            var monthlyManualIncome = monthlyOperations
                .Where(x => x.Type == CashboxOperationType.Income)
                .Sum(x => x.Amount);
            var monthlyExpense = monthlyOperations
                .Where(x => x.Type == CashboxOperationType.Expense)
                .Sum(x => x.Amount);

            return new CashboxMonthlyBalanceResponse
            {
                CashboxId     = cashboxId,
                CashboxName   = cashbox.Name,
                Month         = dto.Month,
                Year          = dto.Year,
                OpeningBalance = dto.OpeningBalance,
                MonthlyIncome  = monthlyIncome + monthlyManualIncome,
                MonthlyExpense = monthlyExpense,
                TotalBalance   = dto.OpeningBalance + monthlyIncome + monthlyManualIncome - monthlyExpense
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

            var monthlyOperations = await _unitOfWork.CashboxOperations.GetByCashboxAndMonthAsync(cashboxId, month, year);
            var monthlyManualIncome = monthlyOperations
                .Where(x => x.Type == CashboxOperationType.Income)
                .Sum(x => x.Amount);
            var monthlyExpense = monthlyOperations
                .Where(x => x.Type == CashboxOperationType.Expense)
                .Sum(x => x.Amount);

            return new CashboxMonthlyBalanceResponse
            {
                CashboxId      = cashboxId,
                CashboxName    = cashbox.Name,
                Month          = month,
                Year           = year,
                OpeningBalance = openingBalance,
                MonthlyIncome  = monthlyIncome + monthlyManualIncome,
                MonthlyExpense = monthlyExpense,
                TotalBalance   = openingBalance + monthlyIncome + monthlyManualIncome - monthlyExpense
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

            var allOperations = await _unitOfWork.CashboxOperations.GetByCashboxAsync(cashboxId);

            return allMonths.Select(key =>
            {
                var openingBalance = balanceDict.TryGetValue(key, out var b) ? b.OpeningBalance : 0;
                var monthlyIncome  = cashbox.Payments
                    .Where(p => p.CreatedAt.Month == key.Month && p.CreatedAt.Year == key.Year)
                    .Sum(p => p.PaidAmount);

                var monthlyOps = allOperations
                    .Where(x => x.OperationDate.Month == key.Month && x.OperationDate.Year == key.Year)
                    .ToList();

                var monthlyManualIncome = monthlyOps
                    .Where(x => x.Type == CashboxOperationType.Income)
                    .Sum(x => x.Amount);

                var monthlyExpense = monthlyOps
                    .Where(x => x.Type == CashboxOperationType.Expense)
                    .Sum(x => x.Amount);

                return new CashboxMonthlyBalanceResponse
                {
                    CashboxId      = cashboxId,
                    CashboxName    = cashbox.Name,
                    Month          = key.Month,
                    Year           = key.Year,
                    OpeningBalance = openingBalance,
                    MonthlyIncome  = monthlyIncome + monthlyManualIncome,
                    MonthlyExpense = monthlyExpense,
                    TotalBalance   = openingBalance + monthlyIncome + monthlyManualIncome - monthlyExpense
                };
            });
        }

        public async Task<CashboxOperationResponse> AddIncomeAsync(int cashboxId, CashboxOperationRequest dto)
        {
            return await AddOperationAsync(cashboxId, dto, CashboxOperationType.Income);
        }

        public async Task<CashboxOperationResponse> AddExpenseAsync(int cashboxId, CashboxOperationRequest dto)
        {
            return await AddOperationAsync(cashboxId, dto, CashboxOperationType.Expense);
        }

        public async Task<IEnumerable<CashboxOperationResponse>> GetOperationsAsync(int cashboxId, int? month = null, int? year = null)
        {
            var cashbox = await _unitOfWork.Cashboxes.GetByIdAsync(cashboxId)
                ?? throw new EntityNotFoundException($"{cashboxId} ID-li kassa tapılmadı.");

            IEnumerable<CashboxOperation> operations;

            if (month.HasValue && year.HasValue)
                operations = await _unitOfWork.CashboxOperations.GetByCashboxAndMonthAsync(cashboxId, month.Value, year.Value);
            else
                operations = await _unitOfWork.CashboxOperations.GetByCashboxAsync(cashboxId);

            return operations.Select(x => new CashboxOperationResponse
            {
                Id = x.Id,
                CashboxId = x.CashboxId,
                CashboxName = cashbox.Name,
                Type = x.Type.ToString(),
                Amount = x.Amount,
                Note = x.Note,
                OperationDate = x.OperationDate
            });
        }

        private async Task<CashboxOperationResponse> AddOperationAsync(int cashboxId, CashboxOperationRequest dto, CashboxOperationType type)
        {
            var cashbox = await _unitOfWork.Cashboxes.GetByIdAsync(cashboxId)
                ?? throw new EntityNotFoundException($"{cashboxId} ID-li kassa tapılmadı.");

            if (!cashbox.IsActive)
                throw new ValidationException("Deaktiv kassada əməliyyat aparmaq olmaz.");

            var operation = new CashboxOperation
            {
                CashboxId = cashboxId,
                Type = type,
                Amount = dto.Amount,
                Note = dto.Note,
                OperationDate = dto.OperationDate ?? _dt.Now
            };

            await _unitOfWork.CashboxOperations.AddAsync(operation);
            await _unitOfWork.SaveChangesAsync();

            return new CashboxOperationResponse
            {
                Id = operation.Id,
                CashboxId = operation.CashboxId,
                CashboxName = cashbox.Name,
                Type = operation.Type.ToString(),
                Amount = operation.Amount,
                Note = operation.Note,
                OperationDate = operation.OperationDate
            };
        }

        public async Task<CashboxTransferResponse> TransferAsync(CashboxTransferRequest dto)
        {
            if (dto.FromCashboxId == dto.ToCashboxId)
                throw new ValidationException("Eyni kassaya köçürmə edilə bilməz.");

            if (dto.Amount <= 0)
                throw new ValidationException("Köçürmə məbləği sıfırdan böyük olmalıdır.");

            var fromCashbox = await _unitOfWork.Cashboxes.GetByIdWithPaymentsAsync(dto.FromCashboxId)
                ?? throw new EntityNotFoundException($"{dto.FromCashboxId} ID-li kassa tapılmadı.");

            var toCashbox = await _unitOfWork.Cashboxes.GetByIdWithPaymentsAsync(dto.ToCashboxId)
                ?? throw new EntityNotFoundException($"{dto.ToCashboxId} ID-li kassa tapılmadı.");

            if (!fromCashbox.IsActive)
                throw new ValidationException($"'{fromCashbox.Name}' kassası deaktivdir.");

            if (!toCashbox.IsActive)
                throw new ValidationException($"'{toCashbox.Name}' kassası deaktivdir.");

            // Göndərən kassanın cari balansını hesabla
            var fromOperations = await _unitOfWork.CashboxOperations.GetByCashboxAsync(dto.FromCashboxId);
            var fromBalance = CalculateCashboxBalance(fromCashbox.Payments, fromOperations);

            if (dto.Amount > fromBalance)
                throw new ValidationException(
                    $"'{fromCashbox.Name}' kassasında kifayət qədər vəsait yoxdur. " +
                    $"Mövcud balans: {fromBalance:N2} ₼, tələb olunan: {dto.Amount:N2} ₼.");

            var now = _dt.Now;
            var transferNote = dto.Note ?? $"Kassalar arası köçürmə: {fromCashbox.Name} → {toCashbox.Name}";

            // Göndərən kassadan məxaric
            var expense = new CashboxOperation
            {
                CashboxId     = dto.FromCashboxId,
                Type          = CashboxOperationType.Expense,
                Amount        = dto.Amount,
                Note          = transferNote,
                OperationDate = now
            };
            await _unitOfWork.CashboxOperations.AddAsync(expense);

            // Qəbul edən kassaya mədaxil
            var income = new CashboxOperation
            {
                CashboxId     = dto.ToCashboxId,
                Type          = CashboxOperationType.Income,
                Amount        = dto.Amount,
                Note          = transferNote,
                OperationDate = now
            };
            await _unitOfWork.CashboxOperations.AddAsync(income);

            // Köçürmə tarixçəsi
            await _unitOfWork.CashboxTransfers.AddAsync(new CashboxTransfer
            {
                FromCashboxId = dto.FromCashboxId,
                ToCashboxId   = dto.ToCashboxId,
                Amount        = dto.Amount,
                Note          = dto.Note,
                TransferDate  = now
            });

            await _unitOfWork.SaveChangesAsync();

            // Yeni balansları hesabla
            var toOperations = await _unitOfWork.CashboxOperations.GetByCashboxAsync(dto.ToCashboxId);
            var toUpdatedCashbox = await _unitOfWork.Cashboxes.GetByIdWithPaymentsAsync(dto.ToCashboxId);

            var fromAllOps = await _unitOfWork.CashboxOperations.GetByCashboxAsync(dto.FromCashboxId);
            var fromUpdatedCashbox = await _unitOfWork.Cashboxes.GetByIdWithPaymentsAsync(dto.FromCashboxId);

            return new CashboxTransferResponse
            {
                FromCashboxName        = fromCashbox.Name,
                ToCashboxName          = toCashbox.Name,
                Amount                 = dto.Amount,
                FromCashboxBalanceAfter = CalculateCashboxBalance(fromUpdatedCashbox!.Payments, fromAllOps),
                ToCashboxBalanceAfter  = CalculateCashboxBalance(toUpdatedCashbox!.Payments, toOperations)
            };
        }

        public async Task<IEnumerable<CashboxTransferHistoryResponse>> GetTransferHistoryAsync(int? cashboxId = null)
        {
            var transfers = await _unitOfWork.CashboxTransfers.GetAllAsync(cashboxId);
            return transfers.Select(t => new CashboxTransferHistoryResponse
            {
                Id               = t.Id,
                FromCashboxId    = t.FromCashboxId,
                FromCashboxName  = t.FromCashbox.Name,
                ToCashboxId      = t.ToCashboxId,
                ToCashboxName    = t.ToCashbox.Name,
                Amount           = t.Amount,
                Note             = t.Note,
                TransferDate     = t.TransferDate
            });
        }

        private static decimal CalculateCashboxBalance(IEnumerable<Payment> payments, IEnumerable<CashboxOperation> operations)
        {
            var paymentIncome = payments.Sum(p => p.PaidAmount);
            var operationIncome = operations.Where(x => x.Type == CashboxOperationType.Income).Sum(x => x.Amount);
            var operationExpense = operations.Where(x => x.Type == CashboxOperationType.Expense).Sum(x => x.Amount);

            return paymentIncome + operationIncome - operationExpense;
        }
    }
}
