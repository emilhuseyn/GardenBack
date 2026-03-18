using App.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.Presistence.Configurations
{
    public class DivisionConfiguration : IEntityTypeConfiguration<Division>
    {
        public void Configure(EntityTypeBuilder<Division> builder)
        {
            builder.ToTable("divisions");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
            builder.Property(d => d.Language).IsRequired().HasMaxLength(100);
            builder.Property(d => d.Description).HasMaxLength(500);
            builder.HasQueryFilter(d => !d.IsDeleted);
        }
    }
}
