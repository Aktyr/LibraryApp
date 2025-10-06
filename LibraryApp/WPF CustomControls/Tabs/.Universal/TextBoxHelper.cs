namespace LibraryApp.WPF_CustomControls;
public static class TextBoxHelper
{
    public static string GetPlaceholder(DependencyObject obj) =>
        (string)obj.GetValue(PlaceholderProperty);

    public static void SetPlaceholder(DependencyObject obj, string value) =>
        obj.SetValue(PlaceholderProperty, value);

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.RegisterAttached(
            "Placeholder",
            typeof(string),
            typeof(TextBoxHelper),
            new FrameworkPropertyMetadata(
                defaultValue: null,
                propertyChangedCallback: OnPlaceholderChanged)
            );

    private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBoxControl)
        {
            if (!textBoxControl.IsLoaded)
            {
                // Ensure that the events are not added multiple times
                textBoxControl.Loaded -= TextBoxControl_Loaded;
                textBoxControl.Loaded += TextBoxControl_Loaded;
            }

            textBoxControl.TextChanged -= TextBoxControl_TextChanged;
            textBoxControl.TextChanged += TextBoxControl_TextChanged;

            // If the adorner exists, invalidate it to draw the current text
            if (GetOrCreateAdorner(textBoxControl, out PlaceholderAdorner adorner))
                adorner.InvalidateVisual();
        }
    }

    private static void TextBoxControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBoxControl)
        {
            textBoxControl.Loaded -= TextBoxControl_Loaded;
            GetOrCreateAdorner(textBoxControl, out _);
        }
    }

    private static void TextBoxControl_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBoxControl
            && GetOrCreateAdorner(textBoxControl, out PlaceholderAdorner adorner))
        {
            // Control has text. Hide the adorner.
            if (textBoxControl.Text.Length > 0)
                adorner.Visibility = Visibility.Hidden;

            // Control has no text. Show the adorner.
            else
                adorner.Visibility = Visibility.Visible;
        }
    }

    private static bool GetOrCreateAdorner(TextBox textBoxControl, out PlaceholderAdorner adorner)
    {
        // Get the adorner layer
        AdornerLayer layer = AdornerLayer.GetAdornerLayer(textBoxControl);

        // If null, it doesn't exist or the control's template isn't loaded
        if (layer == null)
        {
            adorner = null;
            return false;
        }

        // Layer exists, try to find the adorner
        adorner = layer.GetAdorners(textBoxControl)?.OfType<PlaceholderAdorner>().FirstOrDefault();

        // Adorner never added to control, so add it
        if (adorner == null)
        {
            adorner = new PlaceholderAdorner(textBoxControl);
            layer.Add(adorner);
        }

        return true;
    }

    public class PlaceholderAdorner : Adorner
    {
        public PlaceholderAdorner(TextBox textBox) : base(textBox) { }

        protected override void OnRender(DrawingContext drawingContext)
        {
            TextBox textBoxControl = (TextBox)AdornedElement;

            string placeholderValue = TextBoxHelper.GetPlaceholder(textBoxControl);

            if (string.IsNullOrEmpty(placeholderValue))
                return;

            // Create the formatted text object
            FormattedText text = new FormattedText(
                                        placeholderValue,
                                        System.Globalization.CultureInfo.CurrentCulture,
                                        textBoxControl.FlowDirection,
                                        new Typeface(textBoxControl.FontFamily,
                                                     textBoxControl.FontStyle,
                                                     textBoxControl.FontWeight,
                                                     textBoxControl.FontStretch),
                                        textBoxControl.FontSize,
                                        SystemColors.InactiveCaptionBrush,
                                        VisualTreeHelper.GetDpi(textBoxControl).PixelsPerDip);

            text.MaxTextWidth = System.Math.Max(textBoxControl.ActualWidth - textBoxControl.Padding.Left - textBoxControl.Padding.Right, 10);
            text.MaxTextHeight = System.Math.Max(textBoxControl.ActualHeight, 10);

            // Render based on padding of the control, to try and match where the textbox places text
            Point renderingOffset = new Point(textBoxControl.Padding.Left, textBoxControl.Padding.Top);

            // Template contains the content part; adjust sizes to try and align the text
            if (textBoxControl.Template.FindName("PART_ContentHost", textBoxControl) is FrameworkElement part)
            {
                Point partPosition = part.TransformToAncestor(textBoxControl).Transform(new Point(0, 0));
                renderingOffset.X += partPosition.X;
                renderingOffset.Y += partPosition.Y;

                text.MaxTextWidth = System.Math.Max(part.ActualWidth - renderingOffset.X, 10);
                text.MaxTextHeight = System.Math.Max(part.ActualHeight, 10);
            }

            // Draw the text
            drawingContext.DrawText(text, renderingOffset);
        }
    }
}


