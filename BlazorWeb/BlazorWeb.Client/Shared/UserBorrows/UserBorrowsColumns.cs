using BlazorWeb.Components.Shared.DataGrid;
using LibApp.Core.DTO.Borrowing;

namespace BlazorWeb.Client.Shared.UserBorrows;

/// <summary>
/// Фабрики колонок для таблиц выдач. Отличия MyProfile/UserProfile — только
/// в наборе колонок: библиотекарю нужны «Выдана», «Штраф», «Продлено»,
/// читателю — компактный набор.
/// </summary>
public static class UserBorrowsColumns
{
    /// <param name="showBorrowDate">Добавить колонку «Выдана».</param>
    /// <param name="showExtendedDays">Добавить колонку «Продлено» (только библиотекарю).</param>
    /// <param name="penaltyDisplay">Функция расчёта штрафа для отображения. null — без колонки «Штраф».</param>
    /// <param name="maxExtendDeadlineDays">Лимит продления для цветовой индикации колонки «Продлено».</param>
    public static List<DataGridColumn<BorrowedBookDTO>> CreateActive(
        bool showBorrowDate = false,
        bool showExtendedDays = false,
        Func<BorrowedBookDTO, decimal>? penaltyDisplay = null,
        int maxExtendDeadlineDays = 14)
    {
        var columns = new List<DataGridColumn<BorrowedBookDTO>>
        {
            new() { Title = "Книга",   Value = b => b.BookTitle, Width = showExtendedDays ? 240 : 250 },
            new() { Title = "Автор",   Value = b => b.BookAuthor, Width = 180 },
            new() { Title = "Комната", Value = b => b.RoomName,  Width = showBorrowDate ? 120 : 130 }
        };

        if (showBorrowDate)
        {
            columns.Add(new()
            {
                Title = "Выдана",
                Width = 100,
                FilterKind = FilterKind.Date,
                Value = b => b.Borrow,
                FilterText = b => b.Borrow.ToString("yyyy-MM-dd"),
                Template = item => b => b.AddContent(0, item.Borrow.ToString("dd.MM.yyyy"))
            });
        }

        columns.Add(new()
        {
            Title = "Срок",
            Width = 150,
            FilterKind = FilterKind.Date,
            Value = b => b.Deadline,
            FilterText = b => b.Deadline?.ToString("yyyy-MM-dd") ?? "",
            Template = item => b =>
            {
                if (!item.Deadline.HasValue) { b.AddContent(0, "—"); return; }
                if (item.Deadline.Value < DateTime.UtcNow)
                {
                    var daysOverdue = (int)Math.Ceiling((DateTime.UtcNow - item.Deadline.Value).TotalDays);
                    b.OpenElement(0, "span");
                    b.AddAttribute(1, "style", "color:#a30000;font-weight:600;");
                    b.AddAttribute(2, "title", $"Просрочено на {daysOverdue} дн.");
                    b.AddContent(3, $"{item.Deadline.Value:dd.MM.yyyy} ⚠");
                    b.CloseElement();
                }
                else
                {
                    b.AddContent(0, item.Deadline.Value.ToString("dd.MM.yyyy"));
                }
            }
        });

        if (penaltyDisplay is not null)
        {
            columns.Add(new()
            {
                Title = "Штраф",
                Width = 120,
                FilterKind = FilterKind.Number,
                Value = b => penaltyDisplay(b),
                FilterText = b => penaltyDisplay(b).ToString("0.##"),
                Template = item => b =>
                {
                    var amount = penaltyDisplay(item);
                    if (amount <= 0) { b.AddContent(0, "—"); return; }
                    b.OpenElement(0, "span");
                    b.AddAttribute(1, "style", "color:#a30000;font-weight:600;");
                    b.AddContent(2, $"{amount:0.##} ₽");
                    b.CloseElement();
                }
            });
        }

        if (showExtendedDays)
        {
            columns.Add(new()
            {
                Title = "Продлено",
                Width = 130,
                FilterKind = FilterKind.Number,
                Value = b => b.TotalExtendedDays,
                FilterText = b => b.TotalExtendedDays.ToString(),
                Template = item => b =>
                {
                    var ext = item.TotalExtendedDays;
                    var color = ext >= maxExtendDeadlineDays ? "#a30000"
                              : ext > 0 ? "#a85b00"
                              : "#555";
                    b.OpenElement(0, "span");
                    b.AddAttribute(1, "style", $"color:{color};");
                    b.AddContent(2, $"{ext} / {maxExtendDeadlineDays} дн.");
                    b.CloseElement();
                }
            });
        }

        return columns;
    }

    /// <summary>Колонки для истории возвратов — одинаковый набор в обоих профилях.</summary>
    public static List<DataGridColumn<BorrowedBookDTO>> CreateHistory() => new()
    {
        new() { Title = "Книга",   Value = b => b.BookTitle, Width = 240 },
        new() { Title = "Комната", Value = b => b.RoomName,  Width = 130 },
        new()
        {
            Title = "Выдана",
            Width = 100,
            FilterKind = FilterKind.Date,
            Value = b => b.Borrow,
            FilterText = b => b.Borrow.ToString("yyyy-MM-dd"),
            Template = item => b => b.AddContent(0, item.Borrow.ToString("dd.MM.yyyy"))
        },
        new()
        {
            Title = "Срок",
            Width = 110,
            FilterKind = FilterKind.Date,
            Value = b => b.Deadline,
            FilterText = b => b.Deadline?.ToString("yyyy-MM-dd") ?? "",
            Template = item => b => b.AddContent(0, item.Deadline?.ToString("dd.MM.yyyy") ?? "—")
        },
        new()
        {
            Title = "Возвращена",
            Width = 120,
            FilterKind = FilterKind.Date,
            Value = b => b.ReturnDate,
            FilterText = b => b.ReturnDate?.ToString("yyyy-MM-dd") ?? "",
            Template = item => b => b.AddContent(0, item.ReturnDate?.ToString("dd.MM.yyyy") ?? "—")
        },
        new()
        {
            Title = "Штраф",
            Width = 100,
            FilterKind = FilterKind.Number,
            Value = b => b.Penalty,
            FilterText = b => b.Penalty?.ToString("0.##") ?? "",
            Template = item => b =>
            {
                if (item.Penalty is > 0)
                {
                    b.OpenElement(0, "span");
                    b.AddAttribute(1, "style", "color:#a30000;font-weight:600;");
                    b.AddContent(2, $"{item.Penalty.Value:0.##} ₽");
                    b.CloseElement();
                }
                else
                {
                    b.AddContent(0, "—");
                }
            }
        }
    };
}