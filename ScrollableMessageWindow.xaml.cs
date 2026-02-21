using System.Windows;

namespace FloridaLotteryApp;

public partial class ScrollableMessageWindow : Window
{
    public string Message { get; }

    public ScrollableMessageWindow(string title, string message)
    {
        InitializeComponent();
        Title = title;
        Message = message;
        DataContext = this;
    }
}