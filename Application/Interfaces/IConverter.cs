namespace LibApp.Application.Interfaces;

public interface IConverter<TEntity, TDto> where TEntity : IEntity, new() where TDto : new()
{
    TDto ToDto(TEntity entity);
    TEntity ToEntity(TDto dto);
}