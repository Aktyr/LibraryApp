namespace LibApp.Application.Converters;

public abstract class BaseConverter<TEntity, TDto> : IConverter<TEntity, TDto>
    where TEntity : IEntity, new()
    where TDto : new()
{
    public virtual TDto ToDto(TEntity entity)
    {
        var dto = new TDto();
        CopyMatchingProperties(entity, dto, ToDtoConverters);
        return dto;
    }

    public virtual TEntity ToEntity(TDto dto)
    {
        var entity = new TEntity();
        CopyMatchingProperties(dto, entity, ToEntityConverters);
        InitializeCollections(entity);
        return entity;
    }

    protected virtual Dictionary<string, Func<object?, object?>> ToDtoConverters
        => new() { ["Id"] = value => value is Id id ? id.Value : value };

    protected virtual Dictionary<string, Func<object?, object?>> ToEntityConverters
        => new() { ["Id"] = value => value is Guid guid ? new Id(guid) : value };



    // Получает все свойства исходного объекта
    protected void CopyMatchingProperties<TSource, TTarget>(TSource source, TTarget target,
        Dictionary<string, Func<object?, object?>>? customConverters = null)
    {
        var sourceProperties = typeof(TSource).GetProperties();
        var targetProperties = typeof(TTarget).GetProperties().ToDictionary(p => p.Name);

        foreach (var sourceProperty in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty) && targetProperty.CanWrite)
            {
                var sourceValue = sourceProperty.GetValue(source);

                // Применяем кастомный конвертер
                if (customConverters != null && customConverters.TryGetValue(sourceProperty.Name, out var converter))
                    sourceValue = converter(sourceValue);

                // Если типы совместимы - присваиваем
                if (sourceValue != null && targetProperty.PropertyType.IsAssignableFrom(sourceValue.GetType()))
                    targetProperty.SetValue(target, sourceValue);
            }
        }
    }


    // Инициализирует не объявленные свойства в объектах DTO
    protected virtual void InitializeCollections(TEntity entity)  
    {
        var collectionProperties = typeof(TEntity)
            .GetProperties()
            .Where(p => typeof(ICollection).IsAssignableFrom(p.PropertyType)
                        && p.PropertyType != typeof(string)
                        && p.GetValue(entity) == null);

        foreach (var prop in collectionProperties)
        {
            var collectionType = prop.PropertyType;

            if (collectionType.IsGenericType)
            {
                var listType = typeof(List<>).MakeGenericType(collectionType.GetGenericArguments());
                prop.SetValue(entity, Activator.CreateInstance(listType));
            }
        }
    }
}
