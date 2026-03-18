using App.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.Presistence.Configurations
{
    public class SMSNotificationConfiguration : IEntityTypeConfiguration<SMSNotification>
    {
        public void Configure(EntityTypeBuilder<SMSNotification> builder)
        {
            builder.ToTable("sms_notifications");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.RecipientPhone).IsRequired().HasMaxLength(20);
            builder.Property(s => s.Message).IsRequired().HasMaxLength(1000);

            builder.HasOne(s => s.Child)
                .WithMany()
                .HasForeignKey(s => s.ChildId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasQueryFilter(s => !s.IsDeleted);
        }
    }
}
