using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorWeb.Client.Shared.CharCounter;

/// <summary>Многострочное поле с привязкой на oninput вместо onchange. См. <see cref="InputTextLive"/>.</summary>
public class InputTextAreaLive : InputBase<string>
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "textarea");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "value", CurrentValueAsString);
        builder.AddAttribute(3, "oninput",
            EventCallback.Factory.CreateBinder<string?>(
                this, __value => CurrentValueAsString = __value, CurrentValueAsString));
        builder.CloseElement();
    }

    /// <summary>См. <see cref="InputTextLive.TryParseValueFromString"/> — для string парсинг тривиален.</summary>
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