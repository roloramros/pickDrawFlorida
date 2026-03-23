using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using FloridaLotteryApp.Data;

namespace FloridaLotteryApp;

public partial class PlotOption2SavedRecordsWindow : Window, INotifyPropertyChanged
{
    public ObservableCollection<PlotOption2Session> Sessions { get; } = new();

    private readonly List<PlotOption2SavedRecord> _records = new();
    private int _currentIndex = -1;

    private static readonly Brush[] RepeatPalette =
    {
        (Brush)new BrushConverter().ConvertFromString("#0000FF")!,
        (Brush)new BrushConverter().ConvertFromString("#006400")!,
        (Brush)new BrushConverter().ConvertFromString("#dc143c")!,
        (Brush)new BrushConverter().ConvertFromString("#daa520")!,
        (Brush)new BrushConverter().ConvertFromString("#9400d3")!,
        (Brush)new BrushConverter().ConvertFromString("#20b2aa")!
    };

    private string _currentNote = string.Empty;
    public string CurrentNote
    {
        get => _currentNote;
        set
        {
            if (_currentNote == value)
            {
                return;
            }

            _currentNote = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasCurrentNote));
        }
    }

    public bool HasCurrentNote => !string.IsNullOrWhiteSpace(CurrentNote);

    public PlotOption2SavedRecordsWindow()
    {
        InitializeComponent();
        DataContext = this;
        this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
        this.Top = 0;

        Sessions.Add(BuildEmptySession("Top"));
        Sessions.Add(BuildEmptySession("Bottom"));

        LoadRecords();

        Loaded += (_, _) => QueueDrawConnectingLines();
    }

    private void LoadRecords()
    {
        _records.Clear();
        _records.AddRange(PlotOption2SavedRepository.GetAll());
        _currentIndex = _records.Count > 0 ? 0 : -1;
        LoadCurrentRecord();
    }

    private void LoadCurrentRecord()
    {
        if (_currentIndex < 0 || _currentIndex >= _records.Count)
        {
            Sessions[0] = BuildEmptySession("Top");
            Sessions[1] = BuildEmptySession("Bottom");
            CurrentNote = string.Empty;
            UpdateResultsCounter();
            UpdateNavigationButtons();
            QueueDrawConnectingLines();
            return;
        }

        var record = _records[_currentIndex];
        Sessions[0] = BuildSession(record, isGuideSession: true, "Top");
        Sessions[1] = BuildSession(record, isGuideSession: false, "Bottom");
        CurrentNote = string.IsNullOrWhiteSpace(record.Label) ? string.Empty : record.Label.Trim();
        UpdateResultsCounter();
        UpdateNavigationButtons();
        QueueDrawConnectingLines();
    }

    private static PlotOption2Session BuildSession(PlotOption2SavedRecord record, bool isGuideSession, string sessionTag)
    {
        var session = new PlotOption2Session
        {
            Row1 = CreateRowFromRecord(isGuideSession ? record.G1Date : record.R1Date, isGuideSession ? record.G1Time : record.R1Time),
            Row2 = CreateRowFromRecord(isGuideSession ? record.G2Date : record.R2Date, isGuideSession ? record.G2Time : record.R2Time),
            Row3 = CreateRowFromRecord(isGuideSession ? record.G3Date : record.R3Date, isGuideSession ? record.G3Time : record.R3Time),
            Row4 = CreateRowFromRecord(isGuideSession ? record.G4Date : record.R4Date, isGuideSession ? record.G4Time : record.R4Time)
        };

        AssignSessionTags(session, sessionTag);
        UpdateSessionDigitCells(session);
        return session;
    }

    private static PlotOption2Row CreateRowFromRecord(string? dateText, string? drawTime)
    {
        if (!DateTime.TryParse(dateText, out var date) || string.IsNullOrWhiteSpace(drawTime))
        {
            return CreateRow(null, null, null, new List<string>(), " ", " ");
        }

        var normalizedDrawTime = drawTime.Trim().ToUpperInvariant();
        var pick3 = DrawRepository.GetResult("pick3", date, normalizedDrawTime).Number;
        var pick4 = DrawRepository.GetResult("pick4", date, normalizedDrawTime).Number;
        var nextPick3 = DrawRepository.GetNextPick3Number(date, normalizedDrawTime);

        return CreateRow(
            pick3,
            pick4,
            nextPick3,
            BuildCodificacionDigits(pick3, pick4),
            date.ToString("yyyy-MM-dd"),
            DrawIconFromTime(normalizedDrawTime));
    }

    private static PlotOption2Session BuildEmptySession(string sessionTag)
    {
        var session = new PlotOption2Session
        {
            Row1 = CreateRow(null, null, null, new List<string>(), " ", " "),
            Row2 = CreateRow(null, null, null, new List<string>(), " ", " "),
            Row3 = CreateRow(null, null, null, new List<string>(), " ", " "),
            Row4 = CreateRow(null, null, null, new List<string>(), " ", " ")
        };

        AssignSessionTags(session, sessionTag);
        UpdateSessionDigitCells(session);
        return session;
    }

    private static PlotOption2Row CreateRow(string? pick3, string? pick4, string? nextPick3, List<string>? additionalDigits, string? dateText, string? drawIcon)
    {
        return new PlotOption2Row
        {
            Pick3Value = pick3 ?? " ",
            Pick4Value = pick4 ?? " ",
            NextPick3Digits = BuildFixedDigitSlots(nextPick3, 3),
            AdditionalDigits = additionalDigits ?? new List<string>(),
            DateText = string.IsNullOrWhiteSpace(dateText) ? " " : dateText,
            DrawIcon = string.IsNullOrWhiteSpace(drawIcon) ? " " : drawIcon
        };
    }

    private void Anterior_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex <= 0)
        {
            return;
        }

        _currentIndex--;
        LoadCurrentRecord();
    }

    private void Siguiente_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex < 0 || _currentIndex >= _records.Count - 1)
        {
            return;
        }

        _currentIndex++;
        LoadCurrentRecord();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex < 0 || _currentIndex >= _records.Count)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Se borrará el registro guardado {_currentIndex + 1} de {_records.Count}.",
            "Borrar registro",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var currentRecord = _records[_currentIndex];
        if (!PlotOption2SavedRepository.Delete(currentRecord.Id))
        {
            MessageBox.Show("No se pudo borrar el registro.", "Borrar", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _records.RemoveAt(_currentIndex);
        if (_currentIndex >= _records.Count)
        {
            _currentIndex = _records.Count - 1;
        }

        LoadCurrentRecord();
    }

    private void UpdateResultsCounter()
    {
        int total = _records.Count;
        int selected = _currentIndex >= 0 && _currentIndex < total ? _currentIndex + 1 : 0;
        ResultsCounterText.Text = $"{selected} de {total}";
    }

    private void UpdateNavigationButtons()
    {
        AnteriorButton.IsEnabled = _currentIndex > 0;
        SiguienteButton.IsEnabled = _currentIndex >= 0 && _currentIndex < _records.Count - 1;
        DeleteButton.IsEnabled = _currentIndex >= 0 && _currentIndex < _records.Count;
    }

    private static void AssignSessionTags(PlotOption2Session session, string sessionTag)
    {
        session.SessionTag = sessionTag;
        session.Row1.NextPick3Tag = $"{sessionTag}:Row1";
        session.Row2.NextPick3Tag = $"{sessionTag}:Row2";
        session.Row3.NextPick3Tag = $"{sessionTag}:Row3";
        session.Row4.NextPick3Tag = $"{sessionTag}:Row4";
    }

    private void QueueDrawConnectingLines()
    {
        Dispatcher.BeginInvoke(new Action(DrawConnectingLines), DispatcherPriority.Render);
    }

    private void DrawConnectingLines()
    {
        try
        {
            DrawSessionConnections("Top");
            DrawSessionConnections("Bottom");
        }
        catch
        {
        }
    }

    private void DrawSessionConnections(string sessionTag)
    {
        var canvas = FindCanvasByTag(sessionTag);
        if (canvas == null)
        {
            return;
        }

        canvas.Children.Clear();
        var sessionRoot = VisualTreeHelper.GetParent(canvas) as DependencyObject ?? canvas;
        var row1 = FindItemsControlByTag(sessionRoot, $"{sessionTag}:Row1");
        var row2 = FindItemsControlByTag(sessionRoot, $"{sessionTag}:Row2");
        var row3 = FindItemsControlByTag(sessionRoot, $"{sessionTag}:Row3");
        var row4 = FindItemsControlByTag(sessionRoot, $"{sessionTag}:Row4");
        if (row1 == null || row2 == null || row3 == null || row4 == null)
        {
            return;
        }

        ConnectPick3Digits(row1, row2, canvas, Brushes.Black);
        ConnectPick3Digits(row3, row4, canvas, Brushes.Black);
    }

    private Canvas? FindCanvasByTag(string sessionTag)
    {
        return FindVisualChildren<Canvas>(this)
            .FirstOrDefault(c => string.Equals(c.Tag as string, sessionTag, StringComparison.Ordinal));
    }

    private static ItemsControl? FindItemsControlByTag(DependencyObject root, string tag)
    {
        return FindVisualChildren<ItemsControl>(root)
            .FirstOrDefault(c => string.Equals(c.Tag as string, tag, StringComparison.Ordinal));
    }

    private static void ConnectPick3Digits(ItemsControl topItemsControl, ItemsControl bottomItemsControl, Canvas canvas, Brush lineColor)
    {
        var topDigits = GetDigitContainers(topItemsControl);
        var bottomDigits = GetDigitContainers(bottomItemsControl);
        if (topDigits.Count == 0 || bottomDigits.Count == 0)
        {
            return;
        }

        for (int i = 0; i < topDigits.Count; i++)
        {
            var topDigit = topDigits[i];
            var topText = GetDigitText(topDigit);
            if (string.IsNullOrWhiteSpace(topText))
            {
                continue;
            }

            for (int j = 0; j < bottomDigits.Count; j++)
            {
                var bottomDigit = bottomDigits[j];
                var bottomText = GetDigitText(bottomDigit);
                if (string.IsNullOrWhiteSpace(bottomText))
                {
                    continue;
                }

                if (topText == bottomText)
                {
                    DrawConnectingLine(canvas, topDigit, bottomDigit, lineColor);
                }
            }
        }
    }

    private static List<Border> GetDigitContainers(ItemsControl itemsControl)
    {
        var containers = new List<Border>();
        try
        {
            if (itemsControl.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                for (int i = 0; i < itemsControl.Items.Count; i++)
                {
                    var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                    if (container == null)
                    {
                        continue;
                    }

                    var border = FindVisualChild<Border>(container);
                    if (border != null)
                    {
                        containers.Add(border);
                    }
                }
            }

            if (containers.Count == 0)
            {
                foreach (var cp in FindVisualChildren<ContentPresenter>(itemsControl))
                {
                    var border = FindVisualChild<Border>(cp);
                    if (border != null)
                    {
                        containers.Add(border);
                    }
                }
            }
        }
        catch
        {
        }

        return containers;
    }

    private static string GetDigitText(Border digitBorder)
    {
        var textBlock = FindVisualChild<TextBlock>(digitBorder);
        return textBlock?.Text?.Trim() ?? string.Empty;
    }

    private static void DrawConnectingLine(Canvas canvas, Border element1, Border element2, Brush lineColor)
    {
        if (element1.ActualWidth == 0 || element1.ActualHeight == 0 || element2.ActualWidth == 0 || element2.ActualHeight == 0)
        {
            return;
        }

        var center1 = element1.TranslatePoint(new Point(element1.ActualWidth / 2, element1.ActualHeight / 2), canvas);
        var center2 = element2.TranslatePoint(new Point(element2.ActualWidth / 2, element2.ActualHeight / 2), canvas);
        double dx = center2.X - center1.X;
        double dy = center2.Y - center1.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance == 0)
        {
            return;
        }

        double radius1 = element1.ActualWidth / 2;
        double radius2 = element2.ActualWidth / 2;
        double unitX = dx / distance;
        double unitY = dy / distance;

        var line = new System.Windows.Shapes.Line
        {
            X1 = center1.X + (unitX * radius1),
            Y1 = center1.Y + (unitY * radius1),
            X2 = center2.X - (unitX * radius2),
            Y2 = center2.Y - (unitY * radius2),
            Stroke = lineColor,
            StrokeThickness = 2
        };

        canvas.Children.Add(line);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null)
        {
            return null;
        }

        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var found = FindVisualChild<T>(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null)
        {
            yield break;
        }

        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static void UpdateSessionDigitCells(PlotOption2Session session)
    {
        var topA = ExtractDigits(session.Row1.Pick3Value).Concat(ExtractDigits(session.Row1.Pick4Value)).ToList();
        var topB = ExtractDigits(session.Row2.Pick3Value).Concat(ExtractDigits(session.Row2.Pick4Value)).ToList();
        var bottomA = ExtractDigits(session.Row3.Pick3Value).Concat(ExtractDigits(session.Row3.Pick4Value)).ToList();
        var bottomB = ExtractDigits(session.Row4.Pick3Value).Concat(ExtractDigits(session.Row4.Pick4Value)).ToList();

        var topCounts = topA.Concat(topB).GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
        var repeatedTopDigits = topA.Concat(topB).Where(d => topCounts.TryGetValue(d, out var count) && count > 1).Distinct().ToList();

        var topBrushByDigit = new Dictionary<string, Brush>();
        for (int i = 0; i < repeatedTopDigits.Count; i++)
        {
            topBrushByDigit[repeatedTopDigits[i]] = RepeatPalette[i % RepeatPalette.Length];
        }

        var topProfiles = BuildDigitProfiles(topA, topB);
        var topBrushByProfile = topProfiles
            .Where(kv => topBrushByDigit.ContainsKey(kv.Key))
            .ToDictionary(kv => kv.Value, kv => topBrushByDigit[kv.Key]);

        var bottomCounts = bottomA.Concat(bottomB).GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
        var bottomProfiles = BuildDigitProfiles(bottomA, bottomB);
        var bottomBrushByDigit = new Dictionary<string, Brush>();
        var repeatedBottomDigits = bottomA.Concat(bottomB).Where(d => bottomCounts.TryGetValue(d, out var count) && count > 1).Distinct().ToList();

        int nextPaletteIndex = repeatedTopDigits.Count;
        foreach (var digit in repeatedBottomDigits)
        {
            if (bottomProfiles.TryGetValue(digit, out var profile) && topBrushByProfile.TryGetValue(profile, out var mappedBrush))
            {
                bottomBrushByDigit[digit] = mappedBrush;
            }
            else
            {
                bottomBrushByDigit[digit] = RepeatPalette[nextPaletteIndex % RepeatPalette.Length];
                nextPaletteIndex++;
            }
        }

        session.Row1.Pick3Digits = BuildDigitCells(session.Row1.Pick3Value, topCounts, topBrushByDigit);
        session.Row1.Pick4Digits = BuildDigitCells(session.Row1.Pick4Value, topCounts, topBrushByDigit);
        session.Row2.Pick3Digits = BuildDigitCells(session.Row2.Pick3Value, topCounts, topBrushByDigit);
        session.Row2.Pick4Digits = BuildDigitCells(session.Row2.Pick4Value, topCounts, topBrushByDigit);
        session.Row3.Pick3Digits = BuildDigitCells(session.Row3.Pick3Value, bottomCounts, bottomBrushByDigit);
        session.Row3.Pick4Digits = BuildDigitCells(session.Row3.Pick4Value, bottomCounts, bottomBrushByDigit);
        session.Row4.Pick3Digits = BuildDigitCells(session.Row4.Pick3Value, bottomCounts, bottomBrushByDigit);
        session.Row4.Pick4Digits = BuildDigitCells(session.Row4.Pick4Value, bottomCounts, bottomBrushByDigit);
    }

    private static List<string> BuildCodificacionDigits(string? pick3, string? pick4)
    {
        return ((pick3 ?? string.Empty) + (pick4 ?? string.Empty))
            .Where(char.IsDigit)
            .Distinct()
            .OrderBy(c => c)
            .Select(c => c.ToString())
            .ToList();
    }

    private static List<string> BuildFixedDigitSlots(string? value, int size)
    {
        var digits = ExtractDigits(value).Take(size).ToList();
        while (digits.Count < size)
        {
            digits.Add(" ");
        }

        return digits;
    }

    private static List<string> ExtractDigits(string? value)
    {
        return (value ?? string.Empty).Where(char.IsDigit).Select(c => c.ToString()).ToList();
    }

    private static Dictionary<string, string> BuildDigitProfiles(IReadOnlyList<string> rowA, IReadOnlyList<string> rowB)
    {
        var profiles = new Dictionary<string, string>();
        var digits = rowA.Concat(rowB).Distinct().ToList();

        foreach (var digit in digits)
        {
            var aPositions = Enumerable.Range(0, rowA.Count).Where(i => rowA[i] == digit);
            var bPositions = Enumerable.Range(0, rowB.Count).Where(i => rowB[i] == digit);
            profiles[digit] = $"A:{string.Join(",", aPositions)}|B:{string.Join(",", bPositions)}";
        }

        return profiles;
    }

    private static List<PlotOption2DigitCell> BuildDigitCells(string? value, IReadOnlyDictionary<string, int> counts, IReadOnlyDictionary<string, Brush> brushesByDigit)
    {
        return ExtractDigits(value)
            .Select(d => new PlotOption2DigitCell
            {
                Value = d,
                Background = counts.TryGetValue(d, out var count) && count > 1
                    ? (brushesByDigit.TryGetValue(d, out var brush) ? brush : RepeatPalette[0])
                    : Brushes.White
            })
            .ToList();
    }

    private static string DrawIconFromTime(string? drawTime)
    {
        return drawTime == "M" ? "\u2600\uFE0F" : drawTime == "E" ? "\U0001F319" : " ";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
