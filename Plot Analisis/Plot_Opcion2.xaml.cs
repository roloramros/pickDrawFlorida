using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Media;
using FloridaLotteryApp.Data;

namespace FloridaLotteryApp
{
    public partial class Plot_Opcion2 : Window
    {
        public ObservableCollection<PlotOption2Session> Sessions { get; } = new();
        public ObservableCollection<PatternRow> PatternRows { get; } = new();

        private readonly string _guidePatternRow1;
        private readonly string _guidePatternRow2;
        private readonly string _guidePatternRow3;
        private readonly string _guidePatternRow4;
        private readonly string _guideRow2Date;
        private readonly string _guideRow2DrawTime;
        private readonly string _guideRow4Date;
        private readonly string _guideRow4DrawTime;
        private readonly string _guideRow34ConnectionPattern;

        private readonly DateTime _originalDate;
        private readonly string _originalDrawTime;
        private readonly string _originalDateText;
        private readonly string _originalPick3;
        private readonly string _originalPick4;

        private readonly HashSet<string> _seenRows = new();
        private bool _analysisStarted;
        private bool _isLoading;
        private int _currentResultIndex = -1;
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

        public Plot_Opcion2(string dateText, string drawIcon, string pick3, string pick4, string pick3Siguiente, PatternRow selectedRow)
        {
            InitializeComponent();
            DataContext = this;
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
        this.Top = 0;

            _originalWindowHeight = Height;
            _originalWindowWidth = Width;
            _expandedWindowHeight = Height + 20;

            _originalDateText = string.IsNullOrWhiteSpace(dateText) ? " " : dateText;
            _originalPick3 = pick3 ?? " ";
            _originalPick4 = pick4 ?? " ";
            _originalDrawTime = DrawTimeFromIcon(drawIcon);
            _originalDate = DateTime.TryParseExact(dateText, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var parsedDate)
                ? parsedDate
                : DateTime.MinValue;

            Sessions.Add(BuildTopSession(dateText, drawIcon, pick3, pick4, pick3Siguiente, selectedRow));
            Sessions.Add(BuildEmptySession());

            _guidePatternRow1 = BuildRowPattern(pick3 ?? " ", pick4 ?? " ", pick3Siguiente ?? " ");
            _guidePatternRow2 = BuildAnchoredRowPattern(selectedRow?.MatchPick3 ?? " ", selectedRow?.MatchPick4 ?? " ", selectedRow?.MatchNextPick3 ?? " ");
            _guidePatternRow3 = BuildRowPattern(selectedRow?.SimilarPick3 ?? " ", selectedRow?.SimilarPick4 ?? " ", selectedRow?.SimilarNextPick3 ?? " ");
            _guidePatternRow4 = BuildAnchoredRowPattern(selectedRow?.SimilarMatchPick3 ?? " ", selectedRow?.SimilarMatchPick4 ?? " ", selectedRow?.SimilarMatchNextPick3 ?? " ");
            _guideRow2Date = selectedRow?.MatchDate ?? " ";
            _guideRow2DrawTime = selectedRow?.MatchDrawTime ?? " ";
            _guideRow4Date = selectedRow?.SimilarMatchDate ?? " ";
            _guideRow4DrawTime = selectedRow?.SimilarMatchDrawTime ?? " ";
            _guideRow34ConnectionPattern = BuildConnectionPattern(selectedRow?.SimilarNextPick3, selectedRow?.SimilarMatchNextPick3);

            UpdateResultsCounter();
            UpdateNavigationButtons();

            Loaded += async (_, __) =>
            {
                await LoadPatternRowsRealtimeAsync();
                QueueDrawConnectingLines();
            };
        }

