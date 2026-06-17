namespace LibApp.Infrastructure.Db.EFCore.Configuration;

public class DiscardedBookConfiguration : IEntityTypeConfiguration<DiscardedBook>
{
    public void Configure(EntityTypeBuilder<DiscardedBook> builder)
    {
        builder.HasKey(d => d.Id);

        builder.HasOne(d => d.Book)
               .WithMany()
               .HasForeignKey("BookId")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Room)
               .WithMany()
               .HasForeignKey(d => d.RoomId)
               .OnDelete(DeleteBehavior.Restrict);


        builder.Property(d => d.Amount)
               .IsRequired();

        builder.Property(d => d.DiscardReason)
               .IsRequired()
               .HasConversion<string>();

        builder.Property(d => d.ApprovedBy)
               .HasMaxLength(100);

        builder.Property(d => d.CompensationAmount)
               .HasPrecision(18, 2);
    }
}