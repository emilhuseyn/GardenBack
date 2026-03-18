using App.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.Presistence.Configurations
{
    public class ScheduleConfigConfiguration : IEntityTypeConfiguration<ScheduleConfig>
    {
        public void Configure(EntityTypeBuilder<ScheduleConfig> builder)
        {
            builder.ToTable("schedule_configs");
            builder.HasKey(s => s.Id);

            builder.HasOne(s => s.UpdatedBy)
                .WithMany()
                .HasForeignKey(s => s.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.ScheduleType).IsUnique();

            builder.HasQueryFilter(s => !s.IsDeleted);
        }
    }
}
