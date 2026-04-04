using App.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.Presistence.Configurations
{
    public class CashboxConfiguration : IEntityTypeConfiguration<Cashbox>
    {
        public void Configure(EntityTypeBuilder<Cashbox> builder)
        {
            builder.ToTable("cashboxes");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.AccountNumber)
                .HasMaxLength(100);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            builder.HasIndex(x => x.Name).IsUnique();
            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
