using App.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.Presistence.Configurations
{
    public class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.ToTable("groups");
            builder.HasKey(g => g.Id);
            builder.Property(g => g.Name).IsRequired().HasMaxLength(200);
            builder.Property(g => g.AgeCategory).IsRequired().HasMaxLength(50);
            builder.Property(g => g.Language).IsRequired().HasMaxLength(100);
            builder.Property(g => g.TeacherId).IsRequired();

            builder.HasOne(g => g.Division)
                .WithMany(d => d.Groups)
                .HasForeignKey(g => g.DivisionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(g => g.Teacher)
                .WithMany()
                .HasForeignKey(g => g.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(g => !g.IsDeleted);
        }
    }
}
