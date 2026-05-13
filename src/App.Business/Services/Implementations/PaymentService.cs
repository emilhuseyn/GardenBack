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

                    // Pro-rate for children who joined mid-month; bill in whole manats (no qəpik)
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
                    payment.Notes = string.IsNullOrWhiteSpace(payment.Notes)
                        ? "Kütləvi ödənilmiş (sistem köçürməsi)"
                        : payment.Notes + " | Kütləvi ödənilmiş";

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

            if (payment == null)
            {
                var child = await _unitOfWork.Children.GetByIdAsync(dto.ChildId)
                    ?? throw new EntityNotFoundException($"{dto.ChildId} ID-li uşaq tapılmadı.");

                var discountPercent = child.DiscountPercentage ?? 0;
                var hasDiscount = discountPercent > 0;

                // Pro-rate when the child joined and/or left mid-month; bill in whole manats
                var daysInMonth = DateTime.DaysInMonth(dto.Year, dto.Month);
                var startDay = (child.RegistrationDate.Year == dto.Year && child.RegistrationDate.Month == dto.Month)
                    ? child.RegistrationDate.Day
                    : 1;
                var endDay = (child.DeactivationDate.HasValue
                              && child.DeactivationDate.Value.Year == dto.Year
                              && child.DeactivationDate.Value.Month == dto.Month)
                    ? child.DeactivationDate.Value.Day
                    : daysInMonth;
                var daysActive = Math.Max(0, endDay - startDay + 1);
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
                    Notes = isPartialPeriod ? $"Dövr: {startDay}-{endDay} ({daysActive} gün)" : null,
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
            if (dto.RoundingDiscount.HasValue && dto.RoundingDiscount.Value > 0)
            {
                var roundingAmt = Math.Min(dto.RoundingDiscount.Value, payment.FinalAmount);
                payment.FinalAmount = Math.Max(0, payment.FinalAmount - roundingAmt);
                var discountNote = $"Yuvarlaqlaşdırma endirimi: {roundingAmt:F2} ₼";
                payment.Notes = string.IsNullOrWhiteSpace(dto.Notes)
                    ? discountNote
                    : $"{dto.Notes} | {discountNote}";
            }
            else
            {
                payment.Notes = dto.Notes;
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
            var processedPayments = new List<Payment>();
            decimal totalPaid = 0;

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

                    Payment payment;

                    if (existing != null)
                    {
                        payment = existing;
                    }
                    else
                    {
                        // Pro-rate when the child joined and/or left mid-month; bill in whole manats
                        var daysInMonth = DateTime.DaysInMonth(dto.Year, month);
                        var startDay = (child.RegistrationDate.Year == dto.Year && child.RegistrationDate.Month == month)
                            ? child.RegistrationDate.Day
                            : 1;
                        var endDay = (child.DeactivationDate.HasValue
                                      && child.DeactivationDate.Value.Year == dto.Year
                                      && child.DeactivationDate.Value.Month == month)
                            ? child.DeactivationDate.Value.Day
                            : daysInMonth;
                        var daysActive = Math.Max(0, endDay - startDay + 1);
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
                            Month = month,
                            Year = dto.Year,
                            OriginalAmount = baseAmount,
                            FinalAmount = finalAmount,
                            PaidAmount = 0,
                            DiscountType = hasDiscount ? DiscountType.Percentage : DiscountType.None,
                            DiscountValue = hasDiscount ? discountPercent : 0,
                            Notes = isPartialPeriod ? $"Dövr: {startDay}-{endDay} ({daysActive} gün)" : null,
                            RecordedById = recordedById,
                            Status = PaymentStatus.Debt
                        };
                        await _unitOfWork.Payments.AddAsync(payment);
                    }

                    // If final amount is 0 (e.g. 100% discount), just mark paid without charging anything.
                    var delta = Math.Max(0, payment.FinalAmount - payment.PaidAmount);
                    payment.PaidAmount = payment.FinalAmount;
                    payment.LastPaymentAmount = delta > 0 ? delta : payment.FinalAmount;
                    payment.Status = PaymentStatus.Paid;
                    payment.PaymentDate = now;
                    payment.CashboxId = dto.CashboxId;
                    payment.RecordedById = recordedById;

                    if (!string.IsNullOrWhiteSpace(dto.Notes))
                    {
                        var bulkNote = $"Kütləvi ödəniş: {dto.Notes}";
                        payment.Notes = string.IsNullOrWhiteSpace(payment.Notes)
                            ? bulkNote
                            : payment.Notes + " | " + bulkNote;
                    }

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

            return new RecordBulkPaymentResponse
            {
                PaidCount = processedPayments.Count,
                TotalPaid = totalPaid,
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

            var childName = $"{payment.Child.FirstName} {payment.Child.LastName}";
            var fileName = $"PaymentReceipt_{payment.Id}_{_dt.Now:yyyyMMddHHmmss}.pdf";
            var logoPath = Path.Combine(_env.ContentRootPath, "Templates", "KinderGardenLogo.png");
            var hasLogo = File.Exists(logoPath);
            var logoBytes = hasLogo ? File.ReadAllBytes(logoPath) : null;
            var paidDate = payment.PaymentDate ?? payment.UpdatedAt ?? _dt.Now;
            var bakuTimeZone = GetBakuTimeZone();
            var paidDateBaku = TimeZoneInfo.ConvertTimeFromUtc(
                paidDate.Kind == DateTimeKind.Utc ? paidDate : DateTime.SpecifyKind(paidDate, DateTimeKind.Utc),
                bakuTimeZone);
            var nowBaku = TimeZoneInfo.ConvertTimeFromUtc(_dt.Now, bakuTimeZone);
            var paidDateAz = $"{paidDateBaku.Day} {MonthNameAz(paidDateBaku.Month)} {paidDateBaku.Year}";
            var paymentDay = Math.Max(payment.Child.PaymentDay, 1);
            var thisMonthDays = DateTime.DaysInMonth(payment.Year, payment.Month);
            var periodStart = new DateTime(payment.Year, payment.Month, Math.Min(paymentDay, thisMonthDays), 0, 0, 0, DateTimeKind.Utc);
            DateTime periodEnd;
            if (paymentDay == 1)
            {
                // Ödəniş günü 1-dirsə: ayın 1-dən sonuna kimi (məs. 01.04 – 30.04)
                periodEnd = new DateTime(payment.Year, payment.Month, thisMonthDays, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                // Digər günlər: bu ayın N-dən gələn ayın (N-1)-nə kimi (məs. 02.05 – 01.06)
                var nextMonth = periodStart.AddMonths(1);
                var endDay = Math.Max(paymentDay - 1, 1);
                var nextMonthDays = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                periodEnd = new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(endDay, nextMonthDays), 0, 0, 0, DateTimeKind.Utc);
            }
            var periodRange = $"{periodStart:dd.MM.yyyy}-{periodEnd:dd.MM.yyyy}";
            var remaining = Math.Max(0, payment.FinalAmount - payment.PaidAmount);
            var installmentAmount = (payment.LastPaymentAmount ?? 0) > 0
                ? payment.LastPaymentAmount.Value
                : payment.PaidAmount;
            var statusText = payment.Status switch
            {
                PaymentStatus.Paid => "ÖDƏNİB",
                PaymentStatus.PartiallyPaid => "QİSMƏN ÖDƏNİB",
                _ => "BORC"
            };

            void BuildReceiptCopy(IContainer container, string copyTitle)
            {
                container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(9).Column(column =>
                {
                    column.Spacing(5);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("KINDERGARTEN BAKI").Bold().FontSize(16).FontColor(Colors.Blue.Darken2);
                            left.Item().Text("RƏSMİ ÖDƏNİŞ ÇEKİ").SemiBold().FontSize(12).FontColor(Colors.Grey.Darken2);
                            left.Item().Text(copyTitle).SemiBold().FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                        if (logoBytes != null)
                        {
                            row.ConstantItem(126).AlignRight().Height(74).Image(logoBytes, ImageScaling.FitArea);
                        }
                    });

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(7).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Çek №: KG-{payment.Id:D6}").Bold();
                            c.Item().Text($"Tarix: {paidDateAz}");
                            c.Item().Text($"Dövr: {periodRange}");
                        });

                        row.ConstantItem(140).AlignRight().Column(c =>
                        {
                            c.Item().Text("Status").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Background(Colors.Blue.Lighten4).Padding(6)
                                .AlignCenter().Text(statusText).Bold().FontColor(Colors.Blue.Darken3);
                        });
                    });

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(7).Column(c =>
                    {
                        c.Spacing(4);
                        c.Item().Text("Ödəyici məlumatı").SemiBold().FontColor(Colors.Grey.Darken2);
                        c.Item().Text($"Valideyn: {payment.Child.ParentFullName}");
                        c.Item().Text($"Əlaqə: {payment.Child.ParentPhone}");
                        c.Item().Text($"Uşaq: {childName}");
                        c.Item().Text($"Qrup: {payment.Child.Group?.Name ?? "-"}");
                        c.Item().Text($"Kassa: {payment.Cashbox?.Name ?? "-"} ({payment.Cashbox?.Type.ToString() ?? "-"})");
                    });

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(7).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Cell().PaddingBottom(4).Text("Açıqlama").SemiBold();
                        table.Cell().PaddingBottom(4).AlignRight().Text("Məbləğ").SemiBold();

                        table.Cell().Text("Əsas ödəniş");
                        table.Cell().AlignRight().Text($"{payment.OriginalAmount:F2} AZN");

                        table.Cell().Text("Yekun məbləğ").SemiBold();
                        table.Cell().AlignRight().Text($"{payment.FinalAmount:F2} AZN").SemiBold();

                        table.Cell().Text("Ödənilmiş məbləğ").SemiBold();
                        table.Cell().AlignRight().Text($"{installmentAmount:F2} AZN").SemiBold().FontColor(Colors.Green.Darken2);

                        table.Cell().Text("Qalıq borc");
                        table.Cell().AlignRight().Text($"{remaining:F2} AZN").FontColor(remaining > 0 ? Colors.Red.Darken1 : Colors.Green.Darken2);
                    });

                    if (!string.IsNullOrWhiteSpace(payment.Notes))
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(7).Text($"Qeyd: {payment.Notes}");

                    column.Item().PaddingTop(7).Row(row =>
                    {
                        row.RelativeItem().Text("Qəbul edən: __________________").FontSize(9);
                        row.RelativeItem().AlignRight().Text("İmza: __________________").FontSize(9);
                    });
                });
            }

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(6);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Content().Column(column =>
                    {
                        column.Spacing(5);
                        column.Item().MinHeight(320).Element(x => BuildReceiptCopy(x, "Müştəri nüsxəsi"));
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        column.Item().MinHeight(320).Element(x => BuildReceiptCopy(x, "Müəssisə nüsxəsi"));
                    });

                    page.Footer().Column(footer =>
                    {
                        footer.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        footer.Item().PaddingTop(2).AlignCenter().Text($"Uşaq Bağçası İdarəetmə Sistemi • {nowBaku:dd.MM.yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    });
                });
            }).GeneratePdf();

            return (pdfBytes, fileName);
        }

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
    }
}