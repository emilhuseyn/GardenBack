using Microsoft.OpenApi.Models;
using App.Business;
using App.Business.Services.Implementations;
using App.Business.Services.Interfaces;
using App.Core.Entities.Identity;
using App.DAL;
using App.API;
using App.DAL.Presistence;
using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Identity;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Information()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddDataAccess(builder.Configuration)
    .AddBusiness();

builder.Services.AddSwagger();
builder.Services.AddJwt(builder.Configuration);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddCorsPolicy(builder.Configuration);

// Hangfire
var hangfireConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseStorage(new MySqlStorage(hangfireConnectionString, new MySqlStorageOptions
    {
        TablesPrefix = "Hangfire"
    })));
builder.Services.AddHangfireServer();

var app = builder.Build();

// Auto-migrate and seed
using var scope = app.Services.CreateScope();
await AutomatedMigration.MigrateAsync(scope.ServiceProvider);

// Test məlumatlarını doldur (appsettings-də "SeedTestData": true olduqda)
if (builder.Configuration.GetValue<bool>("SeedTestData"))
{
    try
    {
        await TestDataSeeder.SeedTestDataAsync(
            scope.ServiceProvider.GetRequiredService<AppDbContext>(),
            scope.ServiceProvider.GetRequiredService<UserManager<User>>());
    }
    catch (Exception ex)
    {
        Log.Error(ex, "[Seed] Test datası yüklənərkən xəta baş verdi.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

// Middlewares
app.AddMiddlewares();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (Admin only)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() }
});

// Register recurring jobs — bütün cron ifadələri Bakı vaxtı (UTC+4) ilə işləyir
var bakuZone = BakuDateTimeService.Zone;

// Hər ayın 1-i gecə saat 00:01-də — cari ay üçün borcları yarat
RecurringJob.AddOrUpdate<IPaymentService>(
    "generate-monthly-debts",
    s => s.GenerateCurrentMonthDebtsAsync(),
    Cron.Monthly(1, 0, 1),
    new RecurringJobOptions { TimeZone = bakuZone });

// Hər gün saat 18:30-da — gecikmiş gəliş/tez getmə qeydlərini yenilə
RecurringJob.AddOrUpdate<IAttendanceService>(
    "process-attendance-flags",
    s => s.AutoDetectLateAndEarlyLeave(),
    Cron.Daily(18, 30),
    new RecurringJobOptions { TimeZone = bakuZone });

// Hər 30 dəqiqədən bir — Hikvision kameradan davamiyyəti sinxronlaşdır
RecurringJob.AddOrUpdate<HikvisionAttendanceSyncJob>(
    "hikvision-attendance-sync",
    s => s.SyncTodayAttendanceAsync(),
    "*/30 * * * *",
    new RecurringJobOptions { TimeZone = bakuZone });

// Hər gecə saat 02:00-da — gündəlik verilənlər bazası backup-ı
RecurringJob.AddOrUpdate<IBackupService>(
    "daily-database-backup",
    s => s.CreateBackupAsync(),
    "0 2 * * *",
    new RecurringJobOptions { TimeZone = bakuZone });

// Hər gün saat 12:30-da — sabah ödəniş günü olan valideynlərə WABA xatırlatması (xatirlatma_wp)
RecurringJob.AddOrUpdate<INotificationService>(
    "send-payment-due-reminders",
    s => s.SendPaymentDueRemindersAsync(),
    Cron.Daily(12, 30),
    new RecurringJobOptions { TimeZone = bakuZone });

// Hər gün saat 12:30-da — ödəniş günündən 3 gün keçmiş, hələ ödəməyənlərə WABA gecikme xəbərdarlığı (gecikme_wp)
RecurringJob.AddOrUpdate<INotificationService>(
    "send-payment-overdue-reminders",
    s => s.SendPaymentOverdueRemindersAsync(),
    Cron.Daily(12, 30),
    new RecurringJobOptions { TimeZone = bakuZone });

// Hər gün saat 23:50-də — davamiyyəti qeyd olunmayan aktiv uşaqları "Gəlmədi" kimi işarələ
RecurringJob.AddOrUpdate<IAttendanceService>(
    "auto-mark-absent",
    s => s.AutoMarkAbsentAsync(),
    Cron.Daily(23, 50),
    new RecurringJobOptions { TimeZone = bakuZone });

// Hər gün saat 06:00-da — gecə job-u qaçırılıbsa dünənin "Gəlmədi" qeydlərini bərpa et
RecurringJob.AddOrUpdate<IAttendanceService>(
    "recover-missed-absent-marks",
    s => s.RecoverMissedAbsentMarksAsync(),
    Cron.Daily(6, 0),
    new RecurringJobOptions { TimeZone = bakuZone });

app.MapControllers();

//app.Run("http://0.0.0.0:5034");
app.Run();