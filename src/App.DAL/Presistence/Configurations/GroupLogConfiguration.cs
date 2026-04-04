using App.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.Presistence.Configurations
{
    public class GroupLogConfiguration : IEntityTypeConfiguration<GroupLog>
    {
        public void Configure(EntityTypeBuilder<GroupLog> builder)
        {
            builder.ToTable("group_logs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ActionType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Message)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.ActionDate)
                .IsRequired();
        }
    }
}
