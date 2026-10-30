namespace LibApp.Core.Enums;

public static class DiscardReasonExtensions
{
    /// <summary>
    /// Человекочитаемое название причины списания — для бейджей, пунктов select
    /// и текстов ответов API. Единственное место, где формулировки задаются.
    /// </summary>
    public static string DisplayName(this DiscardReason reason) => reason switch
    {
        DiscardReason.Wear => "Износ",
        DiscardReason.Damaged => "Повреждение",
        DiscardReason.Loss => "Утрата",
        DiscardReason.Obsolete => "Устарела",
        _ => "Другое"
    };
}