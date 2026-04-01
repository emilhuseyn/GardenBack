using App.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.Presistence.Configurations
{
    public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
    {
        public void Configure(EntityTypeBuilder<Attendance> builder)
        {
            builder.ToTable("attendances");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Notes).HasMaxLength(500);
            // Audit sahəsi — FK yoxdur, yalnız string kimi saxlanılır
            builder.Property(a => a.RecordedById).HasMaxLength(128).IsRequired(false);

            builder.HasOne(a => a.Child)
                .WithMany(c => c.Attendances)
                .HasForeignKey(a => a.ChildId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => a.ChildId);
            builder.HasIndex(a => a.Date);
            builder.HasIndex(a => new { a.ChildId, a.Date }).IsUnique();

            builder.HasQueryFilter(a => !a.IsDeleted);
        }
    }
}
