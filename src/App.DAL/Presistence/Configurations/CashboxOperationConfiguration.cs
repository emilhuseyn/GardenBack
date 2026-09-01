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

            // L1: ödənişdən yaranan jurnal sətri. FK QOYULMUR — ödəniş sətri silinəndə
            // əməliyyat da xidmət tərəfindən silinir, amma baza səviyyəsində asılılıq
            // yaratmırıq ki, köhnə/uyğunsuz məlumat miqrasiyanı bloklamasın.
            builder.Property(x => x.PaymentId).IsRequired(false);
            builder.HasIndex(x => x.PaymentId);

            builder.HasOne(x => x.Cashbox)
                .WithMany(x => x.Operations)
                .HasForeignKey(x => x.CashboxId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
