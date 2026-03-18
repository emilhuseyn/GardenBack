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
            builder.Property(p => p.Notes).HasMaxLength(500);
            builder.Property(p => p.RecordedById).IsRequired();

            builder.HasOne(p => p.Child)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.ChildId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.RecordedBy)
                .WithMany()
                .HasForeignKey(p => p.RecordedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => new { p.Month, p.Year, p.ChildId }).IsUnique();

            builder.HasQueryFilter(p => !p.IsDeleted);
        }
    }
}
