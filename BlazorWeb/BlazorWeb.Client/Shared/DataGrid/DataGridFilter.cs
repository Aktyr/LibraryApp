namespace BlazorWeb.Components.Shared.DataGrid;

#region FilterKind

/// <summary>
/// Тип данных колонки. Влияет на список доступных операторов фильтра
/// и на вид поля ввода значения.
/// </summary>
public enum FilterKind
{
    /// <summary>Строка. Полный набор текстовых и логических операторов.</summary>
    Text,

    /// <summary>Число. Числовые операторы сравнения.</summary>
    Number,

    /// <summary>Дата. Операторы сравнения с нативным datepicker.</summary>
    Date,

    /// <summary>Булево значение. Только операторы «да» / «нет».</summary>
    Bool
}

#endregion

#region FilterOperator

/// <summary>
/// Оператор условия фильтра.
/// Набор доступных операторов зависит от <see cref="FilterKind"/> колонки.
/// </summary>
public enum FilterOperator
{
    #region Текстовые операторы

    /// <summary>Содержит подстроку.</summary>
    Contains,

    /// <summary>Не содержит подстроку.</summary>
    NotContains,

    /// <summary>Начинается с.</summary>
    StartsWith,

    /// <summary>Не начинается с.</summary>
    NotStartsWith,

    /// <summary>Заканчивается на.</summary>
    EndsWith,

    /// <summary>Не заканчивается на.</summary>
    NotEndsWith,

    /// <summary>Значение входит в список (список — через запятую).</summary>
    In,

    /// <summary>Значение не входит в список.</summary>
    NotIn,

    #endregion

    #region Операторы сравнения

    /// <summary>Равно.</summary>
    Equals,

    /// <summary>Не равно.</summary>
    NotEquals,

    /// <summary>Больше.</summary>
    Greater,

    /// <summary>Больше или равно.</summary>
    GreaterOrEqual,

    /// <summary>Меньше.</summary>
    Less,

    /// <summary>Меньше или равно.</summary>
    LessOrEqual,

    /// <summary>В диапазоне (значение «от» — «до»).</summary>
    Between,

    #endregion

    #region Операторы пустоты

    /// <summary>Значение пустое (null, пустая строка или пробелы).</summary>
    IsEmpty,

    /// <summary>Значение непустое.</summary>
    IsNotEmpty,

    #endregion

    #region Булевы операторы

    /// <summary>Значение — истина.</summary>
    IsTrue,

    /// <summary>Значение — ложь.</summary>
    IsFalse

    #endregion
}

#endregion

#region FilterClause

/// <summary>
/// Одно условие фильтрации по конкретной колонке.
/// Набор сохранённых условий объединяется по AND.
/// </summary>
public class FilterClause
{
    /// <summary>Идентификатор колонки (<see cref="DataGridColumn{TItem}.Id"/>).</summary>
    public string ColumnId { get; set; } = "";

    /// <summary>Оператор условия.</summary>
    public FilterOperator Operator { get; set; } = FilterOperator.Contains;

    /// <summary>
    /// Основное значение условия.
    /// Для <see cref="FilterOperator.Between"/> — «от».
    /// </summary>
    public string? Value { get; set; }

    /// <summary>Верхняя граница диапазона (только для <see cref="FilterOperator.Between"/>).</summary>
    public string? Value2 { get; set; }

    /// <summary>
    /// Создаёт независимую копию условия.
    /// Используется, чтобы редактирование черновика не меняло сохранённые фильтры.
    /// </summary>
    public FilterClause Clone() => new()
    {
        ColumnId = ColumnId,
        Operator = Operator,
        Value = Value,
        Value2 = Value2
    };
}

#endregion