using App.Core.Entities;
using App.Core.Entities.Identity;
using App.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Presistence
{
    public static class TestDataSeeder
    {
        private const string ParentPhone     = "+994503954614";
        private const string DefaultPassword = "Test@1234";
        private const string MarkerEmail     = "teacher1@kindergarden.az";

        public static async Task SeedTestDataAsync(AppDbContext context, UserManager<User> userManager)
        {
           
        }

        private static async Task ClearExistingDataAsync(AppDbContext context)
        {
            Console.WriteLine("[Seed] Köhnə data silinir...");
            await context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM `sms_notifications`");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM `attendances`");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM `payments`");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM `children`");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM `groups`");
            await context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1");
            Console.WriteLine("[Seed] Köhnə data silindi.");
        }

        private static async Task<List<User>> SeedUsersAsync(UserManager<User> userManager)
        {
            var userData = new (string First, string Last, string Email, EUserRole Role)[]
            {
                ("Aynur",  "Həsənova",   "teacher1@kindergarden.az",   EUserRole.Teacher),
                ("Gülnar", "Əliyeva",    "teacher2@kindergarden.az",   EUserRole.Teacher),
                ("Nərmin", "Hüseynova",  "teacher3@kindergarden.az",   EUserRole.Teacher),
                ("Sarah",  "Johnson",    "teacher4@kindergarden.az",   EUserRole.Teacher),
                ("Emily",  "Brown",      "teacher5@kindergarden.az",   EUserRole.Teacher),
                ("Anna",   "Smith",      "teacher6@kindergarden.az",   EUserRole.Teacher),
                ("Rəna",   "Quliyeva",   "accountant@kindergarden.az", EUserRole.Accountant),
                ("Zəhra",  "Nəsirzadə", "admission@kindergarden.az",  EUserRole.AdmissionStaff),
            };

            var teachers = new List<User>();
            foreach (var (first, last, email, role) in userData)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new User
                    {
                        UserName = email, Email = email,
                        FirstName = first, LastName = last,
                        Role = role, IsActive = true, EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(user, DefaultPassword);
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(user, role.ToString());
                    else
                        Console.WriteLine($"[Seed] Xəta ({email}): {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
                if (role == EUserRole.Teacher) teachers.Add(user);
            }
            Console.WriteLine($"[Seed] {teachers.Count} müəllim yaradıldı.");
            return teachers;
        }

        private static async Task<List<Group>> SeedGroupsAsync(
            AppDbContext context, int rusId, int engId, List<User> teachers)
        {
            var definitions = new[]
            {
                (Name: "Göyçək",   DivId: rusId, TIdx: 0, Age: "2-3 yaş", Lang: "Russian"),
                (Name: "Günəşli",  DivId: rusId, TIdx: 1, Age: "3-4 yaş", Lang: "Russian"),
                (Name: "Zərif",    DivId: rusId, TIdx: 2, Age: "4-5 yaş", Lang: "Russian"),
                (Name: "Şən",      DivId: rusId, TIdx: 0, Age: "5-6 yaş", Lang: "Russian"),
                (Name: "Sunshine", DivId: engId, TIdx: 3, Age: "2-3 yaş", Lang: "English"),
                (Name: "Rainbow",  DivId: engId, TIdx: 4, Age: "3-4 yaş", Lang: "English"),
                (Name: "Stars",    DivId: engId, TIdx: 5, Age: "4-5 yaş", Lang: "English"),
                (Name: "Moon",     DivId: engId, TIdx: 3, Age: "5-6 yaş", Lang: "English"),
            };

            var groups = definitions.Select(d => new Group
            {
                Name = d.Name, DivisionId = d.DivId, TeacherId = teachers[d.TIdx].Id,
                MaxChildCount = 15, AgeCategory = d.Age, Language = d.Lang
            }).ToList();

            context.Groups.AddRange(groups);
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seed] {groups.Count} qrup yaradıldı.");
            return groups;
        }

        private static async Task SeedChildrenAsync(AppDbContext context, List<Group> groups)
        {
            var rnd = new Random(42);
            string[] azBoy   = ["Amir","Tural","Rauf","Elvin","Murad","Nihat","Rəşad","Vüsal","Kamil","Elnur"];
            string[] azGirl  = ["Aynur","Günel","Nərmin","Leyla","Səbinə","Fərəh","Nuray","Aytən","Xədicə","Gülnar"];
            string[] azLast  = ["Həsənov","Əliyev","Məmmədov","Hüseynov","Quliyev","İsmayılov","Rəhimov","Əhmədov","Nəsirov","Bağırov"];
            string[] enBoy   = ["Liam","Noah","Oliver","Lucas","James","Ethan","Logan","Mason","Jack","Leo"];
            string[] enGirl  = ["Emma","Olivia","Ava","Sophia","Isabella","Mia","Charlotte","Amelia","Harper","Evelyn"];
            string[] enLast  = ["Smith","Johnson","Brown","Davis","Miller","Wilson","Moore","Taylor","Anderson","Thomas"];
            string[] pFirst  = ["Anar","Elnur","Vüsal","Tural","Murad","Rauf","Nihat","Kamil","Elşən","Rəşad"];
            string[] pLast   = ["Həsənov","Əliyev","Məmmədov","Hüseynov","Quliyev","İsmayılov","Rəhimov","Əhmədov","Nəsirov","Bağırov"];

            var now      = DateTime.UtcNow;
            var children = new List<Child>();

            foreach (var group in groups)
            {
                bool isRus   = group.Language == "Russian";
                var  boys    = isRus ? azBoy  : enBoy;
                var  girls   = isRus ? azGirl : enGirl;
                var  lasts   = isRus ? azLast : enLast;
                int  minAge  = int.Parse(group.AgeCategory[0].ToString());

                for (int i = 0; i < 10; i++)
                {
                    bool isBoy = i % 2 == 0;
                    var  first = isBoy ? boys[i % boys.Length] : girls[i % girls.Length];
                    var  basL  = lasts[i % lasts.Length];
                    var  last  = (isRus && !isBoy) ? basL + "a" : basL;

                    var sched  = i % 3 == 0 ? ScheduleType.HalfDay : ScheduleType.FullDay;
                    var fee    = sched == ScheduleType.FullDay ? rnd.Next(25,36)*10m : rnd.Next(15,21)*10m;

                    children.Add(new Child
                    {
                        FirstName        = first,
                        LastName         = last,
                        DateOfBirth      = now.AddYears(-(minAge + rnd.Next(0,2))).AddDays(-rnd.Next(0,300)),
                        GroupId          = group.Id,
                        ScheduleType     = sched,
                        MonthlyFee       = fee,
                        RegistrationDate = now.AddMonths(-rnd.Next(1,13)),
                        Status           = i == 9 ? ChildStatus.Inactive : ChildStatus.Active,
                        ParentFullName   = $"{pFirst[rnd.Next(pFirst.Length)]} {pLast[i % pLast.Length]}",
                        ParentPhone      = ParentPhone,
                        ParentEmail      = $"parent{i+1}.{group.Name.ToLowerInvariant().Replace(" ","")}@mail.az"
                    });
                }
            }

            context.Children.AddRange(children);
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seed] {children.Count} uşaq yaradıldı.");
        }

        private static async Task SeedPaymentsAsync(AppDbContext context, string adminId)
        {
            var active = await context.Children.Where(c => c.Status == ChildStatus.Active).ToListAsync();
            var now    = DateTime.UtcNow;
            var months = new[]
            {
                (now.AddMonths(-2).Month, now.AddMonths(-2).Year),
                (now.AddMonths(-1).Month, now.AddMonths(-1).Year),
                (now.Month, now.Year),
            };
            var rnd      = new Random(42);
            var payments = new List<Payment>();

            foreach (var child in active)
            {
                bool   disc  = child.Id % 5 == 0;
                var    dType = disc ? DiscountType.Percentage : DiscountType.None;
                var    dVal  = disc ? 10m : 0m;
                var    final = disc ? Math.Round(child.MonthlyFee * 0.9m, 2) : child.MonthlyFee;

                for (int mi = 0; mi < months.Length; mi++)
                {
                    var (month, year) = months[mi];
                    PaymentStatus st; decimal paid;

                    if (mi == 0) { var r=rnd.Next(6); st=r<4?PaymentStatus.Paid:r==4?PaymentStatus.PartiallyPaid:PaymentStatus.Debt; paid=st==PaymentStatus.Paid?final:st==PaymentStatus.PartiallyPaid?Math.Round(final*.8m,2):0m; }
                    else if (mi==1) { var r=rnd.Next(4); st=r<2?PaymentStatus.Paid:r==2?PaymentStatus.PartiallyPaid:PaymentStatus.Debt; paid=st==PaymentStatus.Paid?final:st==PaymentStatus.PartiallyPaid?Math.Round(final*.6m,2):0m; }
                    else { var r=rnd.Next(5); st=r==0?PaymentStatus.Paid:r==1?PaymentStatus.PartiallyPaid:PaymentStatus.Debt; paid=st==PaymentStatus.Paid?final:st==PaymentStatus.PartiallyPaid?Math.Round(final*.5m,2):0m; }

                    payments.Add(new Payment
                    {
                        ChildId=child.Id, Month=month, Year=year,
                        OriginalAmount=child.MonthlyFee, DiscountType=dType, DiscountValue=dVal,
                        FinalAmount=final, PaidAmount=paid, Status=st,
                        PaymentDate=st!=PaymentStatus.Debt?now.AddDays(-rnd.Next(1,25)):null,
                        RecordedById=adminId
                    });
                }
            }

            context.Payments.AddRange(payments);
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seed] {payments.Count} ödəniş yaradıldı.");
        }

        private static async Task SeedAttendanceAsync(AppDbContext context, string adminId)
        {
            var active  = await context.Children.Where(c => c.Status == ChildStatus.Active).ToListAsync();
            var configs = await context.ScheduleConfigs.ToListAsync();
            var fdCfg   = configs.FirstOrDefault(c => c.ScheduleType == ScheduleType.FullDay);
            var hdCfg   = configs.FirstOrDefault(c => c.ScheduleType == ScheduleType.HalfDay);

            var days   = new List<DateOnly>();
            var cursor = DateOnly.FromDateTime(DateTime.UtcNow);
            while (days.Count < 20)
            {
                if (cursor.DayOfWeek != DayOfWeek.Saturday && cursor.DayOfWeek != DayOfWeek.Sunday) days.Add(cursor);
                cursor = cursor.AddDays(-1);
            }

            var today = days[0];
            var rnd   = new Random(42);
            var list  = new List<Attendance>();

            foreach (var child in active)
            {
                var cfg = child.ScheduleType == ScheduleType.FullDay ? fdCfg : hdCfg;
                if (cfg == null) continue;

                foreach (var day in days)
                {
                    bool isToday = day == today;
                    if (rnd.Next(100) < 15) { list.Add(new Attendance { ChildId=child.Id, Date=day, Status=AttendanceStatus.Absent, RecordedById=adminId }); continue; }

                    bool isLate  = rnd.Next(100) < 12;
                    var  arrival = isLate ? cfg.StartTime.AddMinutes(rnd.Next(10,50)) : cfg.StartTime.AddMinutes(-rnd.Next(0,10));
                    TimeOnly? depart = null; bool isEarly = false;
                    if (!isToday) { isEarly=rnd.Next(100)<8; depart=isEarly?cfg.EndTime.AddMinutes(-rnd.Next(30,90)):cfg.EndTime.AddMinutes(rnd.Next(0,15)); }

                    list.Add(new Attendance { ChildId=child.Id, Date=day, Status=AttendanceStatus.Present, ArrivalTime=arrival, DepartureTime=depart, IsLate=isLate, IsEarlyLeave=isEarly, RecordedById=adminId });
                }
            }

            context.Attendances.AddRange(list);
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seed] {list.Count} davamiyyət qeydi yaradıldı.");
        }
    }
}
