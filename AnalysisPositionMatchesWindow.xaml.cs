using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace FloridaLotteryApp;

    public partial class AnalysisPositionMatchesWindow : Window
{
    public ObservableCollection<AnalysisPairCardVM> Cards { get; } = new();
    public string MatchSummary { get; private set; } = string.Empty;

    public AnalysisPositionMatchesWindow(AnalysisPairCardVM selectedCard, IEnumerable<AnalysisPairCardVM> allCards)
    {
        InitializeComponent();
        DataContext = this; 
        LoadMatches(selectedCard, allCards);
    }

    private void LoadMatches(AnalysisPairCardVM selectedCard, IEnumerable<AnalysisPairCardVM> allCards)
    {
        var targetSignature = selectedCard.BuildResultColorSignature();
        var matches = allCards
            .Where(card => card.BuildResultColorSignature() == targetSignature)
            .ToList();


        foreach (var match in matches)
        {
            Cards.Add(match);
        }

        MatchSummary = $"Coincidencias encontradas: {Cards.Count}";
    }
 }
