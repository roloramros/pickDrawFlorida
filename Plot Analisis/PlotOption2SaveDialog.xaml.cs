using System.Windows;

namespace FloridaLotteryApp;

public partial class PlotOption2SaveDialog : Window
{
    public string NoteText => string.IsNullOrWhiteSpace(NoteTextBox.Text)
        ? string.Empty
        : NoteTextBox.Text.Trim();

    public PlotOption2SaveDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => NoteTextBox.Focus();
        this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
        this.Top = 0;
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
