namespace LibApp.Infrastructure.Db.EFCore.Configuration;

public class UserRoomBookConfiguration : IEntityTypeConfiguration<UserRoomBook>
{
    public void Configure(EntityTypeBuilder<UserRoomBook> builder)
    {
        builder.HasKey(urb => urb.Id);

        builder.HasOne(urb => urb.User)
               .WithMany(u => u.RoomBooks)
               .HasForeignKey("UserId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(urb => urb.RoomBook)
               .WithMany()
               .HasForeignKey("RoomBookId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(urb => urb.BorrowDate)
               .HasConversion<DateTimeToUtcConverter>();

        builder.Property(urb => urb.Deadline)
               .HasConversion<DateTimeToUtcConverter>();

        builder.Property(urb => urb.ReturnDate)
               .HasConversion<DateTimeToUtcConverter>();

        builder.Ignore(urb => urb.IsReturned);

        builder.HasIndex(urb => urb.BorrowDate);
        builder.HasIndex(urb => urb.Deadline);
        builder.HasIndex("UserId");
        builder.HasIndex("RoomBookId");
    }
}