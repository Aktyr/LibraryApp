namespace LibApp.Core.Policies;

public static class DiscardPolicy
{
    public const int MaxApprovedByNameLength = 100;

    public static List<string> ValidateDiscard(
        int amount,
        int bookCount,
        int availableCount,
        decimal? compensation,
        DiscardReason? reason = null)
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

        return errors;
    }
}