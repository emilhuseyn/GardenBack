using Microsoft.OpenApi.Models;
using App.Business;
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

// Register recurring jobs

// Hər ayın 1-i gecə saat 00:01-də — cari ay üçün borcları yarat
RecurringJob.AddOrUpdate<IPaymentService>(
    "generate-monthly-debts",
    s => s.GenerateCurrentMonthDebtsAsync(),
    Cron.Monthly(1, 0, 1));

// Hər gün saat 18:30-da — gecikmiş gəliş/tez getmə qeydlərini yenilə
RecurringJob.AddOrUpdate<IAttendanceService>(
    "process-attendance-flags",
    s => s.AutoDetectLateAndEarlyLeave(),
    Cron.Daily(18, 30));

// Hər gecə saat 02:00-da — gündəlik verilənlər bazası backup-ı
RecurringJob.AddOrUpdate<IBackupService>(
    "daily-database-backup",
    s => s.CreateBackupAsync(),
    "0 2 * * *");

// Hər gün saat 20:00-da uşağın qeydiyyat günündən bir gün əvvəl valideynə ödəniş xatırlatması
RecurringJob.AddOrUpdate<INotificationService>(
    "send-payment-due-reminders",
    s => s.SendPaymentDueRemindersAsync(),
    Cron.Daily(20, 0));

app.MapControllers();

//app.Run("http://0.0.0.0:5034");
app.Run();
