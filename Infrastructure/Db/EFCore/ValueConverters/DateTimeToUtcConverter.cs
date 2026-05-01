namespace LibApp.Infrastructure.Db.EFCore.ValueConverters;

public class DateTimeToUtcConverter : ValueConverter<DateTime, DateTime>
{
    public DateTimeToUtcConverter() : base(v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                                           v => DateTime.SpecifyKind(v, DateTimeKind.Utc)) { }
}