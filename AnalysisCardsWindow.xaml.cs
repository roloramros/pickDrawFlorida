using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace FloridaLotteryApp;

public partial class AnalysisCardsWindow : Window
{
    public ObservableCollection<AnalysisPairCardVM> Cards { get; } = new();


    public AnalysisCardsWindow(GuideInfo guide, IEnumerable<AnalysisRow> resultRows)
    {
        InitializeComponent();
        DataContext = this;

        foreach (var card in resultRows.Select(r => AnalysisPairCardVM.Create(guide, r)))
            Cards.Add(card);
    }

    private void CardBorder_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement root)
        {
            return;
        }

        root.Dispatcher.BeginInvoke(new Action(() => Pick3LinksRenderer.DrawPick3Links(root)), DispatcherPriority.Loaded);
    }

    // Botón 1: Análisis de Línea (reemplaza clic izquierdo)
    private void LineAnalysisButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not AnalysisPairCardVM selectedCard)
            return;

        var detailWindow = new AnalysisLineMatchesWindow(selectedCard, Cards)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        detailWindow.Show();
    }

    // Botón 2: Análisis de Posición (reemplaza clic derecho)
    private void PositionAnalysisButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not AnalysisPairCardVM selectedCard)
            return;

        var detailWindow = new AnalysisPositionMatchesWindow(selectedCard, Cards)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        detailWindow.Show();
    }

    private void ThirdAnalysisButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not AnalysisPairCardVM selectedCard)
            return;

        var thirdAnalysisWindow = new ThirdAnalysisWindow(selectedCard)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        thirdAnalysisWindow.ShowDialog();
    }

    
}

public class GuideInfo
{
    public string Pick3 { get; set; } = "";
    public string Pick4 { get; set; } = "";
    public string NextPick3 { get; set; } = "";
    public string Coding { get; set; } = "";
    public string DateText { get; set; } = "";     // yyyy-MM-dd
    public string DrawIcon { get; set; } = "";     // ☀️ / 🌙
    public int RepPosP3 { get; set; }
    public int RepPosP4 { get; set; }
}

public class AnalysisPairCardVM
{

    private static readonly Brush HighlightBrush =
    new SolidColorBrush(Color.FromRgb(255, 140, 0)); // amarillo tenue
    private static readonly Brush AlertBrush =
    new SolidColorBrush(Color.FromRgb(220, 53, 69)); // rojo
    private static readonly Brush InfoBrush =
    new SolidColorBrush(Color.FromRgb(13, 110, 253)); // azul
    private static readonly Brush SuccessBrush =
    new SolidColorBrush(Color.FromRgb(25, 135, 84)); // verde


    // GUÍA (igual en todas las cards)
    public string GuidePick3Value { get; set; } = "";
    public string GuidePick4Value { get; set; } = "";
    public string GuideCodingValue { get; set; } = "";
    public string GuideNextPick3Value { get; set; } = "";
    public string GuideDateText { get; set; } = "";
    public string GuideDrawIcon { get; set; } = "";
    public ObservableCollection<DigitVM> GuidePick3Digits { get; set; } = new();
    public ObservableCollection<DigitVM> GuidePick4Digits { get; set; } = new();
    public ObservableCollection<DigitVM> GuideNextPick3Digits { get; set; } = new();
    public ObservableCollection<DigitVM> GuideCodingDigits { get; set; } = new();
    

    // RESULTADO
    public string ResPick3Value { get; set; } = "";
    public string ResPick4Value { get; set; } = "";
    public string ResNextPick3Value { get; set; } = "";
    public string ResDateText { get; set; } = "";
    public string ResDrawIcon { get; set; } = "";
    public ObservableCollection<DigitVM> ResPick3Digits { get; set; } = new();
    public ObservableCollection<DigitVM> ResPick4Digits { get; set; } = new();
    public ObservableCollection<DigitVM> ResNextPick3Digits { get; set; } = new();
    public ObservableCollection<DigitVM> ResCodingDigits { get; set; } = new();

