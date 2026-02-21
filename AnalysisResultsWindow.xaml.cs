using System.Collections.ObjectModel;
using System.Windows;

namespace FloridaLotteryApp;

public partial class AnalysisResultsWindow : Window
{
    public ObservableCollection<AnalysisRow> Items { get; } = new();

    public AnalysisResultsWindow(string header, IEnumerable<AnalysisRow> rows)
    {
        InitializeComponent();
        DataContext = this;

        TxtHeader.Text = header;

        foreach (var r in rows)
            Items.Add(r);
    }
}

public class AnalysisRow
{
    public bool IsChecked { get; set; }
    public string Date { get; set; } = "";
    public string DrawTime { get; set; } = "";
    public string Pick3 { get; set; } = "";
    public string Pick4 { get; set; } = "";
    public string NextPick3 { get; set; } = "---";
    public string Coding { get; set; } = "";
}
