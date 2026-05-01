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

        builder.HasIndex(b => b.Title);
        builder.HasIndex(b => b.Author);
    }
}