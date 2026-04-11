using App.Core.Entities;
using App.Core.Enums;
using App.Core.Services;
using App.DAL.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace App.Business.Services.Implementations
{
    /// <summary>
    /// Hikvision kamera sistemindən günlük davamiyyət məlumatlarını çəkib
    /// uşaqların davamiyyət qeydlərini yaradır/yeniləyir.
    /// Hər gün saat 23:59-da işləyir.
    /// </summary>
    public class HikvisionAttendanceSyncJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeService _dt;
        private readonly ILogger<HikvisionAttendanceSyncJob> _logger;
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;

        private const int MaxResults = 30;
        private const int Major = 5;
        private const int Minor = 75;

        public HikvisionAttendanceSyncJob(
            IUnitOfWork unitOfWork,
            IDateTimeService dt,
            ILogger<HikvisionAttendanceSyncJob> logger,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _dt = dt;
            _logger = logger;

            var section = configuration.GetSection("Hikvision");
            _baseUrl = section["BaseUrl"] ?? throw new InvalidOperationException("Hikvision:BaseUrl konfiqurasiyada tapılmadı.");
            _username = section["Username"] ?? throw new InvalidOperationException("Hikvision:Username konfiqurasiyada tapılmadı.");
            _password = section["Password"] ?? throw new InvalidOperationException("Hikvision:Password konfiqurasiyada tapılmadı.");
        }

        /// <summary>
        /// Cari günün davamiyyət məlumatlarını Hikvision-dan çəkib DB-yə yazır.
        /// </summary>
        public async Task SyncTodayAttendanceAsync()
        {
            var today = DateOnly.FromDateTime(_dt.Now);
            await SyncAttendanceForDateAsync(today);
        }

        public async Task SyncAttendanceForDateAsync(DateOnly date)
        {
            _logger.LogInformation("[Hikvision] {Date} tarixi üçün davamiyyət sinxronizasiyası başlayır.", date);

            var events = await FetchAllEventsAsync(date);

            if (!events.Any())
            {
                _logger.LogInformation("[Hikvision] {Date} tarixi üçün heç bir hadisə tapılmadı.", date);
                return;
            }

            // PersonId-ə görə qruplaşdır
            var grouped = events
                .Where(e => !string.IsNullOrWhiteSpace(e.EmployeeNoString)
                            && int.TryParse(e.EmployeeNoString, out _))
                .GroupBy(e => int.Parse(e.EmployeeNoString!))
                .ToList();

            // DB-dəki bütün children-ı bir dəfə yüklə (PersonId null olmayan)
            var allChildren = (await _unitOfWork.Children.FindAsync(c => c.PersonId != null)).ToList();
            var childByPersonId = allChildren
                .Where(c => c.PersonId.HasValue)
                .ToDictionary(c => c.PersonId!.Value);

            int synced = 0, skipped = 0;

            foreach (var group in grouped)
            {
                var personId = group.Key;

                if (!childByPersonId.TryGetValue(personId, out var child))
                {
                    _logger.LogDebug("[Hikvision] PersonId={PersonId} olan uşaq tapılmadı, keçildi.", personId);
                    skipped++;
                    continue;
                }

                var sorted = group.OrderBy(e => e.Time).ToList();
                var first = sorted.First();
                var last = sorted.Count > 1 ? sorted.Last() : null;

                var arrivalTime = ParseTime(first.Time);
                var departureTime = last != null ? ParseTime(last.Time) : (TimeOnly?)null;

                // Mövcud qeydi yoxla
                var existing = (await _unitOfWork.Attendances.FindAsync(
                    a => a.ChildId == child.Id && a.Date == date))
                    .FirstOrDefault();

                if (existing != null)
                {
                    existing.ArrivalTime = arrivalTime ?? existing.ArrivalTime;
                    existing.DepartureTime = departureTime ?? existing.DepartureTime;
                    existing.Status = AttendanceStatus.Present;
                    await _unitOfWork.Attendances.UpdateAsync(existing);
                }
                else
                {
                    var attendance = new Attendance
                    {
                        ChildId = child.Id,
                        Date = date,
                        ArrivalTime = arrivalTime,
                        DepartureTime = departureTime,
                        Status = AttendanceStatus.Present,
                        RecordedById = "hikvision-sync"
                    };
                    await _unitOfWork.Attendances.AddAsync(attendance);
                }

                synced++;
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "[Hikvision] {Date} sinxronizasiyası tamamlandı. Yazılan: {Synced}, Tapılmayan: {Skipped}.",
                date, synced, skipped);
        }

        private async Task<List<HikvisionEvent>> FetchAllEventsAsync(DateOnly date)
        {
            var all = new List<HikvisionEvent>();
            int position = 0;
            int searchId = 1;

            var uri = $"{_baseUrl.TrimEnd('/')}/ISAPI/AccessControl/AcsEvent?format=json";
            var credCache = new CredentialCache
            {
                { new Uri(_baseUrl), "Digest", new NetworkCredential(_username, _password) }
            };
            using var handler = new HttpClientHandler
            {
                Credentials = credCache,
                PreAuthenticate = false
            };
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(30);

            var startTime = $"{date:yyyy-MM-dd}T00:00:00+04:00";
            var endTime = $"{date:yyyy-MM-dd}T23:59:59+04:00";

            while (true)
            {
                var body = new
                {
                    AcsEventCond = new
                    {
                        searchID = searchId.ToString(),
                        searchResultPosition = position,
                        maxResults = MaxResults,
                        major = Major,
                        minor = Minor,
                        startTime,
                        endTime
                    }
                };

                var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                HttpResponseMessage response;
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    response = await client.PostAsync(uri, content);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Hikvision] API sorğusu uğursuz oldu. Position={Position}", position);
                    break;
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                List<HikvisionEvent>? items;
                try
                {
                    items = ParseEvents(responseBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Hikvision] Response parse edilə bilmədi.");
                    break;
                }

                if (items == null || items.Count == 0)
                    break;

                all.AddRange(items);
                position += items.Count;
                searchId++;

                if (items.Count < MaxResults)
                    break;
            }

            return all;
        }

        private static List<HikvisionEvent>? ParseEvents(string json)
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("AcsEvent", out var acsEvent))
                return null;
            if (!acsEvent.TryGetProperty("InfoList", out var infoList))
                return null;

            var result = new List<HikvisionEvent>();
            foreach (var item in infoList.EnumerateArray())
            {
                var ev = new HikvisionEvent
                {
                    EmployeeNoString = item.TryGetProperty("employeeNoString", out var emp) ? emp.GetString() : null,
                    Name = item.TryGetProperty("name", out var name) ? name.GetString() : null,
                    Time = item.TryGetProperty("time", out var time) ? time.GetString() : null
                };
                result.Add(ev);
            }

            return result;
        }

        private TimeOnly? ParseTime(string? timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr))
                return null;

            if (DateTimeOffset.TryParse(timeStr, out var dto))
            {
                // Bakı vaxtına çevir
                var bakuZone = BakuDateTimeService.Zone;
                var bakuTime = TimeZoneInfo.ConvertTime(dto, bakuZone);
                return TimeOnly.FromDateTime(bakuTime.DateTime);
            }

            return null;
        }

        private class HikvisionEvent
        {
            public string? EmployeeNoString { get; set; }
            public string? Name { get; set; }
            public string? Time { get; set; }
        }
    }
}
