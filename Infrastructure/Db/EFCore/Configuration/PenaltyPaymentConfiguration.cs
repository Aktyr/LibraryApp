namespace LibApp.Infrastructure.Db.EFCore.Configuration;

public class PenaltyPaymentConfiguration : IEntityTypeConfiguration<PenaltyPayment>
{
    public void Configure(EntityTypeBuilder<PenaltyPayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.UserRoomBook)
               .WithMany(urb => urb.PenaltyPayments)
               .HasForeignKey(p => p.UserRoomBookId)
               .OnDelete(DeleteBehavior.Cascade);

        // PaidByUser — не владелец платежа, а исторический атрибут.
        // Restrict: пользователь-кассир не должен удаляться, если за ним есть платежи.
        builder.HasOne(p => p.PaidByUser)
               .WithMany()
               .HasForeignKey(p => p.PaidByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.Amount)
               .IsRequired()
               .HasPrecision(18, 2);

        builder.Property(p => p.PaidAt)
               .IsRequired()
               .HasConversion<DateTimeToUtcConverter>();

        builder.Property(p => p.Notes)
               .HasMaxLength(PenaltyPaymentPolicy.MaxNotesLength);

        // Один платёж на выдачу: частичные оплаты не поддерживаются.
        // UNIQUE-индекс защищает от двойной оплаты при параллельных запросах.
        builder.HasIndex(p => p.UserRoomBookId).IsUnique();

        builder.HasIndex(p => p.PaidAt);
        builder.HasIndex(p => p.PaidByUserId);
    }
}