using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorWeb.Client.Shared.CharCounter;

/// <summary>
/// InputText с привязкой на oninput вместо onchange. Нужен для живого счётчика символов.
/// Поведение InputBase сохранено: EditContext, ValidationMessage, CSS-классы valid/invalid.
/// </summary>
public class InputTextLive : InputBase<string>
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "value", CurrentValueAsString);
        builder.AddAttribute(3, "oninput",
            EventCallback.Factory.CreateBinder<string?>(
                this, __value => CurrentValueAsString = __value, CurrentValueAsString));
        builder.CloseElement();
    }

    /// <summary>
    /// InputBase требует явной реализации парсинга. Для string парсинг тривиален —
    /// значение уже строка, ошибок быть не может. validationErrorMessage при return true
    /// не читается (см. InputBase.SetCurrentValueAsString), поэтому string.Empty.
    /// </summary>
    protected override bool TryParseValueFromString(
        string? value,
        out string result,
        out string validationErrorMessage)
    {
        result = value ?? string.Empty;
        validationErrorMessage = string.Empty;
        return true;
    }
}