using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;

namespace LibraryApp.WPF_CustomControls;


// Эти классы отвечают за фокус и навикацию внутри полей вввода и кнопок
public class FieldNavigationHelper
{
    #region Attached Properties

    // Включение навигации по Enter
    public static bool GetEnableEnterNavigation(DependencyObject obj)
    {
        return (bool)obj.GetValue(EnableEnterNavigationProperty);
    }

    public static void SetEnableEnterNavigation(DependencyObject obj, bool value)
    {
        obj.SetValue(EnableEnterNavigationProperty, value);
    }

    public static readonly DependencyProperty EnableEnterNavigationProperty =
        DependencyProperty.RegisterAttached("EnableEnterNavigation", typeof(bool),
        typeof(FieldNavigationHelper), new PropertyMetadata(false, OnEnableEnterNavigationChanged));

    // Автоматический фокус при загрузке
    public static bool GetAutoFocus(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoFocusProperty);
    }

    public static void SetAutoFocus(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoFocusProperty, value);
    }

    public static readonly DependencyProperty AutoFocusProperty =
        DependencyProperty.RegisterAttached("AutoFocus", typeof(bool),
        typeof(FieldNavigationHelper), new PropertyMetadata(false, OnAutoFocusChanged));

    #endregion

    #region Private Methods

    private static void OnEnableEnterNavigationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if ((bool)e.NewValue)
            {
                element.PreviewKeyDown += Element_PreviewKeyDown;
            }
            else
            {
                element.PreviewKeyDown -= Element_PreviewKeyDown;
            }
        }
    }

    private static void OnAutoFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element && (bool)e.NewValue)
        {
            element.Loaded += Element_Loaded;
        }
    }

    private static void Element_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.Focus();
            if (element is TextBox textBox)
            {
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
    }

    private static void Element_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is FrameworkElement currentElement)
        {
            e.Handled = true;
            MoveToNextEmptyField(currentElement);
        }
    }

    private static void MoveToNextEmptyField(FrameworkElement currentElement)
    {
        var window = Window.GetWindow(currentElement);
        if (window == null) return;

        // Находим все TextBox с включенной навигацией
        var navigableFields = FindNavigableFields(window);
        if (!navigableFields.Any()) return;

        // Находим текущее поле
        var currentIndex = -1;
        for (int i = 0; i < navigableFields.Count; i++)
        {
            if (navigableFields[i] == currentElement)
            {
                currentIndex = i;
                break;
            }
        }

        // Если текущее поле не найдено, начинаем с первого незаполненного
        if (currentIndex == -1)
        {
            var firstEmptyField = FindFirstEmptyField(navigableFields);
            if (firstEmptyField != null)
            {
                FocusField(firstEmptyField);
            }
            return;
        }

        // Ищем следующее незаполненное поле (начиная со следующего)
        var nextEmptyField = FindNextEmptyField(navigableFields, currentIndex + 1);
        if (nextEmptyField != null)
        {
            FocusField(nextEmptyField);
            return;
        }

        // Если с текущей позиции до конца нет незаполненных полей,
        // ищем с начала до текущей позиции
        var remainingEmptyField = FindNextEmptyField(navigableFields, 0, currentIndex);
        if (remainingEmptyField != null)
        {
            FocusField(remainingEmptyField);
            return;
        }

        // Все поля заполнены - ищем кнопку по умолчанию
        FocusDefaultButton(window);
    }

    private static FrameworkElement FindFirstEmptyField(List<FrameworkElement> fields)
    {
        return fields.FirstOrDefault(IsFieldEmpty);
    }

    private static FrameworkElement FindNextEmptyField(List<FrameworkElement> fields, int startIndex, int endIndex = -1)
    {
        if (endIndex == -1) endIndex = fields.Count - 1;

        for (int i = startIndex; i <= endIndex; i++)
        {
            if (IsFieldEmpty(fields[i]))
            {
                return fields[i];
            }
        }
        return null;
    }

    private static List<FrameworkElement> FindNavigableFields(DependencyObject parent)
    {
        var fields = new List<FrameworkElement>();
        FindNavigableFieldsRecursive(parent, fields);

        // Сортируем по TabIndex для правильного порядка
        return fields.OrderBy(f =>
        {
            var tabIndex = KeyboardNavigation.GetTabIndex(f);
            return tabIndex >= 0 ? tabIndex : int.MaxValue;
        }).ToList();
    }

    private static void FindNavigableFieldsRecursive(DependencyObject parent, List<FrameworkElement> fields)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement element &&
                GetEnableEnterNavigation(element) &&
                element is TextBox)
            {
                fields.Add(element);
            }
            FindNavigableFieldsRecursive(child, fields);
        }
    }

    private static bool IsFieldEmpty(FrameworkElement field)
    {
        if (field is TextBox textBox)
            return string.IsNullOrWhiteSpace(textBox.Text);

        return true;
    }

    private static void FocusField(FrameworkElement field)
    {
        field.Focus();
        if (field is TextBox textBox)
        {
            textBox.CaretIndex = textBox.Text.Length;
        }
    }

    private static void FocusDefaultButton(Window window)
    {
        var defaultButton = FindDefaultButton(window);
        defaultButton?.Focus();
    }

    private static Button FindDefaultButton(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is Button button && button.IsDefault)
                return button;

            var result = FindDefaultButton(child);
            if (result != null)
                return result;
        }
        return null;
    }

    #endregion
}

