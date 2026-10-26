using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Globalization;
using System.Text;

namespace BlazorWeb.Components.Shared.DataGrid;

public partial class DataGrid<TItem> : IAsyncDisposable
{
    #region Параметры

    [Parameter, EditorRequired] public IReadOnlyList<TItem> Items { get; set; } = Array.Empty<TItem>();
    [Parameter, EditorRequired] public IReadOnlyList<DataGridColumn<TItem>> Columns { get; set; } = Array.Empty<DataGridColumn<TItem>>();
    [Parameter] public IReadOnlyList<DataGridAction<TItem>> Actions { get; set; } = Array.Empty<DataGridAction<TItem>>();
    [Parameter] public DataGridSelectionMode SelectionMode { get; set; } = DataGridSelectionMode.Multiple;
    [Parameter] public EventCallback<IReadOnlyList<TItem>> SelectionChanged { get; set; }

    [Parameter] public bool ShowFilter { get; set; } = true;
    [Parameter] public string FilterPlaceholder { get; set; } = "🔍 Поиск...";
    [Parameter] public Func<TItem, string, bool>? FilterPredicate { get; set; }

    [Parameter] public bool ShowStatus { get; set; } = false;
    [Parameter] public string? StatusMessage { get; set; }
    [Parameter] public bool ShowSelectionInfo { get; set; } = true;
    [Parameter] public bool ShowClearSelection { get; set; } = true;
    [Parameter] public bool ShowResetSort { get; set; } = true;
    [Parameter] public bool ShowResetColumnWidths { get; set; } = true;
    [Parameter] public bool ShowFilterMenu { get; set; } = true;
    [Parameter] public bool ShowExport { get; set; } = true;
    [Parameter] public string ExportFileName { get; set; } = "export";

    [Parameter]
    public string? Hint { get; set; } =
        "Ctrl+клик или Shift+клик — добавить к выделению · Shift+клик по заголовку — выделить всё · " +
        "клик по заголовку — сортировка · двойной клик — открыть · правый клик — меню";

    [Parameter] public EventCallback<TItem> OnRowDoubleClick { get; set; }
    [Parameter] public EventCallback<TItem> OnRowClick { get; set; }

    [Parameter] public int PageSize { get; set; } = 50;
    [Parameter] public bool ShowPagination { get; set; } = true;
    [Parameter] public bool ShowPageSizeSelector { get; set; } = true;
    [Parameter] public int[] PageSizeOptions { get; set; } = { 10, 25, 50, 100, 200 };

    #endregion

    #region Состояние

    private ElementReference tableRef;
    private IJSObjectReference? resizeModule;
    private DotNetObjectReference<DataGrid<TItem>>? selfRef;

    private readonly List<TItem> filteredItems = new();
    private readonly List<TItem> _selected = new();
    private readonly Dictionary<string, double> _columnWidths = new();

    private TItem lastSelected = default!;
    private bool hasLastSelected;

    private string filter = "";
    private readonly List<(string ColId, bool Ascending)> sortCriteria = new();

    private bool contextMenuVisible;
    private double contextMenuX;
    private double contextMenuY;
    private int _lastPageSize;

    private readonly List<FilterClause> filterClauses = new();
    private bool filterMenuVisible;
    private FilterClause editingClause = new();
    private int editingIndex = -1;

    private IReadOnlyList<DataGridAction<TItem>> toolbarActions = Array.Empty<DataGridAction<TItem>>();
    private IReadOnlyList<DataGridAction<TItem>> contextMenuActions = Array.Empty<DataGridAction<TItem>>();

    private bool operatorMenuOpen;
    private bool hintOpen;
    private bool settingsMenuOpen;

    public IReadOnlyList<TItem> SelectedItems => _selected;

    #endregion

    #region Жизненный цикл

    protected override void OnParametersSet()
    {
        toolbarActions = Actions.Where(a => a.ShowInToolbar).ToList();
        contextMenuActions = Actions.Where(a => a.ShowInContextMenu).ToList();

        if (PageSize != _lastPageSize)
        {
            currentPage = 1;
            _lastPageSize = PageSize;
        }
        RefreshFiltered();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        try
        {
            selfRef = DotNetObjectReference.Create(this);
            resizeModule = await JS.InvokeAsync<IJSObjectReference>("import", "./Shared/DataGrid/DataGrid.razor.js");
            var minWidths = Columns.Select(c => c.MinWidth).ToArray();
            await resizeModule.InvokeVoidAsync("initColumnResize", tableRef, selfRef, minWidths);
        }
        catch (Exception ex) { Console.WriteLine($"[DataGrid] JS init failed: {ex.Message}"); }
    }

