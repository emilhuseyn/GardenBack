using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentPeriodAndZeroState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PeriodEndDay",
                table: "payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PeriodStartDay",
                table: "payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZeroedByExitDate",
                table: "payments",
                type: "datetime(6)",
                nullable: true);

            // Reaktivasiyada TƏSDİQLƏNMİŞ yoxluq (D2). Köhnə sətirlərin heç biri təsdiqlənməyib,
            // ona görə defolt false-dur — sütun sırf additivdir, backfill tələb etmir.
            migrationBuilder.AddColumn<bool>(
                name: "AbsenceConfirmed",
                table: "payments",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // ───────── BACKFILL ─────────
            // Köhnə sətirlərdə dövr YALNIZ qeyd mətnində idi: "Dövr: {start}-{end} ({n} gün)".
            // Şablon MARKERƏ BAĞLIDIR: "Dövr:" sözünün ASCII quyruğu olan 'vr:' aralıqdan DƏRHAL
            // əvvəl tələb olunur. Beləliklə SQL literalı ASCII qalır (ö, ü hərfləri yoxdur və
            // kodlaşdırma problemi yaranmır), amma marker yoxlaması C#-dakı parserlə eyni sərtlikdə olur.
            // MARKERSİZ uyğunlaşdırmaq OLMAZ: Notes sütununda kassirin sərbəst mətni saxlanılır
            // (məs. "Uşaq 12-14 (3 gün) xəstə olub") və markersiz şablon həmin mətni dövr kimi oxuyub
            // tam ayı 3 günlük hesaba çevirərdi — sonrakı qiymət dəyişikliyində sətir "Paid" olur və
            // yüzlərlə manat borc silinərdi.
            // Marker olmayan sətirlər NULL qalır — bu, "tam ay" deməkdir və düzgün defolt-dur.
            // Şablonda TERS SLEŞ İŞLƏDİLMİR — mötərizə simvol sinfi ilə verilir ([(]),
            // beləliklə NO_BACKSLASH_ESCAPES sql_mode-u nəticəni poza bilmir.
            //
            // D5: qeyddə BİR NEÇƏ "Dövr:" hissəsi ola bilər, çünki RecordBulkPaymentAsync köhnə
            // hissəni silmədən YENİSİNİ ƏLAVƏ edir. Bu halda BİRİNCİ uyğunluq KÖHNƏ (yanlış) dövrdür.
            // Ona görə SONUNCU uyğunluq oxunur: REGEXP_SUBSTR-in occurrence arqumenti (MySQL 8.0.4+)
            // uyğunluq SAYI ilə verilir. Say belə hesablanır: hər uyğunluğu 1 simvollu 'X' ilə əvəz
            // edib uzunluğu, tamamilə silib uzunluğu çıxırıq — fərq uyğunluqların sayıdır
            // (uyğunluqların özlərinin uzunluğu fərqli olsa da nəticə dəyişmir).
            // Uyğunluq indi 'vr:' prefiksi ilə başladığı üçün BAŞLANĞIC günü ':'-dan SONRAKI
            // hissədən oxunur (SUBSTRING_INDEX(..., ':', -1)) — əks halda CAST 0 qaytarardı.
            migrationBuilder.Sql(@"
UPDATE payments
SET PeriodStartDay = CAST(TRIM(SUBSTRING_INDEX(SUBSTRING_INDEX(
        REGEXP_SUBSTR(Notes, 'vr: *[0-9]+ *- *[0-9]+ *[(] *[0-9]+ *g', 1,
            CHAR_LENGTH(REGEXP_REPLACE(Notes, 'vr: *[0-9]+ *- *[0-9]+ *[(] *[0-9]+ *g', 'X'))
          - CHAR_LENGTH(REGEXP_REPLACE(Notes, 'vr: *[0-9]+ *- *[0-9]+ *[(] *[0-9]+ *g', ''))),
        ':', -1), '-', 1)) AS UNSIGNED),
    PeriodEndDay = CAST(TRIM(SUBSTRING_INDEX(SUBSTRING_INDEX(
        REGEXP_SUBSTR(Notes, 'vr: *[0-9]+ *- *[0-9]+ *[(] *[0-9]+ *g', 1,
            CHAR_LENGTH(REGEXP_REPLACE(Notes, 'vr: *[0-9]+ *- *[0-9]+ *[(] *[0-9]+ *g', 'X'))
          - CHAR_LENGTH(REGEXP_REPLACE(Notes, 'vr: *[0-9]+ *- *[0-9]+ *[(] *[0-9]+ *g', ''))),
        '-', -1), '(', 1)) AS UNSIGNED)
WHERE Notes REGEXP 'vr: *[0-9]+ *- *[0-9]+ *[(] *[0-9]+ *g';
");

            // Mənasız aralıq (kəsilmiş qeyd, xarab məlumat) — NULL-a qaytarılır ki, tam ay kimi oxunsun.
            migrationBuilder.Sql(@"
UPDATE payments
SET PeriodStartDay = NULL, PeriodEndDay = NULL
WHERE PeriodStartDay IS NOT NULL
  AND (PeriodEndDay IS NULL
       OR PeriodStartDay < 1
       OR PeriodEndDay < PeriodStartDay
       OR PeriodEndDay > DAY(LAST_DAY(STR_TO_DATE(CONCAT(`Year`, '-', `Month`, '-01'), '%Y-%m-%d'))));
");

            // D6: ZeroedByExitDate üçün BACKFILL YOXDUR — QƏSDƏN.
            // "... tarixindən çıxdığı üçün sıfırlandı" qeyd formatı heç bir buraxılmış commit-də
            // yazılmayıb (`git log --all -S "tarixindən çıxdığı üçün sıfırlandı"` → 0 nəticə;
            // "sıfırlandı" və "ZeroedByExitDate" də tarixçədə heç vaxt görünmür) — sonrakı ayları
            // sıfırlayan dövr MƏHZ bu dəyişikliklə gəlir. Deməli produksiyada həmin şablona uyğun
            // gələ biləcək YEGANƏ mətn admin-in RecordPaymentAsync/RecordBulkPaymentAsync ilə
            // yazdığı sərbəst qeyddir (məs. MonthlyFee = 0 uşağın 0/0/0/Paid sətrinə düşən
            // "01.09.2025 tarixindən pulsuzdur"). Belə sətrə ZeroedByExitDate yazmaq onu heç vaxt
            // baş verməmiş çıxışa bağlayar və sonrakı düzəliş həmin ayı bugünkü MonthlyFee ilə
            // yenidən borclandırardı. Çevriləcək köhnə məlumat olmadığı üçün sütun NULL başlayır.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeriodEndDay",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "PeriodStartDay",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ZeroedByExitDate",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "AbsenceConfirmed",
                table: "payments");
        }
    }
}
