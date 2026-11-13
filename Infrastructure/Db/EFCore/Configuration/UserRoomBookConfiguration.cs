namespace LibApp.Infrastructure.Db.EFCore.Configuration;

public class UserRoomBookConfiguration : IEntityTypeConfiguration<UserRoomBook>
{
    public void Configure(EntityTypeBuilder<UserRoomBook> builder)
    {
        builder.HasKey(urb => urb.Id);

        builder.HasOne(urb => urb.User)
               .WithMany(u => u.RoomBooks)
               .HasForeignKey(urb => urb.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(urb => urb.RoomBook)
               .WithMany()
               .HasForeignKey(urb => urb.RoomBookId)
               .OnDelete(DeleteBehavior.Restrict);

        // Связь заявки с выдачей. Создаётся только при одобрении, поэтому nullable.
        // Restrict: удаление заявки не должно молча терять историю выдачи.
        builder.HasOne(urb => urb.BorrowRequest)
               .WithMany()
               .HasForeignKey(urb => urb.BorrowRequestId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        // Одна заявка порождает не более одной выдачи. Partial unique index:
        // обычные выдачи (BorrowRequestId IS NULL) не конфликтуют между собой.
        builder.HasIndex(urb => urb.BorrowRequestId)
               .IsUnique()
               .HasFilter("\"BorrowRequestId\" IS NOT NULL");

        builder.Property(urb => urb.BorrowDate)
               .HasConversion<DateTimeToUtcConverter>();

        builder.Property(urb => urb.Deadline)
               .HasConversion<DateTimeToUtcConverter>();

        builder.Property(urb => urb.ReturnDate)
               .HasConversion<DateTimeToUtcConverter>();

        builder.Property(urb => urb.ReminderSentAt)
               .HasConversion<DateTimeToUtcConverter>();

        builder.Property(urb => urb.OverdueNoticeSentAt)
               .HasConversion<DateTimeToUtcConverter>();

        builder.Property(urb => urb.Penalty)
               .HasPrecision(18, 2);

        builder.Ignore(urb => urb.IsReturned);
        builder.Ignore(urb => urb.IsPenaltyPaid);

        builder.HasQueryFilter(urb => !urb.User.IsDeleted);

        builder.HasIndex(urb => urb.BorrowDate);
        builder.HasIndex(urb => urb.Deadline);
        builder.HasIndex("UserId");
        builder.HasIndex("RoomBookId");
    }
}