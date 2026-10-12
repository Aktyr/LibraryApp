namespace BlazorWeb.Components.Shared.DataGrid;

/// <summary>
/// Описание действия над выделенными элементами грида.
/// Действие может отображаться в тулбаре, в контекстном меню
/// или в обоих местах одновременно.
/// </summary>
public class DataGridAction<TItem>
{
    /// <summary>Уникальный идентификатор действия (для ключей и отладки).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Подпись кнопки/пункта меню, например «Изменить».</summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Эмодзи или иконка. Необязательно — можно включить прямо в <see cref="Label"/>.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Обработчик. Получает текущее выделение.
    /// </summary>
    public Func<IReadOnlyList<TItem>, Task>? Handler { get; set; }

    /// <summary>
    /// Кастомная проверка доступности действия.
    /// Если не задана — используется <see cref="MinSelection"/>.
    /// </summary>
    public Func<IReadOnlyList<TItem>, bool>? CanExecute { get; set; }

    /// <summary>
    /// Минимальное количество выделенных элементов,
    /// при котором действие доступно. По умолчанию — 1.
    /// </summary>
    public int MinSelection { get; set; } = 1;

    /// <summary>Показывать действие в тулбаре.</summary>
    public bool ShowInToolbar { get; set; } = true;

    /// <summary>Показывать действие в контекстном меню строки.</summary>
    public bool ShowInContextMenu { get; set; } = true;

    /// <summary>Вставить разделитель перед этим пунктом контекстного меню.</summary>
    public bool SeparatorBefore { get; set; }
}