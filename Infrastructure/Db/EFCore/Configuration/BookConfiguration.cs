namespace LibApp.Infrastructure.Db.EFCore.Configuration;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
               .IsRequired()
               .HasMaxLength(BookPolicy.MaxTitleLength);

        builder.Property(b => b.Author)
               .IsRequired()
               .HasMaxLength(BookPolicy.MaxAuthorLength);

        builder.Property(b => b.Genre)
               .IsRequired()
               .HasMaxLength(BookPolicy.MaxGenreLength);

        builder.Property(b => b.Publisher)
               .HasMaxLength(BookPolicy.MaxPublisherLength);

        builder.Property(b => b.Description)
               .HasMaxLength(BookPolicy.MaxDescriptionLength)
               .HasDefaultValue(string.Empty);

        builder.Property(b => b.CoverUrl)
               .HasMaxLength(BookPolicy.MaxCoverUrlLength)
               .HasDefaultValue(string.Empty);

        builder.Property(b => b.IsDiscarded)
               .IsRequired()
               .HasDefaultValue(false);

        builder.HasIndex(b => b.Title);
        builder.HasIndex(b => b.Author);
        builder.HasIndex(b => b.IsDiscarded);
    }
}