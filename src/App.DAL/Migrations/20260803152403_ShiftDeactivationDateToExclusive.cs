using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <summary>
    /// C2 MƏLUMAT KÖÇÜRMƏSİ — children.DeactivationDate-in MƏNASI dəyişdi.
    ///
    /// KÖHNƏ dil: dəyər uşağın GƏLDİYİ SON gün idi (İNKLÜZİV).
    /// YENİ  dil: dəyər uşağın ARTIQ GƏLMƏDİYİ İLK gündür (EKSKLÜZİV).
    /// Tərcümə birmənalıdır: yeni = köhnə + 1 gün.
    ///
    /// Kodu İKİ DİL BİLƏN etmək əvəzinə MƏLUMATI tərcümə edirik — sistemdə yalnız BİR dil qalır.
    /// ChildService.IsProratedByPreviousExit sərt matcher olaraq qalır (PeriodEndDay == prev.Day - 1).
    ///
    /// SÜTUN ƏLAVƏ OLUNMUR və heç bir MƏBLƏĞ dəyişmir.
    ///
    /// payments.PeriodEndDay TOXUNULMUR: o onsuz da İNKLÜZİV "son hesablanan gün"dür və köhnə
    /// çıxışlarda dəyəri = köhnə çıxış günü idi. Çıxış tarixi bir gün irəli sürüşdükdən sonra
    /// `PeriodEndDay == prev.Day - 1` bərabərliyi HƏMİN sətirlərdə avtomatik doğru olur.
    ///
    /// LAKİN payments.ZeroedByExitDate SÜRÜŞDÜRÜLMƏLİDİR: o, IsZeroedByExit-də uşağın
    /// DeactivationDate-i ilə BƏRABƏRLİYƏ görə tutuşdurulur. Yalnız children sürüşdürülsəydi,
    /// açar əbədi uyğunsuz qalardı: çıxış tarixi sonradan İRƏLİ düzəldiləndə sıfırlanmış aylar
    /// bərpa olunmaz, 0 ₼-də qalar və heç bir hesabat siyahısına düşməzdi (aylıq generasiya da
    /// mövcud sətrə toxunmadığı üçün o aylar bir daha hesablana bilməzdi). Hər iki sütunu eyni
    /// gün sürüşdürəndə bərabərlik qorunur; ZeroedByExitDate heç bir hesablamada RƏQƏM kimi
    /// istifadə olunmur, ona görə məbləğlərə təsiri yoxdur.
    ///
    /// DATE(...) əlavə faydası (F2): köhnə sətirlərdə vaxt hissəsi (məs. 11.04.2026 14:32) var idi,
    /// çünki gövdəsiz deaktivasiya endpoint-i _dt.Now işlədirdi. Burada dəyər gecə yarısına
    /// normallaşdırılır — AttendanceService sərhədi (DeactivationDate > dayStart) həmin sətirlərdə də
    /// düzgün işləyir. Yazma nöqtələri artıq .Date yazır, ona görə bu bir dəfəlik təmizləmədir.
    /// </summary>
    public partial class ShiftDeactivationDateToExclusive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE children SET DeactivationDate = DATE_ADD(DATE(DeactivationDate), INTERVAL 1 DAY) WHERE DeactivationDate IS NOT NULL;
");

            // Sıfırlama açarı da EYNİ gün sürüşür — yoxsa bərabərlik pozulur (yuxarıdakı izaha bax).
            migrationBuilder.Sql(@"
UPDATE payments SET ZeroedByExitDate = DATE_ADD(DATE(ZeroedByExitDate), INTERVAL 1 DAY) WHERE ZeroedByExitDate IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Tam əks əməliyyat — dəyər yenidən "gəldiyi SON gün" (inklüziv) olur.
            // Vaxt hissəsi bərpa olunmur (o, onsuz da təsadüfi idi və heç bir hesabata girmirdi).
            migrationBuilder.Sql(@"
UPDATE children SET DeactivationDate = DATE_SUB(DATE(DeactivationDate), INTERVAL 1 DAY) WHERE DeactivationDate IS NOT NULL;
");

            migrationBuilder.Sql(@"
UPDATE payments SET ZeroedByExitDate = DATE_SUB(DATE(ZeroedByExitDate), INTERVAL 1 DAY) WHERE ZeroedByExitDate IS NOT NULL;
");
        }
    }
}
