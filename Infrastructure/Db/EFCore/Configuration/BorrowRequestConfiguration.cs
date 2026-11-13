namespace LibApp.Infrastructure.Db.EFCore.Configuration;

public class BorrowRequestConfiguration : IEntityTypeConfiguration<BorrowRequest>
{
    public void Configure(EntityTypeBuilder<BorrowRequest> builder)
    {
        builder.HasKey(br => br.Id);

        builder.Property(br => br.Status)
               .IsRequired()
               .HasConversion<string>();

        builder.Property(br => br.CreatedAt)
               .IsRequired()
               .HasConversion<DateTimeToUtcConverter>();

        builder.Property(br => br.ProcessedAt)
               .HasConversion<DateTimeToUtcConverter>();

        builder.Property(br => br.ExpiresAt)
               .HasConversion<DateTimeToUtcConverter>();

        builder.Property(br => br.PickedUpAt)
               .HasConversion<DateTimeToUtcConverter>();

        builder.Property(br => br.Comment)
               .HasMaxLength(BorrowRequestPolicy.MaxCommentLength);

        // Restrict для User: заявка — часть истории читателя, hard-delete пользователя
        // с заявками запрещён. GDPR закрывается анонимизацией, а не удалением строки.
        builder.HasOne(br => br.User)
               .WithMany()
               .HasForeignKey(br => br.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // Restrict для RoomBook: удаление экземпляра при наличии активных заявок
        // недопустимо — иначе заявка превратится в «висячую».
        builder.HasOne(br => br.RoomBook)
               .WithMany()
               .HasForeignKey(br => br.RoomBookId)
               .OnDelete(DeleteBehavior.Restrict);

        // SetNull: ProcessedByUserId обнуляется при hard-delete библиотекаря.
        builder.HasOne(br => br.ProcessedByUser)
               .WithMany()
               .HasForeignKey(br => br.ProcessedByUserId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        // Партиальный unique: у читателя одновременно не более одной активной заявки
        // на конкретный RoomBook. Фильтр расширен на Pending + Approved:
        // после одобрения нельзя запросить ту же книгу повторно, пока бронь не закрыта.
        builder.HasIndex(br => new { br.UserId, br.RoomBookId })
               .IsUnique()
               .HasFilter("\"Status\" IN ('Pending', 'Approved')");

        builder.HasIndex(br => br.Status);
        builder.HasIndex(br => br.UserId);
        builder.HasIndex(br => br.RoomBookId);
        builder.HasIndex(br => br.ProcessedByUserId);

        // Индекс для фонового ExpireBorrowRequestsService: ищет Approved с истёкшим ExpiresAt.
        // Частичный — только Approved, иначе полнота индекса раздувается историями.
        builder.HasIndex(br => br.ExpiresAt)
               .HasFilter("\"Status\" = 'Approved'");
    }
}