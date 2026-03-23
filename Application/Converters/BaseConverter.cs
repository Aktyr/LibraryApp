namespace LibApp.Application.Converters;

public abstract class BaseConverter<TEntity, TDto> : IConverter<TEntity, TDto>
    where TEntity : IEntity, new()
    where TDto : new()
{
    public virtual TDto ToDto(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var dto = new TDto();
        MapProperties(entity, dto);
        return dto;
    }

    public virtual TEntity ToEntity(TDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var entity = new TEntity();
        MapProperties(dto, entity);
        InitializeCollections(entity);
        return entity;
    }

    private void MapProperties<TSource, TTarget>(TSource source, TTarget target)
    {
        var sourceProps = typeof(TSource).GetProperties();
        var targetProps = typeof(TTarget).GetProperties().ToDictionary(p => p.Name);

        foreach (var sourceProp in sourceProps)
        {
            // Пропускаем индексаторы и свойства без сеттера
            if (sourceProp.GetIndexParameters().Length > 0) continue;
            if (!targetProps.TryGetValue(sourceProp.Name, out var targetProp)) continue;
            if (!targetProp.CanWrite) continue;

            var value = sourceProp.GetValue(source);
            if (value == null) continue;

            // Конвертируем значение
            var converted = ConvertValue(value, targetProp.PropertyType);
            if (converted != null)
            {
                targetProp.SetValue(target, converted);
            }
        }
    }
    // Конверсия
    private static object? ConvertValue(object value, Type targetType)
    {
        // Id -> Guid
        if (value is Id id && targetType == typeof(Guid))
            return id.Value;

        // Guid -> Id
        if (value is Guid guid && targetType == typeof(Id))
            return new Id(guid);

        // Если типы совместимы, возвращаем как есть
        if (targetType.IsAssignableFrom(value.GetType()))
            return value;

        return null;
    }

    private static void InitializeCollections(TEntity entity)
    {
        var collections = typeof(TEntity)
            .GetProperties()
            .Where(p => typeof(ICollection).IsAssignableFrom(p.PropertyType)
                     && p.PropertyType != typeof(string)
                     && p.GetValue(entity) == null);

        foreach (var prop in collections)
        {
            if (prop.PropertyType.IsGenericType)
            {
                var itemType = prop.PropertyType.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(itemType);
                prop.SetValue(entity, Activator.CreateInstance(listType));
            }
        }
    }
}