namespace LibApp.Core.Policies;

public static class UserLimitStyle
{
    /// <summary>Цвет индикатора количества книг на руках.</summary>
    public static string GetColor(int currentCount, int maxCount) =>
        currentCount >= maxCount ? "#a30000"
        : currentCount >= maxCount - 1 ? "#a85b00"
        : "#333";

    /// <summary>Жирный шрифт, если лимит достигнут.</summary>
    public static string GetFontWeight(int currentCount, int maxCount) =>
        currentCount >= maxCount ? "600" : "400";
}