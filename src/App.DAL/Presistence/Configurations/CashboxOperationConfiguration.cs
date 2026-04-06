using App.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.Presistence.Configurations
{
    public class CashboxOperationConfiguration : IEntityTypeConfiguration<CashboxOperation>
    {
        public void Configure(EntityTypeBuilder<CashboxOperation> builder)
        {
            builder.ToTable("cashbox_operations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(x => x.Note)
                .HasMaxLength(500);

            builder.Property(x => x.OperationDate)
                .IsRequired();

            builder.HasOne(x => x.Cashbox)
                .WithMany(x => x.Operations)
                .HasForeignKey(x => x.CashboxId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