    public async ValueTask DisposeAsync()
    {
        try { if (resizeModule is not null) await resizeModule.DisposeAsync(); } catch (JSDisconnectedException) { }
        selfRef?.Dispose();
    }

    #endregion

    #region Ширины колонок

    private double WidthFor(DataGridColumn<TItem> col) => _columnWidths.TryGetValue(col.Id, out var w) ? w : col.Width;
    private void ResetColumnWidths() { _columnWidths.Clear(); settingsMenuOpen = false; StateHasChanged(); }

    [JSInvokable]
    public Task OnColumnResizeEnd(int colIndex, double width)
    {
        if (colIndex < 0 || colIndex >= Columns.Count) return Task.CompletedTask;
        var col = Columns[colIndex];
        _columnWidths[col.Id] = Math.Max(col.MinWidth, width);
        StateHasChanged();
        return Task.CompletedTask;
    }

    #endregion

    #region Фильтрация и сортировка

    private void RefreshFiltered()
    {
        IEnumerable<TItem> result = Items;
        if (!string.IsNullOrWhiteSpace(filter) || filterClauses.Count > 0)
            result = result.Where(MatchesFilter);
        result = ApplySort(result);
        filteredItems.Clear();
        filteredItems.AddRange(result);
        if (currentPage > TotalPages) currentPage = TotalPages;
        if (currentPage < 1) currentPage = 1;
        RebuildPage();
    }

    private bool MatchesFilter(TItem item)
    {
        if (FilterPredicate is not null && !string.IsNullOrWhiteSpace(filter))
        {
            if (!FilterPredicate(item, filter)) return false;
        }
        else if (!string.IsNullOrWhiteSpace(filter) && !TextSearchMatches(item)) return false;

        foreach (var clause in filterClauses)
            if (!EvalClause(item, clause)) return false;

        return true;
    }

