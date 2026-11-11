namespace LibApp.Infrastructure.Db.EFCore.Configuration;

public class RoomBookConfiguration : IEntityTypeConfiguration<RoomBook>
{
    public void Configure(EntityTypeBuilder<RoomBook> builder)
    {
        builder.HasKey(rb => rb.Id);

        builder.HasOne(rb => rb.Room)
               .WithMany(r => r.RoomBooks)
                .HasForeignKey(rb => rb.RoomId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rb => rb.Book)
               .WithMany(b => b.RoomBook)
               .HasForeignKey(rb => rb.BookId)
               .OnDelete(DeleteBehavior.Cascade);

        // Инварианты БД: код уже держит их в командах, но прямой SQL или баг
        // в команде не должны ломать данные. Проверка на уровне БД — последняя линия защиты.
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_RoomBook_BookCount_NonNegative", "\"BookCount\" >= 0");
            t.HasCheckConstraint("CK_RoomBook_BorrowedCount_NonNegative", "\"BorrowedCount\" >= 0");
            t.HasCheckConstraint("CK_RoomBook_BorrowedCount_LE_BookCount", "\"BorrowedCount\" <= \"BookCount\"");
        });

        builder.Ignore(rb => rb.AvailableCount);

        builder.HasIndex("BookId");
        builder.HasIndex("RoomId");
    }
}