namespace LibApp.Infrastructure.Db.EFCore.Configuration;

public class BookImageConfiguration : IEntityTypeConfiguration<BookImage>
{
    public void Configure(EntityTypeBuilder<BookImage> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Url)
               .IsRequired()
               .HasMaxLength(BookPolicy.MaxImageUrlLength);

        builder.HasOne(i => i.Book)
               .WithMany(b => b.Images)
               .HasForeignKey(i => i.BookId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.BookId);
        builder.HasIndex(i => new { i.BookId, i.SortOrder });
    }
}