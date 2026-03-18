using App.Core.Entities.Identity;
using App.Core.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            var adminExists = await userManager.FindByNameAsync("admin");
            var modExists = await userManager.FindByNameAsync("moderator");

            if (adminExists == null)
            {
                var userAdmin = new User { UserName = "admin", Email = "admin@admin.com", EmailConfirmed = true };
                await userManager.CreateAsync(userAdmin, "!Admin123.?Back3ndFr0nt3nd@");
                await userManager.AddToRoleAsync(userAdmin, EUserRole.Admin.ToString());
            }

            if (modExists == null)
            {
                var userMod = new User { UserName = "moderator", Email = "mod@mod.com", EmailConfirmed = true };
                await userManager.CreateAsync(userMod, "!Mod123.?Back3ndFr0nt3nd@");
                await userManager.AddToRoleAsync(userMod, EUserRole.Moderator.ToString());
            }

            await context.SaveChangesAsync();
        }
    }
}
