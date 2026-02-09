namespace LibApp.Application.Interfaces;

public interface IConverter<TEntity, TDto> where TEntity : IEntity, new() where TDto : new()
{
    TDto ToDto(TEntity entity);
    TEntity ToEntity(TDto dto);
}

// todo ты можешь сделать "базовый конвертер", который с помощью рефлексии копирует совпадающие по именам свойства (в общем виде)