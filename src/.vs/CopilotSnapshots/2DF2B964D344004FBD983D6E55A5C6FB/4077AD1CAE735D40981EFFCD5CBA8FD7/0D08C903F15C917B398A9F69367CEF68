using App.Core.Entities.Commons;
using App.Core.Entities.Identity;
using App.Shared.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace App.DAL.Presistence
{
    public class AppDbContext : IdentityDbContext<User>
    {
        private readonly IClaimService _claimService;

        public AppDbContext(DbContextOptions<AppDbContext> options, IClaimService claimService) : base(options)
        {
            _claimService = claimService;
        }

        // Models here!!!

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }

        public new async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
        {
            foreach (var entry in ChangeTracker.Entries<IAuditedEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedBy = _claimService.GetUserId() ?? "ByServer";
                        entry.Entity.CreatedOn = DateTime.UtcNow;

                        entry.Entity.UpdatedBy = _claimService.GetUserId() ?? "ByServer";
                        entry.Entity.UpdatedOn = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedBy = _claimService.GetUserId() ?? "ByServer";
                        entry.Entity.UpdatedOn = DateTime.UtcNow;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