    private bool TextSearchMatches(TItem item)
    {
        foreach (var col in Columns)
        {
            var text = col.FilterText?.Invoke(item) ?? col.Value?.Invoke(item)?.ToString();
            if (!string.IsNullOrEmpty(text) && text.Contains(filter, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private bool EvalClause(TItem item, FilterClause c)
    {
        var col = Columns.FirstOrDefault(x => x.Id == c.ColumnId);
        if (col is null) return true;
        object? raw = col.Value?.Invoke(item);
        string text = col.FilterText?.Invoke(item) ?? raw?.ToString() ?? "";

        switch (c.Operator)
        {
            case FilterOperator.Contains: return text.Contains(c.Value ?? "", StringComparison.OrdinalIgnoreCase);
            case FilterOperator.NotContains: return !text.Contains(c.Value ?? "", StringComparison.OrdinalIgnoreCase);
            case FilterOperator.StartsWith: return text.StartsWith(c.Value ?? "", StringComparison.OrdinalIgnoreCase);
            case FilterOperator.NotStartsWith: return !text.StartsWith(c.Value ?? "", StringComparison.OrdinalIgnoreCase);
            case FilterOperator.EndsWith: return text.EndsWith(c.Value ?? "", StringComparison.OrdinalIgnoreCase);
            case FilterOperator.NotEndsWith: return !text.EndsWith(c.Value ?? "", StringComparison.OrdinalIgnoreCase);
            case FilterOperator.In: return SplitList(c.Value).Any(v => string.Equals(text, v, StringComparison.OrdinalIgnoreCase));
            case FilterOperator.NotIn: return !SplitList(c.Value).Any(v => string.Equals(text, v, StringComparison.OrdinalIgnoreCase));
            case FilterOperator.Equals: return Compare(raw, c.Value, col.FilterKind) == 0;
            case FilterOperator.NotEquals: return Compare(raw, c.Value, col.FilterKind) != 0;
            case FilterOperator.Greater: return Compare(raw, c.Value, col.FilterKind) > 0;
            case FilterOperator.GreaterOrEqual: return Compare(raw, c.Value, col.FilterKind) >= 0;
            case FilterOperator.Less: return Compare(raw, c.Value, col.FilterKind) < 0;
            case FilterOperator.LessOrEqual: return Compare(raw, c.Value, col.FilterKind) <= 0;
            case FilterOperator.Between: return Compare(raw, c.Value, col.FilterKind) >= 0 && Compare(raw, c.Value2, col.FilterKind) <= 0;
            case FilterOperator.IsEmpty: return string.IsNullOrWhiteSpace(text);
            case FilterOperator.IsNotEmpty: return !string.IsNullOrWhiteSpace(text);
            case FilterOperator.IsTrue: return raw is bool tb && tb;
            case FilterOperator.IsFalse: return raw is bool fb && !fb;
        }
        return true;
    }

    private static IEnumerable<string> SplitList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
        return raw.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0);
    }

    private static int Compare(object? a, string? b, FilterKind kind)
    {
        if (a is null || string.IsNullOrEmpty(b)) return 0;
        try
        {
            switch (kind)
            {
                case FilterKind.Number:
                    if (double.TryParse(b, NumberStyles.Any, CultureInfo.InvariantCulture, out var nd))
                        return Convert.ToDouble(a, CultureInfo.InvariantCulture).CompareTo(nd);
                    break;
                case FilterKind.Date:
                    if (DateTime.TryParse(b, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                        return Convert.ToDateTime(a, CultureInfo.InvariantCulture).CompareTo(dt);
                    break;
                case FilterKind.Bool:
                    if (bool.TryParse(b, out var bv)) return Convert.ToBoolean(a).CompareTo(bv);
                    break;
            }
        }
        catch { }
        return string.Compare(a.ToString(), b, StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<TItem> ApplySort(IEnumerable<TItem> source)
    {
        IOrderedEnumerable<TItem>? ordered = null;
        foreach (var (colId, asc) in sortCriteria)
        {
            var col = Columns.FirstOrDefault(c => c.Id == colId);
            var key = col?.SortKey ?? col?.Value;
            if (key is null) continue;
            ordered = ordered is null
                ? (asc ? source.OrderBy(key) : source.OrderByDescending(key))
                : (asc ? ordered.ThenBy(key) : ordered.ThenByDescending(key));
        }
        return ordered ?? source;
    }

    private void SortBy(DataGridColumn<TItem> col)
    {
        var idx = sortCriteria.FindIndex(s => s.ColId == col.Id);
        if (idx < 0) sortCriteria.Add((col.Id, true));
        else if (sortCriteria[idx].Ascending) sortCriteria[idx] = (col.Id, false);
        else sortCriteria.RemoveAt(idx);

        currentPage = 1;
        RefreshFiltered();
    }

    private void ResetSort() { sortCriteria.Clear(); currentPage = 1; settingsMenuOpen = false; RefreshFiltered(); }

    private string SortIcon(DataGridColumn<TItem> col)
    {
        var idx = sortCriteria.FindIndex(s => s.ColId == col.Id);
        if (idx < 0) return "";
        var arrow = sortCriteria[idx].Ascending ? "▲" : "▼";
        return sortCriteria.Count == 1 ? arrow : $"{arrow}{idx + 1}";
    }

    private void OnFilterChanged(ChangeEventArgs e) { filter = e.Value?.ToString() ?? ""; currentPage = 1; RefreshFiltered(); }

    #endregion

    #region Выделение

    private bool IsSelected(TItem item) => _selected.Contains(item);
    private bool IsAllSelected => filteredItems.Count > 0 && filteredItems.All(i => _selected.Contains(i));
    private async Task NotifySelectionChangedAsync() => await SelectionChanged.InvokeAsync(_selected.ToList());

    private async Task OnHeaderCheckboxClick(MouseEventArgs e)
    {
        if (e.ShiftKey)
        {
            // Shift+Click по заголовку = выбрать ВСЁ (даже то, что не на странице)
            if (IsAllSelected) _selected.Clear();
            else
            {
                _selected.Clear();
                _selected.AddRange(filteredItems);
            }
        }
        else
        {
            // Обычный клик = выбрать только текущую страницу
            if (pageItems.All(i => _selected.Contains(i)))
                foreach (var p in pageItems) _selected.Remove(p);
            else
                foreach (var p in pageItems) if (!_selected.Contains(p)) _selected.Add(p);
        }
        await NotifySelectionChangedAsync();
    }

    private async Task ToggleItemWithShift(TItem item, MouseEventArgs e)
    {
        if (e.ShiftKey && hasLastSelected)
        {
            var lastIndex = filteredItems.IndexOf(lastSelected);
            var currentIndex = filteredItems.IndexOf(item);
            if (lastIndex >= 0 && currentIndex >= 0)
            {
                var start = Math.Min(lastIndex, currentIndex);
                var end = Math.Max(lastIndex, currentIndex);
                for (int i = start; i <= end; i++)
                {
                    if (!_selected.Contains(filteredItems[i])) _selected.Add(filteredItems[i]);
                }
                await NotifySelectionChangedAsync();
                return;
            }
        }

        if (!_selected.Remove(item)) _selected.Add(item);
        lastSelected = item;
        hasLastSelected = true;
        await NotifySelectionChangedAsync();
    }

    private async Task ClearSelection()
    {
        _selected.Clear();
        lastSelected = default!;
        hasLastSelected = false;
        CloseContextMenu();
        await NotifySelectionChangedAsync();
    }

    private async Task HandleRowClick(TItem item, int index, MouseEventArgs e)
    {
        if (e.ShiftKey && hasLastSelected)
        {
            var lastIndex = filteredItems.IndexOf(lastSelected);
            if (lastIndex >= 0)
            {
                var start = Math.Min(lastIndex, index);
                var end = Math.Max(lastIndex, index);
                if (!e.CtrlKey && !e.MetaKey) _selected.Clear();
                for (int i = start; i <= end; i++)
                    if (!_selected.Contains(filteredItems[i])) _selected.Add(filteredItems[i]);
                await NotifySelectionChangedAsync();
                return;
            }
        }

        if (SelectionMode == DataGridSelectionMode.Single) { _selected.Clear(); _selected.Add(item); }
        else if (e.CtrlKey || e.MetaKey) { if (!_selected.Remove(item)) _selected.Add(item); }
        else { _selected.Clear(); _selected.Add(item); }

        lastSelected = item;
        hasLastSelected = true;
        await NotifySelectionChangedAsync();
        await OnRowClick.InvokeAsync(item);
    }

    private async Task HandleRowDoubleClick(TItem item) => await OnRowDoubleClick.InvokeAsync(item);

    #endregion

    #region Действия

    private bool CanExecute(DataGridAction<TItem> action)
    {
        if (action.CanExecute is not null) return action.CanExecute(_selected);
        return _selected.Count >= action.MinSelection;
    }

    private async Task ExecuteActionAsync(DataGridAction<TItem> action)
    {
        CloseContextMenu();
        if (!CanExecute(action)) return;
        if (action.Handler is not null) await action.Handler(_selected.ToList());
    }

    private bool HasUtilityActions => ShowResetSort || ShowResetColumnWidths;

    #endregion

    #region Контекстное меню и настройки

    private void OnRowContextMenu(TItem item, MouseEventArgs e)
    {
        if (!_selected.Contains(item))
        {
            _selected.Clear(); _selected.Add(item);
            lastSelected = item; hasLastSelected = true;
            _ = NotifySelectionChangedAsync();
        }
        contextMenuX = e.ClientX; contextMenuY = e.ClientY;
        contextMenuVisible = true;
    }

    private void CloseContextMenu() { contextMenuVisible = false; hintOpen = false; settingsMenuOpen = false; }
    private void ToggleSettingsMenu() => settingsMenuOpen = !settingsMenuOpen;
    private void ToggleHint() => hintOpen = !hintOpen;

    #endregion

    #region Экспорт

    private async Task ExportToCsv()
    {
        var itemsToExport = _selected.Count > 0 ? _selected.ToList() : filteredItems.ToList();
        if (itemsToExport.Count == 0) return;

        var sb = new StringBuilder();

        var headers = Columns.Select(c => $"\"{c.Title.Replace("\"", "\"\"")}\"");
        sb.AppendLine(string.Join(";", headers));

        foreach (var item in itemsToExport)
        {
            var cells = Columns.Select(c =>
            {
                var text = c.FilterText?.Invoke(item) ?? c.Value?.Invoke(item)?.ToString() ?? "";
                return $"\"{text.Replace("\"", "\"\"")}\"";
            });
            sb.AppendLine(string.Join(";", cells));
        }

        var filename = $"{ExportFileName}_{DateTime.Now:yyyyMMdd_HHmm}.csv";

        if (resizeModule != null)
        {
            await resizeModule.InvokeVoidAsync("downloadFile", filename, sb.ToString());
        }
    }

    #endregion

    #region Клавиатура

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            if (hintOpen) { hintOpen = false; return; }
            if (settingsMenuOpen) { settingsMenuOpen = false; return; }
            if (contextMenuVisible) CloseContextMenu();
            else if (filterMenuVisible) CloseFilterMenu();
            else _ = ClearSelection();
        }
        if (e.Key == "a" && e.CtrlKey && SelectionMode == DataGridSelectionMode.Multiple)
        {
            foreach (var p in filteredItems) if (!_selected.Contains(p)) _selected.Add(p);
            _ = NotifySelectionChangedAsync();
        }
    }

