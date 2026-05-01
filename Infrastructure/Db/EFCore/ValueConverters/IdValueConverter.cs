namespace LibApp.Infrastructure.Db.EFCore.ValueConverters;

public class IdValueConverter : ValueConverter<Id, Guid>
{
    public IdValueConverter() : base(id => id.Value, 
                                     guid => new Id(guid)) { }
}