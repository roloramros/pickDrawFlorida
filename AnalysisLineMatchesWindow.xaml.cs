using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace FloridaLotteryApp;

public partial class AnalysisLineMatchesWindow : Window
{
    public ObservableCollection<AnalysisPairCardVM> Cards { get; } = new();
    public string MatchSummary { get; private set; } = string.Empty;

    public AnalysisLineMatchesWindow(AnalysisPairCardVM selectedCard, IEnumerable<AnalysisPairCardVM> allCards)
    {
        InitializeComponent();
        DataContext = this;
        LoadMatches(selectedCard, allCards);
    }

    private void LoadMatches(AnalysisPairCardVM selectedCard, IEnumerable<AnalysisPairCardVM> allCards)
    {
        var targetSignature = AnalysisPairCardVM.BuildPick3LineSignature(
            selectedCard.GuidePick3Digits,
            selectedCard.ResPick3Digits);
        var matches = allCards
            .Where(card => AnalysisPairCardVM.BuildPick3LineSignature(card.ResPick3Digits, selectedCard.ResPick3Digits) == targetSignature)
            //.Where(card => !IsMoreRecent(card, selectedCard))
            .Select(card => BuildLineMatchCard(card, selectedCard))
            .ToList();

        foreach (var match in matches)
        {
            Cards.Add(match);
        }

        MatchSummary = $"Coincidencias encontradas: {Cards.Count}";
    }

    private static bool IsMoreRecent(AnalysisPairCardVM candidate, AnalysisPairCardVM reference)
    {
        if (!TryBuildDrawKey(candidate, out var candidateKey) || !TryBuildDrawKey(reference, out var referenceKey))
        {
            return false;
        }

        return candidateKey.CompareTo(referenceKey) > 0;
    }

    private static bool TryBuildDrawKey(AnalysisPairCardVM card, out DrawKey key)
    {
        key = default;
        if (!DateTime.TryParse(card.ResDateText, out var date))
        {
            return false;
        }

        key = new DrawKey(date, GetDrawOrder(card.ResDrawIcon));
        return true;
    }

    private static int GetDrawOrder(string? drawIcon)
    {
        return drawIcon?.Trim() switch
        {
            "🌙" => 1,
            _ => 0,
        };
    }

    private readonly record struct DrawKey(DateTime Date, int DrawOrder) : IComparable<DrawKey>
    {
        public int CompareTo(DrawKey other)
        {
            var dateComparison = Date.CompareTo(other.Date);
            if (dateComparison != 0)
            {
                return dateComparison;
            }

            return DrawOrder.CompareTo(other.DrawOrder);
        }
    }

    private static AnalysisPairCardVM BuildLineMatchCard(AnalysisPairCardVM topCard, AnalysisPairCardVM fixedBottom)
    {
        var vm = new AnalysisPairCardVM
        {
            GuidePick3Value = topCard.ResPick3Value,
            GuidePick4Value = topCard.ResPick4Value,
            GuideNextPick3Value = topCard.ResNextPick3Value,
            GuideCodingValue = DigitsToString(topCard.ResCodingDigits),
            GuideDateText = topCard.ResDateText,
            GuideDrawIcon = topCard.ResDrawIcon,
            GuidePick3Digits = CloneDigitsPlain(topCard.ResPick3Digits),
            GuidePick4Digits = CloneDigitsPlain(topCard.ResPick4Digits),
            GuideNextPick3Digits = DigitsFromWhite(topCard.ResNextPick3Value, 3),
            GuideCodingDigits = CloneDigitsPlain(topCard.ResCodingDigits),
            ResPick3Value = fixedBottom.ResPick3Value,
            ResPick4Value = fixedBottom.ResPick4Value,
            ResNextPick3Value = fixedBottom.ResNextPick3Value,
            ResDateText = fixedBottom.ResDateText,
            ResDrawIcon = fixedBottom.ResDrawIcon,
            ResPick3Digits = CloneDigits(fixedBottom.ResPick3Digits),
            ResPick4Digits = CloneDigits(fixedBottom.ResPick4Digits),
            ResNextPick3Digits = CloneDigits(fixedBottom.ResNextPick3Digits),
            ResCodingDigits = CloneDigits(fixedBottom.ResCodingDigits),
        };

        AnalysisPairCardVM.HighlightRepeatedDigits(
            vm.GuidePick3Digits,
            vm.GuidePick4Digits,
            vm.GuidePick3Value,
            vm.GuidePick4Value);

        return vm;
    }

    private static ObservableCollection<DigitVM> CloneDigitsPlain(IEnumerable<DigitVM> digits)
    {
        var clone = new ObservableCollection<DigitVM>();
        foreach (var digit in digits)
        {
            clone.Add(new DigitVM { Value = digit.Value });
        }
        return clone;
    }

    private static ObservableCollection<DigitVM> CloneDigits(IEnumerable<DigitVM> digits)
    {
        var clone = new ObservableCollection<DigitVM>();
        foreach (var digit in digits)
        {
            clone.Add(new DigitVM { Value = digit.Value, Bg = digit.Bg });
        }
        return clone;
    }

    private static ObservableCollection<DigitVM> DigitsFromWhite(string value, int count)
    {
        value = (value ?? string.Empty).Trim();
        var list = new ObservableCollection<DigitVM>();
        for (int i = 0; i < count; i++)
        {
            var val = i < value.Length ? value[i].ToString() : "";
            list.Add(new DigitVM { Value = val, Bg = Brushes.White });
        }
        return list;
    }

    private static string DigitsToString(IEnumerable<DigitVM> digits)
    {
        return string.Concat(digits.Select(digit => digit.Value ?? string.Empty));
    }

    private void CardBorder_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement root)
        {
            return;
        }

        root.Dispatcher.BeginInvoke(new Action(() => Pick3LinksRenderer.DrawPick3Links(root)), DispatcherPriority.Loaded);
    }


}
 