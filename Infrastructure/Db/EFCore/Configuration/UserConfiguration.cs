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

        builder.Property(u => u.IsDeleted)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(u => u.DeletedAt);

        builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasFilter("\"IsDeleted\" = false");

        builder.Property(u => u.SubscriptionStatus)
               .IsRequired()
               .HasConversion<string>()
               .HasDefaultValue(SubscriptionStatus.Unconfirmed);

        builder.HasIndex(u => u.IsDeleted);

        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.Ignore(u => u.NearestReturnTimeSpan);
        builder.Ignore(u => u.FullName);
    }
}