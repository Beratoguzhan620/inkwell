using BlogApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlogApp.Data.Configurations;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.HasKey(bp => bp.Id);

        builder.Property(bp => bp.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(bp => bp.Content)
            .IsRequired();

        builder.Property(bp => bp.Status)
            .IsRequired()
            .HasDefaultValue(BlogPostStatus.Pending);

        builder.Property(bp => bp.CreatedAt)
            .IsRequired();

        builder.HasOne(bp => bp.Category)
            .WithMany(c => c.Posts)
            .HasForeignKey(bp => bp.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bp => bp.Author)
            .WithMany(u => u.Posts)
            .HasForeignKey(bp => bp.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
