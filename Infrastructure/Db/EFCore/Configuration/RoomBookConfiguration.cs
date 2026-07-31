namespace LibApp.Infrastructure.Db.EFCore.Configuration;

public class RoomBookConfiguration : IEntityTypeConfiguration<RoomBook>
{
    public void Configure(EntityTypeBuilder<RoomBook> builder)
    {
        builder.HasKey(rb => rb.Id);

        builder.HasOne(rb => rb.Room)
               .WithMany(r => r.RoomBooks)
               .HasForeignKey("RoomId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rb => rb.Book)
               .WithMany(b => b.RoomBook)
               .HasForeignKey("BookId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(rb => rb.AvailableCount);

        builder.HasIndex("BookId");
        builder.HasIndex("RoomId");
    }
}