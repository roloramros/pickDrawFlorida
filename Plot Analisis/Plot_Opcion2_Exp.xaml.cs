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
    public partial class Plot_Opcion2_Exp : Window
    {
        public ObservableCollection<PlotOption2Session> Sessions { get; } = new();
        public ObservableCollection<PatternRow> PatternRows { get; } = new();


        private readonly DateTime _originalDate;
        private readonly string _originalDrawTime;
        private readonly string _originalPick3;
        private readonly string _originalPick4;
        private readonly string _originalDateText;
        private readonly string _originalDrawIcon;
        private readonly string _originalPick3Siguiente;
        private readonly IReadOnlyList<PatternRow> _guideRows;

        private int _guidesWithResults;
        private int _currentResultIndex = -1;
        private readonly List<ExpandedResultItem> _resultItems = new();
        private int _currentGuideCompleted;
        private int _currentGuideTotal = 1;

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

        public Plot_Opcion2_Exp(string dateText, string drawIcon, string pick3, string pick4, string pick3Siguiente, IEnumerable<PatternRow> guideRows)
        {
            InitializeComponent();
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
        this.Top = 0;
            DataContext = this;

            _originalWindowHeight = Height;
            _originalWindowWidth = Width;
            _expandedWindowHeight = Height + 20;

            _originalDateText = string.IsNullOrWhiteSpace(dateText) ? " " : dateText;
            _originalDrawIcon = string.IsNullOrWhiteSpace(drawIcon) ? " " : drawIcon;
            _originalPick3 = pick3 ?? " ";
            _originalPick4 = pick4 ?? " ";
            _originalPick3Siguiente = pick3Siguiente ?? " ";
            _guideRows = (guideRows ?? Enumerable.Empty<PatternRow>()).ToList();

            _originalDrawTime = DrawTimeFromIcon(drawIcon);
            _originalDate = DateTime.TryParseExact(dateText, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var parsedDate)
                ? parsedDate
                : DateTime.MinValue;

            Sessions.Add(BuildEmptySession("Top"));
            Sessions.Add(BuildEmptySession("Bottom"));
            UpdateResultsCounter();
            UpdateNavigationButtons();

            Loaded += async (_, __) =>
            {
                await LoadPatternRowsRealtimeAsync();
                QueueDrawConnectingLines();
            };
        }

        private PlotOption2Session BuildGuideSession(PatternRow guideRow, string sessionTag)
        {
            var session = new PlotOption2Session
            {
                Row1 = CreateRow(_originalPick3, _originalPick4, _originalPick3Siguiente, BuildCodificacionDigits(_originalPick3, _originalPick4), _originalDateText, _originalDrawIcon),
                Row2 = CreateRow(guideRow?.MatchPick3, guideRow?.MatchPick4, guideRow?.MatchNextPick3, ExtractDigits(guideRow?.MatchCodificacion), guideRow?.MatchDate, DrawIconFromTime(guideRow?.MatchDrawTime)),
                Row3 = CreateRow(guideRow?.SimilarPick3, guideRow?.SimilarPick4, guideRow?.SimilarNextPick3, ExtractDigits(guideRow?.SimilarCodificacion), guideRow?.SimilarDate, DrawIconFromTime(guideRow?.SimilarDrawTime)),
                Row4 = CreateRow(guideRow?.SimilarMatchPick3, guideRow?.SimilarMatchPick4, guideRow?.SimilarMatchNextPick3, ExtractDigits(guideRow?.SimilarMatchCodificacion), guideRow?.SimilarMatchDate, DrawIconFromTime(guideRow?.SimilarMatchDrawTime))
            };

            AssignSessionTags(session, sessionTag);
            UpdateSessionDigitCells(session);
            return session;
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

        private static PlotOption2Session BuildSessionFromPatternRow(PatternRow row, string sessionTag)
        {
            var session = new PlotOption2Session
            {
                Row1 = CreateRow(row.ReferencePick3, row.ReferencePick4, row.ReferenceNextPick3, ExtractDigits(row.ReferenceCodificacion), row.ReferenceDate, DrawIconFromTime(row.ReferenceDrawTime)),
                Row2 = CreateRow(row.MatchPick3, row.MatchPick4, row.MatchNextPick3, ExtractDigits(row.MatchCodificacion), row.MatchDate, DrawIconFromTime(row.MatchDrawTime)),
                Row3 = CreateRow(row.SimilarPick3, row.SimilarPick4, row.SimilarNextPick3, ExtractDigits(row.SimilarCodificacion), row.SimilarDate, DrawIconFromTime(row.SimilarDrawTime)),
                Row4 = CreateRow(row.SimilarMatchPick3, row.SimilarMatchPick4, row.SimilarMatchNextPick3, ExtractDigits(row.SimilarMatchCodificacion), row.SimilarMatchDate, DrawIconFromTime(row.SimilarMatchDrawTime))
            };

            AssignSessionTags(session, sessionTag);
            UpdateSessionDigitCells(session);
            return session;
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
            SetLoadingState(true, "Analizando guias...", 0, Math.Max(1, _guideRows.Count), false);

            var progress = new Progress<GuideAnalysisBatch>(batch =>
            {
                ApplyGuideResultBatch(batch);
            });

            var statusProgress = new Progress<(int processed, int total, int guidesWithResults, int guideCompleted, int guideTotal)>(status =>
            {
                _currentGuideCompleted = status.guideCompleted;
                _currentGuideTotal = Math.Max(1, status.guideTotal);
                SetLoadingState(
                    true,
                    $"Analizando guias... {status.processed} de {status.total} | Guias con resultados: {status.guidesWithResults}",
                    status.processed,
                    Math.Max(1, status.total),
                    false);
            });

            try
            {
                await Task.Run(() => RunAnalysisForAllGuides(progress, statusProgress, token), token);

                if (_resultItems.Count == 0)
                {
                    MessageBox.Show("Ninguna guia valida produjo resultados para Analisis Opcion 2 Expandido.",
                        "Sin resultados", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _currentResultIndex = 0;
                    LoadCurrentResult();
                }
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
            IProgress<GuideAnalysisBatch> progress,
            IProgress<(int processed, int total, int guidesWithResults, int guideCompleted, int guideTotal)> statusProgress,
            CancellationToken cancellationToken)
        {
            if (_guideRows.Count == 0)
            {
                return;
            }

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

            int processed = 0;
            int guidesWithResults = 0;
            int totalGuides = _guideRows.Count;

            foreach (var guideRow in _guideRows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processed++;

                var results = RunAnalysisForSingleGuide(guideRow, orderedGuides, byPos23, byPattern, byPos23Pattern, statusProgress, processed, totalGuides, cancellationToken);
                if (results.Count > 0)
                {
                    guidesWithResults++;
                    progress.Report(new GuideAnalysisBatch
                    {
                        GuideRow = guideRow,
                        Results = results
                    });
                }

                statusProgress.Report((processed, totalGuides, guidesWithResults, _currentGuideTotal, _currentGuideTotal));
            }
        }

        private List<PatternRow> RunAnalysisForSingleGuide(
            PatternRow guideRow,
            IReadOnlyList<CandidateRow> orderedGuides,
            IReadOnlyDictionary<string, List<CandidateRow>> byPos23,
            IReadOnlyDictionary<string, List<CandidateRow>> byPattern,
            IReadOnlyDictionary<string, List<CandidateRow>> byPos23Pattern,
            IProgress<(int processed, int total, int guidesWithResults, int guideCompleted, int guideTotal)> statusProgress,
            int processedGuides,
            int totalGuides,
            CancellationToken cancellationToken)
        {
            var criteria = BuildGuideCriteria(guideRow);
            var results = new List<PatternRow>();
            var seenRows = new HashSet<string>();
            int guideStep = 0;
            int guideTotal = orderedGuides.Count;

            foreach (var guide in orderedGuides)
            {
                cancellationToken.ThrowIfCancellationRequested();
                guideStep++;
                statusProgress.Report((processedGuides, totalGuides, _guidesWithResults, guideStep, guideTotal));


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

                            var row = CreatePatternRow(col2, col3, col4, guide.Number7, guide.Date, guide.Hit.DrawTime, guide.NextPick3, criteria);
                            if (row != null && seenRows.Add(BuildUniqueResultKey(row)))
                            {
                                results.Add(row);
                            }
                        }
                    }
                }
            }

            return results;
        }

        private PatternRow? CreatePatternRow(CandidateRow col2, CandidateRow col3, CandidateRow col4, string guideNumber, DateTime guideDate, string guideDrawTime, string guideNextPick3Value, GuideCriteria criteria)
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

                if (row2Pattern != criteria.GuidePatternRow2 || row4Pattern != criteria.GuidePatternRow4 ||
                    !string.Equals(col2DateText, criteria.GuideRow2Date, StringComparison.Ordinal) ||
                    !string.Equals(col2.Hit.DrawTime, criteria.GuideRow2DrawTime, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(col4DateText, criteria.GuideRow4Date, StringComparison.Ordinal) ||
                    !string.Equals(col4.Hit.DrawTime, criteria.GuideRow4DrawTime, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                string row34ConnectionPattern = BuildConnectionPattern(col3NextPick3, col4NextPick3);
                if (row34ConnectionPattern != criteria.GuideRow34ConnectionPattern)
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

        private void ApplyGuideResultBatch(GuideAnalysisBatch batch)
        {
            foreach (var result in batch.Results)
            {
                _resultItems.Add(new ExpandedResultItem
                {
                    GuideRow = batch.GuideRow,
                    ResultRow = result
                });
            }

            if (_currentResultIndex < 0 && _resultItems.Count > 0)
            {
                _currentResultIndex = 0;
                LoadCurrentResult();
                return;
            }

            _guidesWithResults++;
            UpdateResultsCounter();
            UpdateNavigationButtons();
        }

        private void LoadCurrentResult()
        {
            if (_currentResultIndex < 0 || _currentResultIndex >= _resultItems.Count)
            {
                Sessions[0] = BuildEmptySession("Top");
                Sessions[1] = BuildEmptySession("Bottom");
            }
            else
            {
                var current = _resultItems[_currentResultIndex];
                Sessions[0] = BuildGuideSession(current.GuideRow, "Top");
                Sessions[1] = BuildSessionFromPatternRow(current.ResultRow, "Bottom");
            }

            UpdateResultsCounter();
            UpdateNavigationButtons();
            QueueDrawConnectingLines();
        }

        private void Anterior_Click(object sender, RoutedEventArgs e)
        {
            if (_currentResultIndex <= 0)
            {
                return;
            }

            _currentResultIndex--;
            LoadCurrentResult();
        }

        private void Siguiente_Click(object sender, RoutedEventArgs e)
        {
            if (_currentResultIndex < 0 || _currentResultIndex >= _resultItems.Count - 1)
            {
                return;
            }

            _currentResultIndex++;
            LoadCurrentResult();
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
            int total = _resultItems.Count;
            int selected = _currentResultIndex >= 0 && _currentResultIndex < total ? _currentResultIndex + 1 : 0;
            ResultsCounterText.Text = $"{selected} de {total}";
        }

        private void UpdateNavigationButtons()
        {
            AnteriorButton.IsEnabled = _currentResultIndex > 0;
            SiguienteButton.IsEnabled = _currentResultIndex >= 0 && _currentResultIndex < _resultItems.Count - 1;
            GuardarButton.IsEnabled = _currentResultIndex >= 0 && _currentResultIndex < _resultItems.Count;
        }

        private void SetLoadingState(bool isLoading, string status, int completed, int total, bool isIndeterminate)
        {
            UpdateWindowSize(isLoading);
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            CancelButton.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            CancelButton.IsEnabled = isLoading;

            AnalysisProgressText.Text = string.IsNullOrWhiteSpace(status) ? string.Empty : status;
            OverallProgressBar.IsIndeterminate = isIndeterminate;
            OverallProgressBar.Maximum = Math.Max(1, total);
            OverallProgressBar.Value = isIndeterminate ? 0 : Math.Min(Math.Max(0, completed), OverallProgressBar.Maximum);
            GuideProgressBar.IsIndeterminate = false;
            GuideProgressBar.Maximum = Math.Max(1, _currentGuideTotal);
            GuideProgressBar.Value = isLoading ? Math.Min(Math.Max(0, _currentGuideCompleted), GuideProgressBar.Maximum) : 0;
            UpdateNavigationButtons();
        }

        private void UpdateWindowSize(bool isLoading)
        {
            Height = isLoading ? _expandedWindowHeight : _originalWindowHeight;
            Width = _originalWindowWidth;
        }
        private ExpandedResultItem? GetCurrentResultItem()
        {
            if (_currentResultIndex < 0 || _currentResultIndex >= _resultItems.Count)
            {
                return null;
            }

            return _resultItems[_currentResultIndex];
        }

        private PlotOption2SavedRecord BuildSavedRecord(ExpandedResultItem current, string label)
        {
            return new PlotOption2SavedRecord
            {
                Label = string.IsNullOrWhiteSpace(label) ? null : label,
                G1Date = NormalizeDate(_originalDateText),
                G1Time = NormalizeDrawTime(_originalDrawTime),
                G2Date = NormalizeDate(current.GuideRow.MatchDate),
                G2Time = NormalizeDrawTime(current.GuideRow.MatchDrawTime),
                G3Date = NormalizeDate(current.GuideRow.SimilarDate),
                G3Time = NormalizeDrawTime(current.GuideRow.SimilarDrawTime),
                G4Date = NormalizeDate(current.GuideRow.SimilarMatchDate),
                G4Time = NormalizeDrawTime(current.GuideRow.SimilarMatchDrawTime),
                R1Date = NormalizeDate(current.ResultRow.ReferenceDate),
                R1Time = NormalizeDrawTime(current.ResultRow.ReferenceDrawTime),
                R2Date = NormalizeDate(current.ResultRow.MatchDate),
                R2Time = NormalizeDrawTime(current.ResultRow.MatchDrawTime),
                R3Date = NormalizeDate(current.ResultRow.SimilarDate),
                R3Time = NormalizeDrawTime(current.ResultRow.SimilarDrawTime),
                R4Date = NormalizeDate(current.ResultRow.SimilarMatchDate),
                R4Time = NormalizeDrawTime(current.ResultRow.SimilarMatchDrawTime)
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

        private GuideCriteria BuildGuideCriteria(PatternRow guideRow)
        {
            return new GuideCriteria
            {
                GuidePatternRow2 = BuildAnchoredRowPattern(guideRow?.MatchPick3 ?? " ", guideRow?.MatchPick4 ?? " ", guideRow?.MatchNextPick3 ?? " "),
                GuidePatternRow4 = BuildAnchoredRowPattern(guideRow?.SimilarMatchPick3 ?? " ", guideRow?.SimilarMatchPick4 ?? " ", guideRow?.SimilarMatchNextPick3 ?? " "),
                GuideRow2Date = guideRow?.MatchDate ?? " ",
                GuideRow2DrawTime = guideRow?.MatchDrawTime ?? " ",
                GuideRow4Date = guideRow?.SimilarMatchDate ?? " ",
                GuideRow4DrawTime = guideRow?.SimilarMatchDrawTime ?? " ",
                GuideRow34ConnectionPattern = BuildConnectionPattern(guideRow?.SimilarNextPick3, guideRow?.SimilarMatchNextPick3)
            };
        }

        private sealed class GuideAnalysisBatch
        {
            public PatternRow GuideRow { get; set; } = new();
            public List<PatternRow> Results { get; set; } = new();
        }

        private sealed class ExpandedResultItem
        {
            public PatternRow GuideRow { get; set; } = new();
            public PatternRow ResultRow { get; set; } = new();
        }

        private sealed class GuideCriteria
        {
            public string GuidePatternRow2 { get; set; } = string.Empty;
            public string GuidePatternRow4 { get; set; } = string.Empty;
            public string GuideRow2Date { get; set; } = string.Empty;
            public string GuideRow2DrawTime { get; set; } = string.Empty;
            public string GuideRow4Date { get; set; } = string.Empty;
            public string GuideRow4DrawTime { get; set; } = string.Empty;
            public string GuideRow34ConnectionPattern { get; set; } = string.Empty;
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
}




