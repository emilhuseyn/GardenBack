using App.Core.Entities;
using App.Core.Entities.Identity;
using App.Core.Enums;
using Microsoft.AspNetCore.Identity;

namespace App.DAL.Presistence
{
    public static class AppDbContextSeed
    {
        public static async Task SeedDatabaseAsync(AppDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in Enum.GetValues(typeof(EUserRole)).Cast<EUserRole>().Select(x => x.ToString()))
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminExists = await userManager.FindByEmailAsync("admin@kindergarden.az");
            if (adminExists == null)
            {
                var admin = new User
                {
                    UserName = "admin@kindergarden.az",
                    Email = "admin@kindergarden.az",
                    FirstName = "Admin",
                    LastName = "KinderGarden",
                    Role = EUserRole.Administrator,
                    IsActive = true,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, "Admin@123");
                await userManager.AddToRoleAsync(admin, EUserRole.Administrator.ToString());
            }

            if (!context.Divisions.Any())
            {
                var rusDivision = new Division
                {
                    Name = "Rus Bölməsi",
                    Language = "Russian",
                    Description = "Russian language division"
                };
                var engDivision = new Division
                {
                    Name = "İngilis Bölməsi",
                    Language = "English",
                    Description = "English language division"
                };
                context.Divisions.AddRange(rusDivision, engDivision);
                await context.SaveChangesAsync();
            }

            if (!context.ScheduleConfigs.Any())
            {
                var adminUser = await userManager.FindByEmailAsync("admin@kindergarden.az");
                var fullDay = new ScheduleConfig
                {
                    ScheduleType = ScheduleType.FullDay,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(18, 0),
                    UpdatedById = adminUser!.Id
                };
                var halfDay = new ScheduleConfig
                {
                    ScheduleType = ScheduleType.HalfDay,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(13, 0),
                    UpdatedById = adminUser.Id
                };
                context.ScheduleConfigs.AddRange(fullDay, halfDay);
                await context.SaveChangesAsync();
            }

            if (!context.Cashboxes.Any())
            {
                context.Cashboxes.AddRange(
                    new Cashbox
                    {
                        Name = "Əsas Nəğd Kassa",
                        Type = CashboxType.Cash,
                        IsActive = true
                    },
                    new Cashbox
                    {
                        Name = "Əsas Kart Hesabı",
                        Type = CashboxType.CardAccount,
                        AccountNumber = "AZ00XXXX000000000000000001",
                        IsActive = true
                    });

                await context.SaveChangesAsync();
            }
        }
    }
}