// Этот класс - обработчик цифровых полей
public static class TextBoxHandlers
{
    #region ClearZero Property

    public static bool GetClearZero(DependencyObject obj)
    {
        return (bool)obj.GetValue(ClearZeroProperty);
    }

    public static void SetClearZero(DependencyObject obj, bool value)
    {
        obj.SetValue(ClearZeroProperty, value);
    }

    public static readonly DependencyProperty ClearZeroProperty =
        DependencyProperty.RegisterAttached("ClearZero", typeof(bool),
        typeof(TextBoxHandlers), new PropertyMetadata(false, OnClearZeroChanged));

    private static void OnClearZeroChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
            {
                // Обрабатываем текущее значение
                if (textBox.Text == "0")
                {
                    textBox.Text = "";
                }

                textBox.TextChanged += ClearZeroTextChanged;
            }
            else
            {
                textBox.TextChanged -= ClearZeroTextChanged;
            }
        }
    }

    private static void ClearZeroTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.Text == "0")
        {
            textBox.Text = "";
        }
    }

    #endregion

    #region ResetHighlightOnChange Property

    public static bool GetResetHighlightOnChange(DependencyObject obj)
    {
        return (bool)obj.GetValue(ResetHighlightOnChangeProperty);
    }

    public static void SetResetHighlightOnChange(DependencyObject obj, bool value)
    {
        obj.SetValue(ResetHighlightOnChangeProperty, value);
    }

    public static readonly DependencyProperty ResetHighlightOnChangeProperty =
        DependencyProperty.RegisterAttached("ResetHighlightOnChange", typeof(bool),
        typeof(TextBoxHandlers), new PropertyMetadata(false, OnResetHighlightOnChangeChanged));

    private static void OnResetHighlightOnChangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
            {
                textBox.TextChanged += ResetHighlightTextChanged;
            }
            else
            {
                textBox.TextChanged -= ResetHighlightTextChanged;
            }
        }
    }

    private static void ResetHighlightTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            FieldHighlighter.ResetFieldColor(textBox);
        }
    }



    #endregion

    #region NumbersOnly Property

    public static bool GetNumbersOnly(DependencyObject obj)
    {
        return (bool)obj.GetValue(NumbersOnlyProperty);
    }

    public static void SetNumbersOnly(DependencyObject obj, bool value)
    {
        obj.SetValue(NumbersOnlyProperty, value);
    }

    public static readonly DependencyProperty NumbersOnlyProperty =
        DependencyProperty.RegisterAttached("NumbersOnly", typeof(bool),
        typeof(TextBoxHandlers), new PropertyMetadata(false, OnNumbersOnlyChanged));

    private static void OnNumbersOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
            {
                textBox.PreviewTextInput += NumbersOnlyPreviewTextInput;
                textBox.PreviewKeyDown += NumbersOnlyPreviewKeyDown;

                // Запрещаем вставку через Ctrl+V
                DataObject.AddPastingHandler(textBox, OnPaste);
            }
            else
            {
                textBox.PreviewTextInput -= NumbersOnlyPreviewTextInput;
                textBox.PreviewKeyDown -= NumbersOnlyPreviewKeyDown;
                DataObject.RemovePastingHandler(textBox, OnPaste);
            }
        }
    }

    private static void NumbersOnlyPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Разрешаем только цифры
        e.Handled = !IsTextAllowed(e.Text);
    }

    private static void NumbersOnlyPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Разрешаем Backspace, Delete, Tab, стрелки
        if (e.Key == Key.Back || e.Key == Key.Delete ||
            e.Key == Key.Tab || e.Key == Key.Left ||
            e.Key == Key.Right || e.Key == Key.Up ||
            e.Key == Key.Down)
        {
            return;
        }

        // Запрещаем пробел
        if (e.Key == Key.Space)
        {
            e.Handled = true;
        }
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            string text = (string)e.DataObject.GetData(typeof(string));
            if (!IsTextAllowed(text))
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    private static bool IsTextAllowed(string text)
    {
        // Разрешаем только цифры
        return text.All(c => char.IsDigit(c));
    }

    #endregion

}
