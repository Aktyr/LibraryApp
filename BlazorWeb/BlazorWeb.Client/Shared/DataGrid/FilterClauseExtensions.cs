using System.Globalization;

namespace BlazorWeb.Components.Shared.DataGrid;

/// <summary>Разбор чип-фильтров DataGrid для серверных запросов.</summary>
public static class FilterClauseExtensions
{
    /// <summary>
    /// Извлекает диапазон дат из условия по колонке периода.
    /// Значения date-инпута приходят в формате yyyy-MM-dd.
    /// </summary>
    public static (DateTime? From, DateTime? To) GetDateRange(this IReadOnlyList<FilterClause> clauses, string columnId)
    {
        var clause = clauses.FirstOrDefault(c => c.ColumnId == columnId);
        if (clause is null) return (null, null);

        DateTime? Parse(string? value) =>
            DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date) ? date : null;

        return clause.Operator switch
        {
            FilterOperator.Between => (Parse(clause.Value), Parse(clause.Value2)),
            FilterOperator.Greater or FilterOperator.GreaterOrEqual => (Parse(clause.Value), null),
            FilterOperator.Less or FilterOperator.LessOrEqual => (null, Parse(clause.Value)),
            FilterOperator.Equals => (Parse(clause.Value), Parse(clause.Value)),
            _ => (null, null)
        };
    }
}
