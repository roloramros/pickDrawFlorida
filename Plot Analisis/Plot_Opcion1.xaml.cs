using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FloridaLotteryApp.Data;

namespace FloridaLotteryApp
{
    public partial class Plot_Opcion1 : Window
    {
        public ObservableCollection<PatternRow> PatternRows { get; set; } = new();

        public List<DigitCell> Row1Pick3 { get; set; } = new();
        public List<DigitCell> Row1Pick4 { get; set; } = new();
        public List<string> Row1Pick3Siguiente { get; set; } = new();
        public List<string> Row1Additional { get; set; } = new();

        public List<DigitCell> Row2Pick3 { get; set; } = new();
        public List<DigitCell> Row2Pick4 { get; set; } = new();
        public List<string> Row2Fireball { get; set; } = new();
        public List<string> Row2Additional { get; set; } = new();

        public List<DigitCell> Row3Pick3 { get; set; } = new();
        public List<DigitCell> Row3Pick4 { get; set; } = new();
        public List<string> Row3Fireball { get; set; } = new();
        public List<string> Row3Additional { get; set; } = new();

        public List<DigitCell> Row4Pick3 { get; set; } = new();
        public List<DigitCell> Row4Pick4 { get; set; } = new();
        public List<string> Row4Fireball { get; set; } = new();
        public List<string> Row4Additional { get; set; } = new();

        private string _row1Pick3Number = " ";
        private string _row1Pick4Number = " ";
        private string _row2Pick3Number = " ";
        private string _row2Pick4Number = " ";
        private string _row3Pick3Number = " ";
        private string _row3Pick4Number = " ";
        private string _row4Pick3Number = " ";
        private string _row4Pick4Number = " ";

        private readonly string _guidePatternRow1;
        private readonly string _guidePatternRow2;
        private readonly string _guidePatternRow3;
        private readonly string _guidePatternRow4;

        private readonly DateTime _originalDate;
        private readonly string _originalDrawTime;
        private readonly string _originalPick3;
        private readonly string _originalPick4;

        private readonly HashSet<string> _seenRows = new();
        private bool _analysisStarted;
        private bool _isLoading;
        private readonly double _originalWindowHeight;
        private readonly double _originalWindowWidth;
        private readonly double _expandedWindowHeight;
        private CancellationTokenSource? _cancellationTokenSource;

        private static readonly Brush[] RepeatPalette =
        {
            (Brush)new BrushConverter().ConvertFromString("#0000FF")!,
            (Brush)new BrushConverter().ConvertFromString("#006400")!,
            (Brush)new BrushConverter().ConvertFromString("#dc143c")!,
            (Brush)new BrushConverter().ConvertFromString("#daa520")!,
            (Brush)new BrushConverter().ConvertFromString("#9400d3")!,
            (Brush)new BrushConverter().ConvertFromString("#20b2aa")!
        };

        public Plot_Opcion1(string dateText, string drawIcon, string pick3, string pick4, string pick3Siguiente, PatternRow selectedRow)
        {
            InitializeComponent();
            DataContext = this;
            _originalWindowHeight = Height;
            _originalWindowWidth = Width;
            _expandedWindowHeight = Height + 20;

            _originalPick3 = pick3 ?? " ";
            _originalPick4 = pick4 ?? " ";
            _originalDrawTime = DrawTimeFromIcon(drawIcon);
            _originalDate = DateTime.TryParseExact(dateText, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var parsedDate)
                ? parsedDate
                : DateTime.MinValue;

            _row1Pick3Number = _originalPick3;
            _row1Pick4Number = _originalPick4;

            var pick3SiguienteDigits = (pick3Siguiente ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row1Pick3Siguiente = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                Row1Pick3Siguiente.Add(i < pick3SiguienteDigits.Count ? pick3SiguienteDigits[i] : " ");
            }

             _guidePatternRow1 = BuildRowPattern(_row1Pick3Number, _row1Pick4Number, string.Concat(Row1Pick3Siguiente));
            _guidePatternRow2 = BuildAnchoredRowPattern(selectedRow?.MatchPick3 ?? " ", selectedRow?.MatchPick4 ?? " ", selectedRow?.MatchNextPick3 ?? " ");
            _guidePatternRow3 = BuildRowPattern(selectedRow?.SimilarPick3 ?? " ", selectedRow?.SimilarPick4 ?? " ", selectedRow?.SimilarNextPick3 ?? " ");
            _guidePatternRow4 = BuildAnchoredRowPattern(selectedRow?.SimilarMatchPick3 ?? " ", selectedRow?.SimilarMatchPick4 ?? " ", selectedRow?.SimilarMatchNextPick3 ?? " ");

            ClearDetailSection();

            PatternsTable.ItemsSource = PatternRows;
            PatternsTable.SelectionChanged += PatternsTable_SelectionChanged;

            UpdateAllPickDigitCells();
            AssignItemsSources();
            UpdateResultsCounter();

            Loaded += async (_, __) =>
            {
                await LoadPatternRowsRealtimeAsync();
                QueueDrawConnectingLines();
            };
        }

