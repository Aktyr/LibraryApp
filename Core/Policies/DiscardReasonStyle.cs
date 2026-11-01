namespace LibApp.Core.Policies;

using LibApp.Core.Enums;

public static class DiscardReasonStyle
{
    public static (string Background, string Foreground) Get(DiscardReason reason) => reason switch
    {
        DiscardReason.Wear => ("#fff3d9", "#8a4b00"),
        DiscardReason.Damaged => ("#fde7e9", "#a30000"),
        DiscardReason.Loss => ("#fde7e9", "#a30000"),
        DiscardReason.Obsolete => ("#f3f3f3", "#555"),
        _ => ("#f3f3f3", "#555")
    };
}