        private static PlotOption2Session BuildTopSession(string dateText, string drawIcon, string pick3, string pick4, string pick3Siguiente, PatternRow selectedRow)
        {
            var session = new PlotOption2Session
            {
                Row1 = CreateRow(pick3, pick4, pick3Siguiente, BuildCodificacionDigits(pick3, pick4), dateText, drawIcon),
                Row2 = CreateRow(selectedRow?.MatchPick3, selectedRow?.MatchPick4, selectedRow?.MatchNextPick3, ExtractDigits(selectedRow?.MatchCodificacion), selectedRow?.MatchDate, DrawIconFromTime(selectedRow?.MatchDrawTime)),
                Row3 = CreateRow(selectedRow?.SimilarPick3, selectedRow?.SimilarPick4, selectedRow?.SimilarNextPick3, ExtractDigits(selectedRow?.SimilarCodificacion), selectedRow?.SimilarDate, DrawIconFromTime(selectedRow?.SimilarDrawTime)),
                Row4 = CreateRow(selectedRow?.SimilarMatchPick3, selectedRow?.SimilarMatchPick4, selectedRow?.SimilarMatchNextPick3, ExtractDigits(selectedRow?.SimilarMatchCodificacion), selectedRow?.SimilarMatchDate, DrawIconFromTime(selectedRow?.SimilarMatchDrawTime))
            };

            AssignSessionTags(session, "Top");
            UpdateSessionDigitCells(session);
            return session;
        }

        private static PlotOption2Session BuildSessionFromPatternRow(PatternRow row)
        {
            var session = new PlotOption2Session
            {
                Row1 = CreateRow(row.ReferencePick3, row.ReferencePick4, row.ReferenceNextPick3, ExtractDigits(row.ReferenceCodificacion), row.ReferenceDate, DrawIconFromTime(row.ReferenceDrawTime)),
                Row2 = CreateRow(row.MatchPick3, row.MatchPick4, row.MatchNextPick3, ExtractDigits(row.MatchCodificacion), row.MatchDate, DrawIconFromTime(row.MatchDrawTime)),
                Row3 = CreateRow(row.SimilarPick3, row.SimilarPick4, row.SimilarNextPick3, ExtractDigits(row.SimilarCodificacion), row.SimilarDate, DrawIconFromTime(row.SimilarDrawTime)),
                Row4 = CreateRow(row.SimilarMatchPick3, row.SimilarMatchPick4, row.SimilarMatchNextPick3, ExtractDigits(row.SimilarMatchCodificacion), row.SimilarMatchDate, DrawIconFromTime(row.SimilarMatchDrawTime))
            };

            AssignSessionTags(session, "Bottom");
            UpdateSessionDigitCells(session);
            return session;
        }