    #endregion

    #region Пагинация

    private int currentPage = 1;
    private readonly List<TItem> pageItems = new();
    private int TotalPages => Math.Max(1, (filteredItems.Count + PageSize - 1) / PageSize);

    private void RebuildPage()
    {
        var skip = (currentPage - 1) * PageSize;
        pageItems.Clear();
        pageItems.AddRange(filteredItems.Skip(skip).Take(PageSize));
    }

    private IEnumerable<int> VisiblePages()
    {
        const int window = 5;
        int start = Math.Max(1, currentPage - window / 2);
        int end = Math.Min(TotalPages, start + window - 1);
        start = Math.Max(1, end - window + 1);
        for (int i = start; i <= end; i++) yield return i;
    }

    private void GoToPage(int page)
    {
        var clamped = Math.Clamp(page, 1, TotalPages);
        if (clamped == currentPage) return;
        currentPage = clamped;
        RebuildPage();
        StateHasChanged();
    }

    private void FirstPage() => GoToPage(1);
    private void PrevPage() => GoToPage(currentPage - 1);
    private void NextPage() => GoToPage(currentPage + 1);
    private void LastPage() => GoToPage(TotalPages);

    private void OnPageSizeChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var size) && size > 0)
        {
            PageSize = size; currentPage = 1; RebuildPage(); StateHasChanged();
        }
    }

    #endregion

    #region Панель фильтров

    private void OpenNewFilter()
    {
        editingIndex = -1;
        editingClause = new FilterClause { ColumnId = Columns.FirstOrDefault()?.Id ?? "", Operator = FilterOperator.Contains };
        NormalizeOperatorForColumn(); operatorMenuOpen = false; filterMenuVisible = true;
    }
    private void EditFilter(int index)
    {
        if (index < 0 || index >= filterClauses.Count) return;
        editingIndex = index; editingClause = filterClauses[index].Clone(); operatorMenuOpen = false; filterMenuVisible = true;
    }
    private void CloseFilterMenu() { operatorMenuOpen = false; filterMenuVisible = false; }
    private void RemoveFilter(FilterClause c) { filterClauses.Remove(c); currentPage = 1; RefreshFiltered(); }
    private void ClearFilters() { filterClauses.Clear(); currentPage = 1; RefreshFiltered(); }
    private void ApplyFilter()
    {
        if (string.IsNullOrEmpty(editingClause.ColumnId)) { CloseFilterMenu(); return; }
        if (!IsValueless(editingClause.Operator) && string.IsNullOrWhiteSpace(editingClause.Value) && editingClause.Operator != FilterOperator.Between) { CloseFilterMenu(); return; }
        if (editingIndex >= 0 && editingIndex < filterClauses.Count) filterClauses[editingIndex] = editingClause.Clone();
        else filterClauses.Add(editingClause.Clone());
        CloseFilterMenu(); currentPage = 1; RefreshFiltered(); StateHasChanged();
    }
    private void OnColumnChanged(ChangeEventArgs e) { editingClause.ColumnId = e.Value?.ToString() ?? ""; NormalizeOperatorForColumn(); }
    private void SelectOperator(FilterOperator op) { editingClause.Operator = op; if (IsValueless(op)) { editingClause.Value = null; editingClause.Value2 = null; } if (op != FilterOperator.Between) editingClause.Value2 = null; operatorMenuOpen = false; }
    private void ToggleOperatorMenu() => operatorMenuOpen = !operatorMenuOpen;

    #endregion

    #region Вспомогательные методы фильтра

    private DataGridColumn<TItem>? CurrentEditingColumn() => Columns.FirstOrDefault(c => c.Id == editingClause.ColumnId);

    private void NormalizeOperatorForColumn()
    {
        var col = CurrentEditingColumn(); if (col is null) return;
        var ops = OpsFor(col.FilterKind);
        if (!ops.Contains(editingClause.Operator)) { editingClause.Operator = ops[0]; editingClause.Value = null; editingClause.Value2 = null; }
    }

    private IEnumerable<FilterOperator> AvailableOperators() { var col = CurrentEditingColumn(); return col is null ? Array.Empty<FilterOperator>() : OpsFor(col.FilterKind); }

    private static FilterOperator[] OpsFor(FilterKind kind) => kind switch
    {
        FilterKind.Text => new[]
        {
            FilterOperator.Contains, FilterOperator.NotContains, FilterOperator.StartsWith, FilterOperator.NotStartsWith,
            FilterOperator.EndsWith, FilterOperator.NotEndsWith, FilterOperator.In, FilterOperator.NotIn,
            FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.Greater, FilterOperator.GreaterOrEqual,
            FilterOperator.Less, FilterOperator.LessOrEqual, FilterOperator.IsEmpty, FilterOperator.IsNotEmpty
        },
        FilterKind.Number => new[]
        {
            FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.Greater, FilterOperator.GreaterOrEqual,
            FilterOperator.Less, FilterOperator.LessOrEqual, FilterOperator.IsEmpty, FilterOperator.IsNotEmpty
        },
        FilterKind.Date => new[]
        {
            FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.Greater, FilterOperator.GreaterOrEqual,
            FilterOperator.Less, FilterOperator.LessOrEqual, FilterOperator.IsEmpty, FilterOperator.IsNotEmpty
        },
        FilterKind.Bool => new[] { FilterOperator.IsTrue, FilterOperator.IsFalse },
        _ => new[] { FilterOperator.Contains }
    };

    private static bool IsValueless(FilterOperator op) =>
        op is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty or FilterOperator.IsTrue or FilterOperator.IsFalse;

    private string OperatorMenuLabel(FilterOperator op) => op switch
    {
        FilterOperator.Equals => "= равно",
        FilterOperator.NotEquals => "≠ не равно",
        FilterOperator.Greater => "> больше",
        FilterOperator.GreaterOrEqual => "≥ больше или равно",
        FilterOperator.Less => "< меньше",
        FilterOperator.LessOrEqual => "≤ меньше или равно",
        FilterOperator.Between => "↔ между",
        FilterOperator.IsTrue => "✓ да",
        FilterOperator.IsFalse => "✗ нет",
        FilterOperator.Contains => "содержит",
        FilterOperator.NotContains => "не содержит",
        FilterOperator.StartsWith => "начинается с",
        FilterOperator.NotStartsWith => "не начинается с",
        FilterOperator.EndsWith => "заканчивается на",
        FilterOperator.NotEndsWith => "не заканчивается на",
        FilterOperator.In => "в списке",
        FilterOperator.NotIn => "не в списке",
        FilterOperator.IsEmpty => "пусто",
        FilterOperator.IsNotEmpty => "не пусто",
        _ => op.ToString()
    };

    private string FormatChip(FilterClause c)
    {
        var col = Columns.FirstOrDefault(x => x.Id == c.ColumnId);
        var title = col?.Title ?? "?";
        if (IsValueless(c.Operator)) return $"{title} {OperatorMenuLabel(c.Operator)}";
        var val = col?.FilterKind == FilterKind.Bool ? (c.Value == "true" ? "да" : "нет") : $"«{c.Value}»";
        if (col?.FilterKind is FilterKind.Number or FilterKind.Date) val = c.Value ?? "";
        return $"{title} {OperatorMenuLabel(c.Operator)} {val}";
    }

    private string? InputPlaceholder()
    {
        var col = CurrentEditingColumn(); if (col is null) return null;
        if (col.FilterKind == FilterKind.Date) return null;
        if (editingClause.Operator is FilterOperator.In or FilterOperator.NotIn) return "значения через запятую";
        var sample = Items.Select(i => col.FilterText?.Invoke(i) ?? col.Value?.Invoke(i)?.ToString()).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        if (!string.IsNullOrWhiteSpace(sample)) { if (sample.Length > 40) sample = sample[..40] + "…"; return $"например: {sample}"; }
        return $"значение для «{col.Title}»";
    }

    private string InputType() => CurrentEditingColumn()?.FilterKind switch { FilterKind.Number => "number", FilterKind.Date => "date", _ => "text" };

    #endregion

    #region Подсказка

    private string[] HintLines()
    {
        if (string.IsNullOrWhiteSpace(Hint)) return Array.Empty<string>();
        return Hint.Split('·', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
    }

    #endregion
}