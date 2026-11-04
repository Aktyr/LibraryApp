namespace LibApp.Infrastructure.Db.EFCore.Configuration;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(x => x.EntityType)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(x => x.Details)
               .HasMaxLength(4000); // Ограничение, чтобы не раздувать БД гигантскими JSON

        builder.Property(x => x.ActionType)
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.Timestamp)
            .HasConversion<DateTimeToUtcConverter>();

        // Индексы под типовые сценарии фильтрации в отчётах
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ActionType);
        builder.HasIndex(x => x.EntityType);
    }
}