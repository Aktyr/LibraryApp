namespace LibApp.Infrastructure.Db.EFCore.Configuration;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
               .IsRequired()
               .HasMaxLength(RoomPolicy.MaxNameLength);

        builder.HasIndex(r => r.Name).IsUnique();

        builder.Ignore(r => r.SumOfBooks);
    }
}