        private void ClearDetailSection()
        {
            _row1Pick3Number = " ";
            _row1Pick4Number = " ";
            _row2Pick3Number = " ";
            _row2Pick4Number = " ";
            _row3Pick3Number = " ";
            _row3Pick4Number = " ";
            _row4Pick3Number = " ";
            _row4Pick4Number = " ";

            Row1Pick3Siguiente = new List<string>();
            Row1Additional = new List<string>();
            Row2Fireball = new List<string>();
            Row2Additional = new List<string>();
            Row3Fireball = new List<string>();
            Row3Additional = new List<string>();
            Row4Fireball = new List<string>();
            Row4Additional = new List<string>();

            Row1_DateText.Text = " ";
            Row2_DateText.Text = " ";
            Row3_DateText.Text = " ";
            Row4_DateText.Text = " ";
            Row1_DrawIcon.Text = " ";
            Row2_DrawIcon.Text = " ";
            Row3_DrawIcon.Text = " ";
            Row4_DrawIcon.Text = " ";
        }
        private async Task LoadPatternRowsRealtimeAsync()
        {
            if (_analysisStarted)
            {
                return;
            }

            _analysisStarted = true;
            _isLoading = true;
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            SetLoadingState(true, "Analizando tiradas...", 0, 1, true);

            int matchesFound = 0;
            var progress = new Progress<PatternRow>(row =>
            {
                var key = BuildUniqueResultKey(row);
                if (!_seenRows.Add(key))
                {
                    return;
                }

                PatternRows.Add(row);
                matchesFound++;
                if (PatternRows.Count == 1)
                {
                    PatternsTable.SelectedIndex = 0;
                }

                UpdateResultsCounter();
            });

            var statusProgress = new Progress<(int processed, int total)>(status =>
            {
                int processed = Math.Max(0, status.processed);
                int total = Math.Max(1, status.total);
                bool indeterminate = total <= 1;

                SetLoadingState(
                    true,
                    $"Analizando tiradas... {processed} de {total} | Matches: {matchesFound}",
                    processed,
                    total,
                    indeterminate);
            });

            try
            {
                await Task.Run(() => RunAnalysisForAllGuides(progress, statusProgress, token), token);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Analisis cancelado por el usuario.", "Cancelado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante el analisis: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                SetLoadingState(false, "", 0, 1, false);
                ResultsCounterText.Background = Brushes.Transparent;
                UpdateResultsCounter();
            }
        }

        private void RunAnalysisForAllGuides(
            IProgress<PatternRow> progress,
            IProgress<(int processed, int total)> statusProgress,
            CancellationToken cancellationToken)
        {
            var allHits = BuildCandidateRows(DrawRepository.GetAllPick3WithPick4());
            if (allHits.Count == 0)
            {
                return;
            }

            var orderedGuides = allHits
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => GetDrawRecencyOrder(x.Hit.DrawTime))
                .ToList();

            var byPos23 = allHits.GroupBy(x => x.Pos23).ToDictionary(g => g.Key, g => g.ToList());
            var byPattern = allHits.GroupBy(x => x.Pattern).ToDictionary(g => g.Key, g => g.ToList());
            var byPos23Pattern = allHits
                .GroupBy(x => $"{x.Pos23}|{x.Pattern}")
                .ToDictionary(g => g.Key, g => g.ToList());

            int totalGuides = orderedGuides.Count;
            int processed = 0;

            foreach (var guide in orderedGuides)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processed++;
                statusProgress.Report((processed, totalGuides));

                if (IsOriginalCombo(guide.Hit.Date, guide.Hit.DrawTime, guide.Hit.Pick3, guide.Hit.Pick4))
                {
                    continue;
                }

                if (!byPos23.TryGetValue(guide.Pos23, out var col2Pool))
                {
                    continue;
                }

                if (!byPattern.TryGetValue(guide.Pattern, out var col3Pool))
                {
                    continue;
                }

                var col2Candidates = col2Pool
                    .Where(x => x.Number7 != guide.Number7 && x.Date < guide.Date)
                    .ToList();

                var col3Candidates = col3Pool
                    .Where(x => x.Number7 != guide.Number7)
                    .ToList();

                var refFirstDigit = GetFirstDigit(guide.Hit.Pick3);

