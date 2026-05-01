namespace LibApp.Infrastructure.Db.EFCore.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.LastName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.FirstName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.MiddleName)
               .HasMaxLength(100);

        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(u => u.ContactInfo)
               .IsRequired()
               .HasMaxLength(200);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Ignore(u => u.NearestReturnTimeSpan);
        builder.Ignore(u => u.FullName);
    }
}