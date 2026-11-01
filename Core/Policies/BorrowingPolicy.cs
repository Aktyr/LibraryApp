namespace LibApp.Core.Policies;

public static class BorrowingPolicy
{
    public static List<string> ValidateBorrow(int borrowDays, int minDays, int maxDays)
    {
        var errors = new List<string>();
        if (borrowDays < minDays || borrowDays > maxDays)
            errors.Add($"Срок выдачи должен быть от {minDays} до {maxDays} дней");
        return errors;
    }

    public static List<string> ValidateExtend(int currentExtendedDays, int extraDays, int maxExtendDays)
    {
        var errors = new List<string>();
        if (extraDays <= 0)
            errors.Add("Срок продления должен быть больше 0");
        else if (currentExtendedDays + extraDays > maxExtendDays)
        {
            var available = maxExtendDays - currentExtendedDays;
            errors.Add($"Суммарное продление не может превышать {maxExtendDays} дней. " +
                       $"Уже продлено: {currentExtendedDays}, доступно: {available}.");
        }
        return errors;
    }
}