                foreach (var col2 in col2Candidates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var col2FirstDigit = GetFirstDigit(col2.Hit.Pick3);
                    bool refAndCol2AreEqual = refFirstDigit.HasValue && col2FirstDigit.HasValue && refFirstDigit.Value == col2FirstDigit.Value;
                    bool refAndCol2AreDifferent = refFirstDigit.HasValue && col2FirstDigit.HasValue && refFirstDigit.Value != col2FirstDigit.Value;

                    foreach (var col3 in col3Candidates)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string col4Key = $"{col3.Pos23}|{col2.Pattern}";
                        if (!byPos23Pattern.TryGetValue(col4Key, out var col4Pool))
                        {
                            continue;
                        }

                        var col3FirstDigit = GetFirstDigit(col3.Hit.Pick3);

                        foreach (var col4 in col4Pool)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (col4.Number7 == col2.Number7 || col4.Number7 == col3.Number7)
                            {
                                continue;
                            }

                            if (col4.Date >= col3.Date)
                            {
                                continue;
                            }

                            if (!HasSameCrossPositionEqualityPattern(guide.Number7, col2.Number7, col3.Number7, col4.Number7))
                            {
                                continue;
                            }

                            var col4FirstDigit = GetFirstDigit(col4.Hit.Pick3);
                            if (col3FirstDigit.HasValue && col4FirstDigit.HasValue)
                            {
                                bool col3AndCol4AreEqual = col3FirstDigit.Value == col4FirstDigit.Value;
                                bool col3AndCol4AreDifferent = col3FirstDigit.Value != col4FirstDigit.Value;

                                if (!((refAndCol2AreDifferent && col3AndCol4AreDifferent) || (refAndCol2AreEqual && col3AndCol4AreEqual)))
                                {
                                    continue;
                                }
                            }

                            var row = CreatePatternRow(col2, col3, col4, guide.Number7, guide.Date, guide.Hit.DrawTime, guide.NextPick3);
                            if (row != null)
                            {
                                progress.Report(row);
                            }
                        }
                    }
                }
            }
        }

        private static int GetDrawRecencyOrder(string? drawTime)
        {
            return string.Equals(drawTime, "E", StringComparison.OrdinalIgnoreCase) ? 2
                : string.Equals(drawTime, "M", StringComparison.OrdinalIgnoreCase) ? 1
                : 0;
        }
        private PatternRow? FindFirstRowForGuide(
            CandidateRow guide,
            IReadOnlyDictionary<string, List<CandidateRow>> byPos23,
            IReadOnlyDictionary<string, List<CandidateRow>> byPattern,
            IReadOnlyDictionary<string, List<CandidateRow>> byPos23Pattern,
            CancellationToken cancellationToken)
        {
            if (!byPos23.TryGetValue(guide.Pos23, out var col2Pool))
            {
                return null;
            }

            if (!byPattern.TryGetValue(guide.Pattern, out var col3Pool))
            {
                return null;
            }

            var col2Candidates = col2Pool
                .Where(x => x.Number7 != guide.Number7 && x.Date < guide.Date)
                .ToList();

            var col3Candidates = col3Pool
                .Where(x => x.Number7 != guide.Number7)
                .ToList();

            var refFirstDigit = GetFirstDigit(guide.Hit.Pick3);

            foreach (var col2 in col2Candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var col2FirstDigit = GetFirstDigit(col2.Hit.Pick3);
                bool refAndCol2AreEqual = refFirstDigit.HasValue && col2FirstDigit.HasValue && refFirstDigit.Value == col2FirstDigit.Value;
                bool refAndCol2AreDifferent = refFirstDigit.HasValue && col2FirstDigit.HasValue && refFirstDigit.Value != col2FirstDigit.Value;

                foreach (var col3 in col3Candidates)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string col4Key = $"{col3.Pos23}|{col2.Pattern}";
                    if (!byPos23Pattern.TryGetValue(col4Key, out var col4Pool))
                    {
                        continue;
                    }

                    var col3FirstDigit = GetFirstDigit(col3.Hit.Pick3);

                    foreach (var col4 in col4Pool)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (col4.Number7 == col2.Number7 || col4.Number7 == col3.Number7)
                        {
                            continue;
                        }

                        if (col4.Date >= col3.Date)
                        {
                            continue;
                        }

                        if (!HasSameCrossPositionEqualityPattern(guide.Number7, col2.Number7, col3.Number7, col4.Number7))
                        {
                            continue;
                        }

                        var col4FirstDigit = GetFirstDigit(col4.Hit.Pick3);
                        if (col3FirstDigit.HasValue && col4FirstDigit.HasValue)
                        {
                            bool col3AndCol4AreEqual = col3FirstDigit.Value == col4FirstDigit.Value;
                            bool col3AndCol4AreDifferent = col3FirstDigit.Value != col4FirstDigit.Value;

                            if (!((refAndCol2AreDifferent && col3AndCol4AreDifferent) || (refAndCol2AreEqual && col3AndCol4AreEqual)))
                            {
                                continue;
                            }
                        }

                        var row = CreatePatternRow(col2, col3, col4, guide.Number7, guide.Date, guide.Hit.DrawTime, guide.NextPick3);
                        if (row != null)
                        {
                            return row;
                        }
                    }
                }
            }

            return null;
        }

        private PatternRow? CreatePatternRow(
            CandidateRow col2,
            CandidateRow col3,
            CandidateRow col4,
            string guideNumber,
            DateTime guideDate,
            string guideDrawTime,
            string guideNextPick3Value)
        {
            try
            {
                if (IsOriginalCombo(col2.Hit.Date, col2.Hit.DrawTime, col2.Hit.Pick3, col2.Hit.Pick4) ||
                    IsOriginalCombo(col3.Hit.Date, col3.Hit.DrawTime, col3.Hit.Pick3, col3.Hit.Pick4) ||
                    IsOriginalCombo(col4.Hit.Date, col4.Hit.DrawTime, col4.Hit.Pick3, col4.Hit.Pick4))
                {
                    return null;
                }

                var col2NextPick3 = col2.NextPick3;
                var col3NextPick3 = col3.NextPick3;
                var col4NextPick3 = col4.NextPick3;

                var guidePick3 = guideNumber.Length >= 3 ? guideNumber.Substring(0, 3) : " ";
                var guidePick4 = guideNumber.Length >= 7 ? guideNumber.Substring(3, 4) : " ";
                var guideNextPick3 = guideNextPick3Value ?? " ";

                if (HasAnyInternalRepeatsInRow(guidePick3, guidePick4, guideNextPick3) ||
                    HasAnyInternalRepeatsInRow(col2.Hit.Pick3, col2.Hit.Pick4, col2NextPick3) ||
                    HasAnyInternalRepeatsInRow(col3.Hit.Pick3, col3.Hit.Pick4, col3NextPick3) ||
                    HasAnyInternalRepeatsInRow(col4.Hit.Pick3, col4.Hit.Pick4, col4NextPick3))
                {
                    return null;
                }

                string row2Pattern = BuildAnchoredRowPattern(col2.Hit.Pick3, col2.Hit.Pick4, col2NextPick3);
                string row4Pattern = BuildAnchoredRowPattern(col4.Hit.Pick3, col4.Hit.Pick4, col4NextPick3);
                if (row2Pattern != _guidePatternRow2 || row4Pattern != _guidePatternRow4)
                {
                    return null;
                }

                return new PatternRow
                {
                    ReferenceNumber = guideNumber,
                    ReferenceDate = guideDate.ToString("yyyy-MM-dd"),
                    ReferencePick3 = guidePick3,
                    ReferencePick4 = guidePick4,
                    ReferenceNextPick3 = guideNextPick3,
                    ReferenceDrawTime = guideDrawTime,
                    ReferenceCodificacion = string.Concat(BuildCodificacionDigits(guidePick3, guidePick4)),

                    MatchNumber = col2.Number7,
                    MatchPick3 = col2.Hit.Pick3,
                    MatchPick4 = col2.Hit.Pick4,
                    MatchNextPick3 = col2NextPick3,
                    MatchDrawTime = col2.Hit.DrawTime,
                    MatchDate = col2.Hit.Date.ToString("yyyy-MM-dd"),
                    MatchCodificacion = string.Concat(BuildCodificacionDigits(col2.Hit.Pick3, col2.Hit.Pick4)),

                    SimilarNumber = col3.Number7,
                    SimilarPick3 = col3.Hit.Pick3,
                    SimilarPick4 = col3.Hit.Pick4,
                    SimilarNextPick3 = col3NextPick3,
                    SimilarDrawTime = col3.Hit.DrawTime,
                    SimilarDate = col3.Hit.Date.ToString("yyyy-MM-dd"),
                    SimilarCodificacion = string.Concat(BuildCodificacionDigits(col3.Hit.Pick3, col3.Hit.Pick4)),

                    SimilarMatchNumber = col4.Number7,
                    SimilarMatchPick3 = col4.Hit.Pick3,
                    SimilarMatchPick4 = col4.Hit.Pick4,
                    SimilarMatchNextPick3 = col4NextPick3,
                    SimilarMatchDrawTime = col4.Hit.DrawTime,
                    SimilarMatchDate = col4.Hit.Date.ToString("yyyy-MM-dd"),
                    SimilarMatchCodificacion = string.Concat(BuildCodificacionDigits(col4.Hit.Pick3, col4.Hit.Pick4))
                };
            }
            catch
            {
                return null;
            }
        }

        private static bool HasAnyInternalRepeatsInRow(string? pick3, string? pick4, string? nextPick3)
        {
            return HasInternalRepeatedDigits(pick3) ||
                   HasInternalRepeatedDigits(pick4) ||
                   HasInternalRepeatedDigits(nextPick3);
        }

        private static bool HasInternalRepeatedDigits(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var digits = value.Where(char.IsDigit).ToList();
            if (digits.Count < 2)
            {
                return false;
            }

            return digits.Count != digits.Distinct().Count();
        }
        private bool MatchesGuideLowerRows(PatternRow row)
        {
            string row2Pattern = BuildRowPattern(row.MatchPick3, row.MatchPick4, row.MatchNextPick3);
            string row3Pattern = BuildRowPattern(row.SimilarPick3, row.SimilarPick4, row.SimilarNextPick3);
            string row4Pattern = BuildRowPattern(row.SimilarMatchPick3, row.SimilarMatchPick4, row.SimilarMatchNextPick3);

            return row2Pattern == _guidePatternRow2
                && row3Pattern == _guidePatternRow3
                && row4Pattern == _guidePatternRow4;
        }

        private bool IsOriginalCombo(DateTime date, string drawTime, string pick3, string pick4)
        {
            if (_originalDate == DateTime.MinValue)
            {
                return false;
            }

            return date.Date == _originalDate.Date
                && string.Equals(drawTime, _originalDrawTime, StringComparison.OrdinalIgnoreCase)
                && string.Equals(pick3, _originalPick3, StringComparison.Ordinal)
                && string.Equals(pick4, _originalPick4, StringComparison.Ordinal);
        }

        private static List<CandidateRow> BuildCandidateRows(IEnumerable<ComboHit> hits)
        {
            return hits
                .Select(hit =>
                {
                    var number7 = BuildSevenDigitNumber(hit.Pick3, hit.Pick4);
                    if (number7.Length != 7)
                    {
                        return null;
                    }

                    var nextPick3 = DrawRepository.GetNextPick3Number(hit.Date, hit.DrawTime) ?? " ";

                    // Prefiltro: recorta la base a tiradas sin repetidos internos.
                    if (HasAnyInternalRepeatsInRow(hit.Pick3, hit.Pick4, nextPick3))
                    {
                        return null;
                    }

                    return new CandidateRow
                    {
                        Hit = hit,
                        Number7 = number7,
                        Pos23 = GetPos23Key(number7) ?? " ",
                        Pattern = BuildRepetitionPattern(number7),
                        Date = hit.Date,
                        NextPick3 = nextPick3
                    };
                })
                .Where(x => x != null)
                .Cast<CandidateRow>()
                .ToList();
        }


        private static char? GetFirstDigit(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            foreach (var ch in value)
            {
                if (char.IsDigit(ch))
                {
                    return ch;
                }
            }

            return null;
        }

        private static string DrawTimeFromIcon(string drawIcon)
        {
            if (string.IsNullOrWhiteSpace(drawIcon))
            {
                return " ";
            }

            if (drawIcon.Contains('\u2600'))
            {
                return "M";
            }

            if (drawIcon.Contains("\U0001F319"))
            {
                return "E";
            }

            return " ";
        }

        private static string BuildUniqueResultKey(PatternRow row)
        {
            return string.Join("|", new[]
            {
                row.ReferenceNumber,
                row.MatchDate, row.MatchDrawTime, row.MatchPick3, row.MatchPick4,
                row.SimilarDate, row.SimilarDrawTime, row.SimilarPick3, row.SimilarPick4,
                row.SimilarMatchDate, row.SimilarMatchDrawTime, row.SimilarMatchPick3, row.SimilarMatchPick4
            });
        }

        private void PatternsTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatternsTable.SelectedItem is not PatternRow selected)
            {
                UpdateResultsCounter();
                return;
            }

            LoadSelectedRow(selected);
            UpdateAllPickDigitCells();
            AssignItemsSources();
            QueueDrawConnectingLines();
            UpdateResultsCounter();
        }

        private void LoadSelectedRow(PatternRow selected)
        {
            if (selected == null)
            {
                return;
            }

            _row1Pick3Number = !string.IsNullOrWhiteSpace(selected.ReferencePick3) ? selected.ReferencePick3 : (selected.ReferenceNumber?.Length >= 3 ? selected.ReferenceNumber.Substring(0, 3) : " ");
            _row1Pick4Number = !string.IsNullOrWhiteSpace(selected.ReferencePick4) ? selected.ReferencePick4 : (selected.ReferenceNumber?.Length >= 7 ? selected.ReferenceNumber.Substring(3, 4) : " ");
            var row1NextDigits = (selected.ReferenceNextPick3 ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row1Pick3Siguiente = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                Row1Pick3Siguiente.Add(i < row1NextDigits.Count ? row1NextDigits[i] : " ");
            }
            Row1Additional = string.IsNullOrWhiteSpace(selected.ReferenceCodificacion)
                ? BuildCodificacionDigits(_row1Pick3Number, _row1Pick4Number)
                : (selected.ReferenceCodificacion ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row1_DateText.Text = selected.ReferenceDate ?? " ";
            Row1_DrawIcon.Text = DrawIconFromTime(selected.ReferenceDrawTime);

            _row2Pick3Number = selected.MatchPick3 ?? " ";
            _row2Pick4Number = selected.MatchPick4 ?? " ";
            Row2Fireball = (selected.MatchNextPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row2Additional = (selected.MatchCodificacion ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row2_DateText.Text = selected.MatchDate ?? " ";
            Row2_DrawIcon.Text = DrawIconFromTime(selected.MatchDrawTime);

            _row3Pick3Number = selected.SimilarPick3 ?? " ";
            _row3Pick4Number = selected.SimilarPick4 ?? " ";
            Row3Fireball = (selected.SimilarNextPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row3Additional = (selected.SimilarCodificacion ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row3_DateText.Text = selected.SimilarDate ?? " ";
            Row3_DrawIcon.Text = DrawIconFromTime(selected.SimilarDrawTime);

            _row4Pick3Number = selected.SimilarMatchPick3 ?? " ";
            _row4Pick4Number = selected.SimilarMatchPick4 ?? " ";
            Row4Fireball = (selected.SimilarMatchNextPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row4Additional = (selected.SimilarMatchCodificacion ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row4_DateText.Text = selected.SimilarMatchDate ?? " ";
            Row4_DrawIcon.Text = DrawIconFromTime(selected.SimilarMatchDrawTime);
        }

        private void AssignItemsSources()
        {
            Row1_Pick3SiguienteDigits.ItemsSource = Row1Pick3Siguiente;
            Row1_AdditionalDigits.ItemsSource = Row1Additional;

            Row2_FireballDigits.ItemsSource = Row2Fireball;
            Row2_AdditionalDigits.ItemsSource = Row2Additional;

            Row3_FireballDigits.ItemsSource = Row3Fireball;
            Row3_AdditionalDigits.ItemsSource = Row3Additional;

            Row4_FireballDigits.ItemsSource = Row4Fireball;
            Row4_AdditionalDigits.ItemsSource = Row4Additional;
        }

        private void QueueDrawConnectingLines()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Dispatcher.BeginInvoke(new Action(DrawConnectingLines), DispatcherPriority.Render);
            }), DispatcherPriority.Loaded);
        }

        private void DrawConnectingLines()
        {
            try
            {
                var canvas = ConnectionCanvas;
                if (canvas == null)
                {
                    return;
                }

                canvas.Children.Clear();

                var row1NextPick3 = FindVisualChild<ItemsControl>(this, "Row1_Pick3SiguienteDigits");
                var row2Fireball = FindVisualChild<ItemsControl>(this, "Row2_FireballDigits");
                var row3Fireball = FindVisualChild<ItemsControl>(this, "Row3_FireballDigits");
                var row4Fireball = FindVisualChild<ItemsControl>(this, "Row4_FireballDigits");

                if (row1NextPick3 == null || row2Fireball == null || row3Fireball == null || row4Fireball == null)
                {
                    return;
                }

                ConnectPick3Digits(row1NextPick3, row2Fireball, canvas, Brushes.Black);
                ConnectPick3Digits(row3Fireball, row4Fireball, canvas, Brushes.Black);
            }
            catch
            {
                // Ignorar errores de dibujo para no bloquear la UI.
            }
        }

        private void ConnectPick3Digits(ItemsControl topItemsControl, ItemsControl bottomItemsControl, Canvas canvas, Brush lineColor)
        {
            if (topItemsControl == null || bottomItemsControl == null || canvas == null)
            {
                return;
            }

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

        private List<Border> GetDigitContainers(ItemsControl itemsControl)
        {
            var containers = new List<Border>();

            try
            {
                if (itemsControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
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

                if (containers.Count == 0)
                {
                    foreach (var border in FindVisualChildren<Border>(itemsControl))
                    {
                        var textBlock = FindVisualChild<TextBlock>(border);
                        if (textBlock != null)
                        {
                            containers.Add(border);
                        }
                    }
                }
            }
            catch
            {
                return containers;
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

            Point startPoint = new Point(center1.X + (unitX * radius1), center1.Y + (unitY * radius1));
            Point endPoint = new Point(center2.X - (unitX * radius2), center2.Y - (unitY * radius2));

            var line = new System.Windows.Shapes.Line
            {
                X1 = startPoint.X,
                Y1 = startPoint.Y,
                X2 = endPoint.X,
                Y2 = endPoint.Y,
                Stroke = lineColor,
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
                if (child is T typedChild)
                {
                    if (string.IsNullOrEmpty(childName) || (typedChild is FrameworkElement fe && fe.Name == childName))
                    {
                        return typedChild;
                    }
                }

                var foundChild = FindVisualChild<T>(child, childName);
                if (foundChild != null)
                {
                    return foundChild;
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
        private static List<string> BuildCodificacionDigits(string pick3, string pick4)
        {
            return (pick3 + pick4)
                .Where(char.IsDigit)
                .Distinct()
                .OrderBy(c => c)
                .Select(c => c.ToString())
                .ToList();
        }

        private static string DrawIconFromTime(string drawTime)
        {
            return drawTime == "M" ? "\u2600\uFE0F" : drawTime == "E" ? "\U0001F319" : " ";
        }

        private void UpdateAllPickDigitCells()
        {
            var topA = ExtractDigits(_row1Pick3Number).Concat(ExtractDigits(_row1Pick4Number)).ToList();
            var topB = ExtractDigits(_row2Pick3Number).Concat(ExtractDigits(_row2Pick4Number)).ToList();
            var bottomA = ExtractDigits(_row3Pick3Number).Concat(ExtractDigits(_row3Pick4Number)).ToList();
            var bottomB = ExtractDigits(_row4Pick3Number).Concat(ExtractDigits(_row4Pick4Number)).ToList();

            var topCounts = topA.Concat(topB).GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
            var repeatedTopDigits = topA.Concat(topB)
                .Where(d => topCounts.TryGetValue(d, out var count) && count > 1)
                .Distinct()
                .ToList();

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

            var repeatedBottomDigits = bottomA.Concat(bottomB)
                .Where(d => bottomCounts.TryGetValue(d, out var count) && count > 1)
                .Distinct()
                .ToList();

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

            Row1Pick3 = BuildDigitCells(_row1Pick3Number, topCounts, topBrushByDigit);
            Row1Pick4 = BuildDigitCells(_row1Pick4Number, topCounts, topBrushByDigit);
            Row2Pick3 = BuildDigitCells(_row2Pick3Number, topCounts, topBrushByDigit);
            Row2Pick4 = BuildDigitCells(_row2Pick4Number, topCounts, topBrushByDigit);

            Row3Pick3 = BuildDigitCells(_row3Pick3Number, bottomCounts, bottomBrushByDigit);
            Row3Pick4 = BuildDigitCells(_row3Pick4Number, bottomCounts, bottomBrushByDigit);
            Row4Pick3 = BuildDigitCells(_row4Pick3Number, bottomCounts, bottomBrushByDigit);
            Row4Pick4 = BuildDigitCells(_row4Pick4Number, bottomCounts, bottomBrushByDigit);

            Row1_Pick3Digits.ItemsSource = Row1Pick3;
            Row1_Pick4Digits.ItemsSource = Row1Pick4;
            Row2_Pick3Digits.ItemsSource = Row2Pick3;
            Row2_Pick4Digits.ItemsSource = Row2Pick4;
            Row3_Pick3Digits.ItemsSource = Row3Pick3;
            Row3_Pick4Digits.ItemsSource = Row3Pick4;
            Row4_Pick3Digits.ItemsSource = Row4Pick3;
            Row4_Pick4Digits.ItemsSource = Row4Pick4;
        }

        private static List<string> ExtractDigits(string? value)
        {
            return (value ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        }

        private static Dictionary<string, string> BuildDigitProfiles(IReadOnlyList<string> rowA, IReadOnlyList<string> rowB)
        {
            var profiles = new Dictionary<string, string>();
            var digits = rowA.Concat(rowB).Distinct().ToList();

            foreach (var digit in digits)
            {
                var aPositions = Enumerable.Range(0, rowA.Count).Where(i => rowA[i] == digit);
                var bPositions = Enumerable.Range(0, rowB.Count).Where(i => rowB[i] == digit);
                var profile = $"A:{string.Join(",", aPositions)}|B:{string.Join(",", bPositions)}";
                profiles[digit] = profile;
            }

            return profiles;
        }

        private static List<DigitCell> BuildDigitCells(
            string? value,
            IReadOnlyDictionary<string, int> counts,
            IReadOnlyDictionary<string, Brush> brushesByDigit)
        {
            return ExtractDigits(value)
                .Select(d => new DigitCell
                {
                    Value = d,
                    Background = counts.TryGetValue(d, out var c) && c > 1
                        ? (brushesByDigit.TryGetValue(d, out var brush) ? brush : RepeatPalette[0])
                        : Brushes.White
                })
                .ToList();
        }

        public class DigitCell
        {
            public string Value { get; set; } = " ";
            public Brush Background { get; set; } = Brushes.White;
        }


        private void SetLoadingState(bool isLoading, string status, int completed, int total, bool isIndeterminate)
        {
            UpdateWindowSize(isLoading);
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            CancelButton.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            CancelButton.IsEnabled = isLoading;

            AnalysisProgressText.Text = string.IsNullOrWhiteSpace(status) ? "" : status;
            AnalysisProgressBar.IsIndeterminate = isIndeterminate;
            AnalysisProgressBar.Maximum = Math.Max(1, total);
            AnalysisProgressBar.Value = isIndeterminate ? 0 : Math.Min(Math.Max(0, completed), AnalysisProgressBar.Maximum);
        }

        private void UpdateWindowSize(bool isLoading)
        {
            Height = isLoading ? _expandedWindowHeight : _originalWindowHeight;
            Width = _originalWindowWidth;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoading)
            {
                return;
            }

            CancelButton.IsEnabled = false;
            SetLoadingState(true, "Cancelando analisis...", 0, 1, true);
            _cancellationTokenSource?.Cancel();
        }
        private void UpdateResultsCounter()
        {
            int total = PatternRows.Count;
            int selected = 0;

            if (total > 0 && PatternsTable.SelectedIndex >= 0)
            {
                selected = PatternsTable.SelectedIndex + 1;
            }

            ResultsCounterText.Text = $"{selected} de {total}";
        }

        private static string BuildSevenDigitNumber(string pick3, string pick4)
        {
            return new string(
                (pick3 ?? " ")
                    .Where(char.IsDigit)
                    .Concat((pick4 ?? " ").Where(char.IsDigit))
                    .ToArray());
        }

        private static string? GetPos23Key(string number7)
        {
            if (string.IsNullOrWhiteSpace(number7) || number7.Length < 3)
            {
                return null;
            }

            return number7.Substring(1, 2);
        }

        private static string BuildAnchoredRowPattern(string pick3, string pick4, string nextPick3)
        {
            var nextDigits = (nextPick3 ?? " ").Where(char.IsDigit).Take(3).ToList();
            if (nextDigits.Count != 3)
            {
                return string.Empty;
            }

            var map = new Dictionary<char, char>
            {
                [nextDigits[0]] = 'A',
                [nextDigits[1]] = 'B',
                [nextDigits[2]] = 'C'
            };

            char nextLetter = 'D';

            string EncodeDigits(string value)
            {
                var digits = (value ?? " ").Where(char.IsDigit).ToList();
                var sb = new StringBuilder(digits.Count);
                foreach (var d in digits)
                {
                    if (!map.TryGetValue(d, out var letter))
                    {
                        letter = nextLetter;
                        map[d] = letter;
                        nextLetter++;
                    }

                    sb.Append(letter);
                }

                return sb.ToString();
            }

            string pick3Code = EncodeDigits(pick3);
            string pick4Code = EncodeDigits(pick4);
            const string nextCode = "ABC";

            return $"{pick3Code} {pick4Code} {nextCode}";
        }

        private static string BuildRowPattern(string pick3, string pick4, string nextPick3)
        {
            string rowNumber = new string((pick3 + pick4 + nextPick3).Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(rowNumber))
            {
                return "";
            }

            return BuildRepetitionPattern(rowNumber);
        }

        private static string BuildRepetitionPattern(string number)
        {
            var map = new Dictionary<char, char>();
            var next = 'A';
            var sb = new StringBuilder(number.Length);

            foreach (var d in number)
            {
                if (!map.TryGetValue(d, out var letter))
                {
                    letter = next;
                    map[d] = letter;
                    next++;
                }

                sb.Append(letter);
            }

            return sb.ToString();
        }

        private static bool HasSameCrossPositionEqualityPattern(string referenceTop, string referenceBottom, string candidateTop, string candidateBottom)
        {
            if (string.IsNullOrWhiteSpace(referenceTop) ||
                string.IsNullOrWhiteSpace(referenceBottom) ||
                string.IsNullOrWhiteSpace(candidateTop) ||
                string.IsNullOrWhiteSpace(candidateBottom))
            {
                return false;
            }

            if (referenceTop.Length != referenceBottom.Length ||
                candidateTop.Length != candidateBottom.Length ||
                referenceTop.Length != candidateTop.Length)
            {
                return false;
            }

            int len = referenceTop.Length;
            for (int i = 0; i < len; i++)
            {
                for (int j = 0; j < len; j++)
                {
                    bool referenceEqual = referenceTop[i] == referenceBottom[j];
                    bool candidateEqual = candidateTop[i] == candidateBottom[j];
                    if (referenceEqual != candidateEqual)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private sealed class CandidateRow
        {
            public required ComboHit Hit { get; set; }
            public string Number7 { get; set; } = "";
            public string Pos23 { get; set; } = "";
            public string Pattern { get; set; } = "";
            public DateTime Date { get; set; } = DateTime.MinValue;
            public string NextPick3 { get; set; } = " ";
        }
    }
}



























