using BlogApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace BlogApp.Data.Configurations;

public class VerificationCodeConfiguration : IEntityTypeConfiguration<VerificationCode>
{
    public void Configure(EntityTypeBuilder<VerificationCode> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(v => v.Code);

        builder.HasOne(v => v.User)
            .WithMany(u => u.VerificationCodes)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
