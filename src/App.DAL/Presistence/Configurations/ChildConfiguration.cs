using App.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.Presistence.Configurations
{
    public class ChildConfiguration : IEntityTypeConfiguration<Child>
    {
        public void Configure(EntityTypeBuilder<Child> builder)
        {
            builder.ToTable("children");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(c => c.LastName).IsRequired().HasMaxLength(100);
            builder.Property(c => c.MonthlyFee).HasColumnType("decimal(18,2)");
            builder.Property(c => c.PaymentDay).IsRequired().HasDefaultValue(1);
            builder.HasCheckConstraint("CK_children_PaymentDay", "[PaymentDay] >= 1 AND [PaymentDay] <= 28");
            builder.Property(c => c.ParentFullName).IsRequired().HasMaxLength(200);
            builder.Property(c => c.SecondParentFullName).HasMaxLength(200);
            builder.Property(c => c.ParentPhone).IsRequired().HasMaxLength(20);
            builder.Property(c => c.SecondParentPhone).HasMaxLength(20);
            builder.Property(c => c.ParentEmail).HasMaxLength(200);
            builder.Property(c => c.FaceIdToken).HasMaxLength(500);

            builder.HasOne(c => c.Group)
                .WithMany(g => g.Children)
                .HasForeignKey(c => c.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(c => !c.IsDeleted);
        }
    }
}
