namespace LibApp.Core.Policies;

public static class DiscardPolicy
{
    public const int MaxApprovedByNameLength = 100;
    public const int MaxReaderNameLength = 300;

    /// <summary>
    /// Проверяет параметры списания. Единая точка для DiscardBookCommand (списание из фонда)
    /// и ReturnBookCommand (списание возвращаемого экземпляра).
    /// </summary>
    /// <param name="availableCount">
    /// Сколько экземпляров доступно к списанию прямо сейчас. В сценарии возврата
    /// вызывающая сторона должна передать значение «как будто книга уже вернулась»:
    /// <c>AvailableCount + 1</c>.
    /// </param>
    /// <param name="approvedBy">ФИО утвердившего; null или пусто — проверка длины пропускается.</param>
    public static List<string> ValidateDiscard(
        int amount,
        int bookCount,
        int availableCount,
        decimal? compensation,
        DiscardReason? reason = null,
        string? approvedBy = null)
    {
        var errors = new List<string>();

        if (amount <= 0)
            errors.Add("Количество списываемых экземпляров должно быть больше нуля");

        if (amount > bookCount)
            errors.Add($"Нельзя списать {amount} экз. Доступно: {bookCount}");

        if (amount > availableCount)
            errors.Add($"Нельзя списать {amount} экз. Выдано: {bookCount - availableCount}, доступно: {availableCount}");

        if (reason == DiscardReason.Loss)
        {
            if (!compensation.HasValue || compensation.Value <= 0)
                errors.Add("При утрате книги необходимо указать сумму компенсации (> 0)");
        }

        if (compensation.HasValue && compensation.Value < 0)
            errors.Add("Сумма компенсации не может быть отрицательной");

        if (!string.IsNullOrEmpty(approvedBy) && approvedBy.Length > MaxApprovedByNameLength)
            errors.Add($"ApprovedBy не может превышать {MaxApprovedByNameLength} символов");

        return errors;
    }
}