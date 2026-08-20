using App.Business.DTOs.Children;
using App.Business.Services.Interfaces;
using App.Core.Common;
using App.Core.Entities;
using App.Core.Enums;
using App.Core.Exceptions;
using App.Core.Exceptions.Commons;
using App.Core.Services;
using App.DAL.UnitOfWork;
using App.Shared.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace App.Business.Services.Implementations
{
    /// <summary>
    /// Handles child management operations.
    /// </summary>
    public class ChildService : IChildService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDateTimeService _dt;
        private readonly IClaimService _claimService;

        public ChildService(IUnitOfWork unitOfWork, IMapper mapper, IDateTimeService dt, IClaimService claimService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _dt = dt;
            _claimService = claimService;
        }

        /// <summary>
        /// Müəllim yalnız öz qrupuna aid uşağa girə bilər.
        /// </summary>
        private async Task EnsureTeacherGroupAccessAsync(int childGroupId)
        {
            var role = _claimService.GetUserRole();
            if (role != "Teacher") return;

            var userId = _claimService.GetUserId();
            var group = await _unitOfWork.Groups.GetByIdAsync(childGroupId)
                ?? throw new EntityNotFoundException("Qrup tapılmadı.");

            var isPrimary  = group.TeacherId == userId;
            var isAssigned = await _unitOfWork.GroupTeachers.GetAsync(childGroupId, userId) != null;

            if (!isPrimary && !isAssigned)
                throw new UnauthorizedException("Bu uşağa giriş icazəniz yoxdur.");
        }

        /// <summary>
        /// Creates a new child record.
        /// </summary>
        public async Task<ChildResponse> CreateChildAsync(CreateChildRequest dto)
        {
            var groupExists = await _unitOfWork.Groups.ExistsAsync(dto.GroupId);
            if (!groupExists)
                throw new EntityNotFoundException($"{dto.GroupId} ID-li qrup tapılmadı.");

            // Check PersonId uniqueness BEFORE creating
            if (dto.PersonId.HasValue && dto.PersonId.Value > 0)
            {
                var existingPerson = (await _unitOfWork.Children.FindAsync(c => c.PersonId == dto.PersonId.Value)).FirstOrDefault();
                if (existingPerson != null)
                {
                    if (existingPerson.IsDeleted)
                    {
                        // Transfer PersonId from the deleted child to the new one
                        existingPerson.PersonId = null;
                        await _unitOfWork.Children.UpdateAsync(existingPerson);
                    }
                    else
                    {
                        throw new Core.Exceptions.ValidationException(new[] { $"Bu İVMS ID artıq {existingPerson.FirstName} {existingPerson.LastName} üçün istifadə olunur ({dto.PersonId.Value})" });
                    }
                }
            }

            var child = _mapper.Map<Child>(dto);
            child.RegistrationDate = _dt.Now;
            child.Status = ChildStatus.Active;

            await _unitOfWork.Children.AddAsync(child);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.GroupLogs.AddAsync(new GroupLog
            {
                GroupId = child.GroupId,
                ChildId = child.Id,
                ActionType = GroupLogActionType.ChildAdded,
                Message = $"Uşaq əlavə olundu: {child.FirstName} {child.LastName}",
                ActionDate = _dt.Now
            });
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Children.GetByIdAsync(
                c => c.Id == child.Id,
                c => c.Group,
                c => c.Group.Division);

            return _mapper.Map<ChildResponse>(created);
        }

        /// <summary>
        /// Updates an existing child record.
        /// </summary>
        public async Task<ChildResponse> UpdateChildAsync(int id, UpdateChildRequest dto)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li uşaq tapılmadı.");

            await EnsureTeacherGroupAccessAsync(child.GroupId);

            // Capture pre-change fee/discount so we can resync existing unpaid bills if either of them moves.
            var oldMonthlyFee  = child.MonthlyFee;
            var oldDiscountPct = child.DiscountPercentage ?? 0;
            // Çıxış tarixi dəyişərsə hesabları yenidən qurmaq üçün köhnə dəyəri saxlayırıq (D2/D3)
            var oldDeactivationDate = child.DeactivationDate;

            if (dto.FirstName != null) child.FirstName = dto.FirstName;
            if (dto.LastName != null) child.LastName = dto.LastName;
            if (dto.DateOfBirth.HasValue) child.DateOfBirth = dto.DateOfBirth.Value;
            if (dto.GroupId.HasValue) child.GroupId = dto.GroupId.Value;
            if (!string.IsNullOrWhiteSpace(dto.ScheduleType)) child.ScheduleType = dto.ScheduleType.Trim();
            if (dto.MonthlyFee.HasValue) child.MonthlyFee = dto.MonthlyFee.Value;
            if (dto.DiscountPercentage.HasValue) child.DiscountPercentage = dto.DiscountPercentage.Value;
            if (dto.PaymentDay.HasValue) child.PaymentDay = dto.PaymentDay.Value;
            if (dto.RegistrationDate.HasValue) child.RegistrationDate = dto.RegistrationDate.Value;
            // F2: profil redaktəsi də sütuna YALNIZ gecə yarısı yazır — üç yazma nöqtəsinin
            // (deaktivasiya, toplu deaktivasiya, profil) hamısı eyni formada saxlayır.
            if (dto.DeactivationDate.HasValue) child.DeactivationDate = dto.DeactivationDate.Value.Date;
            if (dto.ParentFullName != null) child.ParentFullName = dto.ParentFullName;
            if (dto.SecondParentFullName != null) child.SecondParentFullName = dto.SecondParentFullName;
            if (dto.ParentPhone != null) child.ParentPhone = dto.ParentPhone;
            if (dto.SecondParentPhone != null) child.SecondParentPhone = dto.SecondParentPhone;
            if (dto.ParentEmail != null) child.ParentEmail = dto.ParentEmail;

            // Check PersonId uniqueness BEFORE updating
            var newPersonId = dto.PersonId.HasValue && dto.PersonId.Value > 0 ? dto.PersonId.Value : (int?)null;

            if (newPersonId.HasValue)
            {
                var existingPerson = (await _unitOfWork.Children.FindAsync(c => c.PersonId == newPersonId.Value && c.Id != id)).FirstOrDefault();
                if (existingPerson != null)
                {
                    if (existingPerson.IsDeleted)
                    {
                        // Transfer PersonId from the deleted child to the new one
                        existingPerson.PersonId = null;
                        await _unitOfWork.Children.UpdateAsync(existingPerson);
                    }
                    else
                    {
                        throw new Core.Exceptions.ValidationException(new[] { $"Bu İVMS ID artıq {existingPerson.FirstName} {existingPerson.LastName} üçün istifadə olunur ({newPersonId.Value})" });
                    }
                }
            }

            child.PersonId = newPersonId;
            if (dto.FaceIdToken != null) child.FaceIdToken = dto.FaceIdToken;

            await _unitOfWork.Children.UpdateAsync(child);

            // If MonthlyFee or DiscountPercentage moved, retroactively fix every still-open bill for
            // this child so the parent isn't shown as a debtor for the gap (or as overpaid).
            var newDiscountPct = child.DiscountPercentage ?? 0;
            if (child.MonthlyFee != oldMonthlyFee || newDiscountPct != oldDiscountPct)
            {
                await ResyncUnpaidPaymentsAfterFeeChangeAsync(child);
            }

            // Çıxış tarixi FAKTİKİ olaraq dəyişibsə hesabları yenidən qur: çıxış ayı yenidən bölünür,
            // sonrakı aylar sıfırlanır. Eyni tarix təkrar göndərildikdə heç nə etmirik ki,
            // adi profil redaktəsi hesabları yenidən yazmasın.
            var deactivationMoved = dto.DeactivationDate.HasValue
                && child.DeactivationDate.HasValue
                && (!oldDeactivationDate.HasValue || oldDeactivationDate.Value.Date != child.DeactivationDate.Value.Date);

            DeactivationRecalcResult? recalcResult = null;

            if (deactivationMoved)
            {
                var exitDate = child.DeactivationDate!.Value;
                ValidateEffectiveDate(child, exitDate);

                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // D8: çıxış tarixi verilib, amma uşaq HƏLƏ AKTİVDİRSƏ — onu həqiqətən çıxarırıq.
                    // Əks halda hesablar çıxışa görə yenidən qurulur, uşaq isə aktiv qalır: aylıq
                    // generasiya ona tam sətir yazmağa davam edir, aktiv borclu siyahısında güzəştlə
                    // görünür və gecə işi onu qayıb kimi işarələməkdə davam edir.
                    // Rədd etmək əvəzinə statusu QURURUQ, çünki profil redaktəsi çıxış tarixini
                    // düzəltməyin əsas yoludur (aşağıda nəticə də qaytarılır) və DeactivateChildAsync
                    // eyni əməliyyatı artıq belə edir — iki yol arasında fərq qalmamalıdır.
                    if (child.Status != ChildStatus.Inactive)
                    {
                        child.Status = ChildStatus.Inactive;
                        await _unitOfWork.Children.UpdateAsync(child);

                        await _unitOfWork.GroupLogs.AddAsync(new GroupLog
                        {
                            GroupId = child.GroupId,
                            ChildId = child.Id,
                            ActionType = GroupLogActionType.ChildRemoved,
                            Message = $"Uşaq çıxarıldı: {child.FirstName} {child.LastName} ({exitDate:dd.MM.yyyy})",
                            ActionDate = _dt.Now
                        });
                    }

                    // Köhnə çıxış tarixi bərpa pəncərəsini determinləşdirir (D1).
                    recalcResult = await RecalculateAfterDeactivationAsync(child, exitDate, oldDeactivationDate);
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
            else
            {
                await _unitOfWork.SaveChangesAsync();
            }

            var updated = await _unitOfWork.Children.GetByIdAsync(
                c => c.Id == id,
                c => c.Group,
                c => c.Group.Division);

            var response = _mapper.Map<ChildResponse>(updated);
            // Nəticəni ATMIRIQ — profil redaktəsi çıxış tarixini düzəltməyin ƏSAS yoludur,
            // ödənişi olan aylar və artıq ödəniş burada da ştaba göstərilməlidir (D4).
            response.Recalculation = recalcResult;
            return response;
        }

        /// <summary>
        /// Re-bills every still-open payment for this child after their MonthlyFee or DiscountPercentage
        /// changed. Skips already-Paid rows, preserves pro-rated entry/exit periods (read from the
        /// PeriodStartDay/PeriodEndDay COLUMNS), reapplies the discount, and re-evaluates Status.
        /// </summary>
        private async Task ResyncUnpaidPaymentsAfterFeeChangeAsync(Child child)
        {
            var openPayments = (await _unitOfWork.Payments
                .FindAsync(p => p.ChildId == child.Id && p.Status != PaymentStatus.Paid))
                .ToList();

            if (openPayments.Count == 0) return;

            var discountPercent = child.DiscountPercentage ?? 0;
            var hasDiscount = discountPercent > 0;

            foreach (var payment in openPayments)
            {
                // Yarımçıq ay SÜTUNLARDAN oxunur — qeyd mətni artıq parse olunmur.
                var daysInMonth = DateTime.DaysInMonth(payment.Year, payment.Month);
                var daysActive = PeriodDays(payment, daysInMonth);

                var newOriginal = daysActive == daysInMonth
                    ? child.MonthlyFee
                    : Math.Round(child.MonthlyFee * daysActive / daysInMonth, 0, MidpointRounding.AwayFromZero);

                var rawFinal = hasDiscount
                    ? newOriginal - (newOriginal * discountPercent / 100m)
                    : newOriginal;
                var newFinal = Math.Round(rawFinal, 0, MidpointRounding.AwayFromZero);

                if (payment.OriginalAmount == newOriginal && payment.FinalAmount == newFinal) continue;

                var oldFinal = payment.FinalAmount;

                payment.OriginalAmount = newOriginal;
                payment.FinalAmount = newFinal;
                payment.DiscountType = hasDiscount ? DiscountType.Percentage : DiscountType.None;
                payment.DiscountValue = hasDiscount ? discountPercent : 0;

                // Audit trail in the Notes column (kosmetik)
                AppendNote(payment, $"Aylıq qiymət yeniləndi: {oldFinal:F0} → {newFinal:F0} ₼");

                // Recompute Status against the new FinalAmount
                if (payment.PaidAmount >= payment.FinalAmount)
                    payment.Status = PaymentStatus.Paid;
                else if (payment.PaidAmount > 0)
                    payment.Status = PaymentStatus.PartiallyPaid;
                else
                    payment.Status = PaymentStatus.Debt;

                await _unitOfWork.Payments.UpdateAsync(payment);
            }
        }

        /// <summary>Notes sütununun DB limiti (PaymentConfiguration).</summary>
        private const int NotesMaxLength = 500;

        /// <summary>
        /// Qeydi DB limitinə sığdıran YEGANƏ yer (F4) — həm əlavə, həm əvəzləmə buradan keçir.
        /// </summary>
        private static string TrimNote(string value) =>
            value.Length <= NotesMaxLength ? value : value[^NotesMaxLength..];

        /// <summary>Qeydi "|" ilə hissələrə ayırır. Yalnız GÖRÜNTÜ üçün — hesab məntiqi buradan oxumur.</summary>
        private static List<string> SplitNoteSegments(string? notes) =>
            string.IsNullOrWhiteSpace(notes)
                ? new List<string>()
                : notes.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        /// <summary>
        /// Qeydə yeni hissə ƏLAVƏ edir (köhnəni silmir). Eyni qeyd təkrarlanmır,
        /// limit aşılarsa ən köhnə hissə kəsilir.
        /// </summary>
        private static void AppendNote(Payment payment, string note)
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
        /// Eyni prefiksli KÖHNƏ hissələri atıb yenisini sona yazır (F2 — qeydlər yığılmasın).
        /// Tamamilə kosmetikdir: hesab vəziyyəti PeriodStartDay/PeriodEndDay/ZeroedByExitDate
        /// sütunlarındadır, bu funksiya yalnız ştabın oxuduğu mətni səliqəli saxlayır.
        /// </summary>
        private static void UpsertNoteSegment(Payment payment, string marker, string? note)
        {
            var segments = SplitNoteSegments(payment.Notes)
                .Where(s => !s.Contains(marker, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!string.IsNullOrWhiteSpace(note)) segments.Add(note);

            payment.Notes = segments.Count == 0 ? null : TrimNote(string.Join(" | ", segments));
        }

        /// <summary>Dövr qeydini (kosmetik) yeniləyir — köhnə "Dövr:" hissəsi atılır.</summary>
        private static void UpsertPeriodNote(Payment payment, string periodNote) =>
            UpsertNoteSegment(payment, PeriodNoteMarker, periodNote);

        /// <summary>Sıfırlama qeydini (kosmetik) yeniləyir — köhnə sıfırlama hissəsi atılır.</summary>
        private static void UpsertZeroedNote(Payment payment, string zeroedNote) =>
            UpsertNoteSegment(payment, ZeroedByExitMarker, zeroedNote);

        /// <summary>Sətir bərpa olunanda köhnə sıfırlama qeydi tamamilə silinir (kosmetik).</summary>
        private static void RemoveZeroedNote(Payment payment) =>
            UpsertNoteSegment(payment, ZeroedByExitMarker, null);

        /// <summary>Qeyddəki dövr hissəsinin prefiksi (yalnız mətn təmizliyi üçün).</summary>
        private const string PeriodNoteMarker = "Dövr:";

        /// <summary>
        /// Sətrin hesablanan gün sayı — YALNIZ sütunlardan. Sütunlar boşdursa tam ay.
        /// Sütun bitişi İNKLÜZİVDİR, ona görə arifmetika endDay - startDay + 1-dir.
        /// C2b: BOŞ dövr (uşaq həmin ay heç gəlməyib) sütunlarda bitiş &lt; başlanğıc kimi saxlanılır
        /// (məs. 1/0). Aşağı sərhəd 0-dır və nəticə mənfi ola bilmədiyi üçün belə sətir düzgün olaraq
        /// 0 gün oxunur — köhnə "endDay = startDay" sıxılması onu səhvən 1 gün kimi göstərirdi
        /// və qiymət dəyişəndə (ResyncUnpaidPaymentsAfterFeeChangeAsync) 0 ₼-lıq ayı diriltmiş olardı.
        /// </summary>
        private static int PeriodDays(Payment payment, int daysInMonth)
        {
            var startDay = Math.Clamp(payment.PeriodStartDay ?? 1, 1, daysInMonth);
            var endDay = Math.Clamp(payment.PeriodEndDay ?? daysInMonth, 0, daysInMonth);
            return Math.Max(0, endDay - startDay + 1);
        }

        /// <summary>
        /// Gets a child's full details including attendance and payment summaries.
        /// </summary>
        public async Task<ChildDetailResponse> GetChildByIdAsync(int id)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(
                c => c.Id == id,
                c => c.Group,
                c => c.Group.Division,
                c => c.Group.Teacher)
                ?? throw new EntityNotFoundException($"{id} ID-li uşaq tapılmadı.");

            await EnsureTeacherGroupAccessAsync(child.GroupId);

                            var response = _mapper.Map<ChildDetailResponse>(child);

            var now = DateOnly.FromDateTime(_dt.Now);
            var monthStart = new DateOnly(now.Year, now.Month, 1);
            var attendances = await _unitOfWork.Attendances.GetChildAttendanceAsync(id, monthStart, now);
            response.AttendanceDays = attendances.Count(a => a.Status == AttendanceStatus.Present);
            response.AbsentDays = attendances.Count(a => a.Status == AttendanceStatus.Absent);

            var payments = await _unitOfWork.Payments.GetPaymentsByChildAsync(id);
            // C1: GÜZƏŞT YOXDUR — borc sətir yarandığı andan görünür (ştabın qərarı).
            // Borclu siyahısı (GetDebtorsAsync) və Deaktivlər siyahısı (GetInactiveDebtorsAsync) də
            // eyni qaydadadır, ona görə uşaq kartı ilə siyahılar arasında fərq qalmır (D1/D5).
            // Uşağın statusuna görə ayrılan köhnə şərt (applyGrace) artıq ölü koddur — silindi.
            response.TotalDebt = payments
                .Where(p => p.Status != PaymentStatus.Paid)
                .Sum(p => p.FinalAmount - p.PaidAmount);

            return response;
        }

        /// <summary>
        /// Gets all children with filtering and pagination.
        /// </summary>
        public async Task<PagedResponse<ChildResponse>> GetAllChildrenAsync(ChildFilterRequest filter)
        {
            var children = await _unitOfWork.Children.GetChildrenWithDetailsAsync();
            var query = children.AsQueryable().Where(x => x.IsDeleted == false);

            // Müəllim yalnız öz qruplarının uşaqlarını görə bilər
            var role = _claimService.GetUserRole();
            if (role == "Teacher")
            {
                var userId = _claimService.GetUserId();
                var teacherGroupIds = (await _unitOfWork.GroupTeachers.GetByGroupAsync(0))
                    .Select(gt => gt.GroupId).ToList();

                // primary teacher olduğu qrupları da əlavə et
                var allGroups = await _unitOfWork.Groups.FindAsync(g => g.TeacherId == userId);
                var primaryGroupIds = allGroups.Select(g => g.Id).ToList();

                var assignedGroupIds = (await _unitOfWork.Context.GroupTeachers
                    .Where(gt => gt.UserId == userId)
                    .Select(gt => gt.GroupId)
                    .ToListAsync());

                var allowedGroupIds = primaryGroupIds.Union(assignedGroupIds).ToHashSet();
                query = query.Where(c => allowedGroupIds.Contains(c.GroupId));
            }

            if (filter.GroupId.HasValue)
                query = query.Where(c => c.GroupId == filter.GroupId.Value);
            if (filter.DivisionId.HasValue)
                query = query.Where(c => c.Group.DivisionId == filter.DivisionId.Value);
            if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<ChildStatus>(filter.Status, true, out var status))
                query = query.Where(c => c.Status == status);
            if (!string.IsNullOrEmpty(filter.ScheduleType))
                query = query.Where(c => c.ScheduleType == filter.ScheduleType);

            var totalCount = query.Count();

            // PageSize <= 0 göndərildikdə bütün nəticələr qaytar
            List<Child> items;
            int pageSize, page, totalPages;

            if (filter.PageSize <= 0)
            {
                items      = query.ToList();
                pageSize   = totalCount == 0 ? 1 : totalCount;
                page       = 1;
                totalPages = 1;
            }
            else
            {
                pageSize   = filter.PageSize;
                page       = Math.Max(filter.Page, 1);
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                items      = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            }

            return new PagedResponse<ChildResponse>
            {
                Items = _mapper.Map<IEnumerable<ChildResponse>>(items),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        /// <summary>
        /// Activates a child. <paramref name="returnDate"/> — uşağın YENİDƏN GƏLDİYİ İLK gün
        /// (H1, İNKLÜZİV): həmin gün HESABLANIR. Boşdursa bugün.
        ///
        /// H1-dən əvvəl qaytarma sadəcə statusu dəyişirdi və BÜTÜN sıfırlanmış ayları — qayıdış
        /// ayının özü də daxil olmaqla — "uşaq həqiqətən gəlmədi" kimi yekunlaşdırırdı (A2).
        /// Nəticədə uşağın geri qayıtdığı ay 0 ₼ / "Ödənilib" kimi donurdu: aylıq generasiya işi
        /// mövcud sətrin üstünə yazmadığı üçün həmin ay bir daha hesablanmırdı və ştab ödənişi
        /// qeyd edə bilmirdi. İndi zaman oxu qayıdış tarixinə görə üç yerə bölünür:
        ///  • qayıdış ayından ƏVVƏLKİ sıfırlanmış aylar — yekunlaşdırılır (A2, dəyişməyib),
        ///  • qayıdış ayı — [qayıdış günü, ayın sonu] üzrə YENİDƏN HESABLANIR (sətir yoxdursa yaradılır),
        ///  • qayıdış ayından SONRAKI sıfırlanmış aylar — tam aya bərpa olunur.
        /// </summary>
        public async Task<ReactivationResult> ActivateChildAsync(int id, DateTime? returnDate = null)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li uşaq tapılmadı.");

            // F2 ilə eyni qayda — tarix HƏMİŞƏ gecə yarısıdır.
            var effectiveReturnDate = (returnDate ?? _dt.Now).Date;
            ValidateReturnDate(child, effectiveReturnDate);

            ReactivationResult result;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                result = await ApplyReturnAsync(child, effectiveReturnDate);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return result;
        }

        /// <summary>
        /// Qayıdışın BÜTÜN yan təsirlərini tətbiq edir (status, hesablar, jurnal).
        /// Tək və toplu qaytarma eyni bu metoddan keçir ki, davranış ayrılmasın.
        /// Transaksiya və SaveChanges çağıran metodun üzərindədir.
        /// </summary>
        private async Task<ReactivationResult> ApplyReturnAsync(Child child, DateTime returnDate)
        {
            // Bağlanan epizodun çıxış tarixi — sahə TƏMİZLƏNMƏDƏN ƏVVƏL saxlanılır (G1).
            var closingExitDate = child.DeactivationDate;

            child.Status = ChildStatus.Active;
            child.DeactivationDate = null;

            var result = new ReactivationResult
            {
                ChildId = child.Id,
                ChildFullName = $"{child.FirstName} {child.LastName}",
                ReturnDate = returnDate
            };

            await ConfirmZeroedMonthsAsFinalAsync(child, closingExitDate, returnDate, result);
            await RebillReturnMonthAsync(child, returnDate, closingExitDate, result);
            await RestoreMonthsAfterReturnAsync(child, returnDate, result);

            await _unitOfWork.GroupLogs.AddAsync(new GroupLog
            {
                GroupId = child.GroupId,
                ChildId = child.Id,
                ActionType = GroupLogActionType.ChildReturned,
                Message = $"Uşaq qrupa geri qaytarıldı: {child.FirstName} {child.LastName} ({returnDate:dd.MM.yyyy})",
                ActionDate = _dt.Now
            });

            await _unitOfWork.Children.UpdateAsync(child);

            return result;
        }

        /// <summary>
        /// Qayıdış tarixini yoxlayır: qəbul tarixindən əvvəl və ya SABAHDAN gec ola bilməz.
        /// Çıxış tarixindən fərqli olaraq bu tarix İNKLÜZİVDİR — uşaq həmin gün gəlir.
        /// </summary>
        private void ValidateReturnDate(Child child, DateTime returnDate)
        {
            var maxDate = _dt.Now.Date.AddDays(1);

            if (returnDate.Date > maxDate)
                throw new Core.Exceptions.ValidationException("Qayıdış tarixi sabahdan gec ola bilməz.");

            if (returnDate.Date < child.RegistrationDate.Date)
                throw new Core.Exceptions.ValidationException(
                    $"Qayıdış tarixi qəbul tarixindən ({child.RegistrationDate:dd.MM.yyyy}) əvvəl ola bilməz.");
        }

        /// <summary>Ay indeksi — il sərhədini (dekabr → yanvar) özü-özünə həll edir.</summary>
        private static int MonthIndex(int year, int month) => year * 12 + (month - 1);

        /// <summary>
        /// Uşaq geri qayıdanda BAĞLANAN epizodun sətirlərini YEKUNLAŞDIRIR (A2/G1).
        /// Məbləğlərə və dövr sütunlarına toxunulmur — yalnız ZeroedByExitDate TƏMİZLƏNİR və
        /// AbsenceConfirmed qoyulur. İki qrup sətir yekunlaşdırılır:
        ///  (a) çıxışla sıfırlanmış aylar (0/0/Paid) — uşaq həmin aylarda həqiqətən gəlməyib,
        ///  (b) <paramref name="closingExitDate"/> ayının sətri — epizodun gün-gün bölünmüş ÇIXIŞ ayı.
        /// (b) olmadan çıxış ayı bağlanan epizodun YEGANƏ qorunmasız sətri qalırdı: sonrakı bir
        /// deaktivasiya onu sıfırlaya, ondan sonrakı düzəliş isə TAM aya qaytara bilirdi (G1).
        ///
        /// H1: pəncərə QAYIDIŞ AYI ilə məhdudlaşır — yalnız ondan ƏVVƏLKİ aylar möhürlənir.
        /// Qayıdış ayının özü və sonrakı aylar uşağın GƏLDİYİ aylardır; onları burada
        /// "həqiqətən gəlmədi" kimi yekunlaşdırmaq həmin ayları əbədi 0 ₼-da dondururdu.
        /// SaveChanges çağıran metodun üzərindədir.
        /// </summary>
        private async Task ConfirmZeroedMonthsAsFinalAsync(
            Child child, DateTime? closingExitDate, DateTime returnDate, ReactivationResult result)
        {
            var returnIndex = MonthIndex(returnDate.Year, returnDate.Month);

            // Yalnız BİZİM sıfırladığımız sətirlər — 100% endirimli qanuni 0/0/Paid sətri kənarda qalır.
            var zeroedPayments = (await _unitOfWork.Payments
                .FindAsync(p => p.ChildId == child.Id && p.ZeroedByExitDate != null))
                .ToList();

            foreach (var payment in zeroedPayments)
            {
                // Qayıdış ayı və ondan sonrakı aylar bu metodun işi deyil (H1).
                if (MonthIndex(payment.Year, payment.Month) >= returnIndex) continue;

                payment.ZeroedByExitDate = null;
                // D2: boş ZeroedByExitDate tək başına "yekunlaşdırılmış" demək DEYİL — heç vaxt
                // sıfırlanmamış sətirdə də boşdur. Yekun AYRICA sütunda saxlanılır ki, sonrakı
                // sıfırlama dövrü bu sətrə toxunmasın və səhv yazılmış çıxış tarixi onu dirildə bilməsin.
                payment.AbsenceConfirmed = true;
                UpsertZeroedNote(payment, FinalAbsenceNote);

                await _unitOfWork.Payments.UpdateAsync(payment);

                result.ConfirmedMonths.Add(new ConfirmedAbsenceMonth
                {
                    PaymentId = payment.Id,
                    Month = payment.Month,
                    Year = payment.Year
                });
            }

            // G1: bağlanan epizodun ÇIXIŞ ayı. Sıfırlanmır (uşaq həmin ayın bir hissəsində gəlib),
            // ona görə yuxarıdakı dövrə düşmür — amma məbləği və dövrü də YEKUNDUR.
            if (!closingExitDate.HasValue) return;

            // H1: uşaq ELƏ HƏMİN ayda (və ya daha sonra) geri qayıdırsa çıxış ayı yekun deyil —
            // qayıdış günlərini RebillReturnMonthAsync həmin sətrə əlavə edəcək.
            if (MonthIndex(closingExitDate.Value.Year, closingExitDate.Value.Month) >= returnIndex) return;

            var exitMonthPayment = (await _unitOfWork.Payments
                .FindAsync(p => p.ChildId == child.Id
                             && p.Month == closingExitDate.Value.Month
                             && p.Year == closingExitDate.Value.Year))
                .FirstOrDefault();

            if (exitMonthPayment == null || exitMonthPayment.AbsenceConfirmed) return;

            // G3: dövr BOŞDURSA (0 gün) yekunlaşdırMIRIQ. C2-dən sonra çıxış ayı qanuni olaraq
            // 0 gün ola bilər — məs. 01.09 seçilib ("son gəlmə 31 avqust"), sentyabr 0 ₼-dir.
            // Belə sətirdə qoruyacaq məbləğ yoxdur; möhür vursaq, sonrakı yenidən hesablamalar
            // ondan yan keçər və uşaq geri qayıdanda həmin ay bir daha düzəldilə bilməzdi.
            //
            // DİQQƏT: dövr sütunları BOŞ (NULL) olanda bu "0 gün" DEMƏK DEYİL — bütün kod bazasında
            // NULL "TAM AY" mənasını verir (25.07 miqrasiyası da yalnız "Dövr:" markeri olan sətirləri
            // doldurub, qalanları qəsdən NULL saxlayıb). Ona görə burada mütləq PeriodDays köməkçisi
            // işlədilir — o, NULL bitişi daysInMonth kimi oxuyur. Əks halda 25.07-dən əvvəlki HƏR
            // tam-ay sətri səhvən "0 gün" sayılıb möhürsüz qalar və G1 qoruması itərdi.
            var daysInExitMonth = DateTime.DaysInMonth(exitMonthPayment.Year, exitMonthPayment.Month);
            if (PeriodDays(exitMonthPayment, daysInExitMonth) <= 0) return;

            // Məbləğ, PaidAmount və PeriodStartDay/PeriodEndDay OLDUĞU KİMİ qalır — yalnız bayraq.
            exitMonthPayment.AbsenceConfirmed = true;
            AppendNote(exitMonthPayment, FinalExitMonthNote);

            await _unitOfWork.Payments.UpdateAsync(exitMonthPayment);
        }

        /// <summary>
        /// Uşağın GERİ QAYITDIĞI ayı yenidən hesablayır (H1) — qaytarmanın əsas düzəlişi.
        /// Dövr [qayıdış günü, ayın sonu]-dur (hər iki ucu daxil), məbləğ isə generasiya ilə EYNİ
        /// qayda üzrə bölünür. Sətir yoxdursa YARADILIR: aylıq generasiya işi mövcud olmayan
        /// keçmiş ayı bir daha yazmır, ona görə həmin ay heç vaxt hesablanmazdı.
        ///
        /// Sətrin vəziyyətinə görə üç davranış var:
        ///  • sıfırlanmış / "gəlmədi" möhürü vurulmuş sətir (0/0) — tamamilə yenidən yazılır,
        ///    möhür götürülür (uşaq həmin ay GƏLİB, yoxluq təsdiqi səhv idi);
        ///  • uşaq elə həmin ay çıxıb geri qayıdıbsa (bölünmüş çıxış ayı) — qayıdış günləri
        ///    mövcud günlərin ÜSTÜNƏ gəlir;
        ///  • real pul ödənilmiş sətrə (D1) və onsuz da tam hesablanmış aya toxunulmur — bildirilir.
        /// SaveChanges çağıran metodun üzərindədir (yeni sətir istisnadır — real ID lazımdır).
        /// </summary>
        private async Task RebillReturnMonthAsync(
            Child child, DateTime returnDate, DateTime? closingExitDate, ReactivationResult result)
        {
            var year = returnDate.Year;
            var month = returnDate.Month;
            var daysInMonth = DateTime.DaysInMonth(year, month);

            // Qayıdış tarixi qəbul tarixindən əvvəl ola bilmir (ValidateReturnDate), ona görə
            // başlanğıc HƏMİŞƏ qayıdış günüdür — qeydiyyat günü ilə toqquşma yaranmır.
            var returnStartDay = returnDate.Day;
            var returnDays = daysInMonth - returnStartDay + 1;

            var existing = (await _unitOfWork.Payments
                .FindAsync(p => p.ChildId == child.Id && p.Month == month && p.Year == year))
                .FirstOrDefault();

            if (existing == null)
            {
                var (newOriginal, newFinal, hasNewDiscount, newDiscountPercent) =
                    BillPartialMonth(child, returnDays, daysInMonth);

                var payment = new Payment
                {
                    ChildId = child.Id,
                    Month = month,
                    Year = year,
                    OriginalAmount = newOriginal,
                    FinalAmount = newFinal,
                    PaidAmount = 0,
                    LastPaymentAmount = null,
                    // Məbləğ 0-dırsa ödəniş gözlənilmir — generasiya ilə eyni qayda.
                    Status = newFinal <= 0 ? PaymentStatus.Paid : PaymentStatus.Debt,
                    DiscountType = hasNewDiscount ? DiscountType.Percentage : DiscountType.None,
                    DiscountValue = hasNewDiscount ? newDiscountPercent : 0,
                    PeriodStartDay = returnStartDay,
                    PeriodEndDay = daysInMonth,
                    Notes = TrimNote($"Dövr: {returnStartDay}-{daysInMonth} ({returnDays} gün) | {BuildReturnNote(returnDate)}"),
                    RecordedById = "system"
                };

                await _unitOfWork.Payments.AddAsync(payment);
                // Hesabatdakı PaymentId real olmalıdır — açıq transaksiyanın İÇİNDƏ saxlanılır.
                await _unitOfWork.SaveChangesAsync();

                result.ReturnMonth = new ReturnMonthOutcome
                {
                    PaymentId = payment.Id,
                    Month = month,
                    Year = year,
                    FinalAmount = newFinal,
                    PaidAmount = 0,
                    PeriodStartDay = returnStartDay,
                    PeriodEndDay = daysInMonth,
                    BilledDays = returnDays,
                    Created = true,
                    NeedsManualReview = false
                };
                return;
            }

            // D1: real pul ödənilmiş sətrin məbləğini avtomatik yenidən yazmırıq — ştab qərar verir.
            if (existing.PaidAmount > 0)
            {
                // Məbləğə toxunmuruq, AMMA sıfırlama açarını da köhnə çıxışda saxlamırıq: uşağın
                // artıq çıxış tarixi yoxdur və PaymentService qapısı (FinalAmount == 0 &&
                // ZeroedByExitDate dolu) ştaba bu ay üçün ödəniş qeyd etməyə imkan verməzdi.
                existing.ZeroedByExitDate = null;
                await _unitOfWork.Payments.UpdateAsync(existing);

                result.ReturnMonth = BuildUntouchedReturnMonth(existing, daysInMonth,
                    "Ay real pul ilə ödənilib — məbləğ avtomatik dəyişdirilmədi");
                return;
            }

            // Sətir sıfırlanmış formadadırsa (çıxışla sıfırlanmış, yaxud səhvən "gəlmədi" möhürü
            // vurulmuş ay) ARTIQ HESABLANMIŞ gün yoxdur — dövr sütunları köhnə hesablamadan qala bilər,
            // ona görə gün sayı SÜTUNDAN DEYİL, MƏBLƏĞ formasından çıxarılır.
            var zeroedShape = existing.OriginalAmount == 0 && existing.FinalAmount == 0;
            var priorDays = zeroedShape ? 0 : PeriodDays(existing, daysInMonth);

            if (priorDays >= daysInMonth)
            {
                result.ReturnMonth = BuildUntouchedReturnMonth(existing, daysInMonth,
                    "Ay onsuz da tam hesablanıb — dəyişiklik lazım deyil");
                return;
            }

            var priorStartDay = Math.Clamp(existing.PeriodStartDay ?? 1, 1, daysInMonth);
            var priorEndDay = Math.Clamp(existing.PeriodEndDay ?? daysInMonth, 0, daysInMonth);

            int startDay;
            int totalDays;
            string periodNote;

            if (priorDays == 0)
            {
                startDay = returnStartDay;
                totalDays = returnDays;
                periodNote = $"Dövr: {startDay}-{daysInMonth} ({totalDays} gün)";
            }
            else if (priorEndDay >= returnStartDay - 1)
            {
                // Boşluq yoxdur — çıxış və qayıdış günləri birləşib bitişik aralıq verir.
                startDay = priorStartDay;
                totalDays = daysInMonth - startDay + 1;
                periodNote = $"Dövr: {startDay}-{daysInMonth} ({totalDays} gün)";
            }
            else
            {
                // İKİ AYRI aralıq (məs. 1-9 gəlib, 10-18 gəlməyib, 19-31 yenidən gəlir). Sxemdə
                // bir sətir üçün YALNIZ BİR dövr saxlanılır, ona görə sütunlara GÜN SAYINI qoruyan
                // aralıq yazılır (ayın sonundan geri sayılır) — bütün hesab məntiqi (PeriodDays,
                // qiymət dəyişəndə resync, UI-dakı "niyə bu məbləğ?" paneli) məhz gün SAYINI oxuyur,
                // ona görə pul hər halda doğru qalır. Həqiqi aralıqlar qeyddə açıq yazılır.
                totalDays = priorDays + returnDays;
                startDay = daysInMonth - totalDays + 1;
                periodNote = $"Dövr: {priorStartDay}-{priorEndDay} + {returnStartDay}-{daysInMonth} ({totalDays} gün)";
            }

            var (originalAmount, finalAmount, hasDiscount, discountPercent) =
                BillPartialMonth(child, totalDays, daysInMonth);

            existing.OriginalAmount = originalAmount;
            existing.FinalAmount = finalAmount;
            existing.DiscountType = hasDiscount ? DiscountType.Percentage : DiscountType.None;
            existing.DiscountValue = hasDiscount ? discountPercent : 0;
            existing.PeriodStartDay = startDay;
            existing.PeriodEndDay = daysInMonth;
            // Sətir yenidən hesablandı: nə "çıxışa görə sıfırlanmış", nə də "təsdiqlənmiş yoxluq"dur.
            existing.ZeroedByExitDate = null;
            existing.AbsenceConfirmed = false;
            existing.Status = finalAmount <= 0 ? PaymentStatus.Paid : PaymentStatus.Debt;

            RemoveZeroedNote(existing);
            RemoveFinalizationNotes(existing);
            UpsertPeriodNote(existing, periodNote);
            AppendNote(existing, BuildReturnNote(returnDate));

            await _unitOfWork.Payments.UpdateAsync(existing);

            result.ReturnMonth = new ReturnMonthOutcome
            {
                PaymentId = existing.Id,
                Month = month,
                Year = year,
                FinalAmount = finalAmount,
                PaidAmount = existing.PaidAmount,
                PeriodStartDay = startDay,
                PeriodEndDay = daysInMonth,
                BilledDays = totalDays,
                Created = false,
                NeedsManualReview = false
            };
        }

        /// <summary>Toxunulmayan qayıdış ayının hesabat sətri — səbəb ştaba göstərilir.</summary>
        private static ReturnMonthOutcome BuildUntouchedReturnMonth(Payment payment, int daysInMonth, string reason) =>
            new()
            {
                PaymentId = payment.Id,
                Month = payment.Month,
                Year = payment.Year,
                FinalAmount = payment.FinalAmount,
                PaidAmount = payment.PaidAmount,
                PeriodStartDay = payment.PeriodStartDay ?? 1,
                PeriodEndDay = payment.PeriodEndDay ?? daysInMonth,
                BilledDays = PeriodDays(payment, daysInMonth),
                Created = false,
                NeedsManualReview = true,
                Reason = reason
            };

        /// <summary>
        /// Qayıdış ayından SONRAKI sıfırlanmış sətirləri tam aya qaytarır (H1). Uşaq artıq aktivdir,
        /// ona görə bu aylar normal hesablanmalıdır; əks halda gələcək ay 0 ₼-da donub qalardı
        /// (aylıq generasiya mövcud sətrin üstünə yazmır).
        /// SaveChanges çağıran metodun üzərindədir.
        /// </summary>
        private async Task RestoreMonthsAfterReturnAsync(Child child, DateTime returnDate, ReactivationResult result)
        {
            var returnIndex = MonthIndex(returnDate.Year, returnDate.Month);

            var zeroedPayments = (await _unitOfWork.Payments
                .FindAsync(p => p.ChildId == child.Id && p.ZeroedByExitDate != null))
                .ToList();

            foreach (var payment in zeroedPayments)
            {
                if (MonthIndex(payment.Year, payment.Month) <= returnIndex) continue;

                var finalAmount = ApplyFullMonthBilling(child, payment);

                RemoveZeroedNote(payment);
                RemoveFinalizationNotes(payment);
                AppendNote(payment, BuildReturnNote(returnDate));

                await _unitOfWork.Payments.UpdateAsync(payment);

                result.RestoredMonths.Add(new RestoredMonth
                {
                    PaymentId = payment.Id,
                    Month = payment.Month,
                    Year = payment.Year,
                    FinalAmount = finalAmount,
                    PaidAmount = payment.PaidAmount
                });
            }
        }

        /// <summary>Qayıdış izi (kosmetik) — sətrin niyə yenidən hesablandığını ştab görməlidir.</summary>
        private static string BuildReturnNote(DateTime returnDate) =>
            $"Uşaq {returnDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} tarixindən geri qayıtdı — ay yenidən hesablandı";

        /// <summary>
        /// Yarımçıq ayın məbləği — PaymentService.GenerateMonthlyDebtsAsync və çıxış ayı hesabı ilə
        /// EYNİ qayda: tam ay üçün MonthlyFee, əks halda gün-gün bölgü, sonra endirim və eyni
        /// yuvarlaqlaşdırma (tam manat, yarımlar yuxarı).
        /// </summary>
        private static (decimal OriginalAmount, decimal FinalAmount, bool HasDiscount, decimal DiscountPercent)
            BillPartialMonth(Child child, int daysActive, int daysInMonth)
        {
            var originalAmount = daysActive >= daysInMonth
                ? child.MonthlyFee
                : Math.Round(child.MonthlyFee * daysActive / daysInMonth, 0, MidpointRounding.AwayFromZero);

            var discountPercent = child.DiscountPercentage ?? 0;
            var hasDiscount = discountPercent > 0;
            var rawFinal = hasDiscount
                ? originalAmount - (originalAmount * discountPercent / 100m)
                : originalAmount;

            return (originalAmount, Math.Round(rawFinal, 0, MidpointRounding.AwayFromZero), hasDiscount, discountPercent);
        }

        /// <summary>
        /// Creates or adjusts the pro-rated payment for a child leaving mid-month.
        /// C2: çıxış tarixi ARTIQ GƏLMƏDİYİ İLK gündür (EKSKLÜZİV) — həmin gün HESABLANMIR.
        /// Qeydiyyat tarixi isə İNKLÜZİVDİR (uşaq qeydiyyat günü gəlir), ona görə dövr yarım-açıqdır:
        /// [startDay, endExclusive) → daysActive = max(0, endExclusive - startDay).
        /// Məs. 1 avqust seçilsə avqust 0 gün (0 ₼); 5-də qeydiyyat + 26-da çıxış → 21 gün.
        /// F3: çıxış ayının nəticəsi HƏMİŞƏ <paramref name="result"/>.ExitMonth-a yazılır — əməliyyatın
        /// toxunduğu heç bir ay hesabatdan kənarda qalmır (yeni yaradılan sətir də daxil).
        /// </summary>
        private async Task ApplyExitMonthPaymentAsync(Child child, DateTime exitDate, DeactivationRecalcResult result)
        {
            var month = exitDate.Month;
            var year = exitDate.Year;
            var daysInMonth = DateTime.DaysInMonth(year, month);

            // If the child registered in the same month, count from registration day; otherwise from day 1.
            var startDay = (child.RegistrationDate.Year == year && child.RegistrationDate.Month == month)
                ? child.RegistrationDate.Day
                : 1;
            // Bu metod HƏMİŞƏ çıxış tarixinin öz ayını hesablayır, ona görə endExclusive = çıxış günü.
            var endExclusive = exitDate.Day;
            var daysActive = Math.Max(0, endExclusive - startDay);
            // C2b: SÜTUN İNKLÜZİV qalır — son HESABLANAN gün. 0 günlük dövrdə startDay - 1 olur
            // (məs. 1 avqust çıxışı → PeriodStartDay=1, PeriodEndDay=0), yəni boş aralıq açıq şəkildə
            // "bitiş < başlanğıc" kimi yazılır və oxuyan tərəf onu 1 gün kimi qəbul edə bilmir.
            var lastBilledDay = endExclusive - 1;

            // Bill in whole manats — no qəpik fractions in the bill
            var proratedBase = Math.Round(child.MonthlyFee * daysActive / daysInMonth, 0, MidpointRounding.AwayFromZero);
            var discountPercent = child.DiscountPercentage ?? 0;
            var hasDiscount = discountPercent > 0;
            var rawFinal = hasDiscount
                ? proratedBase * (1 - discountPercent / 100)
                : proratedBase;
            var finalAmount = Math.Round(rawFinal, 0, MidpointRounding.AwayFromZero);

            // 0 günlük dövrdə "1-0" mənasız görünərdi — marker ("Dövr:") saxlanılır ki,
            // UpsertPeriodNote köhnə hissəni tapıb əvəz edə bilsin.
            var periodNote = daysActive == 0
                ? "Dövr: 0 gün (uşaq bu ay gəlməyib)"
                : $"Dövr: {startDay}-{lastBilledDay} ({daysActive} gün)";

            var existing = (await _unitOfWork.Payments
                .FindAsync(p => p.ChildId == child.Id && p.Month == month && p.Year == year))
                .FirstOrDefault();

            if (existing != null)
            {
                // F3(b): BAĞLANMIŞ epizodun yekun sətri (uşaq geri qayıdanda yekunlaşdırılmış
                // 0/0/Paid "gəlmədiyi ay" və ya həmin epizodun gün-gün bölünmüş çıxış ayı — G1)
                // SONRAKI bir çıxışın ayına düşə bilər. Belə sətri yenidən yazmaq yekunu səssizcə
                // ləğv edərdi — sıfırlama dövrü də məhz buna görə AbsenceConfirmed sətirlərinə
                // toxunmur. Sətir olduğu kimi qalır, ştaba isə ƏL İLƏ yoxlama üçün bildirilir.
                if (existing.AbsenceConfirmed)
                {
                    result.ExitMonth = new ExitMonthOutcome
                    {
                        PaymentId = existing.Id,
                        Month = month,
                        Year = year,
                        FinalAmount = existing.FinalAmount,
                        PaidAmount = existing.PaidAmount,
                        // Sətrə TOXUNULMADIĞI üçün diskdəki dövr bildirilir — hesablanan (yazılmayan)
                        // startDay/exitDay bildirilsəydi məbləğlə dövr bir-birini təkzib edərdi.
                        PeriodStartDay = existing.PeriodStartDay ?? startDay,
                        PeriodEndDay = existing.PeriodEndDay ?? daysInMonth,
                        Created = false,
                        NeedsManualReview = true,
                        Reason = "Ay bağlanmış qeydiyyat dövründə yekunlaşdırılıb — avtomatik yenidən hesablanmadı"
                    };
                    return;
                }

                // Ay yenidən bölünəndə ödənilən məbləğ yeni məbləği keçə bilər (D2).
                // Bu blok REAL-PUL mühafizəsindən ƏVVƏL gəlir: ən çox rast gəlinən hal tam ödənilmiş
                // aydır (300/300 Paid → 10 günlük 97), mühafizə əvvəl işləsəydi artıq ödəniş
                // heç bir ekranda görünməzdi (D-B). Burada YALNIZ hesabat və qeyd yazılır.
                if (existing.PaidAmount > finalAmount)
                {
                    result.ExitMonthOverpayment = new ExitMonthOverpayment
                    {
                        PaymentId = existing.Id,
                        Month = month,
                        Year = year,
                        PaidAmount = existing.PaidAmount,
                        NewFinalAmount = finalAmount,
                        Difference = existing.PaidAmount - finalAmount
                    };
                    AppendNote(existing, $"Artıq ödəniş: {existing.PaidAmount - finalAmount:F0} ₼ geri qaytarılmalıdır");
                    await _unitOfWork.Payments.UpdateAsync(existing);
                }

                // Yalnız REAL pul ödənilmiş sətrin MƏBLƏĞİNƏ toxunmuruq (D1). Sırf Status == Paid kifayət deyil:
                // öz sıfırlama əməliyyatımız da (0/0/Paid, PaidAmount=0) eyni görünür və çıxış tarixi
                // irəli düzəldiləndə hesabı əbədi olaraq silərdi.
                if (existing.Status == PaymentStatus.Paid && existing.PaidAmount > 0)
                {
                    // F5: çıxış tarixi İRƏLİ sürüşəndə yeni hesab ödənilmişdən ÇOX ola bilər
                    // (məs. 10-a görə 97 ödənilib, 25-ə görə 242 olmalıdır). Sətri yenidən yazmırıq,
                    // AMMA fərq səssizcə silinməməlidir — hesabata və qeydə düşür.
                    if (existing.PaidAmount < finalAmount)
                    {
                        result.ExitMonthUnderpayment = new ExitMonthUnderpayment
                        {
                            PaymentId = existing.Id,
                            Month = month,
                            Year = year,
                            PaidAmount = existing.PaidAmount,
                            NewFinalAmount = finalAmount,
                            Difference = finalAmount - existing.PaidAmount
                        };
                        AppendNote(existing, $"Az hesablanıb: {finalAmount - existing.PaidAmount:F0} ₼ əlavə ödənilməlidir");
                        await _unitOfWork.Payments.UpdateAsync(existing);
                    }

                    // F3(a): məbləğə toxunulmadı, amma ay hesabatda GÖRÜNMƏLİDİR.
                    result.ExitMonth = new ExitMonthOutcome
                    {
                        PaymentId = existing.Id,
                        Month = month,
                        Year = year,
                        FinalAmount = existing.FinalAmount,
                        PaidAmount = existing.PaidAmount,
                        PeriodStartDay = existing.PeriodStartDay ?? startDay,
                        PeriodEndDay = existing.PeriodEndDay ?? daysInMonth,
                        Created = false,
                        NeedsManualReview = true,
                        Reason = "Ay real pul ilə ödənilib — məbləğ avtomatik dəyişdirilmədi"
                    };
                    return;
                }

                existing.OriginalAmount = proratedBase;
                existing.FinalAmount = finalAmount;
                // Dövr SÜTUNLARA yazılır — hesab məntiqinin oxuduğu yeganə yer. Bitiş İNKLÜZİVDİR.
                existing.PeriodStartDay = startDay;
                existing.PeriodEndDay = lastBilledDay;
                // Sətir yenidən hesablandı — artıq "çıxışa görə sıfırlanmış" deyil.
                existing.ZeroedByExitDate = null;
                // Aktiv şəkildə yenidən hesablanan çıxış ayı "təsdiqlənmiş yoxluq" ola bilməz (D2).
                existing.AbsenceConfirmed = false;
                // Qeydi üstündən yazmırıq — yuvarlaqlaşdırma/resync izi qalmalıdır,
                // yalnız köhnə "Dövr:" və sıfırlama hissələri yenilənir/atılır.
                RemoveZeroedNote(existing);
                UpsertPeriodNote(existing, periodNote);

                if (existing.PaidAmount >= finalAmount)
                    existing.Status = PaymentStatus.Paid;
                else if (existing.PaidAmount > 0)
                    existing.Status = PaymentStatus.PartiallyPaid;
                else
                    existing.Status = PaymentStatus.Debt;

                await _unitOfWork.Payments.UpdateAsync(existing);

                // F3(a): yenidən hesablanan çıxış ayı da hesabata düşür.
                result.ExitMonth = new ExitMonthOutcome
                {
                    PaymentId = existing.Id,
                    Month = month,
                    Year = year,
                    FinalAmount = finalAmount,
                    PaidAmount = existing.PaidAmount,
                    PeriodStartDay = startDay,
                    PeriodEndDay = lastBilledDay,
                    Created = false,
                    NeedsManualReview = false
                };
            }
            else
            {
                var payment = new Payment
                {
                    ChildId = child.Id,
                    Month = month,
                    Year = year,
                    OriginalAmount = proratedBase,
                    FinalAmount = finalAmount,
                    PaidAmount = 0,
                    LastPaymentAmount = null,
                    // Məbləğ 0-dırsa (100% endirim və ya sıfıra yuvarlaqlaşan bölgü) ödəniş gözlənilmir —
                    // PaymentService.GenerateMonthlyDebtsAsync ilə EYNİ qayda, əks halda fantom borc görünür.
                    Status = finalAmount <= 0 ? PaymentStatus.Paid : PaymentStatus.Debt,
                    DiscountType = hasDiscount ? DiscountType.Percentage : DiscountType.None,
                    DiscountValue = hasDiscount ? discountPercent : 0,
                    // Dövr SÜTUNLARDA saxlanılır; qeyd kosmetikdir. Bitiş İNKLÜZİVDİR.
                    PeriodStartDay = startDay,
                    PeriodEndDay = lastBilledDay,
                    Notes = periodNote,
                    RecordedById = "system"
                };
                await _unitOfWork.Payments.AddAsync(payment);

                // Açıq transaksiyanın İÇİNDƏ saxlayırıq ki, hesabatdakı PaymentId real ID olsun
                // (xəta baş verərsə hər şey birlikdə geri qaytarılır).
                await _unitOfWork.SaveChangesAsync();

                // F3(a): YENİ yaradılan çıxış ayı əvvəllər hesabatda ümumiyyətlə görünmürdü —
                // "3 ay yenidən hesablandı" deyilir, yeni yaranan borc isə susdurulurdu.
                result.ExitMonth = new ExitMonthOutcome
                {
                    PaymentId = payment.Id,
                    Month = month,
                    Year = year,
                    FinalAmount = finalAmount,
                    PaidAmount = payment.PaidAmount,
                    PeriodStartDay = startDay,
                    PeriodEndDay = lastBilledDay,
                    Created = true,
                    NeedsManualReview = false
                };
            }
        }

        /// <summary>
        /// Çıxış tarixini yoxlayır: qəbul tarixindən əvvəl və ya SABAHDAN gec ola bilməz.
        /// C2d: tarix "artıq gəlmədiyi İLK gün" olduğu üçün BU GÜN gələn uşağın düzgün tarixi SABAHDIR —
        /// köhnə "bugündən gec ola bilməz" qaydası belə uşağı yazmağa imkan vermirdi.
        /// </summary>
        private void ValidateEffectiveDate(Child child, DateTime effectiveDate)
        {
            var today = _dt.Now.Date;
            var maxDate = today.AddDays(1);

            if (effectiveDate.Date > maxDate)
                throw new Core.Exceptions.ValidationException("Çıxış tarixi sabahdan gec ola bilməz.");

            if (effectiveDate.Date < child.RegistrationDate.Date)
                throw new Core.Exceptions.ValidationException(
                    $"Çıxış tarixi qəbul tarixindən ({child.RegistrationDate:dd.MM.yyyy}) əvvəl ola bilməz.");
        }

        /// <summary>
        /// Sıfırlama qeydinin dəyişməyən hissəsi. YALNIZ mətn təmizliyi üçündür (köhnə hissəni tapıb
        /// atmaq) — hesab vəziyyəti ZeroedByExitDate sütunundadır.
        /// </summary>
        private const string ZeroedByExitMarker = "tarixindən çıxdığı üçün sıfırlandı";

        private static string BuildZeroedNote(DateTime effectiveDate) =>
            $"Uşaq {effectiveDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} {ZeroedByExitMarker}";

        /// <summary>
        /// Uşaq HƏQİQƏTƏN geri qayıtdıqda sıfırlanmış aylara yazılan qeyd (A2) — kosmetikdir.
        /// Həmin anda ZeroedByExitDate təmizlənir və sətir bir daha bərpa oluna bilmir.
        /// </summary>
        private const string FinalAbsenceNote =
            "Uşaq bu ay həqiqətən gəlmədiyi üçün hesab yekunlaşdırıldı (bərpa olunmur)";

        /// <summary>
        /// Uşaq geri qayıtdıqda BAĞLANAN epizodun çıxış ayına yazılan qeyd (G1) — kosmetikdir.
        /// Sətir sıfırlanmayıb: gün-gün bölünmüş məbləği doğrudur və artıq yekundur.
        /// </summary>
        private const string FinalExitMonthNote =
            "Bağlanmış qeydiyyat dövrünün çıxış ayıdır — məbləğ yekundur (avtomatik yenidən yazılmır)";

        /// <summary>
        /// Yuxarıdakı iki yekun qeydin dəyişməyən hissələri. Uşaq həmin aya GERİ QAYIDANDA (H1)
        /// mətn qalmamalıdır: sətir yenidən hesablanır, "bərpa olunmur" yazısı isə ştabı çaşdırardı.
        /// </summary>
        private const string FinalAbsenceMarker = "həqiqətən gəlmədiyi üçün hesab yekunlaşdırıldı";
        private const string FinalExitMonthMarker = "Bağlanmış qeydiyyat dövrünün çıxış ayıdır";

        /// <summary>Yekunlaşdırma qeydlərini (kosmetik) silir — sətir yenidən hesablananda çağırılır.</summary>
        private static void RemoveFinalizationNotes(Payment payment)
        {
            UpsertNoteSegment(payment, FinalAbsenceMarker, null);
            UpsertNoteSegment(payment, FinalExitMonthMarker, null);
        }

        /// <summary>
        /// Sətir İNDİ düzəldilən KONKRET çıxışın sıfırlamasıdırmı?
        /// Yeganə meyar SÜTUNDUR: ZeroedByExitDate doludur və düzəldilən köhnə çıxış tarixinə bərabərdir.
        /// Reaktivasiyada ZeroedByExitDate təmizləndiyi üçün "həqiqətən gəlmədiyi ay" (A2) avtomatik kənarda qalır;
        /// 100% endirimli qanuni 0/0/Paid sətrində bu sahə heç vaxt dolmur.
        /// </summary>
        private static bool IsZeroedByExit(Payment payment, DateTime? previousExitDate) =>
            payment.ZeroedByExitDate.HasValue
            && previousExitDate.HasValue
            && payment.ZeroedByExitDate.Value.Date == previousExitDate.Value.Date;

        /// <summary>
        /// Sətir ƏVVƏLKİ çıxış tarixi ilə gün-gün bölünmüş çıxış ayıdırmı? Çıxış tarixi irəli sürüşəndə
        /// həmin ay artıq tam ay olur və tam məbləğə qaytarılmalıdır — əks halda ay yarımçıq qiymətdə donub qalır.
        /// Dəqiq əlamət SÜTUNDADIR və C2 ilə BİR GÜN SÜRÜŞDÜ: çıxış tarixi indi "artıq gəlmədiyi İLK gün"
        /// olduğu üçün həmin çıxışın yazdığı İNKLÜZİV PeriodEndDay = prev.Day - 1-dir
        /// (məs. 11.04 çıxışı → aprel 1-10, PeriodEndDay = 10; 01.08 çıxışı → PeriodEndDay = 0).
        /// Digər mühafizələr olduğu kimi qalır:
        ///   • ay/il uyğun gəlməlidir → qeydiyyat ayının sətri (bitiş = ayın sonu) yanlış tutulmur;
        ///   • bitiş ayın sonundan KİÇİK olmalıdır → tam ay bölünmüş sayılmır. Ayın son gününə qədər
        ///     gələn uşağın çıxış tarixi onsuz da GƏLƏN ayın 1-idir, ona görə ay/il şərti onu kənarda saxlayır;
        ///     ayın son gününü göstərən çıxış (məs. 31.08 = 1-30 hesablanır) isə həqiqətən bölünmüş aydır.
        /// </summary>
        private static bool IsProratedByPreviousExit(Payment payment, DateTime? previousExitDate)
        {
            // A1 ilə eyni sərhəd: köhnə çıxış tarixi bilinmirsə heç nə bərpa olunmur.
            if (!previousExitDate.HasValue) return false;
            if (!payment.PeriodEndDay.HasValue) return false;

            var prev = previousExitDate.Value;
            if (payment.Year != prev.Year || payment.Month != prev.Month) return false;

            var daysInMonth = DateTime.DaysInMonth(payment.Year, payment.Month);
            // Ay tam bölünübsə (bitiş = ayın sonu) bölünmə baş verməyib.
            return payment.PeriodEndDay.Value == prev.Day - 1 && payment.PeriodEndDay.Value < daysInMonth;
        }

        /// <summary>
        /// Sətri TAM aya qaytarır (qeydiyyat ayında qeydiyyat günündən ayın sonuna kimi) və yekun
        /// məbləği qaytarır. Həm çıxış tarixi düzəlişi (D1), həm də uşağın geri qayıtması (H1)
        /// buradan keçir ki, hesab qaydası bir yerdə qalsın. Qeyd mətnindən yalnız "Dövr:" hissəsi
        /// yenilənir — qalan izləri çağıran metod özü təmizləyir.
        /// </summary>
        private static decimal ApplyFullMonthBilling(Child child, Payment payment)
        {
            var daysInMonth = DateTime.DaysInMonth(payment.Year, payment.Month);
            var startDay = (child.RegistrationDate.Year == payment.Year && child.RegistrationDate.Month == payment.Month)
                ? child.RegistrationDate.Day
                : 1;
            var daysActive = daysInMonth - startDay + 1;

            var (originalAmount, finalAmount, hasDiscount, discountPercent) =
                BillPartialMonth(child, daysActive, daysInMonth);

            payment.OriginalAmount = originalAmount;
            payment.FinalAmount = finalAmount;
            payment.DiscountType = hasDiscount ? DiscountType.Percentage : DiscountType.None;
            payment.DiscountValue = hasDiscount ? discountPercent : 0;
            // Bərpa = TAM ay (qeydiyyat ayında qeydiyyat günündən ayın sonuna kimi).
            payment.PeriodStartDay = startDay;
            payment.PeriodEndDay = daysInMonth;
            // Sətir artıq sıfırlanmış deyil — bərpanın açarı təmizlənir.
            payment.ZeroedByExitDate = null;
            // Yenidən hesablanan ay bağlanmış epizodun yekun sətri ola bilməz — bayraq da təmizlənir (D2),
            // əks halda sətir gələcək sıfırlama dövründə əbədi atlanardı.
            payment.AbsenceConfirmed = false;

            // Məbləğ 0-dırsa (100% endirim) ödəniş gözlənilmir — generasiya ilə eyni qayda.
            // Real ödəniş varsa (köhnə çıxış ayı qismən ödənilmiş ola bilər) status ona görə qurulur.
            if (finalAmount <= 0 || payment.PaidAmount >= finalAmount)
                payment.Status = PaymentStatus.Paid;
            else if (payment.PaidAmount > 0)
                payment.Status = PaymentStatus.PartiallyPaid;
            else
                payment.Status = PaymentStatus.Debt;

            UpsertPeriodNote(payment, $"Dövr: {startDay}-{daysInMonth} ({daysActive} gün)");

            return finalAmount;
        }

        /// <summary>
        /// Sıfırlanmış və ya köhnə çıxışla bölünmüş sətri PaymentService.GenerateMonthlyDebtsAsync ilə
        /// EYNİ qayda üzrə yenidən hesablayır: yalnız qeydiyyat ayında gün-gün bölünür, digər aylarda
        /// tam MonthlyFee, sonra endirim və eyni yuvarlaqlaşdırma.
        /// </summary>
        private async Task RestorePaymentAsync(Child child, Payment payment, DateTime effectiveDate, DeactivationRecalcResult result)
        {
            var finalAmount = ApplyFullMonthBilling(child, payment);

            // Qeydlər kosmetikdir, amma səliqəli qalmalıdır: köhnə "Dövr:" və sıfırlama hissələri atılır.
            RemoveZeroedNote(payment);
            AppendNote(payment, $"Çıxış tarixi {effectiveDate:dd.MM.yyyy} olaraq dəyişdiyi üçün bərpa olundu");

            await _unitOfWork.Payments.UpdateAsync(payment);

            result.RestoredMonths.Add(new RestoredMonth
            {
                PaymentId = payment.Id,
                Month = payment.Month,
                Year = payment.Year,
                FinalAmount = finalAmount,
                // Bərpa ödənişi olan sətrin üstünə düşübsə (məs. köhnə çıxış ayı 100 ₼ ödənilmişdi,
                // indi 300 ₼-a qayıdır → PartiallyPaid) pul itmir, amma ştab bunu GÖRMƏLİDİR.
                PaidAmount = payment.PaidAmount
            });
        }

        /// <summary>
        /// Çıxış tarixi dəyişdikdə uşağın hesablarını yenidən qurur:
        /// (0) əvvəlki (daha erkən) çıxış tarixi ilə sıfırlanmış, amma yeni çıxış ayına QƏDƏRKİ sətirlər bərpa olunur,
        /// (1) çıxış ayının özü yarım-açıq [başlanğıc, çıxış günü) hesabı ilə yenidən bölünür (C2),
        /// (2) çıxış ayından SONRAKI sətirlər yerində sıfırlanır (silinmir — unikal indeks
        ///     regenerasiyanı əbədi bloklayardı), Status = Paid olur.
        /// Real pul ödənilmiş sətirlərə toxunulmur — onlar nəticədə qaytarılır ki, geri qaytarma əl ilə edilsin.
        /// <paramref name="previousExitDate"/> bərpa pəncərəsini determinləşdirir: sətrin ZeroedByExitDate
        /// sütunu MƏHZ bu tarixə bərabər olmalıdır. Null olduqda (uşaq geri qayıdıb) heç nə bərpa olunmur.
        /// SaveChanges/transaksiya çağıran metodun üzərindədir.
        /// </summary>
        private async Task<DeactivationRecalcResult> RecalculateAfterDeactivationAsync(
            Child child, DateTime effectiveDate, DateTime? previousExitDate = null)
        {
            var result = new DeactivationRecalcResult
            {
                ChildId = child.Id,
                ChildFullName = $"{child.FirstName} {child.LastName}",
                EffectiveDate = effectiveDate
            };

            var exitYear = effectiveDate.Year;
            var exitMonth = effectiveDate.Month;

            // 0) Əvvəlki səhv (daha erkən) çıxış tarixi ilə sıfırlanmış aylar — yeni çıxış ayına qədər bərpa olunur.
            //    Bunsuz tarix İRƏLİ düzəldiləndə aradakı aylar əbədi 0 qalır (gecə işi mövcud sətri yenidən yazmır).
            var earlierPayments = (await _unitOfWork.Payments
                .FindAsync(p => p.ChildId == child.Id
                             && (p.Year < exitYear || (p.Year == exitYear && p.Month <= exitMonth))))
                .ToList();

            foreach (var payment in earlierPayments)
            {
                var wasZeroed = IsZeroedByExit(payment, previousExitDate);

                // Çıxış ayının özünü ApplyExitMonthPaymentAsync gün-gün yenidən bölür və nəticəni
                // result.ExitMonth-a yazır (F3) — burada yalnız kosmetik qeyd yenilənir.
                if (payment.Year == exitYear && payment.Month == exitMonth)
                {
                    if (wasZeroed)
                    {
                        RemoveZeroedNote(payment);
                        AppendNote(payment, $"Çıxış tarixi {effectiveDate:dd.MM.yyyy} olaraq dəyişdiyi üçün bərpa olundu");
                    }
                    continue;
                }

                // G1: bağlanmış qeydiyyat dövrünün ayı toxunulmazdır — SIFIRLAMA dövründəki (aşağıda)
                // eyni qorunma BƏRPA dövründə də olmalıdır. Əks halda: uşaq 10.04-də çıxır (aprel 100 ₼,
                // 1-10 gün) → geri qayıdır (ay yekunlaşdırılır) → yenidən çıxır və ştab eyni səhv tarixi
                // (10.04) yazır → sonra düzəldir. Bu zaman IsProratedByPreviousExit yalnız PeriodEndDay-ə
                // baxdığı üçün "əvvəlki çıxış ayı" sanıb apreli TAM aya (300 ₼) çevirərdi — uşaq həmin ay
                // cəmi 10 gün gəldiyi halda 200 ₼ uydurma borc.
                if (payment.AbsenceConfirmed)
                {
                    result.SkippedConfirmedMonths.Add(new SkippedConfirmedMonth
                    {
                        PaymentId = payment.Id,
                        Month = payment.Month,
                        Year = payment.Year,
                        FinalAmount = payment.FinalAmount,
                        PaidAmount = payment.PaidAmount
                    });
                    continue;
                }

                // (a) əvvəlki çıxışla sıfırlanmış ay, (b) əvvəlki çıxış ayı — indi tam ay olub.
                if (!wasZeroed && !IsProratedByPreviousExit(payment, previousExitDate)) continue;

                await RestorePaymentAsync(child, payment, effectiveDate, result);
            }

            // 0b) ÇATIŞMAYAN aylar (F1). Bərpa dövrü yalnız MÖVCUD sətirləri gəzir, amma uşaq
            //     deaktiv olduğu müddətdə aylıq generasiya işi (GetActiveChildrenAsync) ona heç bir
            //     sətir yazmır. Belə aylar YARADILMIR — yalnız hesabata yazılır (səbəb metodun
            //     sənədində). Beləliklə itki artıq SƏSSİZ deyil, amma uydurma borc da yaranmır.
            await DetectMissingMonthsBeforeExitAsync(child, effectiveDate, previousExitDate, result);

            // 1) Çıxış ayı — gün-gün yenidən bölünür (real pul ödənilmiş və ya təsdiqlənmiş yoxluq
            //    sətrinə toxunmur). Nəticə HƏMİŞƏ result.ExitMonth-a yazılır (F3): əvvəllər çıxış ayı
            //    hesabatda yalnız SIFIRLANMIŞ olduğu halda görünürdü, yeni yaradılan sətir isə heç vaxt.
            await ApplyExitMonthPaymentAsync(child, effectiveDate, result);

            // 2) Çıxış ayından SONRAKI bütün sətirlər
            var laterPayments = (await _unitOfWork.Payments
                .FindAsync(p => p.ChildId == child.Id
                             && (p.Year > exitYear || (p.Year == exitYear && p.Month > exitMonth))))
                .ToList();

            foreach (var payment in laterPayments)
            {
                // D2: uşaq geri qayıdanda YEKUNLAŞDIRILMIŞ ay (0/0/Paid "gəlmədiyi ay", yaxud həmin
                // epizodun gün-gün bölünmüş çıxış ayı — G1) toxunulmazdır. Ona yeni ZeroedByExitDate
                // MÖHÜRLƏMİRİK — əks halda səhv yazılıb sonra düzəldilən çıxış tarixi həmin ayları
                // "bərpa oluna bilən" hala qaytarar və tam aya çevirib fantom borc yazardı.
                // G2: ay heç bir siyahıya düşmədiyi üçün ştab onun ATLANDIĞINI görmürdü — indi görür.
                if (payment.AbsenceConfirmed)
                {
                    result.SkippedConfirmedMonths.Add(new SkippedConfirmedMonth
                    {
                        PaymentId = payment.Id,
                        Month = payment.Month,
                        Year = payment.Year,
                        FinalAmount = payment.FinalAmount,
                        PaidAmount = payment.PaidAmount
                    });
                    continue;
                }

                // Valideyn həqiqətən pul ödəyibsə sıfırlamaq saxta artıq-ödəniş yaradar — toxunmuruq
                if (payment.PaidAmount > 0)
                {
                    result.SkippedPaidMonths.Add(new SkippedPaidMonth
                    {
                        PaymentId = payment.Id,
                        Month = payment.Month,
                        Year = payment.Year,
                        PaidAmount = payment.PaidAmount
                    });

                    // D9: məbləğə toxunmuruq, AMMA bərpa açarını köhnə tarixdə donmuş qoymuruq.
                    // Sıfırlanmış sətrin üstünə pul yazılıbsa (0/0/Paid + PaidAmount > 0) açar
                    // YENİ çıxış tarixinə yenilənir ki, ay hələ də bərpa oluna bilsin; sətir artıq
                    // sıfırlanmış formada deyilsə (real hesab + ödəniş) açar tamamilə təmizlənir.
                    var stillZeroedShape = payment.OriginalAmount == 0
                        && payment.FinalAmount == 0
                        && payment.Status == PaymentStatus.Paid;

                    payment.ZeroedByExitDate = stillZeroedShape ? effectiveDate.Date : null;
                    await _unitOfWork.Payments.UpdateAsync(payment);
                    continue;
                }

                // F1: artıq sıfırlanmış sətri ATLAMIRIQ. Əvvəllər burada 'continue' vardı və sətir
                // KÖHNƏ çıxış tarixi ilə qalırdı; növbəti düzəlişdə həmin tarix uyğun gəlmədiyi üçün
                // ay heç vaxt bərpa olunmurdu (bir aylıq borc səssizcə silinirdi).
                var alreadyZeroed = payment.OriginalAmount == 0
                    && payment.FinalAmount == 0
                    && payment.Status == PaymentStatus.Paid;

                payment.OriginalAmount = 0;
                payment.FinalAmount = 0;
                payment.Status = PaymentStatus.Paid;
                // Sıfırlamanın SƏBƏBİ HƏMİŞƏ yenilənir — bərpanın yeganə açarı budur.
                payment.ZeroedByExitDate = effectiveDate.Date;
                UpsertZeroedNote(payment, BuildZeroedNote(effectiveDate));

                await _unitOfWork.Payments.UpdateAsync(payment);

                if (!alreadyZeroed) result.ZeroedMonths++;
            }

            // 3) Çıxışdan SONRAKI "avtomatik qayıb" davamiyyət sətirləri (D10).
            //    Gecə işi uşaq hələ sistemdə aktiv görünərkən onları yazıb; retroaktiv çıxışdan
            //    sonra bunlar uşağın getdiyi günlərə aid saxta qayıb sayılır.
            await RemoveAutoAbsenceAfterExitAsync(child, effectiveDate);

            return result;
        }

        /// <summary>Gecə işinin yazdığı avtomatik qayıb sətirlərinin audit imzası.</summary>
        private const string AutoAbsentRecordedById = "auto-absent";

        /// <summary>
        /// Çıxış tarixindən SONRAKI AVTOMATİK qayıb sətirlərini yumşaq silir (D10).
        /// Yalnız RecordedById == "auto-absent" olan sətirlər silinir — müəllimin/ştabın ƏL İLƏ
        /// yazdığı davamiyyətə heç vaxt toxunulmur.
        /// Çağıran metodun transaksiyası və SaveChanges-i üzərindədir.
        /// </summary>
        private async Task RemoveAutoAbsenceAfterExitAsync(Child child, DateTime effectiveDate)
        {
            var exitDay = DateOnly.FromDateTime(effectiveDate.Date);

            // FindAsync IgnoreQueryFilters() işlədir — onsuz da silinmiş sətri təkrar işləməmək üçün
            // IsDeleted şərti AÇIQ yazılır.
            // C2: çıxış tarixi EKSKLÜZİVDİR — həmin günün ÖZÜ də uşağın gəlmədiyi ilk gündür,
            // ona görə süpürgənin o gün yazdığı avtomatik qayıb da saxta sayılır (sərhəd >= oldu).
            var ghostAbsences = (await _unitOfWork.Attendances
                .FindAsync(a => a.ChildId == child.Id
                             && a.Date >= exitDay
                             && a.RecordedById == AutoAbsentRecordedById
                             && !a.IsDeleted))
                .ToList();

            foreach (var absence in ghostAbsences)
            {
                absence.IsDeleted = true;
                await _unitOfWork.Attendances.UpdateAsync(absence);
            }
        }

        /// <summary>
        /// Çıxış ayından ƏVVƏLKİ, amma sətri ÜMUMİYYƏTLƏ OLMAYAN ayları AŞKARLAYIR və hesabata yazır (F1).
        /// Sətir QƏSDƏN yaradılmır: sxemdə "uşaq həmin ay həqiqətən gəlməyib" (hesab olmamalıdır) ilə
        /// "həmin ay üçün sətir sadəcə generasiya olunmayıb" (hesab olmalıdır) FƏRQLƏNMİR —
        /// AbsenceConfirmed yalnız MÖVCUD sətri qoruyur, olmayan ay üçün heç bir əlamət yoxdur.
        /// Avtomatik yaratmaq deməli real valideynə uydurma borc yazmaq riski daşıyır (məs. uşaq
        /// geri qayıdıb, sonra çıxış tarixi səhv yazılıb düzəldilir → aradakı boş aylar tam qiymətlə
        /// "dirilərdi" və gündəlik gecikmə mesajlarını sürərdi).
        /// Bunun əvəzinə ştaba BİLDİRİLİR; lazım olsa ay əl ilə yaradılır (RecordPaymentAsync çatışmayan
        /// sətri düzgün gün-gün bölgü ilə özü yaradır, yaxud aylıq generasiya endpoint-i işlədilir).
        /// Pəncərə: max(əvvəlki çıxış ayı, qeydiyyat ayı) → çıxış ayından ƏVVƏLKİ ay (daxil).
        /// Əvvəlki çıxış tarixi yoxdursa heç nə edilmir — o halda uşaq bu ana qədər aktiv olub və
        /// gecə işi sətirləri onsuz da yazıb.
        /// Yalnız OXUYUR — heç bir sətir yazmır/silmir.
        /// </summary>
        private async Task DetectMissingMonthsBeforeExitAsync(
            Child child, DateTime effectiveDate, DateTime? previousExitDate, DeactivationRecalcResult result)
        {
            if (!previousExitDate.HasValue) return;

            // Ay indeksi ilə işləyirik ki, il sərhədi (dekabr → yanvar) özü-özünə həll olsun.
            var registrationIndex = child.RegistrationDate.Year * 12 + (child.RegistrationDate.Month - 1);
            var previousExitIndex = previousExitDate.Value.Year * 12 + (previousExitDate.Value.Month - 1);
            var exitIndex = effectiveDate.Year * 12 + (effectiveDate.Month - 1);

            var startIndex = Math.Max(registrationIndex, previousExitIndex);
            // Çıxış ayının ÖZÜ bura daxil deyil — onu ApplyExitMonthPaymentAsync gün-gün bölür
            // və nəticəsi result.ExitMonth-da ayrıca göstərilir (F3).
            var endIndex = exitIndex - 1;

            for (var index = startIndex; index <= endIndex; index++)
            {
                var year = index / 12;
                var month = index % 12 + 1;

                // FindAsync IgnoreQueryFilters() işlədir — YUMŞAQ SİLİNMİŞ sətir də "mövcuddur"
                // sayılır (unikal indeks onu da tutur), ona görə belə ay çatışmayan kimi bildirilmir.
                var exists = (await _unitOfWork.Payments
                    .FindAsync(p => p.ChildId == child.Id && p.Month == month && p.Year == year))
                    .Any();

                if (exists) continue;

                result.MissingMonths.Add(new MissingMonth { Month = month, Year = year });
            }
        }

        /// <summary>
        /// Deactivates a child. <paramref name="effectiveDate"/> — uşağın ARTIQ GƏLMƏDİYİ İLK gün
        /// (C2, eksklüziv): həmin gün hesablanmır. Boşdursa bugün — endpoint gövdəsiz də işləyir.
        /// </summary>
        public async Task<DeactivationRecalcResult> DeactivateChildAsync(int id, DateTime? effectiveDate = null)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li uşaq tapılmadı.");

            // Tarix verilməyibsə bugün — köhnə davranış olduğu kimi qalır.
            // F2: sütun HƏMİŞƏ gecə yarısı saxlanılır. Gövdəsiz çağırışda (EmptyBodyBehavior.Allow)
            // _dt.Now vaxt hissəsi gətirirdi; AttendanceService sərhədi isə dayStart (00:00) ilə
            // müqayisə etdiyi üçün çıxış GÜNÜNÜN ÖZÜ filtri keçir və avtomatik qayıb yazılırdı —
            // halbuki hesab məhz həmin günü artıq xaric edir. .Date iki sərhədi eyniləşdirir.
            var exitDate = (effectiveDate ?? _dt.Now).Date;
            ValidateEffectiveDate(child, exitDate);

            // Təkrar deaktivasiyada köhnə tarix bərpa pəncərəsini determinləşdirir (D1)
            var previousExitDate = child.DeactivationDate;

            DeactivationRecalcResult result;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                child.Status = ChildStatus.Inactive;
                child.DeactivationDate = exitDate;

                await _unitOfWork.GroupLogs.AddAsync(new GroupLog
                {
                    GroupId = child.GroupId,
                    ChildId = child.Id,
                    ActionType = GroupLogActionType.ChildRemoved,
                    Message = $"Uşaq çıxarıldı: {child.FirstName} {child.LastName} ({exitDate:dd.MM.yyyy})",
                    ActionDate = _dt.Now
                });

                await _unitOfWork.Children.UpdateAsync(child);

                // Çıxış ayı yenidən bölünür, sonrakı aylar sıfırlanır, əvvəlki sıfırlamalar bərpa olunur
                result = await RecalculateAfterDeactivationAsync(child, exitDate, previousExitDate);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return result;
        }

        /// <summary>
        /// Soft-deletes a child.
        /// </summary>
        public async Task DeleteChildAsync(int id)
        {
            var child = await _unitOfWork.Children.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li uşaq tapılmadı.");

            await _unitOfWork.GroupLogs.AddAsync(new GroupLog
            {
                GroupId = child.GroupId,
                ChildId = child.Id,
                ActionType = GroupLogActionType.ChildRemoved,
                Message = $"Uşaq silindi: {child.FirstName} {child.LastName}",
                ActionDate = _dt.Now
            });

            await _unitOfWork.Children.SoftDeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Searches children by name, parent name, or phone.
        /// </summary>
        public async Task<IEnumerable<ChildResponse>> SearchChildrenAsync(string term)
        {
            var children = await _unitOfWork.Children.SearchChildrenAsync(term);
            return _mapper.Map<IEnumerable<ChildResponse>>(children);
        }

        /// <summary>
        /// Deactivates a list of children. <paramref name="effectiveDate"/> bütün siyahıya tətbiq olunur.
        /// </summary>
        public async Task<List<DeactivationRecalcResult>> DeactivateChildrenAsync(List<int> ids, DateTime? effectiveDate = null)
        {
            var now = _dt.Now;
            var results = new List<DeactivationRecalcResult>();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var id in ids)
                {
                    var child = await _unitOfWork.Children.GetByIdAsync(id);
                    if (child == null) continue;

                    // F2: tək deaktivasiya ilə eyni qayda — sütun HƏMİŞƏ gecə yarısıdır.
                    var exitDate = (effectiveDate ?? now).Date;
                    ValidateEffectiveDate(child, exitDate);

                    // Təkrar deaktivasiyada köhnə tarix bərpa pəncərəsini determinləşdirir (D1)
                    var previousExitDate = child.DeactivationDate;

                    child.Status = ChildStatus.Inactive;
                    child.DeactivationDate = exitDate;

                    await _unitOfWork.GroupLogs.AddAsync(new GroupLog
                    {
                        GroupId = child.GroupId,
                        ChildId = child.Id,
                        ActionType = GroupLogActionType.ChildRemoved,
                        Message = $"Uşaq çıxarıldı: {child.FirstName} {child.LastName} ({exitDate:dd.MM.yyyy})",
                        ActionDate = now
                    });

                    await _unitOfWork.Children.UpdateAsync(child);
                    results.Add(await RecalculateAfterDeactivationAsync(child, exitDate, previousExitDate));
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return results;
        }

        /// <summary>
        /// Activates a list of children. <paramref name="returnDate"/> bütün siyahıya tətbiq olunur;
        /// boşdursa bugün. Tək qaytarma ilə EYNİ məntiqdən keçir (H1).
        /// </summary>
        public async Task<List<ReactivationResult>> ActivateChildrenAsync(List<int> ids, DateTime? returnDate = null)
        {
            var results = new List<ReactivationResult>();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var id in ids)
                {
                    var child = await _unitOfWork.Children.GetByIdAsync(id);
                    if (child == null) continue;

                    var effectiveReturnDate = (returnDate ?? _dt.Now).Date;
                    ValidateReturnDate(child, effectiveReturnDate);

                    results.Add(await ApplyReturnAsync(child, effectiveReturnDate));
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return results;
        }

        /// <summary>
        /// Soft-deletes a list of children.
        /// </summary>
        public async Task DeleteChildrenAsync(List<int> ids)
        {
            foreach (var id in ids)
            {
                var child = await _unitOfWork.Children.GetByIdAsync(id);
                if (child != null)
                {
                    await _unitOfWork.GroupLogs.AddAsync(new GroupLog
                    {
                        GroupId = child.GroupId,
                        ChildId = child.Id,
                        ActionType = GroupLogActionType.ChildRemoved,
                        Message = $"Uşaq silindi: {child.FirstName} {child.LastName}",
                        ActionDate = _dt.Now
                    });
                }

                await _unitOfWork.Children.SoftDeleteAsync(id);
            }
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
