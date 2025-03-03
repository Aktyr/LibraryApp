using System.Media;

namespace LibraryApp.WPF_CustomControls;

/// <summary>
/// Логика взаимодействия для Window1.xaml
/// </summary>
public partial class ConfirmDeletion : Window
{
    public bool Confirm = false;
    public ConfirmDeletion()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (Confirm_TextBlock.Text.ToLower() == "удалить")
        {
            Confirm = true;
            Close();
        }
        else SystemSounds.Beep.Play();

    }

}