    public static AnalysisPairCardVM Create(GuideInfo guide, AnalysisRow r)
    {
        var vm = new AnalysisPairCardVM
        {
            GuidePick3Value = guide.Pick3,
            GuidePick4Value = guide.Pick4,
            GuideNextPick3Value = guide.NextPick3,
            GuideCodingValue = guide.Coding,
            GuideDateText = guide.DateText,
            GuideDrawIcon = guide.DrawIcon,
            GuidePick3Digits = DigitsFrom(guide.Pick3, 3),
            GuidePick4Digits = DigitsFrom(guide.Pick4, 4),
            GuideNextPick3Digits = DigitsFrom(guide.NextPick3, 3, Brushes.White),
            GuideCodingDigits = DigitsFrom(guide.Coding, 6),

            ResPick3Value = r.Pick3,
            ResPick4Value = r.Pick4,
            ResNextPick3Value = r.NextPick3,
            ResDateText = r.Date,
            ResDrawIcon = r.DrawTime, // ☀️/🌙
            ResPick3Digits = DigitsFrom(r.Pick3, 3),
            ResPick4Digits = DigitsFrom(r.Pick4, 4),
            ResNextPick3Digits = DigitsFrom(r.NextPick3, 3),
            ResCodingDigits = DigitsFrom(r.Coding, 6),
        };

        // Colorear POSICIONES (no el valor): en Guía y Resultado
        HighlightPosition(vm.GuidePick3Digits, guide.RepPosP3);
        HighlightPosition(vm.GuidePick4Digits, guide.RepPosP4);
        HighlightPosition(vm.ResPick3Digits, guide.RepPosP3);
        HighlightPosition(vm.ResPick4Digits, guide.RepPosP4);

        HighlightNextPick3Digits(vm.ResNextPick3Digits, vm.ResPick3Digits, vm.ResPick4Digits, r.Pick3, r.Pick4);

        return vm;
    }

    public string BuildColorSignature()
    {
        var builder = new StringBuilder();
        AppendColors(builder, GuidePick3Digits);
        AppendColors(builder, GuidePick4Digits);
        AppendColors(builder, ResPick3Digits);
        AppendColors(builder, ResPick4Digits);
        AppendColors(builder, ResNextPick3Digits);
        return builder.ToString();
    }

    public string BuildResultColorSignature()
    {
        var builder = new StringBuilder();
        AppendColors(builder, ResPick3Digits);
        AppendColors(builder, ResPick4Digits);
        AppendColors(builder, ResNextPick3Digits);
        AppendColors(builder, ResCodingDigits);
        return builder.ToString();
    }

    public string BuildPick3LineSignature()
    {
        return BuildPick3LineSignature(GuidePick3Digits, ResPick3Digits);
    }

    public static string BuildPick3LineSignature(IReadOnlyList<DigitVM> topDigits, IReadOnlyList<DigitVM> bottomDigits)

    
    {
        var builder = new StringBuilder();
        var matches = topDigits
            .Select((digit, topIndex) => (digit.Value, topIndex))
            .Where(digit => !string.IsNullOrWhiteSpace(digit.Value))
            .SelectMany(top =>
                bottomDigits
                    .Select((digit, bottomIndex) => (digit.Value, bottomIndex))
                    .Where(bottom => string.Equals(bottom.Value, top.Value, StringComparison.Ordinal))
                    .Select(bottom => (TopIndex: top.topIndex, BottomIndex: bottom.bottomIndex)))
            .OrderBy(match => match.TopIndex)
            .ThenBy(match => match.BottomIndex);

        foreach (var match in matches)
        {
            builder.Append(match.TopIndex);
            builder.Append('-');
            builder.Append(match.BottomIndex);
            builder.Append('|');
        }

        return builder.ToString();
    }

    public static void HighlightRepeatedDigits(
        ObservableCollection<DigitVM> pick3Digits,
        ObservableCollection<DigitVM> pick4Digits,
        string pick3,
        string pick4)
    {
        var repeat = FindRepeatedDigit(pick3, pick4);
        if (repeat == null)
        {
            return;
        }

        HighlightMatchingDigits(pick3Digits, repeat.Value.ToString(), HighlightBrush);
        HighlightMatchingDigits(pick4Digits, repeat.Value.ToString(), HighlightBrush);
    }

