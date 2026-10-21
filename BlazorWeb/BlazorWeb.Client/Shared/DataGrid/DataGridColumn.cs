using Microsoft.AspNetCore.Components;

namespace BlazorWeb.Components.Shared.DataGrid;

/// <summary>
/// Описание колонки грида: заголовок, ширина, доступ к значению,
/// шаблон рендера и параметры фильтрации.
/// </summary>
public class DataGridColumn<TItem>
{
    /// <summary>
    /// Уникальный идентификатор колонки.
    /// Используется для хранения ширины, состояния сортировки и фильтров.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Заголовок колонки.</summary>
    public string Title { get; set; } = "";

    /// <summary>Начальная ширина колонки в пикселях.</summary>
    public double Width { get; set; } = 150;

    /// <summary>Минимальная ширина при ресайзе, в пикселях.</summary>
    public double MinWidth { get; set; } = 50;

    /// <summary>Разрешить изменение ширины пользователем.</summary>
    public bool Resizable { get; set; } = true;

    /// <summary>Разрешить сортировку по этой колонке.</summary>
    public bool Sortable { get; set; } = true;

    /// <summary>Дополнительный CSS-класс для ячейки &lt;td&gt;.</summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Значение ячейки.
    /// Используется для вывода по умолчанию, для сортировки
    /// (если не задан <see cref="SortKey"/>) и как fallback для фильтра.
    /// </summary>
    public Func<TItem, IComparable?>? Value { get; set; }

    /// <summary>
    /// Отдельный ключ сортировки, если он отличается от <see cref="Value"/>.
    /// </summary>
    public Func<TItem, IComparable?>? SortKey { get; set; }

    /// <summary>
    /// Строка, по которой колонка участвует в строке быстрого поиска
    /// и в операторах сравнения строк.
    /// Если не задана — берётся <c>Value(item)?.ToString()</c>.
    /// </summary>
    public Func<TItem, string?>? FilterText { get; set; }

    /// <summary>Кастомный рендер содержимого ячейки.</summary>
    public RenderFragment<TItem>? Template { get; set; }

    /// <summary>
    /// Тип данных колонки. Влияет на список доступных операторов фильтра
    /// и на вид поля ввода значения.
    /// </summary>
    public FilterKind FilterKind { get; set; } = FilterKind.Text;
}