public static class SmartButtonFocus
{
    public static bool GetSmartFocus(DependencyObject obj)
    {
        return (bool)obj.GetValue(SmartFocusProperty);
    }

    public static void SetSmartFocus(DependencyObject obj, bool value)
    {
        obj.SetValue(SmartFocusProperty, value);
    }

    public static readonly DependencyProperty SmartFocusProperty =
        DependencyProperty.RegisterAttached("SmartFocus", typeof(bool),
        typeof(SmartButtonFocus), new PropertyMetadata(false, OnSmartFocusChanged));

    private static void OnSmartFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Button button && (bool)e.NewValue)
        {
            bool isFirstEnter = false;

            // Обработчик для всего окна
            if (Window.GetWindow(button) is Window window)
            {
                window.PreviewKeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter && button.IsEnabled)
                    {
                        if (!button.IsFocused && !isFirstEnter)
                        {
                            // Первый Enter - фокус
                            button.Focus();
                            isFirstEnter = true;
                            e.Handled = true;
                        }
                        else if (button.IsFocused && isFirstEnter)
                        {
                            // Второй Enter - клик
                            isFirstEnter = false;
                            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                            e.Handled = true;
                        }
                    }
                };
            }

            // Сброс при потере фокуса
            button.LostFocus += (s, e) => isFirstEnter = false;
        }
    }
}

public static class WindowBehaviors
{
    #region CloseOnEsc Property

    public static bool GetCloseOnEsc(DependencyObject obj)
    {
        return (bool)obj.GetValue(CloseOnEscProperty);
    }

    public static void SetCloseOnEsc(DependencyObject obj, bool value)
    {
        obj.SetValue(CloseOnEscProperty, value);
    }

    public static readonly DependencyProperty CloseOnEscProperty =
        DependencyProperty.RegisterAttached("CloseOnEsc", typeof(bool),
        typeof(WindowBehaviors), new PropertyMetadata(false, OnCloseOnEscChanged));

    private static void OnCloseOnEscChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window)
        {
            if ((bool)e.NewValue)
            {
                window.PreviewKeyDown += Window_PreviewKeyDown;
            }
            else
            {
                window.PreviewKeyDown -= Window_PreviewKeyDown;
            }
        }
    }

    private static void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && sender is Window window)
        {
            window.Close();
        }
    }

    #endregion
}
