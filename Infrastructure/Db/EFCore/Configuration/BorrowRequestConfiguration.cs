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

        builder.Property(br => br.Comment)
               .HasMaxLength(BorrowRequestPolicy.MaxCommentLength);

        // Restrict для User: заявка — часть истории читателя, hard-delete пользователя
        // с заявками запрещён. GDPR закрывается анонимизацией (AnonymizeUserCommand),
        // а не удалением строки.
        builder.HasOne(br => br.User)
               .WithMany()
               .HasForeignKey(br => br.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // Restrict для RoomBook: удаление экземпляра из комнаты при наличии
        // pending/approved заявок недопустимо — иначе заявка превратится в «висячую».
        // Проверка на уровне приложения добавляется в RoomBookSynchronizer (следующий шаг).
        builder.HasOne(br => br.RoomBook)
               .WithMany()
               .HasForeignKey(br => br.RoomBookId)
               .OnDelete(DeleteBehavior.Restrict);

        // SetNull: ProcessedByUserId обнуляется при hard-delete библиотекаря.
        // Полное удаление сотрудников не практикуется (soft-delete через IsDeleted),
        // но FK не должен ломать строку заявки, если такое когда-то случится.
        builder.HasOne(br => br.ProcessedByUser)
               .WithMany()
               .HasForeignKey(br => br.ProcessedByUserId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        // Партиальный unique-индекс: у читателя одновременно не более одной pending-заявки
        // на конкретный RoomBook. Approved/Rejected/Completed не участвуют в фильтре —
        // после отказа или возврата книгу можно запросить повторно.
        // HasFilter совпадает со строковым значением enum (HasConversion<string> выше).
        builder.HasIndex(br => new { br.UserId, br.RoomBookId })
               .IsUnique()
               .HasFilter("\"Status\" = 'Pending'");

        builder.HasIndex(br => br.Status);
        builder.HasIndex(br => br.UserId);
        builder.HasIndex(br => br.RoomBookId);
        builder.HasIndex(br => br.ProcessedByUserId);
    }
}