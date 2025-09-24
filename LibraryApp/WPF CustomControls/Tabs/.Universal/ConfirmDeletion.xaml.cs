using System.Media;

namespace LibraryApp.WPF_CustomControls;

/// <summary>
/// Логика взаимодействия для Window1.xaml
/// </summary>
public partial class ConfirmDeletion : Window
{
    public bool IsConfirm;
    public ConfirmDeletion()
    {
        InitializeComponent();
        IsConfirm = false;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (Confirm_TextBlock.Text.ToLower() == "удалить")
        {
            IsConfirm = true;
            Close();
        }
        else SystemSounds.Beep.Play();

    }

}