    private static ObservableCollection<DigitVM> DigitsFrom(string s, int count)
        => DigitsFrom(s, count, Brushes.Transparent);

    private static ObservableCollection<DigitVM> DigitsFrom(string s, int count, Brush background)
    {
        s = (s ?? "").Trim();
        var list = new ObservableCollection<DigitVM>();
        for (int i = 0; i < count; i++)
        {
            var val = (i < s.Length) ? s[i].ToString() : "";
            list.Add(new DigitVM { Value = val, Bg = background });
        }
        return list;
    }


    private static void HighlightPosition(ObservableCollection<DigitVM> list, int pos1Based)
    {
        int idx = pos1Based - 1;
        if (idx < 0 || idx >= list.Count) return;
        list[idx].Bg = HighlightBrush;
    }

    private static void HighlightNextPick3Digits(
        ObservableCollection<DigitVM> list,
        ObservableCollection<DigitVM> pick3Digits,
        ObservableCollection<DigitVM> pick4Digits,
        string pick3,
        string pick4)
    {
        var repeat = FindRepeatedDigit(pick3, pick4);
        if (repeat != null)
        {
            foreach (var digit in list)
            {
                if (digit.Value == repeat.ToString())
                    digit.Bg = HighlightBrush;
            }
        }

        HighlightNextPick3Position(list, pick3Digits, pick4Digits, pick3, pick4, repeat, 0, AlertBrush);
        HighlightNextPick3Position(list, pick3Digits, pick4Digits, pick3, pick4, repeat, 1, InfoBrush);
        HighlightNextPick3Position(list, pick3Digits, pick4Digits, pick3, pick4, repeat, 2, SuccessBrush);
    }

    private static char? FindRepeatedDigit(string pick3, string pick4)
    {
        if (string.IsNullOrWhiteSpace(pick3) || string.IsNullOrWhiteSpace(pick4)) return null;

        var repeats = pick3.Intersect(pick4).ToList();
        return repeats.Count == 1 ? repeats[0] : null;
    }

    private static void HighlightNextPick3Position(
        ObservableCollection<DigitVM> list,
        ObservableCollection<DigitVM> pick3Digits,
        ObservableCollection<DigitVM> pick4Digits,
        string pick3,
        string pick4,
        char? repeat,
        int index,
        Brush highlight)
    {
        if (index < 0 || index >= list.Count) return;

        var digit = list[index];
        if (string.IsNullOrWhiteSpace(digit.Value)) return;
        if (repeat != null && digit.Value == repeat.ToString()) return;

        if (pick3.Contains(digit.Value) || pick4.Contains(digit.Value))
        {
            digit.Bg = highlight;
            HighlightMatchingDigits(pick3Digits, digit.Value, highlight);
            HighlightMatchingDigits(pick4Digits, digit.Value, highlight);
        }
    }

    private static void HighlightMatchingDigits(
        ObservableCollection<DigitVM> list,
        string value,
        Brush highlight)
    {
        foreach (var digit in list)
        {
            if (digit.Value == value)
                digit.Bg = highlight;
        }
    }

    private static void AppendColors(StringBuilder builder, ObservableCollection<DigitVM> digits)
    {
        foreach (var digit in digits)
        {
            builder.Append(GetBrushCode(digit.Bg));
        }
    }

    private static char GetBrushCode(Brush brush)
    {
        if (IsSameBrush(brush, HighlightBrush)) return 'O';
        if (IsSameBrush(brush, AlertBrush)) return 'R';
        if (IsSameBrush(brush, InfoBrush)) return 'B';
        if (IsSameBrush(brush, SuccessBrush)) return 'G';
        return 'N';
    }

    private static bool IsSameBrush(Brush? left, Brush? right)
    {
        if (left == null || right == null) return false;
        if (ReferenceEquals(left, right)) return true;
        if (left is SolidColorBrush leftSolid && right is SolidColorBrush rightSolid)
        {
            return leftSolid.Color.Equals(rightSolid.Color);
        }
        return false;
    }
}

public class DigitVM
{
    public string Value { get; set; } = "";
    public Brush Bg { get; set; } = Brushes.Transparent;
}
 