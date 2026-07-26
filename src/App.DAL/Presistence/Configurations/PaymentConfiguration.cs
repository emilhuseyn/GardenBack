using App.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.Presistence.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("payments");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.OriginalAmount).HasColumnType("decimal(18,2)");
            builder.Property(p => p.DiscountValue).HasColumnType("decimal(18,2)");
            builder.Property(p => p.FinalAmount).HasColumnType("decimal(18,2)");
            builder.Property(p => p.PaidAmount).HasColumnType("decimal(18,2)");
            builder.Property(p => p.LastPaymentAmount).HasColumnType("decimal(18,2)").IsRequired(false);
            builder.Property(p => p.Notes).HasMaxLength(500);
            // Kütləvi ödənişin vahid çeki bu ID ilə tarixçədən yenidən çap olunur
            builder.Property(p => p.PaymentBatchId).IsRequired(false);
            // Hesablanan dövr sütunları — "Dövr:" qeydinin əvəzi (null = tam ay)
            builder.Property(p => p.PeriodStartDay).IsRequired(false);
            builder.Property(p => p.PeriodEndDay).IsRequired(false);
            // Sıfırlamanın səbəbi olan çıxış tarixi — bərpanın yeganə açarı
            builder.Property(p => p.ZeroedByExitDate).IsRequired(false);
            // Reaktivasiyada təsdiqlənmiş yoxluq — bir daha sıfırlanmır/borca çevrilmir (D2)
            builder.Property(p => p.AbsenceConfirmed).IsRequired().HasDefaultValue(false);
            // Audit sahəsi — FK yoxdur, yalnız string kimi saxlanılır
            builder.Property(p => p.RecordedById).HasMaxLength(128).IsRequired(false);

            builder.HasOne(p => p.Child)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.ChildId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Cashbox)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.CashboxId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => new { p.Month, p.Year, p.ChildId }).IsUnique();

            builder.HasQueryFilter(p => !p.IsDeleted);
        }
    }
}
