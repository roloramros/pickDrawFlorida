using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using FloridaLotteryApp.Data;

namespace FloridaLotteryApp;

public partial class Analisis_3_1_MatchWindow : Window, INotifyPropertyChanged
{
    private sealed record CuartetoOccurrence(FilteredCodificacion Row1, FilteredCodificacion Row2, FilteredCodificacion Row3, FilteredCodificacion Row4);

    private ThirdAnalysisCardVM? _currentResultCard;
    private int _currentIndex = -1;
    private bool _isLoading;
    private string _progressMessage = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ThirdAnalysisCardVM GuideCard { get; }
    public ObservableCollection<ThirdAnalysisCardVM> ResultCards { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(ProgressVisibility));
            UpdateNavigationButtons();
        }
    }

    public string ProgressMessage
    {
        get => _progressMessage;
        set
        {
            _progressMessage = value;
            OnPropertyChanged(nameof(ProgressMessage));
        }
    }

    public Visibility ProgressVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

    public ThirdAnalysisCardVM? CurrentResultCard
    {
        get => _currentResultCard;
        set
        {
            _currentResultCard = value;
            OnPropertyChanged(nameof(CurrentResultCard));
            UpdateNavigationButtons();
        }
    }

    public Analisis_3_1_MatchWindow(ThirdAnalysisCardVM selectedCard)
    {
        InitializeComponent();
        this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
        this.Top = 0;
        GuideCard = CloneCard(selectedCard);
        CurrentResultCard = BuildStatusCard("Calculando resultados...");
        DataContext = this;
        Loaded += Analisis31MatchWindow_Loaded;
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private async void Analisis31MatchWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Analisis31MatchWindow_Loaded;
        await LoadOption1ResultsAsync();
    }

    private async Task LoadOption1ResultsAsync()
    {
        IsLoading = true;
        ProgressMessage = "Calculando resultados del análisis 3 opción 1...";

        try
        {
            CurrentResultCard = BuildStatusCard("Calculando resultados...");
            var cards = await Task.Run(BuildOption1ResultCards);

            ResultCards.Clear();
            foreach (var card in cards)
            {
                ResultCards.Add(card);
            }

            if (ResultCards.Count > 0)
            {
                _currentIndex = 0;
                CurrentResultCard = ResultCards[0];
            }
            else
            {
                _currentIndex = -1;
                CurrentResultCard = BuildEmptyResultCard();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar resultados del análisis: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _currentIndex = -1;
            CurrentResultCard = BuildEmptyResultCard();
        }
        finally
        {
            IsLoading = false;
            ProgressMessage = string.Empty;
        }
    }

    private List<ThirdAnalysisCardVM> BuildOption1ResultCards()
    {
        string row3Full = DigitsToString(GuideCard.Row3Pick3Digits) + DigitsToString(GuideCard.Row3Pick4Digits) + DigitsToString(GuideCard.Row3NextPick3Digits);
        string row1Full = DigitsToString(GuideCard.Row1Pick3Digits) + DigitsToString(GuideCard.Row1Pick4Digits) + DigitsToString(GuideCard.Row1NextPick3Digits);
        var guidePair = (row3Full, row1Full);

        var codificaciones = DrawRepository.GetCodificacionesWithSingleCommonDigit();
        var validCodificaciones = codificaciones
            .Where(c => !string.IsNullOrWhiteSpace(c.FullNumber) && c.FullNumber.Length == 10)
            .ToList();

        var codificacionesByFullNumber = validCodificaciones
            .GroupBy(c => c.FullNumber)
            .ToDictionary(g => g.Key, g => g.ToList());

        var allFullNumbers = codificacionesByFullNumber.Keys.ToList();
        var allResults = new List<(string Original, string Compatible)>();

        foreach (var fullNumber in allFullNumbers)
        {
            allResults.AddRange(FindFilteredPairs(fullNumber, allFullNumbers, guidePair));
        }

        var validCuartetos = FormCuartetosFromPairs(allResults);
        var cuartetosFiltrados = FilterCuartetosByCounters(
            AnalysisMode.Opcion1,
            validCuartetos,
            GuideCard.Pick4MatchesRow1Row2,
            GuideCard.CodingMatchesRow1Row2,
            GuideCard.Pick4MatchesRow3Row4,
            GuideCard.CodingMatchesRow3Row4);

        var cuartetoOccurrences = ExpandCuartetosWithOccurrences(cuartetosFiltrados, codificacionesByFullNumber);
        var cards = new List<ThirdAnalysisCardVM>();

        foreach (var cuarteto in cuartetoOccurrences)
        {
            var card = BuildCardFromCuarteto(cuarteto);
            if (IsValidDateOrder(card))
            {
                cards.Add(card);
            }
        }

        return ApplyRow4NextPick3PositionFilter(ExcludeGuideCard(ApplyCodingRowMatchFilter(ApplyMatchGuideFilter(cards))));
    }

    private static bool IsValidDateOrder(ThirdAnalysisCardVM card)
    {
        if (!DateTime.TryParse(card.Row2DateText, out var row2Date) ||
            !DateTime.TryParse(card.Row1DateText, out var row1Date) ||
            !DateTime.TryParse(card.Row3DateText, out var row3Date))
        {
            return false;
        }

        return row2Date < row1Date && row1Date < row3Date;
    }

    private static ThirdAnalysisCardVM CloneCard(ThirdAnalysisCardVM source)
    {
        return new ThirdAnalysisCardVM
        {
            Row1Pick3Digits = CopyDigits(source.Row1Pick3Digits),
            Row1Pick4Digits = CopyDigits(source.Row1Pick4Digits),
            Row1NextPick3Digits = CopyDigits(source.Row1NextPick3Digits),
            Row1CodingDigits = CopyDigits(source.Row1CodingDigits),
            Row1DateText = source.Row1DateText,
            Row1DrawIcon = source.Row1DrawIcon,
            Row2Pick3Digits = CopyDigits(source.Row2Pick3Digits),
            Row2Pick4Digits = CopyDigits(source.Row2Pick4Digits),
            Row2NextPick3Digits = CopyDigits(source.Row2NextPick3Digits),
            Row2CodingDigits = CopyDigits(source.Row2CodingDigits),
            Row2DateText = source.Row2DateText,
            Row2DrawIcon = source.Row2DrawIcon,
            Row3Pick3Digits = CopyDigits(source.Row3Pick3Digits),
            Row3Pick4Digits = CopyDigits(source.Row3Pick4Digits),
            Row3NextPick3Digits = CopyDigits(source.Row3NextPick3Digits),
            Row3CodingDigits = CopyDigits(source.Row3CodingDigits),
            Row3DateText = source.Row3DateText,
            Row3DrawIcon = source.Row3DrawIcon,
            Row4Pick3Digits = CopyDigits(source.Row4Pick3Digits),
            Row4Pick4Digits = CopyDigits(source.Row4Pick4Digits),
            Row4NextPick3Digits = CopyDigits(source.Row4NextPick3Digits),
            Row4CodingDigits = CopyDigits(source.Row4CodingDigits),
            Row4DateText = source.Row4DateText,
            Row4DrawIcon = source.Row4DrawIcon,
            Pick4MatchesRow1Row2 = source.Pick4MatchesRow1Row2,
            CodingMatchesRow1Row2 = source.CodingMatchesRow1Row2,
            Pick4MatchesRow3Row4 = source.Pick4MatchesRow3Row4,
            CodingMatchesRow3Row4 = source.CodingMatchesRow3Row4,
            AnalysisSummary = source.AnalysisSummary
        };
    }

    private static ObservableCollection<DigitVM> CopyDigits(IEnumerable<DigitVM> digits)
    {
        return new ObservableCollection<DigitVM>(
            digits.Select(d => new DigitVM
            {
                Value = d.Value,
                Bg = d.Bg is SolidColorBrush brush ? CreateFrozenBrush(brush.Color) : CreateFrozenBrush(Colors.White)
            }));
    }

    private ThirdAnalysisCardVM BuildStatusCard(string message)
    {
        var empty = CloneCard(GuideCard);
        empty.Row1DateText = message;
        empty.Row1DrawIcon = string.Empty;
        empty.Row1Pick3Digits = new ObservableCollection<DigitVM>();
        empty.Row1Pick4Digits = new ObservableCollection<DigitVM>();
        empty.Row1NextPick3Digits = new ObservableCollection<DigitVM>();
        empty.Row1CodingDigits = new ObservableCollection<DigitVM>();
        empty.Row2DateText = string.Empty;
        empty.Row2DrawIcon = string.Empty;
        empty.Row2Pick3Digits = new ObservableCollection<DigitVM>();
        empty.Row2Pick4Digits = new ObservableCollection<DigitVM>();
        empty.Row2NextPick3Digits = new ObservableCollection<DigitVM>();
        empty.Row2CodingDigits = new ObservableCollection<DigitVM>();
        empty.Row3DateText = string.Empty;
        empty.Row3DrawIcon = string.Empty;
        empty.Row3Pick3Digits = new ObservableCollection<DigitVM>();
        empty.Row3Pick4Digits = new ObservableCollection<DigitVM>();
        empty.Row3NextPick3Digits = new ObservableCollection<DigitVM>();
        empty.Row3CodingDigits = new ObservableCollection<DigitVM>();
        empty.Row4DateText = string.Empty;
        empty.Row4DrawIcon = string.Empty;
        empty.Row4Pick3Digits = new ObservableCollection<DigitVM>();
        empty.Row4Pick4Digits = new ObservableCollection<DigitVM>();
        empty.Row4NextPick3Digits = new ObservableCollection<DigitVM>();
        empty.Row4CodingDigits = new ObservableCollection<DigitVM>();
        empty.Pick4MatchesRow1Row2 = "0C";
        empty.CodingMatchesRow1Row2 = "0C";
        empty.Pick4MatchesRow3Row4 = "0C";
        empty.CodingMatchesRow3Row4 = "0C";
        empty.AnalysisSummary = message;
        return empty;
    }

    private ThirdAnalysisCardVM BuildEmptyResultCard()
    {
        return BuildStatusCard("Sin resultados");
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex <= 0 || ResultCards.Count == 0)
        {
            return;
        }

        _currentIndex--;
        CurrentResultCard = ResultCards[_currentIndex];
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex < 0 || _currentIndex >= ResultCards.Count - 1)
        {
            return;
        }

        _currentIndex++;
        CurrentResultCard = ResultCards[_currentIndex];
    }

    private void UpdateNavigationButtons()
    {
        if (PreviousButton == null || NextButton == null)
        {
            return;
        }

        if (IsLoading)
        {
            PreviousButton.IsEnabled = false;
            NextButton.IsEnabled = false;
            return;
        }

        PreviousButton.IsEnabled = _currentIndex > 0;
        NextButton.IsEnabled = _currentIndex >= 0 && _currentIndex < ResultCards.Count - 1;
    }





    private List<ThirdAnalysisCardVM> ApplyRow4NextPick3PositionFilter(IEnumerable<ThirdAnalysisCardVM> sourceCards)
    {
        var guideConnections = GetRow4NextPick3PositionConnections(GuideCard);
        bool guideHasConnections = guideConnections.Count > 0;
        var filteredCards = new List<ThirdAnalysisCardVM>();

        foreach (var card in sourceCards)
        {
            var cardConnections = GetRow4NextPick3PositionConnections(card);

            if (!guideHasConnections)
            {
                if (cardConnections.Count == 0)
                {
                    filteredCards.Add(card);
                }

                continue;
            }

            if (cardConnections.Any(connection => guideConnections.Contains(connection)))
            {
                filteredCards.Add(card);
            }
        }

        return filteredCards;
    }

    private static HashSet<string> GetRow4NextPick3PositionConnections(ThirdAnalysisCardVM card)
    {
        var connections = new HashSet<string>();
        string row4Pick3 = DigitsToString(card.Row4Pick3Digits);
        string row4Pick4 = DigitsToString(card.Row4Pick4Digits);
        string row4NextPick3 = DigitsToString(card.Row4NextPick3Digits);

        if (row4Pick3.Length != 3 || row4Pick4.Length != 4 || row4NextPick3.Length != 3)
        {
            return connections;
        }

        for (int nextIndex = 0; nextIndex < 3; nextIndex++)
        {
            char nextDigit = row4NextPick3[nextIndex];

            for (int pick3Index = 0; pick3Index < 3; pick3Index++)
            {
                if (row4Pick3[pick3Index] == nextDigit)
                {
                    connections.Add($"N{nextIndex}-P3{pick3Index}");
                }
            }

            for (int pick4Index = 0; pick4Index < 4; pick4Index++)
            {
                if (row4Pick4[pick4Index] == nextDigit)
                {
                    connections.Add($"N{nextIndex}-P4{pick4Index}");
                }
            }
        }

        return connections;
    }
    private List<ThirdAnalysisCardVM> ExcludeGuideCard(IEnumerable<ThirdAnalysisCardVM> sourceCards)
    {
        return sourceCards
            .Where(card => !IsSameAsGuideCard(card))
            .ToList();
    }

    private bool IsSameAsGuideCard(ThirdAnalysisCardVM card)
    {
        return RowsAreEqual(card.Row1DateText, card.Row1DrawIcon, card.Row1Pick3Digits, card.Row1Pick4Digits, card.Row1NextPick3Digits, card.Row1CodingDigits,
                            GuideCard.Row1DateText, GuideCard.Row1DrawIcon, GuideCard.Row1Pick3Digits, GuideCard.Row1Pick4Digits, GuideCard.Row1NextPick3Digits, GuideCard.Row1CodingDigits)
            && RowsAreEqual(card.Row2DateText, card.Row2DrawIcon, card.Row2Pick3Digits, card.Row2Pick4Digits, card.Row2NextPick3Digits, card.Row2CodingDigits,
                            GuideCard.Row2DateText, GuideCard.Row2DrawIcon, GuideCard.Row2Pick3Digits, GuideCard.Row2Pick4Digits, GuideCard.Row2NextPick3Digits, GuideCard.Row2CodingDigits)
            && RowsAreEqual(card.Row3DateText, card.Row3DrawIcon, card.Row3Pick3Digits, card.Row3Pick4Digits, card.Row3NextPick3Digits, card.Row3CodingDigits,
                            GuideCard.Row3DateText, GuideCard.Row3DrawIcon, GuideCard.Row3Pick3Digits, GuideCard.Row3Pick4Digits, GuideCard.Row3NextPick3Digits, GuideCard.Row3CodingDigits)
            && RowsAreEqual(card.Row4DateText, card.Row4DrawIcon, card.Row4Pick3Digits, card.Row4Pick4Digits, card.Row4NextPick3Digits, card.Row4CodingDigits,
                            GuideCard.Row4DateText, GuideCard.Row4DrawIcon, GuideCard.Row4Pick3Digits, GuideCard.Row4Pick4Digits, GuideCard.Row4NextPick3Digits, GuideCard.Row4CodingDigits);
    }

    private static bool RowsAreEqual(
        string date1,
        string icon1,
        IEnumerable<DigitVM> pick31,
        IEnumerable<DigitVM> pick41,
        IEnumerable<DigitVM> nextPick31,
        IEnumerable<DigitVM> coding1,
        string date2,
        string icon2,
        IEnumerable<DigitVM> pick32,
        IEnumerable<DigitVM> pick42,
        IEnumerable<DigitVM> nextPick32,
        IEnumerable<DigitVM> coding2)
    {
        return string.Equals(date1, date2, StringComparison.Ordinal)
            && string.Equals(icon1, icon2, StringComparison.Ordinal)
            && string.Equals(DigitsToString(pick31), DigitsToString(pick32), StringComparison.Ordinal)
            && string.Equals(DigitsToString(pick41), DigitsToString(pick42), StringComparison.Ordinal)
            && string.Equals(DigitsToString(nextPick31), DigitsToString(nextPick32), StringComparison.Ordinal)
            && string.Equals(DigitsToString(coding1), DigitsToString(coding2), StringComparison.Ordinal);
    }
    private List<ThirdAnalysisCardVM> ApplyCodingRowMatchFilter(IEnumerable<ThirdAnalysisCardVM> sourceCards)
    {
        var filteredCards = new List<ThirdAnalysisCardVM>();

        foreach (var card in sourceCards)
        {
            int row1Matches = CountMatches(DigitsToString(card.Row1CodingDigits), DigitsToString(GuideCard.Row1CodingDigits));
            int row2Matches = CountMatches(DigitsToString(card.Row2CodingDigits), DigitsToString(GuideCard.Row2CodingDigits));
            int row3Matches = CountMatches(DigitsToString(card.Row3CodingDigits), DigitsToString(GuideCard.Row3CodingDigits));
            int row4Matches = CountMatches(DigitsToString(card.Row4CodingDigits), DigitsToString(GuideCard.Row4CodingDigits));

            if (row1Matches >= 5 || row2Matches >= 5 || row3Matches >= 5 || row4Matches >= 5)
            {
                filteredCards.Add(card);
            }
        }

        return filteredCards;
    }
    private List<ThirdAnalysisCardVM> ApplyMatchGuideFilter(IEnumerable<ThirdAnalysisCardVM> sourceCards)
    {
        var filteredCards = new List<ThirdAnalysisCardVM>();
        var guideSourcesRow3ToRow4FirstTwo = GetGuideSourcePositionsToPick4Half(
            DigitsToString(GuideCard.Row3Pick3Digits),
            DigitsToString(GuideCard.Row4Pick4Digits),
            useFirstTwo: true);

        var guideSourcesRow3ToRow4LastTwo = GetGuideSourcePositionsToPick4Half(
            DigitsToString(GuideCard.Row3Pick3Digits),
            DigitsToString(GuideCard.Row4Pick4Digits),
            useFirstTwo: false);

        var guideSourcesRow4ToRow3FirstTwo = GetGuideSourcePositionsToPick4Half(
            DigitsToString(GuideCard.Row4Pick3Digits),
            DigitsToString(GuideCard.Row3Pick4Digits),
            useFirstTwo: true);

        var guideSourcesRow4ToRow3LastTwo = GetGuideSourcePositionsToPick4Half(
            DigitsToString(GuideCard.Row4Pick3Digits),
            DigitsToString(GuideCard.Row3Pick4Digits),
            useFirstTwo: false);

        var guideSourcesRow1ToRow2 = GetGuideSourcePositionsToAny(
            DigitsToString(GuideCard.Row1Pick3Digits),
            DigitsToString(GuideCard.Row2Pick4Digits));

        var guideSourcesRow2ToRow1 = GetGuideSourcePositionsToAny(
            DigitsToString(GuideCard.Row2Pick3Digits),
            DigitsToString(GuideCard.Row1Pick4Digits));

        string guiaRow1NextPick3 = DigitsToString(GuideCard.Row1NextPick3Digits);
        string guiaRow2NextPick3 = DigitsToString(GuideCard.Row2NextPick3Digits);
        int escenario = GetNextPick3Scenario(guiaRow1NextPick3, guiaRow2NextPick3);

        if (escenario < 0)
        {
            return filteredCards;
        }

        foreach (var card in sourceCards)
        {
            bool matchesGuide = true;

            var cardSourcesRow3ToRow4FirstTwo = GetGuideSourcePositionsToPick4Half(
                DigitsToString(card.Row3Pick3Digits),
                DigitsToString(card.Row4Pick4Digits),
                useFirstTwo: true);

            var cardSourcesRow3ToRow4LastTwo = GetGuideSourcePositionsToPick4Half(
                DigitsToString(card.Row3Pick3Digits),
                DigitsToString(card.Row4Pick4Digits),
                useFirstTwo: false);

            var cardSourcesRow4ToRow3FirstTwo = GetGuideSourcePositionsToPick4Half(
                DigitsToString(card.Row4Pick3Digits),
                DigitsToString(card.Row3Pick4Digits),
                useFirstTwo: true);

            var cardSourcesRow4ToRow3LastTwo = GetGuideSourcePositionsToPick4Half(
                DigitsToString(card.Row4Pick3Digits),
                DigitsToString(card.Row3Pick4Digits),
                useFirstTwo: false);

            bool guideHasRow3ToRow4FirstTwo = guideSourcesRow3ToRow4FirstTwo.Count > 0;
            bool guideHasRow3ToRow4LastTwo = guideSourcesRow3ToRow4LastTwo.Count > 0;
            bool guideHasRow4ToRow3FirstTwo = guideSourcesRow4ToRow3FirstTwo.Count > 0;
            bool guideHasRow4ToRow3LastTwo = guideSourcesRow4ToRow3LastTwo.Count > 0;
            bool cardHasRow3ToRow4FirstTwo = cardSourcesRow3ToRow4FirstTwo.Count > 0;
            bool cardHasRow3ToRow4LastTwo = cardSourcesRow3ToRow4LastTwo.Count > 0;
            bool cardHasRow4ToRow3FirstTwo = cardSourcesRow4ToRow3FirstTwo.Count > 0;
            bool cardHasRow4ToRow3LastTwo = cardSourcesRow4ToRow3LastTwo.Count > 0;

            if ((guideHasRow3ToRow4FirstTwo && !cardHasRow3ToRow4FirstTwo) ||
                (!guideHasRow3ToRow4FirstTwo && cardHasRow3ToRow4FirstTwo) ||
                (guideHasRow3ToRow4LastTwo && !cardHasRow3ToRow4LastTwo) ||
                (!guideHasRow3ToRow4LastTwo && cardHasRow3ToRow4LastTwo) ||
                (guideHasRow4ToRow3FirstTwo && !cardHasRow4ToRow3FirstTwo) ||
                (!guideHasRow4ToRow3FirstTwo && cardHasRow4ToRow3FirstTwo) ||
                (guideHasRow4ToRow3LastTwo && !cardHasRow4ToRow3LastTwo) ||
                (!guideHasRow4ToRow3LastTwo && cardHasRow4ToRow3LastTwo))
            {
                matchesGuide = false;
            }

            if (matchesGuide && guideHasRow3ToRow4FirstTwo)
            {
                matchesGuide &= MatchesGuideSourceRuleForPick4Half(
                    DigitsToString(card.Row3Pick3Digits),
                    DigitsToString(card.Row4Pick4Digits),
                    guideSourcesRow3ToRow4FirstTwo,
                    useFirstTwo: true);
            }

            if (matchesGuide && guideHasRow3ToRow4LastTwo)
            {
                matchesGuide &= MatchesGuideSourceRuleForPick4Half(
                    DigitsToString(card.Row3Pick3Digits),
                    DigitsToString(card.Row4Pick4Digits),
                    guideSourcesRow3ToRow4LastTwo,
                    useFirstTwo: false);
            }

            if (matchesGuide && guideHasRow4ToRow3FirstTwo)
            {
                matchesGuide &= MatchesGuideSourceRuleForPick4Half(
                    DigitsToString(card.Row4Pick3Digits),
                    DigitsToString(card.Row3Pick4Digits),
                    guideSourcesRow4ToRow3FirstTwo,
                    useFirstTwo: true);
            }

            if (matchesGuide && guideHasRow4ToRow3LastTwo)
            {
                matchesGuide &= MatchesGuideSourceRuleForPick4Half(
                    DigitsToString(card.Row4Pick3Digits),
                    DigitsToString(card.Row3Pick4Digits),
                    guideSourcesRow4ToRow3LastTwo,
                    useFirstTwo: false);
            }

            if (matchesGuide)
            {
                var cardSourcesRow1ToRow2 = GetGuideSourcePositionsToAny(
                    DigitsToString(card.Row1Pick3Digits),
                    DigitsToString(card.Row2Pick4Digits));

                var cardSourcesRow2ToRow1 = GetGuideSourcePositionsToAny(
                    DigitsToString(card.Row2Pick3Digits),
                    DigitsToString(card.Row1Pick4Digits));

                bool guideHasRow1ToRow2 = guideSourcesRow1ToRow2.Count > 0;
                bool guideHasRow2ToRow1 = guideSourcesRow2ToRow1.Count > 0;
                bool cardHasRow1ToRow2 = cardSourcesRow1ToRow2.Count > 0;
                bool cardHasRow2ToRow1 = cardSourcesRow2ToRow1.Count > 0;
                bool cardHasAnyRow1ToRow2 = HasAnyConnectionPick3ToPick4(
                    DigitsToString(card.Row1Pick3Digits),
                    DigitsToString(card.Row2Pick4Digits));
                bool cardHasAnyRow2ToRow1 = HasAnyConnectionPick3ToPick4(
                    DigitsToString(card.Row2Pick3Digits),
                    DigitsToString(card.Row1Pick4Digits));

                if ((guideHasRow1ToRow2 && !cardHasRow1ToRow2) ||
                    (!guideHasRow1ToRow2 && cardHasAnyRow1ToRow2) ||
                    (guideHasRow2ToRow1 && !cardHasRow2ToRow1) ||
                    (!guideHasRow2ToRow1 && cardHasAnyRow2ToRow1))
                {
                    matchesGuide = false;
                }

                if (matchesGuide && guideHasRow1ToRow2)
                {
                    matchesGuide &= MatchesGuideSourceRuleAny(
                        DigitsToString(card.Row1Pick3Digits),
                        DigitsToString(card.Row2Pick4Digits),
                        guideSourcesRow1ToRow2);
                }

                if (matchesGuide && guideHasRow2ToRow1)
                {
                    matchesGuide &= MatchesGuideSourceRuleAny(
                        DigitsToString(card.Row2Pick3Digits),
                        DigitsToString(card.Row1Pick4Digits),
                        guideSourcesRow2ToRow1);
                }
            }

            if (matchesGuide)
            {
                string cardRow1NextPick3 = DigitsToString(card.Row1NextPick3Digits);
                string cardRow2NextPick3 = DigitsToString(card.Row2NextPick3Digits);
                matchesGuide &= MatchesNextPick3Scenario(cardRow1NextPick3, cardRow2NextPick3, escenario);
            }

            if (matchesGuide)
            {
                filteredCards.Add(card);
            }
        }

        return filteredCards;
    }

    private static int GetNextPick3Scenario(string row1NextPick3, string row2NextPick3)
    {
        if (string.IsNullOrWhiteSpace(row1NextPick3) || row1NextPick3.Length != 3 ||
            string.IsNullOrWhiteSpace(row2NextPick3) || row2NextPick3.Length != 3)
        {
            return -1;
        }

        bool guiaPos1Conecta = row2NextPick3.Contains(row1NextPick3[0]);
        bool guiaPos2Conecta = row2NextPick3.Contains(row1NextPick3[1]);
        bool guiaPos3Conecta = row2NextPick3.Contains(row1NextPick3[2]);

        if (!guiaPos1Conecta && !guiaPos2Conecta && !guiaPos3Conecta) return 0;
        if (guiaPos1Conecta && !guiaPos2Conecta && !guiaPos3Conecta) return 1;
        if (!guiaPos1Conecta && guiaPos2Conecta && !guiaPos3Conecta) return 2;
        if (!guiaPos1Conecta && !guiaPos2Conecta && guiaPos3Conecta) return 3;
        return -1;
    }

    private static bool MatchesNextPick3Scenario(string row1NextPick3, string row2NextPick3, int escenario)
    {
        if (string.IsNullOrWhiteSpace(row1NextPick3) || row1NextPick3.Length != 3 ||
            string.IsNullOrWhiteSpace(row2NextPick3) || row2NextPick3.Length != 3)
        {
            return false;
        }

        bool cardPos1Conecta = row2NextPick3.Contains(row1NextPick3[0]);
        bool cardPos2Conecta = row2NextPick3.Contains(row1NextPick3[1]);
        bool cardPos3Conecta = row2NextPick3.Contains(row1NextPick3[2]);
        int totalConexiones = (cardPos1Conecta ? 1 : 0) + (cardPos2Conecta ? 1 : 0) + (cardPos3Conecta ? 1 : 0);

        return escenario switch
        {
            0 => totalConexiones == 0,
            1 => totalConexiones == 1 && cardPos1Conecta,
            2 => totalConexiones == 1 && (cardPos2Conecta || cardPos3Conecta),
            3 => totalConexiones == 1 && (cardPos2Conecta || cardPos3Conecta),
            _ => false
        };
    }

    private static List<int> GetGuideSourcePositionsToPick4Half(string pick3, string pick4, bool useFirstTwo)
    {
        var sourcePositions = new List<int>();

        if (string.IsNullOrWhiteSpace(pick3) || pick3.Length != 3 ||
            string.IsNullOrWhiteSpace(pick4) || pick4.Length < 4)
        {
            return sourcePositions;
        }

        int firstTargetIndex = useFirstTwo ? 0 : 2;
        int secondTargetIndex = useFirstTwo ? 1 : 3;

        for (int pick3Index = 0; pick3Index < 3; pick3Index++)
        {
            var sourceDigit = pick3[pick3Index];
            if (!char.IsDigit(sourceDigit))
            {
                continue;
            }

            if (pick4[firstTargetIndex] == sourceDigit || pick4[secondTargetIndex] == sourceDigit)
            {
                sourcePositions.Add(pick3Index);
            }
        }

        return sourcePositions;
    }

    private static bool MatchesGuideSourceRuleForPick4Half(string pick3, string pick4, List<int> guideSourcePositions, bool useFirstTwo)
    {
        if (guideSourcePositions == null || guideSourcePositions.Count == 0)
        {
            return true;
        }

        var candidateSourcePositions = GetGuideSourcePositionsToPick4Half(pick3, pick4, useFirstTwo);

        if (guideSourcePositions.Count == 1)
        {
            return candidateSourcePositions.Count == 1 && candidateSourcePositions[0] == guideSourcePositions[0];
        }

        return candidateSourcePositions.Count == guideSourcePositions.Count &&
               candidateSourcePositions.All(guideSourcePositions.Contains);
    }

    private static List<int> GetGuideSourcePositionsToAny(string pick3, string pick4)
    {
        var sourcePositions = new List<int>();

        if (string.IsNullOrWhiteSpace(pick3) || pick3.Length != 3 ||
            string.IsNullOrWhiteSpace(pick4) || pick4.Length != 4)
        {
            return sourcePositions;
        }

        for (int pick3Index = 0; pick3Index < 3; pick3Index++)
        {
            var sourceDigit = pick3[pick3Index];
            if (!char.IsDigit(sourceDigit))
            {
                continue;
            }

            if (pick4.Contains(sourceDigit))
            {
                sourcePositions.Add(pick3Index);
            }
        }

        return sourcePositions;
    }

    private static bool MatchesGuideSourceRuleAny(string pick3, string pick4, List<int> guideSourcePositions)
    {
        if (guideSourcePositions == null || guideSourcePositions.Count == 0)
        {
            return true;
        }

        var candidateSourcePositions = GetGuideSourcePositionsToAny(pick3, pick4);

        if (guideSourcePositions.Count == 1)
        {
            return candidateSourcePositions.Count == 1 && candidateSourcePositions[0] == guideSourcePositions[0];
        }

        return candidateSourcePositions.Count == guideSourcePositions.Count &&
               candidateSourcePositions.All(guideSourcePositions.Contains);
    }

    private static bool HasAnyConnectionPick3ToPick4(string pick3, string pick4)
    {
        if (string.IsNullOrWhiteSpace(pick3) || pick3.Length != 3 ||
            string.IsNullOrWhiteSpace(pick4) || pick4.Length != 4)
        {
            return false;
        }

        for (int pick3Index = 0; pick3Index < 3; pick3Index++)
        {
            var sourceDigit = pick3[pick3Index];
            if (!char.IsDigit(sourceDigit))
            {
                continue;
            }

            if (pick4.Contains(sourceDigit))
            {
                return true;
            }
        }

        return false;
    }
    private List<(string Original, string Compatible)> FindFilteredPairs(string fullNumberOriginal, List<string> allFullNumbers, (string First, string Second) guidePair)
    {
        var result = new List<(string, string)>();

        if (string.IsNullOrWhiteSpace(fullNumberOriginal) || fullNumberOriginal.Length != 10)
        {
            return result;
        }

        string originalP3 = fullNumberOriginal.Substring(0, 3);
        string originalP4 = fullNumberOriginal.Substring(3, 4);

        char? originalRepeatedDigit = null;
        int originalPosP3 = -1;
        int originalPosP4 = -1;

        for (int i = 0; i < 3; i++)
        {
            char digit = originalP3[i];
            int posInP4 = originalP4.IndexOf(digit, StringComparison.Ordinal);
            if (posInP4 >= 0)
            {
                originalRepeatedDigit = digit;
                originalPosP3 = i;
                originalPosP4 = posInP4;
                break;
            }
        }

        if (!originalRepeatedDigit.HasValue)
        {
            return result;
        }

        bool[] guidePattern = new bool[3];
        string guideP3_1 = guidePair.First.Substring(0, 3);
        string guideP3_2 = guidePair.Second.Substring(0, 3);
        for (int i = 0; i < 3; i++)
        {
            guidePattern[i] = guideP3_1[i] == guideP3_2[i];
        }

        foreach (string candidate in allFullNumbers)
        {
            if (candidate == fullNumberOriginal || string.IsNullOrWhiteSpace(candidate) || candidate.Length != 10)
            {
                continue;
            }

            string candidateP3 = candidate.Substring(0, 3);
            string candidateP4 = candidate.Substring(3, 4);

            char? candidateRepeatedDigit = null;
            int candidatePosP3 = -1;
            int candidatePosP4 = -1;

            for (int i = 0; i < 3; i++)
            {
                char digit = candidateP3[i];
                int posInP4 = candidateP4.IndexOf(digit, StringComparison.Ordinal);
                if (posInP4 >= 0)
                {
                    candidateRepeatedDigit = digit;
                    candidatePosP3 = i;
                    candidatePosP4 = posInP4;
                    break;
                }
            }

            if (!candidateRepeatedDigit.HasValue || originalPosP3 != candidatePosP3 || originalPosP4 != candidatePosP4)
            {
                continue;
            }

            bool candidatePatternValid = true;
            for (int i = 0; i < 3; i++)
            {
                bool positionsAreEqual = originalP3[i] == candidateP3[i];
                if (guidePattern[i] != positionsAreEqual)
                {
                    candidatePatternValid = false;
                    break;
                }
            }

            if (candidatePatternValid)
            {
                result.Add((fullNumberOriginal, candidate));
            }
        }

        return result;
    }

    private List<(string First, string Second, string Third, string Fourth)> FormCuartetosFromPairs(List<(string Original, string Compatible)> allPairs)
    {
        var allValidCuartetos = new List<(string, string, string, string)>();
        var groupsByOriginal = new Dictionary<string, List<string>>();

        foreach (var pair in allPairs)
        {
            if (!groupsByOriginal.ContainsKey(pair.Original))
            {
                groupsByOriginal[pair.Original] = new List<string>();
            }

            if (!groupsByOriginal[pair.Original].Contains(pair.Compatible))
            {
                groupsByOriginal[pair.Original].Add(pair.Compatible);
            }
        }

        foreach (var group in groupsByOriginal)
        {
            string original = group.Key;
            List<string> compatibles = group.Value;

            if (compatibles.Count < 2)
            {
                continue;
            }

            for (int i = 0; i < compatibles.Count - 1; i++)
            {
                for (int j = i + 1; j < compatibles.Count; j++)
                {
                    string compatible1 = compatibles[i];
                    string compatible2 = compatibles[j];

                    string pick3_1 = original.Substring(0, 3);
                    string pick3_2 = compatible1.Substring(0, 3);
                    string pick3_3 = compatible2.Substring(0, 3);

                    bool isValid = true;
                    var digitsFromNonEqualPositions = new List<char>();

                    for (int pos = 0; pos < 3; pos++)
                    {
                        if (!(pick3_1[pos] == pick3_2[pos] && pick3_2[pos] == pick3_3[pos]))
                        {
                            digitsFromNonEqualPositions.Add(pick3_1[pos]);
                            digitsFromNonEqualPositions.Add(pick3_2[pos]);
                            digitsFromNonEqualPositions.Add(pick3_3[pos]);
                        }
                    }

                    if (digitsFromNonEqualPositions.Count > 0)
                    {
                        var uniqueDigits = new HashSet<char>(digitsFromNonEqualPositions);
                        if (uniqueDigits.Count != digitsFromNonEqualPositions.Count)
                        {
                            isValid = false;
                        }
                    }

                    if (!isValid)
                    {
                        continue;
                    }

                    string nextPick3 = compatible2.Substring(7, 3);
                    if (nextPick3.Length != 3 || nextPick3.Distinct().Count() < 3)
                    {
                        continue;
                    }

                    allValidCuartetos.Add((compatible1, compatible2, original, compatible2));
                }
            }
        }

        return allValidCuartetos;
    }

    private List<(string First, string Second, string Third, string Fourth)> FilterCuartetosByCounters(
        AnalysisMode mode,
        List<(string First, string Second, string Third, string Fourth)> cuartetos,
        string refPick4MatchesRow1Row2,
        string refCodingMatchesRow1Row2,
        string refPick4MatchesRow3Row4,
        string refCodingMatchesRow3Row4)
    {
        var filteredCuartetos = new List<(string, string, string, string)>();

        foreach (var cuarteto in cuartetos)
        {
            string row1Full = cuarteto.First;
            string row2Full = cuarteto.Second;
            string row3Full = cuarteto.Third;
            string row4Full = cuarteto.Fourth;

            string row1Pick4 = row1Full.Substring(3, 4);
            string row2Pick4 = row2Full.Substring(3, 4);
            string row3Pick4 = row3Full.Substring(3, 4);
            string row4Pick4 = row4Full.Substring(3, 4);

            string row1Coding = CalculateCoding(row1Full.Substring(0, 3), row1Pick4);
            string row2Coding = CalculateCoding(row2Full.Substring(0, 3), row2Pick4);
            string row3Coding = CalculateCoding(row3Full.Substring(0, 3), row3Pick4);
            string row4Coding = CalculateCoding(row4Full.Substring(0, 3), row4Pick4);

            string cuartetoPick4MatchesRow1Row2 = $"{CountMatches(row1Pick4, row2Pick4)}C";
            string cuartetoCodingMatchesRow1Row2 = $"{CountMatches(row1Coding, row2Coding)}C";
            string cuartetoPick4MatchesRow3Row4 = $"{CountMatches(row3Pick4, row4Pick4)}C";
            string cuartetoCodingMatchesRow3Row4 = $"{CountMatches(row3Coding, row4Coding)}C";

            switch (mode)
            {
                case AnalysisMode.Opcion1:
                    if (cuartetoPick4MatchesRow1Row2 == NormalizeCounter(refPick4MatchesRow1Row2) &&
                        cuartetoCodingMatchesRow1Row2 == NormalizeCounter(refCodingMatchesRow1Row2) &&
                        cuartetoPick4MatchesRow3Row4 == NormalizeCounter(refPick4MatchesRow3Row4) &&
                        cuartetoCodingMatchesRow3Row4 == NormalizeCounter(refCodingMatchesRow3Row4))
                    {
                        filteredCuartetos.Add(cuarteto);
                    }
                    break;

                case AnalysisMode.Opcion2:
                    bool pick4CountersMatch =
                        cuartetoPick4MatchesRow1Row2 == NormalizeCounter(refPick4MatchesRow1Row2) &&
                        cuartetoPick4MatchesRow3Row4 == NormalizeCounter(refPick4MatchesRow3Row4);
                    bool codingCountersXor =
                        (cuartetoCodingMatchesRow1Row2 == NormalizeCounter(refCodingMatchesRow1Row2)) ^
                        (cuartetoCodingMatchesRow3Row4 == NormalizeCounter(refCodingMatchesRow3Row4));
                    if (pick4CountersMatch && codingCountersXor)
                    {
                        filteredCuartetos.Add(cuarteto);
                    }
                    break;
            }
        }

        return filteredCuartetos;
    }

    private static List<CuartetoOccurrence> ExpandCuartetosWithOccurrences(
        List<(string First, string Second, string Third, string Fourth)> cuartetos,
        Dictionary<string, List<FilteredCodificacion>> codificacionesByFullNumber)
    {
        var expanded = new List<CuartetoOccurrence>();

        foreach (var cuarteto in cuartetos)
        {
            if (!codificacionesByFullNumber.TryGetValue(cuarteto.First, out var row1List) ||
                !codificacionesByFullNumber.TryGetValue(cuarteto.Second, out var row2List) ||
                !codificacionesByFullNumber.TryGetValue(cuarteto.Third, out var row3List))
            {
                continue;
            }

            foreach (var row1 in row1List)
            {
                foreach (var row2 in row2List)
                {
                    foreach (var row3 in row3List)
                    {
                        expanded.Add(new CuartetoOccurrence(row1, row2, row3, row2));
                    }
                }
            }
        }

        return expanded;
    }

    private static ThirdAnalysisCardVM BuildCardFromCuarteto(CuartetoOccurrence cuarteto)
    {
        var card = new ThirdAnalysisCardVM
        {
            Row1DateText = cuarteto.Row1.Date.ToString("yyyy-MM-dd"),
            Row1DrawIcon = DrawTimeToIcon(cuarteto.Row1.DrawTime),
            Row1Pick3Digits = BuildDigitsFromNumber(cuarteto.Row1.Pick3),
            Row1Pick4Digits = BuildDigitsFromNumber(cuarteto.Row1.Pick4),
            Row1NextPick3Digits = BuildDigitsFromNumber(cuarteto.Row1.NextPick3),
            Row1CodingDigits = BuildDigitsFromNumber(CalculateCoding(cuarteto.Row1.Pick3, cuarteto.Row1.Pick4)),

            Row2DateText = cuarteto.Row2.Date.ToString("yyyy-MM-dd"),
            Row2DrawIcon = DrawTimeToIcon(cuarteto.Row2.DrawTime),
            Row2Pick3Digits = BuildDigitsFromNumber(cuarteto.Row2.Pick3),
            Row2Pick4Digits = BuildDigitsFromNumber(cuarteto.Row2.Pick4),
            Row2NextPick3Digits = BuildDigitsFromNumber(cuarteto.Row2.NextPick3),
            Row2CodingDigits = BuildDigitsFromNumber(CalculateCoding(cuarteto.Row2.Pick3, cuarteto.Row2.Pick4)),

            Row3DateText = cuarteto.Row3.Date.ToString("yyyy-MM-dd"),
            Row3DrawIcon = DrawTimeToIcon(cuarteto.Row3.DrawTime),
            Row3Pick3Digits = BuildDigitsFromNumber(cuarteto.Row3.Pick3),
            Row3Pick4Digits = BuildDigitsFromNumber(cuarteto.Row3.Pick4),
            Row3NextPick3Digits = BuildDigitsFromNumber(cuarteto.Row3.NextPick3),
            Row3CodingDigits = BuildDigitsFromNumber(CalculateCoding(cuarteto.Row3.Pick3, cuarteto.Row3.Pick4))
        };

        card.Row4DateText = card.Row2DateText;
        card.Row4DrawIcon = card.Row2DrawIcon;
        card.Row4Pick3Digits = CopyDigitCollection(card.Row2Pick3Digits);
        card.Row4Pick4Digits = CopyDigitCollection(card.Row2Pick4Digits);
        card.Row4NextPick3Digits = CopyDigitCollection(card.Row2NextPick3Digits);
        card.Row4CodingDigits = CopyDigitCollection(card.Row2CodingDigits);

        ProcessCodingColors(card);
        return card;
    }

    private static string NormalizeCounter(string? value)
        => string.IsNullOrWhiteSpace(value) ? "0C" : value.Trim();

    private static int CountMatches(string str1, string str2)
    {
        int matches = 0;
        foreach (char digit in str1)
        {
            if (str2.Contains(digit))
            {
                matches++;
            }
        }

        return matches;
    }

    private static string DrawTimeToIcon(string drawTime)
    {
        return drawTime switch
        {
            "M" => "\u2600\uFE0F",
            "E" => "\uD83C\uDF19",
            _ => string.Empty
        };
    }

    private static string CalculateCoding(string pick3, string pick4)
    {
        return new string((pick3 + pick4)
            .Where(char.IsDigit)
            .Distinct()
            .OrderBy(c => c)
            .ToArray());
    }

    private static ObservableCollection<DigitVM> BuildDigitsFromNumber(string number)
    {
        var digits = new ObservableCollection<DigitVM>();
        if (string.IsNullOrWhiteSpace(number))
        {
            return digits;
        }

        foreach (char digit in number)
        {
            digits.Add(new DigitVM
            {
                Value = digit.ToString(),
                Bg = CreateFrozenBrush(Colors.White)
            });
        }

        return digits;
    }

    private static string DigitsToString(IEnumerable<DigitVM> digits)
        => string.Concat(digits.Select(d => d.Value));

    private static void ProcessCodingColors(ThirdAnalysisCardVM card)
    {
        int codingMatches1 = ColorRepeatedDigitsInCollections(card.Row1CodingDigits, card.Row2CodingDigits, Colors.LightPink);
        card.CodingMatchesRow1Row2 = $"{codingMatches1}C";

        int pick4Matches1 = ColorRepeatedDigitsInCollections(card.Row1Pick4Digits, card.Row2Pick4Digits, Colors.LightBlue);
        card.Pick4MatchesRow1Row2 = $"{pick4Matches1}C";

        int codingMatches2 = ColorRepeatedDigitsInCollections(card.Row3CodingDigits, card.Row4CodingDigits, Colors.LightPink);
        card.CodingMatchesRow3Row4 = $"{codingMatches2}C";

        int pick4Matches2 = ColorRepeatedDigitsInCollections(card.Row3Pick4Digits, card.Row4Pick4Digits, Colors.LightBlue);
        card.Pick4MatchesRow3Row4 = $"{pick4Matches2}C";

        ColorRepeatedDigitInRow4(card);
    }

    private static void ColorRepeatedDigitInRow4(ThirdAnalysisCardVM card)
    {
        var repeatedDigits = card.Row4Pick3Digits.Select(d => d.Value).Intersect(card.Row4Pick4Digits.Select(d => d.Value)).ToList();
        if (repeatedDigits.Count != 1)
        {
            return;
        }

        string repeatedDigit = repeatedDigits[0];
        if (!card.Row4NextPick3Digits.Select(d => d.Value).Contains(repeatedDigit))
        {
            return;
        }

        foreach (var digitVM in card.Row4Pick3Digits.Where(d => d.Value == repeatedDigit))
        {
            digitVM.Bg = CreateFrozenBrush(Colors.LightBlue);
        }

        foreach (var digitVM in card.Row4Pick4Digits.Where(d => d.Value == repeatedDigit))
        {
            digitVM.Bg = CreateFrozenBrush(Colors.LightBlue);
        }

        foreach (var digitVM in card.Row4NextPick3Digits.Where(d => d.Value == repeatedDigit))
        {
            digitVM.Bg = CreateFrozenBrush(Colors.LightBlue);
        }
    }

    private static int ColorRepeatedDigitsInCollections(ObservableCollection<DigitVM> collection1, ObservableCollection<DigitVM> collection2, Color highlightColor)
    {
        var repeatedValues = collection1.Select(d => d.Value).Intersect(collection2.Select(d => d.Value)).ToList();
        ColorRepeatedDigits(collection1, repeatedValues, highlightColor);
        ColorRepeatedDigits(collection2, repeatedValues, highlightColor);
        return repeatedValues.Count;
    }

    private static void ColorRepeatedDigits(ObservableCollection<DigitVM> collection, List<string> repeatedValues, Color highlightColor)
    {
        foreach (var digitVM in collection)
        {
            digitVM.Bg = repeatedValues.Contains(digitVM.Value)
                ? CreateFrozenBrush(highlightColor)
                : CreateFrozenBrush(Colors.White);
        }
    }

    private static ObservableCollection<DigitVM> CopyDigitCollection(ObservableCollection<DigitVM> source)
    {
        var copy = new ObservableCollection<DigitVM>();
        foreach (var digit in source)
        {
            copy.Add(new DigitVM
            {
                Value = digit.Value,
                Bg = CreateFrozenBrush(Colors.White)
            });
        }

        return copy;
    }

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        if (brush.CanFreeze)
        {
            brush.Freeze();
        }

        return brush;
    }

    private void AnalysisCard_Loaded(object sender, RoutedEventArgs e)
    {
        RedrawAnalysisCardConnections(sender as FrameworkElement);
    }

    private void AnalysisCard_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        RedrawAnalysisCardConnections(sender as FrameworkElement);
    }

    private void RedrawAnalysisCardConnections(FrameworkElement? root)
    {
        if (root == null)
        {
            return;
        }

        root.Dispatcher.BeginInvoke(new Action(() =>
        {
            ConnectPick3Digits(root, "Row1Pick3Items", "Row2Pick3Items", "Pick3LinksCanvas");
            ConnectPick3Digits(root, "Row1NextPick3Items", "Row2NextPick3Items", "NextPick3LinksCanvas");
            ConnectPick3Digits(root, "Row3Pick3Items", "Row4Pick3Items", "Pick3LinksCanvas", false);
            ConnectPick3Digits(root, "Row3NextPick3Items", "Row4NextPick3Items", "NextPick3LinksCanvas", false);
            ConnectPick3ToPick4Digits(root, "Row3Pick3Items", "Row4Pick4Items", "Pick3Row3ToPick4Row4LinksCanvas");
            ConnectPick3ToPick4Digits(root, "Row4Pick3Items", "Row3Pick4Items", "Pick3Row4ToPick4Row3LinksCanvas");
            ConnectPick3ToPick4DigitsWithRedLine(root, "Row1Pick3Items", "Row2Pick4Items", "Pick3Row1ToPick4Row2LinksCanvas");
            ConnectPick3ToPick4DigitsWithRedLine(root, "Row2Pick3Items", "Row1Pick4Items", "Pick3Row2ToPick4Row1LinksCanvas");
        }), DispatcherPriority.Loaded);
    }


    private void ConnectPick3Digits(FrameworkElement root, string topItemsControlName, string bottomItemsControlName, string canvasName, bool clearCanvas = true)
    {
        var topItemsControl = FindVisualChild<ItemsControl>(root, topItemsControlName);
        var bottomItemsControl = FindVisualChild<ItemsControl>(root, bottomItemsControlName);
        var canvas = FindVisualChild<Canvas>(root, canvasName);

        if (topItemsControl == null || bottomItemsControl == null || canvas == null)
        {
            return;
        }

        var topDigits = GetDigitContainers(topItemsControl);
        var bottomDigits = GetDigitContainers(bottomItemsControl);

        if (topDigits.Count != 3 || bottomDigits.Count != 3)
        {
            return;
        }

        if (clearCanvas)
        {
            canvas.Children.Clear();
        }

        for (int i = 0; i < 3; i++)
        {
            var topDigit = topDigits[i];
            var topText = GetDigitText(topDigit);

            for (int j = 0; j < 3; j++)
            {
                var bottomDigit = bottomDigits[j];
                var bottomText = GetDigitText(bottomDigit);

                if (topText == bottomText)
                {
                    DrawConnectingLine(canvas, topDigit, bottomDigit, Brushes.Black);
                }
            }
        }
    }

    private void ConnectPick3ToPick4Digits(FrameworkElement root, string pick3ItemsControlName, string pick4ItemsControlName, string canvasName)
    {
        var pick3ItemsControl = FindVisualChild<ItemsControl>(root, pick3ItemsControlName);
        var pick4ItemsControl = FindVisualChild<ItemsControl>(root, pick4ItemsControlName);
        var canvas = FindVisualChild<Canvas>(root, canvasName);

        if (pick3ItemsControl == null || pick4ItemsControl == null || canvas == null)
        {
            return;
        }

        var pick3Digits = GetDigitContainers(pick3ItemsControl);
        var pick4Digits = GetDigitContainers(pick4ItemsControl);

        if (pick3Digits.Count != 3 || pick4Digits.Count != 4)
        {
            return;
        }

        canvas.Children.Clear();

        for (int i = 0; i < 3; i++)
        {
            var pick3Digit = pick3Digits[i];
            var pick3Text = GetDigitText(pick3Digit);

            for (int j = 0; j < 4; j++)
            {
                var pick4Digit = pick4Digits[j];
                var pick4Text = GetDigitText(pick4Digit);

                if (pick3Text == pick4Text)
                {
                    DrawConnectingLine(canvas, pick3Digit, pick4Digit, Brushes.Black);
                }
            }
        }
    }

    private void ConnectPick3ToPick4DigitsWithRedLine(FrameworkElement root, string pick3ItemsControlName, string pick4ItemsControlName, string canvasName)
    {
        var pick3ItemsControl = FindVisualChild<ItemsControl>(root, pick3ItemsControlName);
        var pick4ItemsControl = FindVisualChild<ItemsControl>(root, pick4ItemsControlName);
        var canvas = FindVisualChild<Canvas>(root, canvasName);

        if (pick3ItemsControl == null || pick4ItemsControl == null || canvas == null)
        {
            return;
        }

        var pick3Digits = GetDigitContainers(pick3ItemsControl);
        var pick4Digits = GetDigitContainers(pick4ItemsControl);

        if (pick3Digits.Count != 3 || pick4Digits.Count != 4)
        {
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            var pick3Digit = pick3Digits[i];
            var pick3Text = GetDigitText(pick3Digit);

            for (int j = 0; j < 4; j++)
            {
                var pick4Digit = pick4Digits[j];
                var pick4Text = GetDigitText(pick4Digit);

                if (pick3Text == pick4Text)
                {
                    DrawConnectingLine(canvas, pick3Digit, pick4Digit, Brushes.Red);
                }
            }
        }
    }

    private static List<Border> GetDigitContainers(ItemsControl itemsControl)
    {
        var containers = new List<Border>();

        for (int i = 0; i < itemsControl.Items.Count; i++)
        {
            if (itemsControl.ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter container)
            {
                var border = FindVisualChild<Border>(container);
                if (border != null)
                {
                    containers.Add(border);
                }
            }
        }

        return containers;
    }

    private static string GetDigitText(Border digitBorder)
    {
        var textBlock = FindVisualChild<TextBlock>(digitBorder);
        return textBlock?.Text ?? string.Empty;
    }

    private static void DrawConnectingLine(Canvas canvas, Border element1, Border element2, Brush stroke)
    {
        if (element1.ActualWidth <= 0 || element1.ActualHeight <= 0 || element2.ActualWidth <= 0 || element2.ActualHeight <= 0)
        {
            return;
        }

        var center1 = element1.TranslatePoint(new Point(element1.ActualWidth / 2, element1.ActualHeight / 2), canvas);
        var center2 = element2.TranslatePoint(new Point(element2.ActualWidth / 2, element2.ActualHeight / 2), canvas);
        double dx = center2.X - center1.X;
        double dy = center2.Y - center1.Y;
        double distance = Math.Sqrt((dx * dx) + (dy * dy));

        if (distance == 0)
        {
            return;
        }

        double radius1 = element1.ActualWidth / 2;
        double radius2 = element2.ActualWidth / 2;
        double unitX = dx / distance;
        double unitY = dy / distance;

        var line = new Line
        {
            X1 = center1.X + (unitX * radius1),
            Y1 = center1.Y + (unitY * radius1),
            X2 = center2.X - (unitX * radius2),
            Y2 = center2.Y - (unitY * radius2),
            Stroke = stroke,
            StrokeThickness = 2
        };

        canvas.Children.Add(line);
    }

    private static T? FindVisualChild<T>(DependencyObject parent, string? childName = null) where T : DependencyObject
    {
        if (parent == null)
        {
            return null;
        }

        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is FrameworkElement frameworkElement && !string.IsNullOrEmpty(childName) && frameworkElement.Name == childName)
            {
                return child as T;
            }

            if (child is T result && (string.IsNullOrEmpty(childName) || childName == result.GetValue(FrameworkElement.NameProperty) as string))
            {
                return result;
            }

            var foundChild = FindVisualChild<T>(child, childName);
            if (foundChild != null)
            {
                return foundChild;
            }
        }

        return null;
    }
}










