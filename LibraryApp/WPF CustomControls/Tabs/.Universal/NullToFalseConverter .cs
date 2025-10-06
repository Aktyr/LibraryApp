using System.Globalization;

namespace LibraryApp.WPF_CustomControls;


// Этот класс делает кнопки кликабельными/не кликабельными
public class NullToFalseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}