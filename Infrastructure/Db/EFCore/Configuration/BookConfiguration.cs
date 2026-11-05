using LibApp.Core.Policies;

namespace LibApp.Infrastructure.Db.EFCore.Configuration;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(b => b.Author)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(b => b.Publisher)
               .HasMaxLength(100);

        // Лимиты — из BookPolicy (SSOT). Description необязателен, поэтому .IsRequired() не ставим:
        // колонка будет NOT NULL с default '' после миграции (сущность инициализирует string.Empty).
        builder.Property(b => b.Description)
               .HasMaxLength(BookPolicy.MaxDescriptionLength);

        builder.Property(b => b.CoverUrl)
               .HasMaxLength(BookPolicy.MaxCoverUrlLength);

        builder.Property(b => b.IsDiscarded)
               .IsRequired()
               .HasDefaultValue(false);

        builder.HasIndex(b => b.Title);
        builder.HasIndex(b => b.Author);
        builder.HasIndex(b => b.IsDiscarded);
    }
}