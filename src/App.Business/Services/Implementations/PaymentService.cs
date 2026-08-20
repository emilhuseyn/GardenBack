using App.Business.DTOs.Payments;
using App.Business.Services.Interfaces;
using App.Core.Common;
using App.Core.Entities;
using App.Core.Enums;
using App.Core.Exceptions.Commons;
using App.Core.Services;
using App.DAL.UnitOfWork;
using AutoMapper;
using Microsoft.Extensions.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace App.Business.Services.Implementations
{
    /// <summary>
    /// Handles payment and billing operations.
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IHostEnvironment _env;
        private readonly IDateTimeService _dt;

        public PaymentService(IUnitOfWork unitOfWork, IMapper mapper, INotificationService notificationService, IHostEnvironment env, IDateTimeService dt)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
            _env = env;
            _dt = dt;
        }

        /// <summary>Notes sütununun DB limiti (PaymentConfiguration).</summary>
        private const int NotesMaxLength = 500;

        /// <summary>Qeydi DB limitinə sığdırır — ən köhnə hissə kəsilir.</summary>
        private static string TrimNote(string value) =>
            value.Length <= NotesMaxLength ? value : value[^NotesMaxLength..];

        /// <summary>
        /// Qeydə yeni hissə ƏLAVƏ edir (köhnəni SİLMİR — F3). Eyni hissə təkrarlanmır,
        /// limit aşılarsa ən köhnə hissə kəsilir.
        /// Qeydlər tamamilə kosmetikdir: heç bir hesab məntiqi buradan oxumur.
        /// </summary>
        private static void AppendNote(Payment payment, string? note)
        {
            if (string.IsNullOrWhiteSpace(note)) return;

            if (string.IsNullOrWhiteSpace(payment.Notes))
            {
                payment.Notes = TrimNote(note);
                return;
            }

            if (payment.Notes.Contains(note, StringComparison.OrdinalIgnoreCase)) return;

            payment.Notes = TrimNote($"{payment.Notes} | {note}");
        }

        /// <summary>
        /// Generates debt records for the current month. Used by Hangfire to avoid
        /// capturing DateTime.Now at job-registration time.
        /// </summary>
        public Task GenerateCurrentMonthDebtsAsync()
        {
            var now = _dt.Now;
            return GenerateMonthlyDebtsAsync(now.Month, now.Year);
        }

        /// <summary>
        /// Generates monthly debt records for all active children.
        /// </summary>
        public async Task GenerateMonthlyDebtsAsync(int month, int year)
        {
            var activeChildren = await _unitOfWork.Children.GetActiveChildrenAsync();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var child in activeChildren)
                {
                    var existing = (await _unitOfWork.Payments
                        .FindAsync(p => p.ChildId == child.Id && p.Month == month && p.Year == year))
                        .FirstOrDefault();

                    if (existing != null) continue;

                    var discountPercent = child.DiscountPercentage ?? 0;
                    var hasDiscount = discountPercent > 0;

                    // Pro-rate for children who joined mid-month; bill in whole manats (no qəpik).
                    // C2: burada ÇIXIŞ tarixi iştirak etmir (yalnız aktiv uşaqlar üçün işləyir), ona görə
                    // düstur DƏYİŞMİR — yarım-açıq qaydada endExclusive = daysInMonth + 1 olduğu üçün
                    // daysInMonth + 1 - startDay ≡ daysInMonth - startDay + 1, PeriodEndDay isə eyni şəkildə daysInMonth.
                    var daysInMonth = DateTime.DaysInMonth(year, month);
                    var startDay = (child.RegistrationDate.Year == year && child.RegistrationDate.Month == month)
                        ? child.RegistrationDate.Day
                        : 1;
                    var daysActive = daysInMonth - startDay + 1;
                    var baseAmount = startDay == 1
                        ? child.MonthlyFee
                        : Math.Round(child.MonthlyFee * daysActive / daysInMonth, 0, MidpointRounding.AwayFromZero);

                    var rawFinal = hasDiscount
                        ? CalculateFinalAmount(baseAmount, DiscountType.Percentage, discountPercent)
                        : baseAmount;
                    var finalAmount = Math.Round(rawFinal, 0, MidpointRounding.AwayFromZero);

                    var payment = new Payment
                    {
                        ChildId = child.Id,
                        Month = month,
                        Year = year,
                        OriginalAmount = baseAmount,
                        FinalAmount = finalAmount,
                        PaidAmount = 0,
                        LastPaymentAmount = null,
                        // If fee is 0 (e.g. 100% discount) — no payment needed, mark Paid immediately
                        Status = finalAmount <= 0 ? PaymentStatus.Paid : PaymentStatus.Debt,
                        DiscountType = hasDiscount ? DiscountType.Percentage : DiscountType.None,
                        DiscountValue = hasDiscount ? discountPercent : 0,
                        // Dövr SÜTUNLARDA saxlanılır; qeyd yalnız ştabın oxuması üçündür (kosmetik).
                        PeriodStartDay = startDay,
                        PeriodEndDay = daysInMonth,
                        Notes = startDay > 1 ? $"Dövr: {startDay}-{daysInMonth} ({daysActive} gün)" : null,
                        RecordedById = "system"
                    };

                    await _unitOfWork.Payments.AddAsync(payment);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Bulk-marks all non-paid payments for the given month/year as fully paid.
        /// Returns the count of records updated.
        /// </summary>
        public async Task<int> BulkMarkAsPaidAsync(int month, int year, string userId)
        {
            var payments = (await _unitOfWork.Payments.GetMonthlyPaymentsAsync(month, year))
                .Where(p => p.Status != PaymentStatus.Paid)
                .ToList();

            if (payments.Count == 0) return 0;

            var now = _dt.Now;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var payment in payments)
                {
                    payment.PaidAmount = payment.FinalAmount;
                    payment.LastPaymentAmount = payment.FinalAmount;
                    payment.Status = PaymentStatus.Paid;
                    payment.PaymentDate = now;
                    payment.RecordedById = userId;
                    AppendNote(payment, "Kütləvi ödənilmiş (sistem köçürməsi)");

                    await _unitOfWork.Payments.UpdateAsync(payment);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return payments.Count;
        }

        /// <summary>
        /// Records a payment against a child's debt.
        /// If no payment record exists for the given month/year, one is created automatically.
        /// </summary>
        public async Task<PaymentResponse> RecordPaymentAsync(RecordPaymentRequest dto, string recordedById)
        {
            var cashbox = await _unitOfWork.Cashboxes.GetByIdAsync(dto.CashboxId)
                ?? throw new EntityNotFoundException($"{dto.CashboxId} ID-li kassa tapılmadı.");

            if (!cashbox.IsActive)
                throw new Core.Exceptions.ValidationException("Deaktiv kassaya ödəniş yazıla bilməz.");

            var payment = (await _unitOfWork.Payments
                .FindAsync(p => p.ChildId == dto.ChildId && p.Month == dto.Month && p.Year == dto.Year))
                .FirstOrDefault();

            // D9: uşağın çıxış tarixinə görə sıfırlanmış aya pul yazmaq olmaz. Yazılsaydı, sonrakı
            // geriyə düzəlişdə sətir "ödənişi var" deyə atlanardı, bərpa açarı köhnə tarixdə donardı
            // və həmin ay bir daha nə yenidən hesablana, nə də düzgün hesabata düşə bilərdi.
            if (payment != null && payment.FinalAmount == 0 && payment.ZeroedByExitDate.HasValue)
                throw new Core.Exceptions.ValidationException(
                    "Bu ay uşağın çıxış tarixinə görə sıfırlanıb — ödəniş qeyd edilə bilməz. " +
                    "Əvvəlcə çıxış tarixini düzəldin; ay bərpa olunandan sonra ödənişi yazın.");

            if (payment == null)
            {
                var child = await _unitOfWork.Children.GetByIdAsync(dto.ChildId)
                    ?? throw new EntityNotFoundException($"{dto.ChildId} ID-li uşaq tapılmadı.");

                var discountPercent = child.DiscountPercentage ?? 0;
                var hasDiscount = discountPercent > 0;

                // Pro-rate when the child joined and/or left mid-month; bill in whole manats.
                // C2: çıxış tarixi ARTIQ GƏLMƏDİYİ İLK gündür (eksklüziv), qeydiyyat tarixi isə inklüziv →
                // yarım-açıq aralıq [startDay, endExclusive). Çıxış bu ayda deyilsə endExclusive = daysInMonth + 1,
                // yəni köhnə "daysInMonth - startDay + 1" nəticəsi eynilə qalır.
                var daysInMonth = DateTime.DaysInMonth(dto.Year, dto.Month);
                var startDay = (child.RegistrationDate.Year == dto.Year && child.RegistrationDate.Month == dto.Month)
                    ? child.RegistrationDate.Day
                    : 1;
                var endExclusive = (child.DeactivationDate.HasValue
                              && child.DeactivationDate.Value.Year == dto.Year
                              && child.DeactivationDate.Value.Month == dto.Month)
                    ? child.DeactivationDate.Value.Day
                    : daysInMonth + 1;
                var daysActive = Math.Max(0, endExclusive - startDay);
                // C2b: sütun İNKLÜZİV qalır — son hesablanan gün. 0 günlük dövrdə startDay - 1 olur.
                var endDay = endExclusive - 1;
                var isPartialPeriod = startDay != 1 || endDay != daysInMonth;
                var baseAmount = isPartialPeriod
                    ? Math.Round(child.MonthlyFee * daysActive / daysInMonth, 0, MidpointRounding.AwayFromZero)
                    : child.MonthlyFee;

                var rawFinal = hasDiscount
                    ? CalculateFinalAmount(baseAmount, DiscountType.Percentage, discountPercent)
                    : baseAmount;
                var finalAmount = Math.Round(rawFinal, 0, MidpointRounding.AwayFromZero);

                payment = new Payment
                {
                    ChildId = dto.ChildId,
                    Month = dto.Month,
                    Year = dto.Year,
                    OriginalAmount = baseAmount,
                    FinalAmount = finalAmount,
                    PaidAmount = 0,
                    LastPaymentAmount = null,
                    Status = PaymentStatus.Debt,
                    DiscountType = hasDiscount ? DiscountType.Percentage : DiscountType.None,
                    DiscountValue = hasDiscount ? discountPercent : 0,
                    // Dövr SÜTUNLARDA saxlanılır; qeyd yalnız ştabın oxuması üçündür (kosmetik).
                    PeriodStartDay = startDay,
                    PeriodEndDay = endDay,
                    Notes = isPartialPeriod
                        ? (daysActive == 0
                            ? "Dövr: 0 gün (uşaq bu ay gəlməyib)"
                            : $"Dövr: {startDay}-{endDay} ({daysActive} gün)")
                        : null,
                    RecordedById = recordedById
                };

                await _unitOfWork.Payments.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();
            }

            payment.PaidAmount += dto.Amount;
            payment.LastPaymentAmount = dto.Amount;
            payment.CashboxId = dto.CashboxId;
            payment.PaymentDate = _dt.Now;
            payment.RecordedById = recordedById;

            // Apply admin courtesy rounding (e.g. bill 203 ₼ → customer pays 200 ₼ → forgive 3 ₼)
            // F3: qeyd ÜSTÜNDƏN YAZILMIR — RecordBulkPaymentAsync ilə eyni "əlavə et + təkrarı ötür"
            // qaydası işləyir. Əks halda ştabın qeydləri və "Dövr:" izi səssizcə silinirdi.
            AppendNote(payment, dto.Notes);

            if (dto.RoundingDiscount.HasValue && dto.RoundingDiscount.Value > 0)
            {
                var roundingAmt = Math.Min(dto.RoundingDiscount.Value, payment.FinalAmount);
                payment.FinalAmount = Math.Max(0, payment.FinalAmount - roundingAmt);
                AppendNote(payment, $"Yuvarlaqlaşdırma endirimi: {roundingAmt:F2} ₼");
            }

            if (payment.PaidAmount >= payment.FinalAmount)
                payment.Status = PaymentStatus.Paid;
            else if (payment.PaidAmount > 0)
                payment.Status = PaymentStatus.PartiallyPaid;

            await _unitOfWork.Payments.UpdateAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            var result = await _unitOfWork.Payments.GetByIdAsync(
                p => p.Id == payment.Id,
                p => p.Child,
                p => p.Child.Group,
                p => p.Cashbox);

            await _notificationService.SendPaymentConfirmationAsync(payment.Id);

            return _mapper.Map<PaymentResponse>(result);
        }

        /// <summary>
        /// Records full payments for multiple months at once for a single child.
        /// Each selected month is created if missing and marked fully paid (PaidAmount = FinalAmount).
        /// Months that are already fully paid are silently skipped.
        /// </summary>
        public async Task<RecordBulkPaymentResponse> RecordBulkPaymentAsync(RecordBulkPaymentRequest dto, string recordedById)
        {
            if (dto.Months == null || dto.Months.Count == 0)
                throw new Core.Exceptions.ValidationException("Ən azı bir ay seçilməlidir.");

            var distinctMonths = dto.Months
                .Where(m => m >= 1 && m <= 12)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            if (distinctMonths.Count == 0)
                throw new Core.Exceptions.ValidationException("Düzgün ay seçilməyib.");

            var child = await _unitOfWork.Children.GetByIdAsync(dto.ChildId)
                ?? throw new EntityNotFoundException($"{dto.ChildId} ID-li uşaq tapılmadı.");

            var cashbox = await _unitOfWork.Cashboxes.GetByIdAsync(dto.CashboxId)
                ?? throw new EntityNotFoundException($"{dto.CashboxId} ID-li kassa tapılmadı.");

            if (!cashbox.IsActive)
                throw new Core.Exceptions.ValidationException("Deaktiv kassaya ödəniş yazıla bilməz.");

            var discountPercent = child.DiscountPercentage ?? 0;
            var hasDiscount = discountPercent > 0;
            var now = _dt.Now;
            // Bir kütləvi çağırış = bir paket ID-si; vahid çek bu ID ilə yenidən çap olunur
            var batchId = Guid.NewGuid();
            var processedPayments = new List<Payment>();
            var overpaidMonths = new List<BulkOverpaidMonth>();
            decimal totalPaid = 0;
            var overridesByMonth = (dto.MonthOverrides ?? new List<MonthPeriodOverride>())
                .Where(o => o.Month >= 1 && o.Month <= 12)
                .GroupBy(o => o.Month)
                .ToDictionary(g => g.Key, g => g.Last());

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var month in distinctMonths)
                {
                    var existing = (await _unitOfWork.Payments
                        .FindAsync(p => p.ChildId == dto.ChildId && p.Month == month && p.Year == dto.Year))
                        .FirstOrDefault();

                    if (existing != null && existing.Status == PaymentStatus.Paid)
                    {
                        // Already paid — skip silently
                        continue;
                    }

                    // Compute the billing period for this month: respect an admin override if present,
                    // otherwise fall back to registration / deactivation dates.
                    overridesByMonth.TryGetValue(month, out var overrideForMonth);
                    var daysInMonth = DateTime.DaysInMonth(dto.Year, month);

                    // Sətir artıq varsa, ONUN dövr sütunları həqiqətdir — uşağın cari DeactivationDate-i
                    // yox. Çıxış tarixi düzəldiləndən (və ya uşaq gedib-qayıdandan) sonra gün-gün bölünmüş
                    // ay çox vaxt DeactivationDate-in ayından FƏRQLİ olur. Əks halda: aprel 100 ₼-lik
                    // (1-10 gün) sətir, uşağın çıxışı isə oktyabrdadır → aprel "tam ay" sanılıb 300 ₼-ə
                    // yenidən yazılardı; kassir ekranda 100 ₼ görüb o qədər pul alsa da, kassaya 300 ₼
                    // düşərdi. Admin override yenə də üstündür (aşağıdakı ?? sırası).
                    int defaultStart = existing?.PeriodStartDay
                        ?? ((child.RegistrationDate.Year == dto.Year && child.RegistrationDate.Month == month)
                            ? child.RegistrationDate.Day
                            : 1);
                    // C2: uşağın DeactivationDate-i ARTIQ GƏLMƏDİYİ İLK gündür (eksklüziv) → son
                    // hesablanan (inklüziv) gün onun BİR GÜN ƏVVƏLİDİR. Sətrin öz sütunu isə onsuz da
                    // inklüziv saxlanılır, ona görə ondan gələn dəyər olduğu kimi götürülür.
                    int defaultEnd = existing?.PeriodEndDay
                        ?? ((child.DeactivationDate.HasValue
                             && child.DeactivationDate.Value.Year == dto.Year
                             && child.DeactivationDate.Value.Month == month)
                            ? child.DeactivationDate.Value.Day - 1
                            : daysInMonth);

                    var startDay = Math.Clamp(overrideForMonth?.StartDay ?? defaultStart, 1, daysInMonth);

                    int endDay;
                    if (overrideForMonth?.EndDay is int overrideEndDay)
                    {
                        // Admin ƏL İLƏ aralıq verirsə davranış DƏYİŞMİR: bitiş inklüzivdir və ən azı 1 gün qalır
                        // (səhv yazılmış "bitiş < başlanğıc" girişi 0 ₼-lıq çek yaratmasın).
                        endDay = Math.Clamp(overrideEndDay, 1, daysInMonth);
                        if (endDay < startDay) endDay = startDay;
                    }
                    else
                    {
                        // Sütundan/çıxış tarixindən gələn bitiş 0 ola bilər (uşaq bu ay heç gəlməyib) —
                        // 1-ə sıxılsaydı 0 günlük ay səhvən 1 gün kimi hesablanardı.
                        endDay = Math.Clamp(defaultEnd, 0, daysInMonth);
                    }

                    var daysActive = Math.Max(0, endDay - startDay + 1);
                    var isPartialPeriod = startDay != 1 || endDay != daysInMonth;
                    var baseAmount = isPartialPeriod
                        ? Math.Round(child.MonthlyFee * daysActive / daysInMonth, 0, MidpointRounding.AwayFromZero)
                        : child.MonthlyFee;
                    var rawFinal = hasDiscount
                        ? CalculateFinalAmount(baseAmount, DiscountType.Percentage, discountPercent)
                        : baseAmount;
                    var finalAmount = Math.Round(rawFinal, 0, MidpointRounding.AwayFromZero);

                    // Apply optional admin courtesy rounding (e.g. 284 ₼ → 280 ₼, forgive 4 ₼)
                    decimal roundingApplied = 0;
                    if (overrideForMonth?.RoundingDiscount.HasValue == true && overrideForMonth.RoundingDiscount.Value > 0)
                    {
                        roundingApplied = Math.Min(overrideForMonth.RoundingDiscount.Value, finalAmount);
                        finalAmount = Math.Max(0, finalAmount - roundingApplied);
                    }

                    var periodNote = isPartialPeriod
                        ? (daysActive == 0
                            ? "Dövr: 0 gün (uşaq bu ay gəlməyib)"
                            : $"Dövr: {startDay}-{endDay} ({daysActive} gün)")
                        : null;
                    if (roundingApplied > 0)
                    {
                        var roundNote = $"Yuvarlaqlaşdırma endirimi: {roundingApplied:F2} ₼";
                        periodNote = string.IsNullOrEmpty(periodNote)
                            ? roundNote
                            : $"{periodNote} | {roundNote}";
                    }

                    Payment payment;

                    if (existing != null)
                    {
                        payment = existing;
                        // Dövr sütunları HƏMİŞƏ yazılır — hesabladığımız aralıq bu sətrin həqiqətidir.
                        payment.PeriodStartDay = startDay;
                        payment.PeriodEndDay = endDay;
                        // If admin supplied an override (or default partial period changed), rebill the row.
                        if (payment.OriginalAmount != baseAmount || payment.FinalAmount != finalAmount)
                        {
                            payment.OriginalAmount = baseAmount;
                            payment.FinalAmount = finalAmount;
                            payment.DiscountType = hasDiscount ? DiscountType.Percentage : DiscountType.None;
                            payment.DiscountValue = hasDiscount ? discountPercent : 0;
                            // Preserve any existing audit prefix, but make sure the new period note is present.
                            AppendNote(payment, periodNote);
                        }
                    }
                    else
                    {
                        payment = new Payment
                        {
                            ChildId = dto.ChildId,
                            Month = month,
                            Year = dto.Year,
                            OriginalAmount = baseAmount,
                            FinalAmount = finalAmount,
                            PaidAmount = 0,
                            DiscountType = hasDiscount ? DiscountType.Percentage : DiscountType.None,
                            DiscountValue = hasDiscount ? discountPercent : 0,
                            // Dövr SÜTUNLARDA saxlanılır; qeyd kosmetikdir.
                            PeriodStartDay = startDay,
                            PeriodEndDay = endDay,
                            Notes = periodNote,
                            RecordedById = recordedById,
                            Status = PaymentStatus.Debt
                        };
                        await _unitOfWork.Payments.AddAsync(payment);
                    }

                    // If final amount is 0 (e.g. 100% discount), just mark paid without charging anything.
                    var delta = Math.Max(0, payment.FinalAmount - payment.PaidAmount);

                    // REAL PUL HEÇ VAXT AŞAĞI YAZILMIR. Yenidən hesablanan məbləğ artıq ödənilmişdən
                    // az ola bilər (məs. çıxış tarixinə görə ay 0 günə düşüb). Əvvəllər burada
                    // PaidAmount birbaşa FinalAmount-a bərabərləşdirilirdi və valideynin real ödədiyi
                    // pul kassadan silinirdi. İndi yalnız YUXARI qalxır; fərq isə ştaba bildirilir.
                    if (payment.FinalAmount > payment.PaidAmount)
                    {
                        payment.PaidAmount = payment.FinalAmount;
                        payment.LastPaymentAmount = delta;
                    }
                    else if (payment.PaidAmount > payment.FinalAmount)
                    {
                        overpaidMonths.Add(new BulkOverpaidMonth
                        {
                            PaymentId = payment.Id,
                            Month = payment.Month,
                            Year = payment.Year,
                            PaidAmount = payment.PaidAmount,
                            NewFinalAmount = payment.FinalAmount,
                            Difference = payment.PaidAmount - payment.FinalAmount
                        });
                        AppendNote(payment, $"Artıq ödəniş: {payment.PaidAmount - payment.FinalAmount:F2} ₼ — yoxlanılmalıdır");
                    }

                    payment.Status = PaymentStatus.Paid;
                    payment.PaymentDate = now;
                    payment.CashboxId = dto.CashboxId;
                    payment.RecordedById = recordedById;
                    payment.PaymentBatchId = batchId;

                    if (!string.IsNullOrWhiteSpace(dto.Notes))
                        AppendNote(payment, $"Kütləvi ödəniş: {dto.Notes}");

                    if (existing != null)
                        await _unitOfWork.Payments.UpdateAsync(payment);

                    processedPayments.Add(payment);
                    totalPaid += delta;
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            // Re-fetch with navigation properties for response mapping
            var responses = new List<PaymentResponse>();
            foreach (var p in processedPayments)
            {
                var full = await _unitOfWork.Payments.GetByIdAsync(
                    x => x.Id == p.Id,
                    x => x.Child,
                    x => x.Child.Group,
                    x => x.Cashbox);
                if (full != null)
                    responses.Add(_mapper.Map<PaymentResponse>(full));
            }

            // WABA "odenish_wp" — hər ödənilən ay üçün ayrıca təsdiq SMS-i göndər.
            // SMS göndərilərkən xəta olarsa, bulk əməliyyatı pozulmasın.
            foreach (var p in processedPayments)
            {
                // 0 ₼-lik ay üçün təsdiq göndərilmir — valideynə "0 azn ödəniş qəbul olundu"
                // mesajı getməsi yanlış və çaşdırıcıdır (məs. çıxış tarixinə görə sıfırlanmış ay).
                if (p.FinalAmount <= 0) continue;

                try
                {
                    await _notificationService.SendPaymentConfirmationAsync(p.Id);
                }
                catch
                {
                    // Sessiz — SMS xətası bulk əməliyyatı pozmasın
                }
            }

            return new RecordBulkPaymentResponse
            {
                PaidCount = processedPayments.Count,
                TotalPaid = totalPaid,
                PaymentBatchId = processedPayments.Count > 0 ? batchId : null,
                OverpaidMonths = overpaidMonths,
                Payments = responses
            };
        }

        /// <summary>
        /// Applies a discount to an existing payment.
        /// </summary>
        public async Task<PaymentResponse> ApplyDiscountAsync(int paymentId, DiscountRequest dto)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(
                p => p.Id == paymentId,
                p => p.Child,
                p => p.Child.Group,
                p => p.Cashbox)
                ?? throw new EntityNotFoundException($"{paymentId} ID-li ödəniş tapılmadı.");

            payment.DiscountType = dto.DiscountType;
            payment.DiscountValue = dto.DiscountValue;
            payment.FinalAmount = CalculateFinalAmount(payment.OriginalAmount, dto.DiscountType, dto.DiscountValue);

            if (payment.PaidAmount >= payment.FinalAmount)
                payment.Status = PaymentStatus.Paid;
            else if (payment.PaidAmount > 0)
                payment.Status = PaymentStatus.PartiallyPaid;
            else
                payment.Status = PaymentStatus.Debt;

            await _unitOfWork.Payments.UpdateAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PaymentResponse>(payment);
        }

        public async Task<(byte[] FileBytes, string FileName)> GeneratePaymentReceiptPdfAsync(int paymentId)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(
                p => p.Id == paymentId,
                p => p.Child,
                p => p.Child.Group,
                p => p.Cashbox)
                ?? throw new EntityNotFoundException($"{paymentId} ID-li ödəniş tapılmadı.");

            QuestPDF.Settings.License = LicenseType.Community;

            var fileName = $"PaymentReceipt_{payment.Id}_{_dt.Now:yyyyMMddHHmmss}.pdf";
            var paidDate = payment.PaymentDate ?? payment.UpdatedAt ?? _dt.Now;
            var bakuTimeZone = GetBakuTimeZone();
            var nowBaku = TimeZoneInfo.ConvertTimeFromUtc(_dt.Now, bakuTimeZone);
            var period = ComputeBillingPeriod(payment.Child.PaymentDay, payment.Year, payment.Month);
            var installmentAmount = (payment.LastPaymentAmount ?? 0) > 0
                ? payment.LastPaymentAmount.Value
                : payment.PaidAmount;

            var model = new ReceiptModel
            {
                ReceiptNo = $"KG-{payment.Id:D6}",
                PaidDateAz = FormatPaidDateAz(paidDate, bakuTimeZone),
                PeriodRange = $"{period.Start:dd.MM.yyyy}-{period.End:dd.MM.yyyy}",
                // Tək aylıq çekdə "Aylar" sətri və sətir cədvəli yoxdur — köhnə görünüş olduğu kimi qalır
                MonthsLine = null,
                ShowLineItems = false,
                ParentFullName = payment.Child.ParentFullName,
                ParentPhone = payment.Child.ParentPhone,
                ChildName = $"{payment.Child.FirstName} {payment.Child.LastName}",
                GroupName = payment.Child.Group?.Name ?? "-",
                CashboxName = payment.Cashbox?.Name ?? "-",
                CashboxType = payment.Cashbox?.Type.ToString() ?? "-",
                StatusText = payment.Status switch
                {
                    PaymentStatus.Paid => "ÖDƏNİB",
                    PaymentStatus.PartiallyPaid => "QİSMƏN ÖDƏNİB",
                    _ => "BORC"
                },
                Lines = new List<ReceiptLine>
                {
                    new($"{MonthNameAzTitle(payment.Month)} {payment.Year}",
                        BuildPeriodMarker(payment),
                        payment.FinalAmount)
                },
                TotalOriginal = payment.OriginalAmount,
                TotalFinal = payment.FinalAmount,
                TotalPaid = installmentAmount,
                TotalRemaining = Math.Max(0, payment.FinalAmount - payment.PaidAmount),
                Notes = payment.Notes,
                LogoBytes = LoadLogoBytes()
            };

            return (RenderReceiptDocument(model, nowBaku), fileName);
        }

        /// <summary>
        /// Bir uşağın seçilmiş ödəniş sətirləri üçün vahid (çoxaylı) çek yaradır.
        /// </summary>
        public async Task<(byte[] FileBytes, string FileName)> GenerateBulkPaymentReceiptPdfAsync(IReadOnlyCollection<int> paymentIds)
        {
            if (paymentIds == null || paymentIds.Count == 0)
                throw new Core.Exceptions.ValidationException("Vahid çek üçün ən azı bir ödəniş seçilməlidir.");

            var ids = paymentIds.Distinct().ToList();

            var rows = await _unitOfWork.Payments.GetAllAsync(
                p => ids.Contains(p.Id),
                p => p.Child,
                p => p.Child.Group,
                p => p.Cashbox);

            var missing = ids.Except(rows.Select(r => r.Id)).ToList();
            if (missing.Count > 0)
                throw new EntityNotFoundException($"{string.Join(", ", missing)} ID-li ödəniş tapılmadı.");

            return BuildUnifiedReceipt(rows);
        }

        /// <summary>
        /// Kütləvi ödənişin paket ID-si ilə vahid çeki yenidən yaradır (tarixçədən təkrar çap).
        /// </summary>
        public async Task<(byte[] FileBytes, string FileName)> GenerateBulkPaymentReceiptPdfAsync(Guid batchId)
        {
            if (batchId == Guid.Empty)
                throw new Core.Exceptions.ValidationException("Ödəniş paketinin ID-si boş ola bilməz.");

            var rows = await _unitOfWork.Payments.GetAllAsync(
                p => p.PaymentBatchId == batchId,
                p => p.Child,
                p => p.Child.Group,
                p => p.Cashbox);

            if (rows.Count == 0)
                throw new EntityNotFoundException($"{batchId} ID-li ödəniş paketi tapılmadı.");

            return BuildUnifiedReceipt(rows);
        }

        /// <summary>
        /// Vahid çekin məzmununu qurur: sətirlər aya görə sıralanır, cəmlər HƏR SƏTİR üzrə toplanır.
        /// </summary>
        private (byte[] FileBytes, string FileName) BuildUnifiedReceipt(ICollection<Payment> rows)
        {
            // Çek bir şagirdə aiddir — qarışıq uşaqlar üçün vahid çek olmaz
            if (rows.Select(p => p.ChildId).Distinct().Count() > 1)
                throw new Core.Exceptions.ValidationException("Vahid çek yalnız bir uşağa aid ödənişlər üçün yaradıla bilər.");

            QuestPDF.Settings.License = LicenseType.Community;

            var ordered = rows.OrderBy(p => p.Year).ThenBy(p => p.Month).ToList();
            var first = ordered[0];
            var last = ordered[^1];
            var child = first.Child;

            var minId = ordered.Min(p => p.Id);
            var maxId = ordered.Max(p => p.Id);
            var periodStart = ComputeBillingPeriod(child.PaymentDay, first.Year, first.Month).Start;
            var periodEnd = ComputeBillingPeriod(child.PaymentDay, last.Year, last.Month).End;

            var paidDate = ordered.Where(p => p.PaymentDate.HasValue).Max(p => p.PaymentDate)
                           ?? ordered.Where(p => p.UpdatedAt.HasValue).Max(p => p.UpdatedAt)
                           ?? _dt.Now;
            var bakuTimeZone = GetBakuTimeZone();
            var nowBaku = TimeZoneInfo.ConvertTimeFromUtc(_dt.Now, bakuTimeZone);
            var cashbox = ordered.Select(p => p.Cashbox).FirstOrDefault(c => c != null);

            var statusText = ordered.All(p => p.Status == PaymentStatus.Paid)
                ? "ÖDƏNİB"
                : ordered.Any(p => p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PartiallyPaid)
                    ? "QİSMƏN ÖDƏNİB"
                    : "BORC";

            // Qeydlərdən "Dövr:" markeri sətir cədvəlinə düşür — qalan hissələr ümumi qeyd blokunda birləşir
            var noteSegments = ordered
                .SelectMany(p => SplitNotes(p.Notes))
                .Where(s => !s.StartsWith("Dövr:", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToList();

            var model = new ReceiptModel
            {
                ReceiptNo = $"KG-{minId:D6}-{maxId:D6}",
                PaidDateAz = FormatPaidDateAz(paidDate, bakuTimeZone),
                PeriodRange = $"{periodStart:dd.MM.yyyy}-{periodEnd:dd.MM.yyyy}",
                MonthsLine = BuildMonthsLine(ordered),
                ShowLineItems = true,
                ParentFullName = child.ParentFullName,
                ParentPhone = child.ParentPhone,
                ChildName = $"{child.FirstName} {child.LastName}",
                GroupName = child.Group?.Name ?? "-",
                CashboxName = cashbox?.Name ?? "-",
                CashboxType = cashbox?.Type.ToString() ?? "-",
                StatusText = statusText,
                Lines = ordered
                    .Select(p => new ReceiptLine(
                        $"{MonthNameAzTitle(p.Month)} {p.Year}",
                        BuildPeriodMarker(p),
                        p.FinalAmount))
                    .ToList(),
                TotalOriginal = ordered.Sum(p => p.OriginalAmount),
                TotalFinal = ordered.Sum(p => p.FinalAmount),
                TotalPaid = ordered.Sum(p => (p.LastPaymentAmount ?? 0) > 0 ? p.LastPaymentAmount!.Value : p.PaidAmount),
                // Qalıq hər sətir üzrə ayrıca sıfıra kəsilir ki, artıq ödənilmiş ay borclu ayı gizlətməsin
                TotalRemaining = ordered.Sum(p => Math.Max(0, p.FinalAmount - p.PaidAmount)),
                Notes = noteSegments.Count > 0 ? string.Join(" | ", noteSegments) : null,
                LogoBytes = LoadLogoBytes()
            };

            var fileName = $"PaymentReceipt_Bulk_{first.ChildId}_{_dt.Now:yyyyMMddHHmmss}.pdf";
            return (RenderReceiptDocument(model, nowBaku), fileName);
        }

        /// <summary>
        /// Çekin dövr qaydası: başlanğıc — həmin ayın ödəniş günü (ayın uzunluğuna kəsilir);
        /// ödəniş günü 1-dirsə son — həmin ayın sonu, əks halda gələn ayın (N-1) günü.
        /// </summary>
        private static (DateTime Start, DateTime End) ComputeBillingPeriod(int paymentDay, int year, int month)
        {
            var day = Math.Max(paymentDay, 1);
            var thisMonthDays = DateTime.DaysInMonth(year, month);
            var start = new DateTime(year, month, Math.Min(day, thisMonthDays), 0, 0, 0, DateTimeKind.Utc);

            DateTime end;
            if (day == 1)
            {
                // Ödəniş günü 1-dirsə: ayın 1-dən sonuna kimi (məs. 01.04 – 30.04)
                end = new DateTime(year, month, thisMonthDays, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                // Digər günlər: bu ayın N-dən gələn ayın (N-1)-nə kimi (məs. 02.05 – 01.06)
                var nextMonth = start.AddMonths(1);
                var endDay = Math.Max(day - 1, 1);
                var nextMonthDays = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                end = new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(endDay, nextMonthDays), 0, 0, 0, DateTimeKind.Utc);
            }

            return (start, end);
        }

        private byte[]? LoadLogoBytes()
        {
            var logoPath = Path.Combine(_env.ContentRootPath, "Templates", "KinderGardenLogo.png");
            return File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;
        }

        private static string FormatPaidDateAz(DateTime paidDate, TimeZoneInfo bakuTimeZone)
        {
            var paidDateBaku = TimeZoneInfo.ConvertTimeFromUtc(
                paidDate.Kind == DateTimeKind.Utc ? paidDate : DateTime.SpecifyKind(paidDate, DateTimeKind.Utc),
                bakuTimeZone);
            return $"{paidDateBaku.Day} {MonthNameAz(paidDateBaku.Month)} {paidDateBaku.Year}";
        }

        private static IEnumerable<string> SplitNotes(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return Array.Empty<string>();
            return notes.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        /// <summary>
        /// Çek sətrindəki dövr markerini SÜTUNLARDAN qurur (qeyd mətnindən DEYİL).
        /// Tam ay üçün marker göstərilmir.
        /// </summary>
        private static string? BuildPeriodMarker(Payment payment)
        {
            if (!payment.PeriodStartDay.HasValue || !payment.PeriodEndDay.HasValue) return null;

            var daysInMonth = DateTime.DaysInMonth(payment.Year, payment.Month);
            var startDay = payment.PeriodStartDay.Value;
            var endDay = payment.PeriodEndDay.Value;
            if (startDay <= 1 && endDay >= daysInMonth) return null;

            var daysActive = Math.Max(0, endDay - startDay + 1);
            // C2b: boş aralıq (bitiş < başlanğıc) "1-0" kimi görünməsin.
            return daysActive == 0
                ? "Dövr: 0 gün"
                : $"Dövr: {startDay}-{endDay} ({daysActive} gün)";
        }

        /// <summary>
        /// Seçilmiş aylar ardıcıldırsa diapazon, deyilsə hər ay ayrıca sadalanır.
        /// </summary>
        private static string BuildMonthsLine(IReadOnlyList<Payment> ordered)
        {
            var first = ordered[0];
            var last = ordered[^1];
            var singleYear = ordered.All(p => p.Year == first.Year);

            if (ordered.Count == 1)
                return $"Aylar: {MonthNameAzTitle(first.Month)} {first.Year}";

            var keys = ordered.Select(p => p.Year * 12 + (p.Month - 1)).Distinct().OrderBy(k => k).ToList();
            var contiguous = keys.Count == ordered.Count && keys[^1] - keys[0] == keys.Count - 1;

            if (contiguous)
                return singleYear
                    ? $"Aylar: {MonthNameAzTitle(first.Month)}-{MonthNameAzTitle(last.Month)} {first.Year}"
                    : $"Aylar: {MonthNameAzTitle(first.Month)} {first.Year}-{MonthNameAzTitle(last.Month)} {last.Year}";

            return singleYear
                ? $"Aylar: {string.Join(", ", ordered.Select(p => MonthNameAzTitle(p.Month)))} {first.Year}"
                : $"Aylar: {string.Join(", ", ordered.Select(p => $"{MonthNameAzTitle(p.Month)} {p.Year}"))}";
        }

        private static byte[] RenderReceiptDocument(ReceiptModel model, DateTime nowBaku)
        {
            // Çoxaylı çekdə iki nüsxə bir A4-ə sığmır — müəssisə nüsxəsi ayrı səhifəyə keçir
            var separatePages = model.ShowLineItems;

            // Tək aylıq çekdə iki nüsxə HƏMİŞƏ bir A4-ə sığmalıdır. Əvvəllər nüsxələr sərbəst
            // hündürlükdə idi: qeyd sətri (məs. "ПРЕДОПЛАТА НА СЕНТЯБРЬ") və ya uzun ad
            // əlavə olunan kimi ikinci nüsxə 2-ci vərəqə düşürdü. İndi səhifə sabit hissələrdən
            // (kənar + futer + ayırıcı) təmizlənib yarıya bölünür, nüsxə isə həmin qutuya
            // ScaleToFit ilə sıxılır — kompakt ölçülər onsuz da sığır, ScaleToFit yalnız
            // ekstremal uzun mətndə (bir neçə sətrə keçən ad/qeyd) işə düşən qorumadır.
            const float pageMargin = 6f;
            const float footerHeight = 18f;
            const float copySpacing = 5f;
            var copyHeight = (PageSizes.A4.Height - pageMargin * 2 - footerHeight - (copySpacing * 2 + 1)) / 2;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(pageMargin);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Content().Column(column =>
                    {
                        column.Spacing(copySpacing);

                        if (separatePages)
                        {
                            column.Item().Element(x => BuildReceiptCopy(x, model, "Müştəri nüsxəsi", false));
                            column.Item().PageBreak();
                            column.Item().Element(x => BuildReceiptCopy(x, model, "Müəssisə nüsxəsi", false));
                        }
                        else
                        {
                            column.Item().Height(copyHeight).ScaleToFit()
                                .Element(x => BuildReceiptCopy(x, model, "Müştəri nüsxəsi", true));
                            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            column.Item().Height(copyHeight).ScaleToFit()
                                .Element(x => BuildReceiptCopy(x, model, "Müəssisə nüsxəsi", true));
                        }
                    });

                    // Futer hündürlüyü SABİTDİR — copyHeight hesabı məhz bu dəyərdən çıxır.
                    page.Footer().Height(footerHeight).Column(footer =>
                    {
                        footer.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        footer.Item().PaddingTop(2).AlignCenter().Text($"Uşaq Bağçası İdarəetmə Sistemi • {nowBaku:dd.MM.yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    });
                });
            }).GeneratePdf();
        }

        /// <summary>
        /// Bir nüsxəni qurur. <paramref name="compact"/> — iki nüsxə bir A4-ə yerləşdiyi tək aylıq
        /// çek üçün daralmış ölçülər (loqo, boşluqlar, daxili paddinq). Şrift ölçüsü DƏYİŞMİR;
        /// qazanc yalnız boş sahədən götürülür ki, çek eyni cür oxunaqlı qalsın.
        /// </summary>
        private static void BuildReceiptCopy(IContainer container, ReceiptModel model, string copyTitle, bool compact)
        {
            var outerPadding = compact ? 6f : 9f;
            var boxPadding = compact ? 5f : 7f;
            var itemSpacing = compact ? 4f : 5f;
            var logoHeight = compact ? 52f : 74f;
            var titleSize = compact ? 14f : 16f;

            container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(outerPadding).Column(column =>
            {
                column.Spacing(itemSpacing);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text("KINDERGARTEN BAKI").Bold().FontSize(titleSize).FontColor(Colors.Blue.Darken2);
                        left.Item().Text("RƏSMİ ÖDƏNİŞ ÇEKİ").SemiBold().FontSize(12).FontColor(Colors.Grey.Darken2);
                        left.Item().Text(copyTitle).SemiBold().FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    if (model.LogoBytes != null)
                    {
                        row.ConstantItem(126).AlignRight().Height(logoHeight).Image(model.LogoBytes, ImageScaling.FitArea);
                    }
                });

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(boxPadding).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Çek №: {model.ReceiptNo}").Bold();
                        c.Item().Text($"Tarix: {model.PaidDateAz}");
                        c.Item().Text($"Dövr: {model.PeriodRange}");
                        if (!string.IsNullOrWhiteSpace(model.MonthsLine))
                            c.Item().Text(model.MonthsLine);
                    });

                    row.ConstantItem(140).AlignRight().Column(c =>
                    {
                        c.Item().Text("Status").FontSize(9).FontColor(Colors.Grey.Darken1);
                        c.Item().Background(Colors.Blue.Lighten4).Padding(compact ? 4 : 6)
                            .AlignCenter().Text(model.StatusText).Bold().FontColor(Colors.Blue.Darken3);
                    });
                });

                column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(boxPadding).Column(c =>
                {
                    c.Spacing(compact ? 2 : 4);
                    c.Item().Text("Ödəyici məlumatı").SemiBold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text($"Valideyn: {model.ParentFullName}");
                    c.Item().Text($"Əlaqə: {model.ParentPhone}");
                    c.Item().Text($"Uşaq: {model.ChildName}");
                    c.Item().Text($"Qrup: {model.GroupName}");
                    c.Item().Text($"Kassa: {model.CashboxName} ({model.CashboxType})");
                });

                column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(boxPadding).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    table.Cell().PaddingBottom(4).Text("Açıqlama").SemiBold();
                    table.Cell().PaddingBottom(4).AlignRight().Text("Məbləğ").SemiBold();

                    // Yalnız çoxaylı çekdə: hər ay üçün ayrıca sətir
                    if (model.ShowLineItems)
                    {
                        foreach (var line in model.Lines)
                        {
                            var description = string.IsNullOrWhiteSpace(line.PeriodMarker)
                                ? line.Description
                                : $"{line.Description} — {line.PeriodMarker}";

                            table.Cell().PaddingBottom(2).Text(description);
                            table.Cell().PaddingBottom(2).AlignRight().Text($"{line.Amount:F2} AZN");
                        }

                        table.Cell().ColumnSpan(2).PaddingBottom(4)
                            .LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    }

                    table.Cell().Text("Əsas ödəniş");
                    table.Cell().AlignRight().Text($"{model.TotalOriginal:F2} AZN");

                    table.Cell().Text("Yekun məbləğ").SemiBold();
                    table.Cell().AlignRight().Text($"{model.TotalFinal:F2} AZN").SemiBold();

                    table.Cell().Text("Ödənilmiş məbləğ").SemiBold();
                    table.Cell().AlignRight().Text($"{model.TotalPaid:F2} AZN").SemiBold().FontColor(Colors.Green.Darken2);

                    table.Cell().Text("Qalıq borc");
                    table.Cell().AlignRight().Text($"{model.TotalRemaining:F2} AZN").FontColor(model.TotalRemaining > 0 ? Colors.Red.Darken1 : Colors.Green.Darken2);
                });

                // Qeyd 500 simvola qədər ola bilər (sistem özü də ora "Dövr: ...", "Aylıq qiymət
                // yeniləndi: ..." kimi izlər yazır). Kompakt nüsxədə 2 sətirlə məhdudlaşdırılır ki,
                // uzun qeyd bütün çeki ScaleToFit ilə kiçiltməsin.
                if (!string.IsNullOrWhiteSpace(model.Notes))
                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(boxPadding)
                        .Text(text =>
                        {
                            if (compact) text.ClampLines(2, "…");
                            text.Span($"Qeyd: {model.Notes}");
                        });

                column.Item().PaddingTop(compact ? 4 : 7).Row(row =>
                {
                    row.RelativeItem().Text("Qəbul edən: __________________").FontSize(9);
                    row.RelativeItem().AlignRight().Text("İmza: __________________").FontSize(9);
                });
            });
        }

        /// <summary>Çek şablonuna ötürülən vahid model — həm tək aylıq, həm çoxaylı çek üçün.</summary>
        private sealed class ReceiptModel
        {
            public string ReceiptNo { get; init; } = string.Empty;
            public string PaidDateAz { get; init; } = string.Empty;
            public string PeriodRange { get; init; } = string.Empty;
            public string? MonthsLine { get; init; }
            public string ParentFullName { get; init; } = string.Empty;
            public string ParentPhone { get; init; } = string.Empty;
            public string ChildName { get; init; } = string.Empty;
            public string GroupName { get; init; } = string.Empty;
            public string CashboxName { get; init; } = string.Empty;
            public string CashboxType { get; init; } = string.Empty;
            public string StatusText { get; init; } = string.Empty;
            /// <summary>Cədvəldə aylıq sətirlər göstərilsin (yalnız vahid çek)</summary>
            public bool ShowLineItems { get; init; }
            public List<ReceiptLine> Lines { get; init; } = new();
            public decimal TotalOriginal { get; init; }
            public decimal TotalFinal { get; init; }
            public decimal TotalPaid { get; init; }
            public decimal TotalRemaining { get; init; }
            public string? Notes { get; init; }
            public byte[]? LogoBytes { get; init; }
        }

        /// <summary>Çekin bir sətri: ay adı + ili, qeyddəki dövr markeri və həmin ayın yekun məbləği.</summary>
        private sealed record ReceiptLine(string Description, string? PeriodMarker, decimal Amount);

        public async Task DeletePaymentAsync(int paymentId)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId)
                ?? throw new EntityNotFoundException($"{paymentId} ID-li ödəniş tapılmadı.");

            await _unitOfWork.Payments.RemoveAsync(payment);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Gets all children with unpaid debts.
        /// </summary>
        public async Task<IEnumerable<DebtorListItem>> GetDebtorsAsync()
        {
            // C1: güzəşt YOXDUR — borc sətir yarandığı andan siyahıda görünür.
            var debts = await _unitOfWork.Payments.GetDebtorsAsync();
            return BuildDebtorList(debts);
        }

        /// <summary>
        /// Gets all INACTIVE (deactivated) children that still owe money.
        /// </summary>
        public async Task<IEnumerable<DebtorListItem>> GetInactiveDebtorsAsync()
        {
            var debts = await _unitOfWork.Payments.GetInactiveDebtorsAsync();
            return BuildDebtorList(debts);
        }

        private IEnumerable<DebtorListItem> BuildDebtorList(IEnumerable<Payment> debts)
        {
            return debts
                .GroupBy(p => p.ChildId)
                .Select(g =>
                {
                    var first = g.First();
                    return new DebtorListItem
                    {
                        ChildId = g.Key,
                        ChildFullName = $"{first.Child.FirstName} {first.Child.LastName}",
                        GroupName = first.Child.Group?.Name ?? string.Empty,
                        DivisionName = first.Child.Group?.Division?.Name ?? string.Empty,
                        ParentPhone = first.Child.ParentPhone,
                        TotalDebt = g.Sum(p => p.FinalAmount - p.PaidAmount),
                        UnpaidMonths = _mapper.Map<List<PaymentResponse>>(g.ToList())
                    };
                });
        }

        /// <summary>
        /// Gets payment history for a specific child.
        /// </summary>
        public async Task<IEnumerable<PaymentResponse>> GetChildPaymentHistoryAsync(int childId)
        {
            var payments = await _unitOfWork.Payments.GetPaymentsByChildAsync(childId);
            return _mapper.Map<IEnumerable<PaymentResponse>>(payments);
        }

        /// <summary>
        /// Gets a filtered, paged list of payments.
        /// </summary>
        public async Task<PagedResponse<PaymentResponse>> GetFilteredPaymentsAsync(PaymentFilterRequest filter)
        {
            var pageSize = Math.Clamp(filter.PageSize, 1, 100);
            var page     = Math.Max(filter.Page, 1);

            PaymentStatus? status = Enum.TryParse<PaymentStatus>(filter.Status, true, out var s) ? s : null;

            var (items, totalCount) = await _unitOfWork.Payments.GetFilteredAsync(
                filter.ChildId, filter.GroupId, filter.DivisionId,
                status, filter.Month, filter.Year,
                page, pageSize);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResponse<PaymentResponse>
            {
                Items           = _mapper.Map<IEnumerable<PaymentResponse>>(items),
                TotalCount      = totalCount,
                Page            = page,
                PageSize        = pageSize,
                TotalPages      = totalPages,
                HasNextPage     = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        /// <summary>
        /// Gets the daily cash collection report.
        /// </summary>
        public async Task<DailyCashReport> GetDailyCashReportAsync(DateOnly date)
        {
            var payments = await _unitOfWork.Payments.GetDailyCollectionAsync(date);
            return new DailyCashReport
            {
                Date = date,
                TotalCollected = payments.Sum(p => p.PaidAmount),
                PaymentCount = payments.Count(),
                Payments = _mapper.Map<List<PaymentResponse>>(payments)
            };
        }

        /// <summary>
        /// Gets the monthly cash report.
        /// </summary>
        public async Task<MonthlyCashReport> GetMonthlyCashReportAsync(int month, int year)
        {
            var payments = await _unitOfWork.Payments.GetMonthlyPaymentsAsync(month, year);
            return BuildMonthlyCashReport(payments, month, year);
        }

        /// <summary>
        /// Gets income report for a specific group.
        /// </summary>
        public async Task<MonthlyCashReport> GetGroupIncomeReportAsync(int groupId, int month, int year)
        {
            var payments = await _unitOfWork.Payments.GetPaymentsByGroupAsync(groupId, month, year);
            return BuildMonthlyCashReport(payments, month, year);
        }

        /// <summary>
        /// Gets income report for a specific division.
        /// </summary>
        public async Task<MonthlyCashReport> GetDivisionIncomeReportAsync(int divisionId, int month, int year)
        {
            var payments = await _unitOfWork.Payments.GetMonthlyPaymentsAsync(month, year);
            var filtered = payments.Where(p => p.Child.Group?.DivisionId == divisionId);
            return BuildMonthlyCashReport(filtered, month, year);
        }

        /// <summary>
        /// Calculates the final amount after discount.
        /// </summary>
        public decimal CalculateFinalAmount(decimal original, DiscountType type, decimal value)
        {
            return type switch
            {
                DiscountType.Percentage => original - (original * value / 100),
                DiscountType.Fixed => original - value,
                _ => original
            };
        }

        private static MonthlyCashReport BuildMonthlyCashReport(IEnumerable<Payment> payments, int month, int year)
        {
            var list = payments.ToList();
            return new MonthlyCashReport
            {
                Month = month,
                Year = year,
                TotalExpected = list.Sum(p => p.FinalAmount),
                TotalCollected = list.Sum(p => p.PaidAmount),
                TotalDebt = list.Sum(p => p.FinalAmount - p.PaidAmount),
                PaidCount = list.Count(p => p.Status == PaymentStatus.Paid),
                PartialCount = list.Count(p => p.Status == PaymentStatus.PartiallyPaid),
                DebtCount = list.Count(p => p.Status == PaymentStatus.Debt)
            };
        }

        private static TimeZoneInfo GetBakuTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Azerbaijan Standard Time"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Baku"); }
                catch { return TimeZoneInfo.Utc; }
            }
        }

        private static string MonthNameAz(int month) => month switch
        {
            1 => "yanvar",
            2 => "fevral",
            3 => "mart",
            4 => "aprel",
            5 => "may",
            6 => "iyun",
            7 => "iyul",
            8 => "avqust",
            9 => "sentyabr",
            10 => "oktyabr",
            11 => "noyabr",
            12 => "dekabr",
            _ => month.ToString()
        };

        /// <summary>Ay adı baş hərflə — Azərbaycan əlifbasında kiçik "i"-nin böyüyü "İ"-dir.</summary>
        private static string MonthNameAzTitle(int month)
        {
            var name = MonthNameAz(month);
            if (string.IsNullOrEmpty(name)) return name;
            var first = name[0] == 'i' ? "İ" : name[..1].ToUpperInvariant();
            return first + name[1..];
        }
    }
}