        private static PlotOption2Session BuildEmptySession()
        {
            var session = new PlotOption2Session
            {
                Row1 = CreateRow(null, null, null, new List<string>(), " ", " "),
                Row2 = CreateRow(null, null, null, new List<string>(), " ", " "),
                Row3 = CreateRow(null, null, null, new List<string>(), " ", " "),
                Row4 = CreateRow(null, null, null, new List<string>(), " ", " ")
            };

            AssignSessionTags(session, "Bottom");
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

                if (_currentResultIndex < 0)
                {
                    _currentResultIndex = 0;
                    LoadBottomSession(row);
                    QueueDrawConnectingLines();
                }

                UpdateResultsCounter();
                UpdateNavigationButtons();
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
                UpdateResultsCounter();
                UpdateNavigationButtons();
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

        private PatternRow? CreatePatternRow(CandidateRow col2, CandidateRow col3, CandidateRow col4, string guideNumber, DateTime guideDate, string guideDrawTime, string guideNextPick3Value)
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
                string col2DateText = col2.Hit.Date.ToString("yyyy-MM-dd");
                string col4DateText = col4.Hit.Date.ToString("yyyy-MM-dd");

                if (row2Pattern != _guidePatternRow2 || row4Pattern != _guidePatternRow4 ||
                    !string.Equals(col2DateText, _guideRow2Date, StringComparison.Ordinal) ||
                    !string.Equals(col2.Hit.DrawTime, _guideRow2DrawTime, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(col4DateText, _guideRow4Date, StringComparison.Ordinal) ||
                    !string.Equals(col4.Hit.DrawTime, _guideRow4DrawTime, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                string row34ConnectionPattern = BuildConnectionPattern(col3NextPick3, col4NextPick3);
                if (row34ConnectionPattern != _guideRow34ConnectionPattern)
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

        private void LoadBottomSession(PatternRow row)
        {
            Sessions[1] = BuildSessionFromPatternRow(row);
        }

        private void Anterior_Click(object sender, RoutedEventArgs e)
        {
            if (_currentResultIndex <= 0)
            {
                return;
            }

            _currentResultIndex--;
            LoadBottomSession(PatternRows[_currentResultIndex]);
            QueueDrawConnectingLines();
            UpdateResultsCounter();
            UpdateNavigationButtons();
        }

        private void Siguiente_Click(object sender, RoutedEventArgs e)
        {
            if (_currentResultIndex < 0 || _currentResultIndex >= PatternRows.Count - 1)
            {
                return;
            }
            _currentResultIndex++;
            LoadBottomSession(PatternRows[_currentResultIndex]);
            QueueDrawConnectingLines();
            UpdateResultsCounter();
            UpdateNavigationButtons();
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

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            var current = GetCurrentResultItem();
            if (current == null)
            {
                MessageBox.Show("No hay un resultado visible para guardar.", 
                            "Guardar", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                return;
            }

            // ✅ Usar el nuevo AnalisisSaveDialog con el tipo de análisis fijo
            var saveDialog = new AnalisisSaveDialog("Plot Opcion 2");
            
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // ✅ Construir el record con los datos actuales
                    var record = BuildSavedRecord(current, saveDialog.NoteText);
                    
                    // ✅ Insertar en la base de datos con el tipo y folder correctos
                    using var conn = Db.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO saved_analisis (
                            label, tipo_analisis, folder,
                            g1_date, g1_time, g2_date, g2_time,
                            g3_date, g3_time, g4_date, g4_time,
                            r1_date, r1_time, r2_date, r2_time,
                            r3_date, r3_time, r4_date, r4_time
                        )
                        VALUES (
                            $label, $tipo_analisis, $folder,
                            $g1_date, $g1_time, $g2_date, $g2_time,
                            $g3_date, $g3_time, $g4_date, $g4_time,
                            $r1_date, $r1_time, $r2_date, $r2_time,
                            $r3_date, $r3_time, $r4_date, $r4_time
                        );
                    ";
                    
                    cmd.Parameters.AddWithValue("$label", (object?)record.Label ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$tipo_analisis", saveDialog.TipoAnalisis);
                    cmd.Parameters.AddWithValue("$folder", saveDialog.SelectedFolder);
                    cmd.Parameters.AddWithValue("$g1_date", record.G1Date);
                    cmd.Parameters.AddWithValue("$g1_time", record.G1Time);
                    cmd.Parameters.AddWithValue("$g2_date", record.G2Date);
                    cmd.Parameters.AddWithValue("$g2_time", record.G2Time);
                    cmd.Parameters.AddWithValue("$g3_date", record.G3Date);
                    cmd.Parameters.AddWithValue("$g3_time", record.G3Time);
                    cmd.Parameters.AddWithValue("$g4_date", record.G4Date);
                    cmd.Parameters.AddWithValue("$g4_time", record.G4Time);
                    cmd.Parameters.AddWithValue("$r1_date", record.R1Date);
                    cmd.Parameters.AddWithValue("$r1_time", record.R1Time);
                    cmd.Parameters.AddWithValue("$r2_date", record.R2Date);
                    cmd.Parameters.AddWithValue("$r2_time", record.R2Time);
                    cmd.Parameters.AddWithValue("$r3_date", record.R3Date);
                    cmd.Parameters.AddWithValue("$r3_time", record.R3Time);
                    cmd.Parameters.AddWithValue("$r4_date", record.R4Date);
                    cmd.Parameters.AddWithValue("$r4_time", record.R4Time);
                    
                    cmd.ExecuteNonQuery();
                    
                    MessageBox.Show("Análisis guardado correctamente.", 
                                "Guardado exitoso", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar el análisis: {ex.Message}", 
                                "Error", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
                }
            }
        }

        private void UpdateResultsCounter()
        {
            int total = PatternRows.Count;
            int selected = _currentResultIndex >= 0 && _currentResultIndex < total ? _currentResultIndex + 1 : 0;
            ResultsCounterText.Text = $"{selected} de {total}";
        }

        private void UpdateNavigationButtons()
        {
            AnteriorButton.IsEnabled = _currentResultIndex > 0;
            SiguienteButton.IsEnabled = _currentResultIndex >= 0 && _currentResultIndex < PatternRows.Count - 1;
            GuardarButton.IsEnabled = _currentResultIndex >= 0 && _currentResultIndex < PatternRows.Count;
        }

        private void SetLoadingState(bool isLoading, string status, int completed, int total, bool isIndeterminate)
        {
            UpdateWindowSize(isLoading);
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            CancelButton.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            CancelButton.IsEnabled = isLoading;

            AnalysisProgressText.Text = string.IsNullOrWhiteSpace(status) ? string.Empty : status;
            AnalysisProgressBar.IsIndeterminate = isIndeterminate;
            AnalysisProgressBar.Maximum = Math.Max(1, total);
            AnalysisProgressBar.Value = isIndeterminate ? 0 : Math.Min(Math.Max(0, completed), AnalysisProgressBar.Maximum);
        }

        private void UpdateWindowSize(bool isLoading)
        {
            Height = isLoading ? _expandedWindowHeight : _originalWindowHeight;
            Width = _originalWindowWidth;
        }
        private PatternRow? GetCurrentResultItem()
        {
            if (_currentResultIndex < 0 || _currentResultIndex >= PatternRows.Count)
            {
                return null;
            }

            return PatternRows[_currentResultIndex];
        }

        private PlotOption2SavedRecord BuildSavedRecord(PatternRow current, string label)
        {
            return new PlotOption2SavedRecord
            {
                Label = string.IsNullOrWhiteSpace(label) ? null : label,
                G1Date = NormalizeDate(_originalDateText),
                G1Time = NormalizeDrawTime(_originalDrawTime),
                G2Date = NormalizeDate(Sessions[0].Row2.DateText),
                G2Time = NormalizeDrawTime(DrawTimeFromIcon(Sessions[0].Row2.DrawIcon)),
                G3Date = NormalizeDate(Sessions[0].Row3.DateText),
                G3Time = NormalizeDrawTime(DrawTimeFromIcon(Sessions[0].Row3.DrawIcon)),
                G4Date = NormalizeDate(Sessions[0].Row4.DateText),
                G4Time = NormalizeDrawTime(DrawTimeFromIcon(Sessions[0].Row4.DrawIcon)),
                R1Date = NormalizeDate(current.ReferenceDate),
                R1Time = NormalizeDrawTime(current.ReferenceDrawTime),
                R2Date = NormalizeDate(current.MatchDate),
                R2Time = NormalizeDrawTime(current.MatchDrawTime),
                R3Date = NormalizeDate(current.SimilarDate),
                R3Time = NormalizeDrawTime(current.SimilarDrawTime),
                R4Date = NormalizeDate(current.SimilarMatchDate),
                R4Time = NormalizeDrawTime(current.SimilarMatchDrawTime)
            };
        }

        private static string NormalizeDate(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string NormalizeDrawTime(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
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

        private ItemsControl? FindItemsControlByTag(DependencyObject root, string tag)
        {
            return FindVisualChildren<ItemsControl>(root)
                .FirstOrDefault(c => string.Equals(c.Tag as string, tag, StringComparison.Ordinal));
        }

        private void ConnectPick3Digits(ItemsControl topItemsControl, ItemsControl bottomItemsControl, Canvas canvas, Brush lineColor)
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

        private List<Border> GetDigitContainers(ItemsControl itemsControl)
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

        private static bool HasAnyInternalRepeatsInRow(string? pick3, string? pick4, string? nextPick3)
        {
            return HasInternalRepeatedDigits(pick3) || HasInternalRepeatedDigits(pick4) || HasInternalRepeatedDigits(nextPick3);
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

        private static int GetDrawRecencyOrder(string? drawTime)
        {
            return string.Equals(drawTime, "E", StringComparison.OrdinalIgnoreCase) ? 2
                : string.Equals(drawTime, "M", StringComparison.OrdinalIgnoreCase) ? 1
                : 0;
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

        private static string BuildSevenDigitNumber(string pick3, string pick4)
        {
            return new string((pick3 ?? " ").Where(char.IsDigit).Concat((pick4 ?? " ").Where(char.IsDigit)).ToArray());
        }

        private static string? GetPos23Key(string number7)
        {
            if (string.IsNullOrWhiteSpace(number7) || number7.Length < 3)
            {
                return null;
            }

            return number7.Substring(1, 2);
        }

        private static string BuildConnectionPattern(string? topNextPick3, string? bottomNextPick3)
        {
            var topDigits = BuildFixedDigitSlots(topNextPick3, 3);
            var bottomDigits = BuildFixedDigitSlots(bottomNextPick3, 3);
            var connections = new List<string>();

            for (int i = 0; i < topDigits.Count; i++)
            {
                var top = topDigits[i]?.Trim();
                if (string.IsNullOrWhiteSpace(top))
                {
                    continue;
                }

                for (int j = 0; j < bottomDigits.Count; j++)
                {
                    var bottom = bottomDigits[j]?.Trim();
                    if (string.IsNullOrWhiteSpace(bottom))
                    {
                        continue;
                    }

                    if (string.Equals(top, bottom, StringComparison.Ordinal))
                    {
                        connections.Add($"{i}-{j}");
                    }
                }
            }

            return string.Join("|", connections);
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

            return $"{EncodeDigits(pick3)} {EncodeDigits(pick4)} ABC";
        }

        private static string BuildRowPattern(string pick3, string pick4, string nextPick3)
        {
            string rowNumber = new string((pick3 + pick4 + nextPick3).Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(rowNumber))
            {
                return string.Empty;
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
            if (string.IsNullOrWhiteSpace(referenceTop) || string.IsNullOrWhiteSpace(referenceBottom) || string.IsNullOrWhiteSpace(candidateTop) || string.IsNullOrWhiteSpace(candidateBottom))
            {
                return false;
            }

            if (referenceTop.Length != referenceBottom.Length || candidateTop.Length != candidateBottom.Length || referenceTop.Length != candidateTop.Length)
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
            var topBrushByProfile = topProfiles.Where(kv => topBrushByDigit.ContainsKey(kv.Key)).ToDictionary(kv => kv.Value, kv => topBrushByDigit[kv.Key]);

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

        private sealed class CandidateRow
        {
            public required ComboHit Hit { get; set; }
            public string Number7 { get; set; } = string.Empty;
            public string Pos23 { get; set; } = string.Empty;
            public string Pattern { get; set; } = string.Empty;
            public DateTime Date { get; set; } = DateTime.MinValue;
            public string NextPick3 { get; set; } = " ";
        }
    }

    public sealed class PlotOption2Session
    {
        public string SessionTag { get; set; } = string.Empty;
        public PlotOption2Row Row1 { get; set; } = new();
        public PlotOption2Row Row2 { get; set; } = new();
        public PlotOption2Row Row3 { get; set; } = new();
        public PlotOption2Row Row4 { get; set; } = new();
    }

    public sealed class PlotOption2Row
    {
        public string Pick3Value { get; set; } = " ";
        public string Pick4Value { get; set; } = " ";
        public List<PlotOption2DigitCell> Pick3Digits { get; set; } = new();
        public List<PlotOption2DigitCell> Pick4Digits { get; set; } = new();
        public List<string> NextPick3Digits { get; set; } = new();
        public string NextPick3Tag { get; set; } = string.Empty;
        public List<string> AdditionalDigits { get; set; } = new();
        public string DateText { get; set; } = " ";
        public string DrawIcon { get; set; } = " ";
    }

    public sealed class PlotOption2DigitCell
    {
        public string Value { get; set; } = " ";
        public Brush Background { get; set; } = Brushes.White;
    }
}








