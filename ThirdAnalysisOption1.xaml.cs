using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using FloridaLotteryApp.Data;

namespace FloridaLotteryApp;

public enum AnalysisMode
{
    Opcion1,
    Opcion2
}

public partial class ThirdAnalysisOption1 : Window, INotifyPropertyChanged
{
    private ThirdAnalysisCardVM? _currentCard;
    private int _currentIndex = -1;
    private ThirdAnalysisCardVM? _currentSearchCard;
    private AnalysisMode _currentMode;
    
    public ObservableCollection<ThirdAnalysisCardVM> SearchCards { get; } = new ObservableCollection<ThirdAnalysisCardVM>();
    
    public ThirdAnalysisCardVM? CurrentSearchCard
    {
        get => _currentSearchCard;
        set
        {
            _currentSearchCard = value;
            OnPropertyChanged(nameof(CurrentSearchCard));
            UpdateSearchCardUI();
            UpdateNavigationButtons();
        }
    }
    
    private sealed record CuartetoOccurrence(FilteredCodificacion Row1, FilteredCodificacion Row2, FilteredCodificacion Row3, FilteredCodificacion Row4);

    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ThirdAnalysisOption1()
    {
        InitializeComponent();
        DataContext = this;
        
        // Inicializar botones deshabilitados
        if (PreviousButton != null) PreviousButton.IsEnabled = false;
        if (NextButton != null) NextButton.IsEnabled = false;
    }

    public ThirdAnalysisOption1(ThirdAnalysisCardVM card, AnalysisMode mode)
    {
        InitializeComponent();
        DataContext = this;
        _currentCard = card;
        _currentMode = mode; // Guardar el modo
        SearchTab.IsSelected = true;
        Loaded += ThirdAnalysisOption1_Loaded;

        LoadCard(card);
        Loaded += (_, _) => QueueDrawAnalysisCardConnectingLines();
       
    }

    private async void ThirdAnalysisOption1_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= ThirdAnalysisOption1_Loaded;
        await LoadFilteredCodificacionesAsync();
    }

    private async Task LoadFilteredCodificacionesAsync()
    {
        try
        {
            ShowProgress(true, "Cargando codificaciones...");
            ResultsProgress.IsIndeterminate = true;

            var codificaciones = await Task.Run(
                () => DrawRepository.GetCodificacionesWithSingleCommonDigit());      

            if (_currentCard == null)
            {
                ResultsCounter.Text = "Resultados: 0 encontrados";
                return;
            }

            // ======= EXTRAER EL GUIDEPAIR DEL CURRENT_CARD =======
            string row3Full = DigitsToString(_currentCard.Row3Pick3Digits) + DigitsToString(_currentCard.Row3Pick4Digits) + DigitsToString(_currentCard.Row3NextPick3Digits);
            string row1Full = DigitsToString(_currentCard.Row1Pick3Digits) + DigitsToString(_currentCard.Row1Pick4Digits) + DigitsToString(_currentCard.Row1NextPick3Digits);
            var guidePair = (row3Full, row1Full); // (tercera fila, primera fila)
            

            // ======= CREAR LISTA DE TODOS LOS FULLNUMBERS =======
            // Extraer FullNumber de cada codificación
            var validCodificaciones = codificaciones
                .Where(c => !string.IsNullOrWhiteSpace(c.FullNumber) && c.FullNumber.Length == 10)
                .ToList();

            var codificacionesByFullNumber = validCodificaciones
                .GroupBy(c => c.FullNumber)
                .ToDictionary(g => g.Key, g => g.ToList());

            var allFullNumbers = codificacionesByFullNumber.Keys.ToList();

            // ======= PROCESAR CADA FULLNUMBER =======
            ResultsProgress.IsIndeterminate = false;
            ResultsProgress.Minimum = 0;
            ResultsProgress.Maximum = allFullNumbers.Count;
            ResultsProgress.Value = 0;
            ResultsProgressText.Text = $"Procesando 0 de {allFullNumbers.Count}...";

            IProgress<int> progress = new System.Progress<int>(value =>
            {
                ResultsProgress.Value = value;
                ResultsProgressText.Text = $"Procesando {value} de {allFullNumbers.Count}...";
            });

            var allResults = await Task.Run(() =>
            {
                var results = new List<(string Original, string Compatible)>();
                int processedCount = 0;

                foreach (var fullNumber in allFullNumbers)
                {
                    var resultsForThisNumber = FindFilteredPairs(
                        fullNumberOriginal: fullNumber,
                        allFullNumbers: allFullNumbers,
                        guidePair: guidePair);

                    results.AddRange(resultsForThisNumber);
                    processedCount++;
                    progress.Report(processedCount);
                }

                progress.Report(processedCount);
                return results;
            });

            var validCuartetos = FormCuartetosFromPairs(allResults);

            string mensaje = $"Cuartetos válidos encontrados: {validCuartetos.Count}";

            string pick4MatchesRow1Row2 = _currentCard?.Pick4MatchesRow1Row2 ?? "0C";
            string codingMatchesRow1Row2 = _currentCard?.CodingMatchesRow1Row2 ?? "0C";
            string pick4MatchesRow3Row4 = _currentCard?.Pick4MatchesRow3Row4 ?? "0C";
            string codingMatchesRow3Row4 = _currentCard?.CodingMatchesRow3Row4 ?? "0C";

            // 2. Filtrar cuartetos por contadores
            var cuartetosFiltrados = FilterCuartetosByCounters(
            mode: _currentMode,  // Usar Opcion1 por defecto
            cuartetos: validCuartetos,
            refPick4MatchesRow1Row2: pick4MatchesRow1Row2,
            refCodingMatchesRow1Row2: codingMatchesRow1Row2,
            refPick4MatchesRow3Row4: pick4MatchesRow3Row4,
            refCodingMatchesRow3Row4: codingMatchesRow3Row4);
            
            var cuartetoOccurrences = ExpandCuartetosWithOccurrences(
                cuartetosFiltrados,
                codificacionesByFullNumber);

            ResultsProgress.IsIndeterminate = false;
            ResultsProgress.Minimum = 0;
            ResultsProgress.Maximum = cuartetoOccurrences.Count;
            ResultsProgress.Value = 0;
            ResultsProgressText.Text = $"Construyendo 0 de {cuartetoOccurrences.Count}...";

            SearchCards.Clear();
            int builtCount = 0;
            foreach (var cuarteto in cuartetoOccurrences)
            {
                SearchCards.Add(BuildCardFromCuarteto(cuarteto));
                builtCount++;
                ResultsProgress.Value = builtCount;
                ResultsProgressText.Text = $"Construyendo {builtCount} de {cuartetoOccurrences.Count}...";
            }
            // Condición: Row3 (mayor) > Row1 (intermedia) > Row2 (menor)
            var filteredByDate = new ObservableCollection<ThirdAnalysisCardVM>();
            int skippedCount = 0;

            foreach (var card in SearchCards)
            {
                try
                {
                    // Parsear las fechas de las tres filas importantes
                    DateTime row2Date = DateTime.Parse(card.Row2DateText);  // Fecha menor (más antigua)
                    DateTime row1Date = DateTime.Parse(card.Row1DateText);  // Fecha intermedia
                    DateTime row3Date = DateTime.Parse(card.Row3DateText);  // Fecha mayor (más reciente)
                    
                    // Verificar el orden: Row2 < Row1 < Row3
                    // Es decir: Fecha más antigua < Fecha intermedia < Fecha más reciente
                    if (row2Date < row1Date && row1Date < row3Date)
                    {
                        // Orden correcto: Row2 (antigua) -> Row1 (intermedia) -> Row3 (reciente)
                        filteredByDate.Add(card);
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parseando fechas: {ex.Message}");
                    skippedCount++;
                    continue;
                }
            }

            // Reemplazar SearchCards con las filtradas
            SearchCards.Clear();
            foreach (var card in filteredByDate)
            {
                SearchCards.Add(card);
            }








            if (SearchCards.Count > 0)
            {
                _currentIndex = 0;
                CurrentSearchCard = SearchCards[0];
                UpdateResultsCounter();
            }
            else
            {
                ResultsCounter.Text = "Resultados: 0 encontrados";
            }

            return;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar codificaciones: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ShowProgress(false, string.Empty);
        }
    }

    public List<(string Original, string Compatible)> FindFilteredPairs(string fullNumberOriginal, List<string> allFullNumbers, (string First, string Second) guidePair)
    {
        var result = new List<(string, string)>();
        
        // Validación básica
        if (string.IsNullOrWhiteSpace(fullNumberOriginal) || fullNumberOriginal.Length != 10)
            return result;
        
        if (allFullNumbers == null || allFullNumbers.Count == 0)
            return result;
        
        // 1. Extraer y analizar el fullNumberOriginal
        string originalP3 = fullNumberOriginal.Substring(0, 3);
        string originalP4 = fullNumberOriginal.Substring(3, 4);
        
        // Encontrar dígito repetido y posiciones en el original
        char? originalRepeatedDigit = null;
        int originalPosP3 = -1;
        int originalPosP4 = -1;
        
        for (int i = 0; i < 3; i++)
        {
            char digit = originalP3[i];
            int posInP4 = originalP4.IndexOf(digit);
            if (posInP4 >= 0)
            {
                originalRepeatedDigit = digit;
                originalPosP3 = i;
                originalPosP4 = posInP4;
                break;
            }
        }
        
        // Si el original no tiene dígito repetido, retornar vacío
        if (!originalRepeatedDigit.HasValue)
            return result;
        
        // 2. Analizar el patrón de la guidePair
        bool[] guidePattern = new bool[3];
        string guideP3_1 = guidePair.First.Substring(0, 3);
        string guideP3_2 = guidePair.Second.Substring(0, 3);
        
        for (int i = 0; i < 3; i++)
        {
            guidePattern[i] = (guideP3_1[i] == guideP3_2[i]);
        }
        
        // 3. Procesar cada candidato en allFullNumbers
        foreach (string candidate in allFullNumbers)
        {
            // Saltar si es el mismo número original
            if (candidate == fullNumberOriginal)
                continue;
            
            // Validar que tenga 10 dígitos
            if (string.IsNullOrWhiteSpace(candidate) || candidate.Length != 10)
                continue;
            
            // 3.1 Extraer Pick3 y Pick4 del candidato
            string candidateP3 = candidate.Substring(0, 3);
            string candidateP4 = candidate.Substring(3, 4);
            
            // 3.2 Encontrar dígito repetido en el candidato
            char? candidateRepeatedDigit = null;
            int candidatePosP3 = -1;
            int candidatePosP4 = -1;
            
            for (int i = 0; i < 3; i++)
            {
                char digit = candidateP3[i];
                int posInP4 = candidateP4.IndexOf(digit);
                if (posInP4 >= 0)
                {
                    candidateRepeatedDigit = digit;
                    candidatePosP3 = i;
                    candidatePosP4 = posInP4;
                    break;
                }
            }
            
            // Si el candidato no tiene dígito repetido, saltar
            if (!candidateRepeatedDigit.HasValue)
                continue;
            
            // 3.3 Primer filtro: mismas posiciones del dígito repetido
            if (originalPosP3 != candidatePosP3 || originalPosP4 != candidatePosP4)
                continue;
            
            // 3.4 Segundo filtro: mismo patrón que guidePair
            bool candidatePatternValid = true;
            
            // Comparar las posiciones de los Pick3 del par (original, candidato)
            for (int i = 0; i < 3; i++)
            {
                bool positionsAreEqual = (originalP3[i] == candidateP3[i]);
                
                // El patrón debe coincidir exactamente
                if (guidePattern[i] != positionsAreEqual)
                {
                    candidatePatternValid = false;
                    break;
                }
            }
            
            // 3.5 Si pasa ambos filtros, agregar a resultados
            if (candidatePatternValid)
            {
                result.Add((fullNumberOriginal, candidate));
            }
        }
        
        return result;
    }

    private void MatchGuiaButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentCard == null)
        {
            MessageBox.Show("No hay patrón guía disponible para filtrar.", "Match Guía",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var guideSourcesRow3ToRow4 = GetGuideSourcePositionsToFirstTwo(
            DigitsToString(_currentCard.Row3Pick3Digits),
            DigitsToString(_currentCard.Row4Pick4Digits));

        var guideSourcesRow4ToRow3 = GetGuideSourcePositionsToFirstTwo(
            DigitsToString(_currentCard.Row4Pick3Digits),
            DigitsToString(_currentCard.Row3Pick4Digits));

        // Segunda fase: mismo patrón para filas 1 y 2,
        // permitiendo conexiones desde Pick3 hacia cualquier dígito de Pick4.
        var guideSourcesRow1ToRow2 = GetGuideSourcePositionsToAny(
            DigitsToString(_currentCard.Row1Pick3Digits),
            DigitsToString(_currentCard.Row2Pick4Digits));

        var guideSourcesRow2ToRow1 = GetGuideSourcePositionsToAny(
            DigitsToString(_currentCard.Row2Pick3Digits),
            DigitsToString(_currentCard.Row1Pick4Digits));

        var filteredCards = new ObservableCollection<ThirdAnalysisCardVM>();

        foreach (var card in SearchCards)
        {
            bool matchesGuide = true;

            var cardSourcesRow3ToRow4 = GetGuideSourcePositionsToFirstTwo(
                DigitsToString(card.Row3Pick3Digits),
                DigitsToString(card.Row4Pick4Digits));

            var cardSourcesRow4ToRow3 = GetGuideSourcePositionsToFirstTwo(
                DigitsToString(card.Row4Pick3Digits),
                DigitsToString(card.Row3Pick4Digits));

            bool guideHasRow3ToRow4 = guideSourcesRow3ToRow4.Count > 0;
            bool guideHasRow4ToRow3 = guideSourcesRow4ToRow3.Count > 0;
            bool cardHasRow3ToRow4FirstTwo = cardSourcesRow3ToRow4.Count > 0;
            bool cardHasRow4ToRow3FirstTwo = cardSourcesRow4ToRow3.Count > 0;

            bool cardHasAnyRow3ToRow4 = HasAnyConnectionPick3ToPick4(
                DigitsToString(card.Row3Pick3Digits),
                DigitsToString(card.Row4Pick4Digits));

            bool cardHasAnyRow4ToRow3 = HasAnyConnectionPick3ToPick4(
                DigitsToString(card.Row4Pick3Digits),
                DigitsToString(card.Row3Pick4Digits));

            // Debe salir de los mismos lados que en el patrón guía (arriba/abajo), sin mezclar.
            // Si el guía no sale por un lado, el resultado no puede tener ninguna línea por ese lado.
            if ((guideHasRow3ToRow4 && !cardHasRow3ToRow4FirstTwo) ||
                (!guideHasRow3ToRow4 && cardHasAnyRow3ToRow4) ||
                (guideHasRow4ToRow3 && !cardHasRow4ToRow3FirstTwo) ||
                (!guideHasRow4ToRow3 && cardHasAnyRow4ToRow3))
            {
                matchesGuide = false;
            }

            if (matchesGuide && guideHasRow3ToRow4)
            {
                matchesGuide &= MatchesGuideSourceRule(
                    DigitsToString(card.Row3Pick3Digits),
                    DigitsToString(card.Row4Pick4Digits),
                    guideSourcesRow3ToRow4);
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

            if (matchesGuide && guideHasRow4ToRow3)
            {
                matchesGuide &= MatchesGuideSourceRule(
                    DigitsToString(card.Row4Pick3Digits),
                    DigitsToString(card.Row3Pick4Digits),
                    guideSourcesRow4ToRow3);
            }

            if (matchesGuide)
            {
                filteredCards.Add(card);
            }
        }

        SearchCards.Clear();
        foreach (var card in filteredCards)
        {
            SearchCards.Add(card);
        }

        if (SearchCards.Count > 0)
        {
            _currentIndex = 0;
            CurrentSearchCard = SearchCards[0];
            UpdateResultsCounter();
        }
        else
        {
            ResultsCounter.Text = "Resultados: 0 encontrados (match guía aplicado)";
            CurrentSearchCard = null;
        }

        UpdateNavigationButtons();
    }

    private static List<int> GetGuideSourcePositionsToFirstTwo(string pick3, string pick4)
    {
        var sourcePositions = new List<int>();

        if (string.IsNullOrWhiteSpace(pick3) || pick3.Length != 3 ||
            string.IsNullOrWhiteSpace(pick4) || pick4.Length < 2)
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

            if (pick4[0] == sourceDigit || pick4[1] == sourceDigit)
            {
                sourcePositions.Add(pick3Index);
            }
        }

        return sourcePositions;
    }

    private static bool MatchesGuideSourceRule(string pick3, string pick4, List<int> guideSourcePositions)
    {
        if (guideSourcePositions == null || guideSourcePositions.Count == 0)
        {
            return true;
        }

        var candidateSourcePositions = GetGuideSourcePositionsToFirstTwo(pick3, pick4);

        // Regla adicional: no permitir líneas hacia posiciones 3 o 4 del Pick4.
        if (HasAnyConnectionToLastTwo(pick3, pick4))
        {
            return false;
        }

        if (guideSourcePositions.Count == 1)
        {
            // Regla pedida: si el guía sale de un solo dígito,
            // el resultado también debe salir únicamente de ese mismo dígito.
            return candidateSourcePositions.Count == 1 &&
                   candidateSourcePositions[0] == guideSourcePositions[0];
        }

        // Para guías con múltiples dígitos de salida, se exige el mismo conjunto de posiciones.
        return candidateSourcePositions.Count == guideSourcePositions.Count &&
               candidateSourcePositions.All(guideSourcePositions.Contains);
    }

    private static bool HasAnyConnectionToLastTwo(string pick3, string pick4)
    {
        if (string.IsNullOrWhiteSpace(pick3) || pick3.Length != 3 ||
            string.IsNullOrWhiteSpace(pick4) || pick4.Length < 4)
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

            if (pick4[2] == sourceDigit || pick4[3] == sourceDigit)
            {
                return true;
            }
        }

        return false;
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
            return candidateSourcePositions.Count == 1 &&
                   candidateSourcePositions[0] == guideSourcePositions[0];
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


    public List<(string First, string Second, string Third, string Fourth)> FormCuartetosFromPairs(List<(string Original, string Compatible)> allPairs)
    {
        var allValidCuartetos = new List<(string, string, string, string)>();
        
        // 1. Agrupar todas las parejas por su Original
        var groupsByOriginal = new Dictionary<string, List<string>>();
        
        foreach (var pair in allPairs)
        {
            if (!groupsByOriginal.ContainsKey(pair.Original))
            {
                groupsByOriginal[pair.Original] = new List<string>();
            }
            
            // Evitar duplicados dentro del mismo grupo
            if (!groupsByOriginal[pair.Original].Contains(pair.Compatible))
            {
                groupsByOriginal[pair.Original].Add(pair.Compatible);
            }
        }

        // Reiniciar progreso para construcción de cuartetos por cada grupo
        ResultsProgress.IsIndeterminate = false;
        ResultsProgress.Minimum = 0;
        ResultsProgress.Value = 0;
        ResultsProgressText.Text = "Construyendo 0 de 0...";
        
        // 2. Procesar cada grupo
        int groupIndex = 0;
        int totalGroups = groupsByOriginal.Count;
        foreach (var group in groupsByOriginal)
        {
            groupIndex++;
            string original = group.Key;        // Primero
            List<string> compatibles = group.Value;
            
            // Necesitamos al menos 2 compatibles para formar cuartetos
            if (compatibles.Count >= 2)
            {
                long expectedTotal = (long)compatibles.Count * (compatibles.Count - 1) / 2;
                ResultsProgress.Maximum = expectedTotal > 0 ? expectedTotal : 1;
                ResultsProgress.Value = 0;
                ResultsProgressText.Text = $"Construyendo 0 de {expectedTotal} (grupo {groupIndex}/{totalGroups})...";
                long processedCuartetos = 0;

                // 3. Generar todas las combinaciones de 2 compatibles
                for (int i = 0; i < compatibles.Count - 1; i++)
                {
                    for (int j = i + 1; j < compatibles.Count; j++)
                    {
                        processedCuartetos++;
                        ResultsProgress.Value = processedCuartetos;
                        ResultsProgressText.Text = $"Construyendo {processedCuartetos} de {expectedTotal} (grupo {groupIndex}/{totalGroups})...";

                        string compatible1 = compatibles[i];  // Segundo
                        string compatible2 = compatibles[j];  // Tercero
                        
                        // 4. Extraer los Pick3 de cada FullNumber (primeros 3 dígitos)
                        string pick3_1 = original.Substring(0, 3);
                        string pick3_2 = compatible1.Substring(0, 3);
                        string pick3_3 = compatible2.Substring(0, 3);
                        
                        // 5. Validar según la regla de los Pick3
                        bool isValid = true;
                        
                        // Contar posiciones con los 3 dígitos iguales
                        int equalPositions = 0;
                        for (int pos = 0; pos < 3; pos++)
                        {
                            if (pick3_1[pos] == pick3_2[pos] && pick3_2[pos] == pick3_3[pos])
                            {
                                equalPositions++;
                            }
                        }
                        
                        // Para posiciones no-iguales, reunir todos los dígitos
                        var digitsFromNonEqualPositions = new List<char>();
                        
                        for (int pos = 0; pos < 3; pos++)
                        {
                            // Solo si NO es una posición con los 3 iguales
                            if (!(pick3_1[pos] == pick3_2[pos] && pick3_2[pos] == pick3_3[pos]))
                            {
                                digitsFromNonEqualPositions.Add(pick3_1[pos]);
                                digitsFromNonEqualPositions.Add(pick3_2[pos]);
                                digitsFromNonEqualPositions.Add(pick3_3[pos]);
                            }
                        }
                        
                        // Verificar que todos los dígitos en posiciones no-iguales sean diferentes
                        if (digitsFromNonEqualPositions.Count > 0)
                        {
                            // Contar dígitos únicos
                            var uniqueDigits = new HashSet<char>();
                            foreach (char digit in digitsFromNonEqualPositions)
                            {
                                uniqueDigits.Add(digit);
                            }
                            
                            // Si hay menos únicos que totales, hay repeticiones
                            if (uniqueDigits.Count != digitsFromNonEqualPositions.Count)
                            {
                                isValid = false;
                            }
                        }
                        
                        // 6. Si pasa la validación de Pick3, verificar nueva regla del NextPick3
                        if (isValid)
                        {
                            // NUEVA VALIDACIÓN: Los últimos 3 dígitos del compatible2 (NextPick3) 
                            // no pueden tener dígitos repetidos
                            string nextPick3 = compatible2.Substring(7, 3);  // Últimos 3 dígitos
                            
                            bool nextPick3Valid = true;
                            
                            // Verificar que los 3 dígitos sean diferentes
                            if (nextPick3.Length == 3)
                            {
                                // Usar HashSet para contar dígitos únicos
                                var uniqueNextPickDigits = new HashSet<char>();
                                foreach (char digit in nextPick3)
                                {
                                    uniqueNextPickDigits.Add(digit);
                                }
                                
                                // Si hay menos de 3 dígitos únicos, hay repetición
                                if (uniqueNextPickDigits.Count < 3)
                                {
                                    nextPick3Valid = false;
                                }
                            }
                            else
                            {
                                nextPick3Valid = false;  // Si no tiene 3 dígitos, es inválido
                            }
                            
                            // Solo agregar si pasa TODAS las validaciones
                            if (nextPick3Valid)
                            {
                                // Orden del cuarteto:
                                // 1. Segundo (compatible1)
                                // 2. Tercero (compatible2)  ← Este debe tener NextPick3 sin dígitos repetidos
                                // 3. Primero (original)
                                // 4. Tercero nuevamente (compatible2)
                                allValidCuartetos.Add((
                                    compatible1,    // Primero del cuarteto
                                    compatible2,    // Segundo del cuarteto (validar NextPick3)
                                    original,       // Tercero del cuarteto
                                    compatible2     // Cuarto del cuarteto (repetición del tercero)
                                ));
                            }
                        }
                    }
                }
            }
        }
        
        return allValidCuartetos;
    }

    public List<(string First, string Second, string Third, string Fourth)> FilterCuartetosByCounters(
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
            // Los 4 FullNumbers del cuarteto
            string row1Full = cuarteto.First;   // Primera fila
            string row2Full = cuarteto.Second;  // Segunda fila (tiene restricción de NextPick3)
            string row3Full = cuarteto.Third;   // Tercera fila
            string row4Full = cuarteto.Fourth;  // Cuarta fila = Segunda fila
            
            // 1. Extraer Pick4 de cada fila (dígitos 3-6)
            string row1Pick4 = row1Full.Substring(3, 4);
            string row2Pick4 = row2Full.Substring(3, 4);
            string row3Pick4 = row3Full.Substring(3, 4);
            string row4Pick4 = row4Full.Substring(3, 4);
            
            // 2. Calcular Coding de cada fila (Pick3 + Pick4, dígitos únicos ordenados)
            string row1Coding = CalculateCoding(row1Full.Substring(0, 3), row1Pick4);
            string row2Coding = CalculateCoding(row2Full.Substring(0, 3), row2Pick4);
            string row3Coding = CalculateCoding(row3Full.Substring(0, 3), row3Pick4);
            string row4Coding = CalculateCoding(row4Full.Substring(0, 3), row4Pick4);
            
            // 3. Calcular contadores
            int pick4MatchesRow1Row2 = CountMatches(row1Pick4, row2Pick4);
            int codingMatchesRow1Row2 = CountMatches(row1Coding, row2Coding);
            int pick4MatchesRow3Row4 = CountMatches(row3Pick4, row4Pick4);
            int codingMatchesRow3Row4 = CountMatches(row3Coding, row4Coding);
            
            // 4. Convertir a formato de contador (ej: "1C")
            string cuartetoPick4MatchesRow1Row2 = $"{pick4MatchesRow1Row2}C";
            string cuartetoCodingMatchesRow1Row2 = $"{codingMatchesRow1Row2}C";
            string cuartetoPick4MatchesRow3Row4 = $"{pick4MatchesRow3Row4}C";
            string cuartetoCodingMatchesRow3Row4 = $"{codingMatchesRow3Row4}C";

            

            switch (mode)
            {
                case AnalysisMode.Opcion1:
                        bool countersMatch = 
                        cuartetoPick4MatchesRow1Row2 == refPick4MatchesRow1Row2 &&
                        cuartetoCodingMatchesRow1Row2 == refCodingMatchesRow1Row2 &&
                        cuartetoPick4MatchesRow3Row4 == refPick4MatchesRow3Row4 &&
                        cuartetoCodingMatchesRow3Row4 == refCodingMatchesRow3Row4;
                    // 6. Si coinciden todos los contadores, agregar a resultados
                    if (countersMatch)
                    {
                        filteredCuartetos.Add(cuarteto);
                    }
                    break;
                case AnalysisMode.Opcion2:
                    bool pick4CountersMatch = 
                    cuartetoPick4MatchesRow1Row2 == refPick4MatchesRow1Row2 &&
                    cuartetoPick4MatchesRow3Row4 == refPick4MatchesRow3Row4;
                
                // XOR: exactamente uno debe ser verdadero
                bool codingCountersXor = 
                    (cuartetoCodingMatchesRow1Row2 == refCodingMatchesRow1Row2) ^ 
                    (cuartetoCodingMatchesRow3Row4 == refCodingMatchesRow3Row4);
                
                if (pick4CountersMatch && codingCountersXor)
                {
                    filteredCuartetos.Add(cuarteto);
                }
                    break;
            }            
        }
        return filteredCuartetos;
    }



    // Función auxiliar para calcular Coding
    private static string CalculateCoding(string pick3, string pick4)
    {
        // Unir Pick3 + Pick4, tomar dígitos únicos, ordenar
        var allDigits = (pick3 + pick4).Where(char.IsDigit).Distinct().OrderBy(c => c);
        return new string(allDigits.ToArray());
    }

    // Función auxiliar para contar coincidencias entre dos strings
    private int CountMatches(string str1, string str2)
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

    private void ShowProgress(bool isVisible, string message)
    {
        var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        ResultsProgress.Visibility = visibility;
        ResultsProgressText.Visibility = visibility;
        ResultsProgressText.Text = message;
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

        ApplyMatchColors(card);

        return card;
    }

    private void SearchCardBorder_Loaded(object sender, RoutedEventArgs e)
    {
        // Este método ya no es necesario ya que usamos una tarjeta individual
        // Pero lo mantenemos por si acaso
    }

    private void LoadCard(ThirdAnalysisCardVM card)
    {
        try
        {
            // Cargar datos de la tarjeta
            Row1DateText.Text = card.Row1DateText;
            Row1DrawIcon.Text = card.Row1DrawIcon;
            Row1Pick3Items.ItemsSource = CopyDigitCollection(card.Row1Pick3Digits);
            Row1Pick4Items.ItemsSource = CopyDigitCollection(card.Row1Pick4Digits);
            Row1NextPick3Items.ItemsSource = CopyDigitCollection(card.Row1NextPick3Digits);
            Row1CodingItems.ItemsSource = CopyDigitCollection(card.Row1CodingDigits);
            
            Row2DateText.Text = card.Row2DateText;
            Row2DrawIcon.Text = card.Row2DrawIcon;
            Row2Pick3Items.ItemsSource = CopyDigitCollection(card.Row2Pick3Digits);
            Row2Pick4Items.ItemsSource = CopyDigitCollection(card.Row2Pick4Digits);
            Row2NextPick3Items.ItemsSource = CopyDigitCollection(card.Row2NextPick3Digits);
            Row2CodingItems.ItemsSource = CopyDigitCollection(card.Row2CodingDigits);
            
            Row3DateText.Text = card.Row3DateText;
            Row3DrawIcon.Text = card.Row3DrawIcon;
            Row3Pick3Items.ItemsSource = CopyDigitCollection(card.Row3Pick3Digits);
            Row3Pick4Items.ItemsSource = CopyDigitCollection(card.Row3Pick4Digits);
            Row3NextPick3Items.ItemsSource = CopyDigitCollection(card.Row3NextPick3Digits);
            Row3CodingItems.ItemsSource = CopyDigitCollection(card.Row3CodingDigits);
            
            Row4DateText.Text = card.Row4DateText;
            Row4DrawIcon.Text = card.Row4DrawIcon;
            Row4Pick3Items.ItemsSource = CopyDigitCollection(card.Row4Pick3Digits);
            Row4Pick4Items.ItemsSource = CopyDigitCollection(card.Row4Pick4Digits);
            Row4NextPick3Items.ItemsSource = CopyDigitCollection(card.Row4NextPick3Digits);
            Row4CodingItems.ItemsSource = CopyDigitCollection(card.Row4CodingDigits);
            
            // Cargar contadores
            Pick4MatchesRow1Row2.Text = card.Pick4MatchesRow1Row2 ?? "0C";
            CodingMatchesRow1Row2.Text = card.CodingMatchesRow1Row2 ?? "0C";
            Pick4MatchesRow3Row4.Text = card.Pick4MatchesRow3Row4 ?? "0C";
            CodingMatchesRow3Row4.Text = card.CodingMatchesRow3Row4 ?? "0C";
            

            // Esperar a que la UI se cargue completamente antes de dibujar las líneas
            this.Loaded += OnWindowLoaded;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar la tarjeta: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static bool TryBuildFirstAnalysisGuide(FilteredCodificacion codificacion, out GuideInfo guide, out List<AnalysisRow> rows)
    {
        guide = new GuideInfo();
        rows = new List<AnalysisRow>();

        var pick3 = codificacion.Pick3.Trim();
        var pick4 = codificacion.Pick4.Trim();

        if (pick3.Length != 3 || pick4.Length != 4)
        {
            return false;
        }

        if (pick3.Distinct().Count() != 3 || pick4.Distinct().Count() != 4)
        {
            return false;
        }

        var commonDigits = pick3.Intersect(pick4).ToList();
        if (commonDigits.Count != 1)
        {
            return false;
        }

        char repeatedDigit = commonDigits[0];
        int posP3 = pick3.IndexOf(repeatedDigit, StringComparison.Ordinal) + 1;
        int posP4 = pick4.IndexOf(repeatedDigit, StringComparison.Ordinal) + 1;

        var guideDate = codificacion.Date;
        var guideDrawTime = codificacion.DrawTime;

        var hits = DrawRepository.FindPositionMatches(posP3, posP4);

        rows = hits
            .Where(h => !IsSamePattern(h.Pick3, h.Pick4, pick3, pick4))
            .Where(h => IsBeforeGuide(h.Date, h.DrawTime, guideDate, guideDrawTime))
            .Select(h => new
            {
                Hit = h,
                NextPick3 = DrawRepository.GetNextPick3Number(h.Date, h.DrawTime)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.NextPick3))
            .Select(x => new AnalysisRow
            {
                IsChecked = false,
                Date = x.Hit.Date.ToString("yyyy-MM-dd"),
                DrawTime = x.Hit.DrawTime == "M" ? "☀️" : "🌙",
                Pick3 = x.Hit.Pick3,
                Pick4 = x.Hit.Pick4,
                NextPick3 = x.NextPick3!,
                Coding = BuildCoding(x.Hit.Pick3, x.Hit.Pick4)
            })
            .ToList();

        guide = new GuideInfo
        {
            Pick3 = pick3,
            Pick4 = pick4,
            NextPick3 = DrawRepository.GetNextPick3Number(guideDate, guideDrawTime) ?? "",
            Coding = BuildCoding(pick3, pick4),
            DateText = guideDate.ToString("yyyy-MM-dd"),
            DrawIcon = DrawTimeToIcon(guideDrawTime),
            RepPosP3 = posP3,
            RepPosP4 = posP4
        };

        return rows.Count > 0;
    }

    private bool MatchesGuideCounters(ThirdAnalysisCardVM card)
    {
        if (_currentCard == null)
        {
            return false;
        }

        return NormalizeCounter(card.Pick4MatchesRow1Row2) == NormalizeCounter(_currentCard.Pick4MatchesRow1Row2)
            && NormalizeCounter(card.CodingMatchesRow1Row2) == NormalizeCounter(_currentCard.CodingMatchesRow1Row2)
            && NormalizeCounter(card.Pick4MatchesRow3Row4) == NormalizeCounter(_currentCard.Pick4MatchesRow3Row4)
            && NormalizeCounter(card.CodingMatchesRow3Row4) == NormalizeCounter(_currentCard.CodingMatchesRow3Row4);
    }

    private static string NormalizeCounter(string? value)
        => string.IsNullOrWhiteSpace(value) ? "0C" : value.Trim();


    private static string DrawTimeToIcon(string drawTime)
    {
        return drawTime switch
        {
            "M" => "☀️",
            "E" => "🌙",
            _ => ""
        };
    }

    private static string BuildCoding(string pick3, string pick4)
    {
        return new string((pick3 + pick4)
            .Where(char.IsDigit)
            .Distinct()
            .OrderBy(c => c)
            .ToArray());
    }

    private static bool IsSamePattern(string pick3, string pick4, string inputPick3, string inputPick4)
    {
        return string.Equals(pick3, inputPick3, StringComparison.Ordinal)
            && string.Equals(pick4, inputPick4, StringComparison.Ordinal);
    }

    private static bool IsBeforeGuide(DateTime hitDate, string hitDrawTime, DateTime guideDate, string? guideDrawTime)
    {
        if (hitDate.Date < guideDate.Date)
        {
            return true;
        }

        if (hitDate.Date > guideDate.Date)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(guideDrawTime))
        {
            return false;
        }

        return guideDrawTime == "E" && hitDrawTime == "M";
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

    private static ObservableCollection<DigitVM> BuildCodingDigits(string pick3, string pick4)
    {
        var allDigits = (pick3 + pick4)
            .Where(char.IsDigit)
            .Select(d => int.Parse(d.ToString()))
            .Distinct()
            .OrderBy(d => d)
            .Select(d => d.ToString())
            .ToList();

        while (allDigits.Count < 6)
        {
            allDigits.Add("");
        }

        return new ObservableCollection<DigitVM>(
            allDigits.Take(6)
                .Select(digit => new DigitVM
                {
                    Value = digit,
                    Bg = CreateFrozenBrush(Colors.White)
                }));
    }

    private static string DigitsToString(IEnumerable<DigitVM> digits)
        => string.Concat(digits.Select(d => d.Value));

    private static bool[]? BuildPick3RepeatPattern(string row1Pick3, string row3Pick3)
    {
        if (string.IsNullOrWhiteSpace(row1Pick3) || row1Pick3.Length != 3 ||
            string.IsNullOrWhiteSpace(row3Pick3) || row3Pick3.Length != 3)
        {
            return null;
        }

        var pattern = new bool[3];
        for (int i = 0; i < 3; i++)
        {
            char top = row1Pick3[i];
            char bottom = row3Pick3[i];

            if (!char.IsDigit(top) || !char.IsDigit(bottom))
            {
                return null;
            }

            pattern[i] = top == bottom;
        }

        return pattern;
    }

    private static bool MatchesPick3RepeatPattern(ThirdAnalysisCardVM card, bool[] guidePattern)
    {
        var cardPattern = BuildPick3RepeatPattern(
            DigitsToString(card.Row1Pick3Digits),
            DigitsToString(card.Row3Pick3Digits));

        if (cardPattern == null || guidePattern.Length != 3)
        {
            return false;
        }

        for (int i = 0; i < 3; i++)
        {
            if (cardPattern[i] != guidePattern[i])
            {
                return false;
            }
        }

        return true;
    }

    private static (char digit, int pick3Position, int pick4Position)? GetRepeatedDigitPositions(string pick3, string pick4)
    {
        if (string.IsNullOrWhiteSpace(pick3) || pick3.Length != 3 ||
            string.IsNullOrWhiteSpace(pick4) || pick4.Length != 4)
            return null;

        for (int i = 0; i < pick3.Length; i++)
        {
            char digit = pick3[i];
            int positionInPick4 = pick4.IndexOf(digit, StringComparison.Ordinal);

            if (positionInPick4 >= 0)
            {
                return (digit, i, positionInPick4);
            }
        }

        return null;
    }

    private void SetAllFixedPick3Values()
    {
        // Intentionally empty: the search tab now uses an ItemsControl DataTemplate.
    }
    
    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Esperar un poco más para asegurar que todos los controles estén renderizados
        Dispatcher.BeginInvoke(new Action(() =>
        {
            QueueDrawAnalysisCardConnectingLines();
        }), DispatcherPriority.ContextIdle);
    }

    private void QueueDrawAnalysisCardConnectingLines()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            Dispatcher.BeginInvoke(new Action(CreateAnalysisCardConnectingLines), DispatcherPriority.Render);
        }), DispatcherPriority.Loaded);
    }

    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AnalysisTab?.IsSelected == true)
        {
            QueueDrawAnalysisCardConnectingLines();
        }
    }

    private void CreateAnalysisCardConnectingLines()
    {
        try
        {
            var analysisCard = AnalysisCardContainer;
            if (analysisCard == null)
            {
                return;
            }

            var row1Pick3Items = FindVisualChild<ItemsControl>(analysisCard, "Row1Pick3Items");
            var row1NextPick3Items = FindVisualChild<ItemsControl>(analysisCard, "Row1NextPick3Items");
            var row1Pick4Items = FindVisualChild<ItemsControl>(analysisCard, "Row1Pick4Items");

            var row2Pick3Items = FindVisualChild<ItemsControl>(analysisCard, "Row2Pick3Items");
            var row2NextPick3Items = FindVisualChild<ItemsControl>(analysisCard, "Row2NextPick3Items");
            var row2Pick4Items = FindVisualChild<ItemsControl>(analysisCard, "Row2Pick4Items");

            var row3Pick3Items = FindVisualChild<ItemsControl>(analysisCard, "Row3Pick3Items");
            var row3NextPick3Items = FindVisualChild<ItemsControl>(analysisCard, "Row3NextPick3Items");
            var row3Pick4Items = FindVisualChild<ItemsControl>(analysisCard, "Row3Pick4Items");

            var row4Pick3Items = FindVisualChild<ItemsControl>(analysisCard, "Row4Pick3Items");
            var row4NextPick3Items = FindVisualChild<ItemsControl>(analysisCard, "Row4NextPick3Items");
            var row4Pick4Items = FindVisualChild<ItemsControl>(analysisCard, "Row4Pick4Items");

            var pick3LinksCanvas = FindVisualChild<Canvas>(analysisCard, "Pick3LinksCanvas");
            var nextPick3LinksCanvas = FindVisualChild<Canvas>(analysisCard, "NextPick3LinksCanvas");
            var pick3Row3ToPick4Row4LinksCanvas = FindVisualChild<Canvas>(analysisCard, "Pick3Row3ToPick4Row4LinksCanvas");
            var pick3Row4ToPick4Row3LinksCanvas = FindVisualChild<Canvas>(analysisCard, "Pick3Row4ToPick4Row3LinksCanvas");
            var pick3Row1ToPick4Row2LinksCanvas = FindVisualChild<Canvas>(analysisCard, "Pick3Row1ToPick4Row2LinksCanvas");
            var pick3Row2ToPick4Row1LinksCanvas = FindVisualChild<Canvas>(analysisCard, "Pick3Row2ToPick4Row1LinksCanvas");

            if (row1Pick3Items == null || row2Pick3Items == null ||
                row1NextPick3Items == null || row2NextPick3Items == null ||
                row3Pick3Items == null || row4Pick3Items == null ||
                row3NextPick3Items == null || row4NextPick3Items == null ||
                row3Pick4Items == null || row4Pick4Items == null ||
                row1Pick4Items == null || row2Pick4Items == null ||
                pick3LinksCanvas == null || nextPick3LinksCanvas == null ||
                pick3Row3ToPick4Row4LinksCanvas == null || pick3Row4ToPick4Row3LinksCanvas == null ||
                pick3Row1ToPick4Row2LinksCanvas == null || pick3Row2ToPick4Row1LinksCanvas == null)
            {
                return;
            }

            pick3LinksCanvas.Children.Clear();
            nextPick3LinksCanvas.Children.Clear();
            pick3Row3ToPick4Row4LinksCanvas.Children.Clear();
            pick3Row4ToPick4Row3LinksCanvas.Children.Clear();
            pick3Row1ToPick4Row2LinksCanvas.Children.Clear();
            pick3Row2ToPick4Row1LinksCanvas.Children.Clear();

            ConnectPick3Digits(row1Pick3Items, row2Pick3Items, pick3LinksCanvas);
            ConnectPick3Digits(row1NextPick3Items, row2NextPick3Items, nextPick3LinksCanvas);
            ConnectPick3Digits(row3Pick3Items, row4Pick3Items, pick3LinksCanvas);
            ConnectPick3Digits(row3NextPick3Items, row4NextPick3Items, nextPick3LinksCanvas);

            ConnectPick3ToPick4Digits(row3Pick3Items, row4Pick4Items, pick3Row3ToPick4Row4LinksCanvas);
            ConnectPick3ToPick4Digits(row4Pick3Items, row3Pick4Items, pick3Row4ToPick4Row3LinksCanvas);

            ConnectPick3ToPick4DigitsWithRedLine(row1Pick3Items, row2Pick4Items, pick3Row1ToPick4Row2LinksCanvas);
            ConnectPick3ToPick4DigitsWithRedLine(row2Pick3Items, row1Pick4Items, pick3Row2ToPick4Row1LinksCanvas);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creando líneas de patrón guía: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    private void ForceItemsControlUpdate(ItemsControl itemsControl)
    {
        // Forzar la actualización del ItemsControl para que genere sus contenedores
        itemsControl.UpdateLayout();
        itemsControl.ApplyTemplate();
        
        // Si es un ItemsControl con ItemContainerGenerator, forzar la generación
        if (itemsControl.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
        {
            itemsControl.ItemContainerGenerator.StatusChanged += (s, e) => 
            {
                if (itemsControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                {
                    // Los contenedores están listos
                }
            };
        }
    }

    private void ConnectPick3Digits(ItemsControl topItemsControl, ItemsControl bottomItemsControl, Canvas canvas)
    {
        if (topItemsControl == null || bottomItemsControl == null || canvas == null)
            return;

        try
        {
            // Obtener los contenedores de los dígitos
            var topDigits = GetDigitContainers(topItemsControl);
            var bottomDigits = GetDigitContainers(bottomItemsControl);
            

            if (topDigits.Count != 3 || bottomDigits.Count != 3)
            {
                return;
            }

            // Conectar dígitos que coincidan
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
                        // Dibujar línea conectando los dígitos
                        DrawConnectingLine(canvas, topDigit, bottomDigit);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error conectando Pick3 dígitos: {ex.Message}");
        }
    }

    private void ConnectPick3ToPick4Digits(ItemsControl pick3ItemsControl, ItemsControl pick4ItemsControl, Canvas canvas)
    {
        if (pick3ItemsControl == null || pick4ItemsControl == null || canvas == null)
            return;
        try
        {
            // Obtener los contenedores de los dígitos
            var pick3Digits = GetDigitContainers(pick3ItemsControl);
            var pick4Digits = GetDigitContainers(pick4ItemsControl);

            if (pick3Digits.Count != 3 || pick4Digits.Count != 4)
            {
                return;
            }

            // Limpiar canvas antes de dibujar
            canvas.Children.Clear();

            // Conectar dígitos que coincidan
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
                        // Dibujar línea conectando los dígitos
                        DrawConnectingLine(canvas, pick3Digit, pick4Digit);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error conectando Pick3 a Pick4: {ex.Message}");
        }
    }

    private List<Border> GetDigitContainers(ItemsControl itemsControl)
    {
        var containers = new List<Border>();
        
        try
        {
            if (itemsControl == null)
            {
                return containers;
            }

            // 1. PRIMERO: Intentar usar ItemContainerGenerator
            if (itemsControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                for (int i = 0; i < itemsControl.Items.Count; i++)
                {
                    var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                    if (container != null)
                    {
                        // Buscar el Border dentro del ContentPresenter
                        var border = FindVisualChild<Border>(container);
                        if (border != null)
                        {
                            containers.Add(border);
                        }
                    }
                }
            }

            // 2. SEGUNDO: Si no encontró contenedores, usar el método visual
            if (containers.Count == 0)
            {
                
                // Buscar ContentPresenters en el árbol visual
                var contentPresenters = FindVisualChildren<ContentPresenter>(itemsControl).ToList();
                
                foreach (var cp in contentPresenters)
                {
                    var border = FindVisualChild<Border>(cp);
                    if (border != null)
                    {
                        containers.Add(border);
                    }
                }
            }

            // 3. TERCERO: Si aún no encontró, buscar directamente Borders
            if (containers.Count == 0)
            {
                var allBorders = FindVisualChildren<Border>(itemsControl).ToList();
                // Filtrar solo los Borders que tienen TextBlock como hijo
                foreach (var border in allBorders)
                {
                    var textBlock = FindVisualChild<TextBlock>(border);
                    if (textBlock != null && !string.IsNullOrEmpty(textBlock.Text))
                    {
                        containers.Add(border);
                    }
                }
            }

            

            
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error en GetDigitContainers: {ex.Message}\n{ex.StackTrace}");
        }
        
        return containers;
    }

    private string GetDigitText(Border digitBorder)
    {
        try
        {
            var textBlock = FindVisualChild<TextBlock>(digitBorder);
            return textBlock?.Text ?? "";
        }
        catch
        {
            return "";
        }
    }

    private void DrawConnectingLine(Canvas canvas, Border element1, Border element2)
    {
        try
        {
            // Verificar que los elementos tengan dimensiones válidas
            if (element1.ActualWidth == 0 || element1.ActualHeight == 0 ||
                element2.ActualWidth == 0 || element2.ActualHeight == 0)
                return;

            // Calcular centros absolutos
            var center1 = element1.TranslatePoint(new Point(element1.ActualWidth / 2, element1.ActualHeight / 2), canvas);
            var center2 = element2.TranslatePoint(new Point(element2.ActualWidth / 2, element2.ActualHeight / 2), canvas);
            
            // Calcular vector dirección de element1 a element2
            double dx = center2.X - center1.X;
            double dy = center2.Y - center1.Y;
            
            // Calcular distancia entre centros
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            // Si están en la misma posición, no dibujar línea
            if (distance == 0) return;
            
            // Calcular radio de cada círculo (mitad del ancho, ya que son círculos)
            double radius1 = element1.ActualWidth / 2;
            double radius2 = element2.ActualWidth / 2;
            
            // Normalizar vector dirección
            double unitX = dx / distance;
            double unitY = dy / distance;
            
            // Calcular puntos en los bordes
            Point startPoint = new Point(
                center1.X + (unitX * radius1),
                center1.Y + (unitY * radius1)
            );
            
            Point endPoint = new Point(
                center2.X - (unitX * radius2),
                center2.Y - (unitY * radius2)
            );
            
            // Crear línea CONTINUA NEGRA
            var line = new Line
            {
                X1 = startPoint.X,
                Y1 = startPoint.Y,
                X2 = endPoint.X,
                Y2 = endPoint.Y,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            
            canvas.Children.Add(line);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error dibujando línea: {ex.Message}");
        }
    }

    private T FindVisualChild<T>(DependencyObject parent, string childName = null) where T : DependencyObject
    {
        if (parent == null) return null;

        try
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                // Buscar por nombre si se especifica
                if (child is FrameworkElement frameworkElement && 
                    !string.IsNullOrEmpty(childName) && 
                    frameworkElement.Name == childName)
                {
                    return child as T;
                }
                
                // Buscar por tipo
                if (child is T result && (string.IsNullOrEmpty(childName) || childName == result.GetValue(FrameworkElement.NameProperty) as string))
                {
                    return result;
                }
                
                // Buscar recursivamente
                var foundChild = FindVisualChild<T>(child, childName);
                if (foundChild != null) return foundChild;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en FindVisualChild: {ex.Message}");
        }
        
        return null;
    }

    private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        var results = new List<T>();
        
        if (parent == null) return results;

        try
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T childOfType)
                {
                    results.Add(childOfType);
                }

                foreach (var descendant in FindVisualChildren<T>(child))
                {
                    results.Add(descendant);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en FindVisualChildren: {ex.Message}");
        }
        
        return results;
    }

    private static ObservableCollection<DigitVM> CopyDigitCollection(ObservableCollection<DigitVM> source)
    {
        var copy = new ObservableCollection<DigitVM>();
        foreach (var digit in source)
        {
            Color color = Colors.White;
            
            // Verificar si el brush es SolidColorBrush y obtener su color
            if (digit.Bg is SolidColorBrush solidBrush)
            {
                color = solidBrush.Color;
            }
            
            copy.Add(new DigitVM
            {
                Value = digit.Value,
                Bg = CreateFrozenBrush(color)  // Usar el color original
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

    private static void ApplyMatchColors(ThirdAnalysisCardVM card)
    {
        int codingMatches1 = ColorRepeatedDigitsInCollections(
            card.Row1CodingDigits,
            card.Row2CodingDigits,
            Colors.LightPink);
        card.CodingMatchesRow1Row2 = $"{codingMatches1}C";

        int pick4Matches1 = ColorRepeatedDigitsInCollections(
            card.Row1Pick4Digits,
            card.Row2Pick4Digits,
            Colors.LightBlue);
        card.Pick4MatchesRow1Row2 = $"{pick4Matches1}C";

        int codingMatches2 = ColorRepeatedDigitsInCollections(
            card.Row3CodingDigits,
            card.Row4CodingDigits,
            Colors.LightPink);
        card.CodingMatchesRow3Row4 = $"{codingMatches2}C";

        int pick4Matches2 = ColorRepeatedDigitsInCollections(
            card.Row3Pick4Digits,
            card.Row4Pick4Digits,
            Colors.LightBlue);
        card.Pick4MatchesRow3Row4 = $"{pick4Matches2}C";

        ColorRepeatedDigitInRow4(card);
    }

    private static int ColorRepeatedDigitsInCollections(
        ObservableCollection<DigitVM> collection1,
        ObservableCollection<DigitVM> collection2,
        Color highlightColor)
    {
        var values1 = collection1.Select(d => d.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
        var values2 = collection2.Select(d => d.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        var repeatedValues = values1.Intersect(values2).ToList();
        int matchCount = repeatedValues.Count;

        ColorRepeatedDigits(collection1, repeatedValues, highlightColor);
        ColorRepeatedDigits(collection2, repeatedValues, highlightColor);

        return matchCount;
    }

    private static void ColorRepeatedDigits(
        ObservableCollection<DigitVM> collection,
        List<string> repeatedValues,
        Color highlightColor)
    {
        foreach (var digitVM in collection)
        {
            if (repeatedValues.Contains(digitVM.Value))
            {
                digitVM.Bg = CreateFrozenBrush(highlightColor);
            }
            else
            {
                digitVM.Bg = CreateFrozenBrush(Colors.White);
            }
        }
    }

    private static void ColorRepeatedDigitInRow4(ThirdAnalysisCardVM card)
    {
        var pick3Digits = card.Row4Pick3Digits.Select(d => d.Value).ToList();
        var pick4Digits = card.Row4Pick4Digits.Select(d => d.Value).ToList();

        var repeatedDigits = pick3Digits.Intersect(pick4Digits).ToList();
        if (repeatedDigits.Count != 1)
        {
            return;
        }

        string repeatedDigit = repeatedDigits[0];
        var nextPick3Digits = card.Row4NextPick3Digits.Select(d => d.Value).ToList();

        if (!nextPick3Digits.Contains(repeatedDigit))
        {
            return;
        }

        foreach (var digitVM in card.Row4Pick3Digits)
        {
            if (digitVM.Value == repeatedDigit)
            {
                digitVM.Bg = CreateFrozenBrush(Colors.LightBlue);
            }
        }

        foreach (var digitVM in card.Row4Pick4Digits)
        {
            if (digitVM.Value == repeatedDigit)
            {
                digitVM.Bg = CreateFrozenBrush(Colors.LightBlue);
            }
        }

        foreach (var digitVM in card.Row4NextPick3Digits)
        {
            if (digitVM.Value == repeatedDigit)
            {
                digitVM.Bg = CreateFrozenBrush(Colors.LightBlue);
            }
        }
    }



    private void analysisButton1(object sender, RoutedEventArgs e)
    {
    }
    
    private void UpdateResultsCounter()
    {
        ResultsCounter.Text = $"Resultado: {_currentIndex + 1} de {SearchCards.Count}";
    }

    private void UpdateNavigationButtons()
    {
        if (PreviousButton != null)
        {
            PreviousButton.IsEnabled = _currentIndex > 0;
        }
        
        if (NextButton != null)
        {
            NextButton.IsEnabled = _currentIndex < SearchCards.Count - 1;
        }
    }

    private void UpdateSearchCardUI()
    {
        if (CurrentSearchCard == null) 
        {
            Console.WriteLine("UpdateSearchCardUI: CurrentSearchCard es null");
            return;
        }

        try
        {
            
            // Actualizar los TextBlocks de fechas
            SearchRow1DateText.Text = CurrentSearchCard.Row1DateText;
            SearchRow1DrawIcon.Text = CurrentSearchCard.Row1DrawIcon;
            SearchRow2DateText.Text = CurrentSearchCard.Row2DateText;
            SearchRow2DrawIcon.Text = CurrentSearchCard.Row2DrawIcon;
            SearchRow3DateText.Text = CurrentSearchCard.Row3DateText;
            SearchRow3DrawIcon.Text = CurrentSearchCard.Row3DrawIcon;
            SearchRow4DateText.Text = CurrentSearchCard.Row4DateText;
            SearchRow4DrawIcon.Text = CurrentSearchCard.Row4DrawIcon;


            // ACTUALIZAR LOS DÍGITOS DE CADA FILA
            SearchRow1Pick3Items.ItemsSource = null; // Limpiar primero para forzar actualización
            SearchRow1Pick3Items.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row1Pick3Digits);
            
            SearchRow1Pick4Items.ItemsSource = null;
            SearchRow1Pick4Items.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row1Pick4Digits);
            
            SearchRow1NextPick3Items.ItemsSource = null;
            SearchRow1NextPick3Items.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row1NextPick3Digits);
            
            SearchRow1CodingItems.ItemsSource = null;
            SearchRow1CodingItems.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row1CodingDigits);
            
            SearchRow2Pick3Items.ItemsSource = null;
            SearchRow2Pick3Items.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row2Pick3Digits);
            
            SearchRow2Pick4Items.ItemsSource = null;
            SearchRow2Pick4Items.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row2Pick4Digits);
            
            SearchRow2NextPick3Items.ItemsSource = null;
            SearchRow2NextPick3Items.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row2NextPick3Digits);
            
            SearchRow2CodingItems.ItemsSource = null;
            SearchRow2CodingItems.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row2CodingDigits);
            
            SearchRow3Pick3Items.ItemsSource = null;
            SearchRow3Pick3Items.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row3Pick3Digits);
            
            SearchRow3Pick4Items.ItemsSource = null;
            SearchRow3Pick4Items.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row3Pick4Digits);
            
            SearchRow3NextPick3Items.ItemsSource = null;
            SearchRow3NextPick3Items.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row3NextPick3Digits);
            
            SearchRow3CodingItems.ItemsSource = null;
            SearchRow3CodingItems.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row3CodingDigits);
            
            SearchRow4Pick3Items.ItemsSource = null;
            SearchRow4Pick3Items.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row4Pick3Digits);
            
            SearchRow4Pick4Items.ItemsSource = null;
            SearchRow4Pick4Items.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row4Pick4Digits);
            
            SearchRow4NextPick3Items.ItemsSource = null;
            SearchRow4NextPick3Items.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row4NextPick3Digits);
            
            SearchRow4CodingItems.ItemsSource = null;
            SearchRow4CodingItems.ItemsSource = CopyDigitCollection(CurrentSearchCard.Row4CodingDigits);
            

            // ACTUALIZAR CONTADORES
            SearchPick4MatchesRow1Row2.Text = CurrentSearchCard.Pick4MatchesRow1Row2 ?? "0C";
            SearchCodingMatchesRow1Row2.Text = CurrentSearchCard.CodingMatchesRow1Row2 ?? "0C";
            SearchPick4MatchesRow3Row4.Text = CurrentSearchCard.Pick4MatchesRow3Row4 ?? "0C";
            SearchCodingMatchesRow3Row4.Text = CurrentSearchCard.CodingMatchesRow3Row4 ?? "0C";
            
            // Forzar actualización de la UI antes de dibujar las líneas
            this.UpdateLayout();
            
            // Dar tiempo para que los ItemsControls generen sus contenedores
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Esperar un ciclo adicional para asegurar que todo está renderizado
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    CreateSearchCardConnectingLines();
                }), DispatcherPriority.Render);
                
            }), DispatcherPriority.Loaded);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en UpdateSearchCardUI: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    private void CreateSearchCardConnectingLines()
    {
        try
        {
            // Buscar el contenedor principal de la tarjeta de búsqueda
            var searchCard = SearchCardContainer;
            if (searchCard == null)
            {
                Console.WriteLine("SearchCardContainer no encontrado");
                return;
            }
            
            var row1Pick3Items = FindVisualChild<ItemsControl>(searchCard, "SearchRow1Pick3Items");
            var row1NextPick3Items = FindVisualChild<ItemsControl>(searchCard, "SearchRow1NextPick3Items");
            var row1Pick4Items = FindVisualChild<ItemsControl>(searchCard, "SearchRow1Pick4Items");
            
            var row2Pick3Items = FindVisualChild<ItemsControl>(searchCard, "SearchRow2Pick3Items");
            var row2NextPick3Items = FindVisualChild<ItemsControl>(searchCard, "SearchRow2NextPick3Items");
            var row2Pick4Items = FindVisualChild<ItemsControl>(searchCard, "SearchRow2Pick4Items");
            
            var row3Pick3Items = FindVisualChild<ItemsControl>(searchCard, "SearchRow3Pick3Items");
            var row3NextPick3Items = FindVisualChild<ItemsControl>(searchCard, "SearchRow3NextPick3Items");
            var row3Pick4Items = FindVisualChild<ItemsControl>(searchCard, "SearchRow3Pick4Items");
            
            var row4Pick3Items = FindVisualChild<ItemsControl>(searchCard, "SearchRow4Pick3Items");
            var row4NextPick3Items = FindVisualChild<ItemsControl>(searchCard, "SearchRow4NextPick3Items");
            var row4Pick4Items = FindVisualChild<ItemsControl>(searchCard, "SearchRow4Pick4Items");
            
            
            var pick3LinksCanvas = FindVisualChild<Canvas>(searchCard, "SearchPick3LinksCanvas");
            var nextPick3LinksCanvas = FindVisualChild<Canvas>(searchCard, "SearchNextPick3LinksCanvas");
            var pick3Row3ToPick4Row4LinksCanvas = FindVisualChild<Canvas>(searchCard, "SearchPick3Row3ToPick4Row4LinksCanvas");
            var pick3Row4ToPick4Row3LinksCanvas = FindVisualChild<Canvas>(searchCard, "SearchPick3Row4ToPick4Row3LinksCanvas");
            var pick3Row1ToPick4Row2LinksCanvas = FindVisualChild<Canvas>(searchCard, "SearchPick3Row1ToPick4Row2LinksCanvas");
            var pick3Row2ToPick4Row1LinksCanvas = FindVisualChild<Canvas>(searchCard, "SearchPick3Row2ToPick4Row1LinksCanvas");
            



            if (row1Pick3Items == null || row2Pick3Items == null || 
                row1NextPick3Items == null || row2NextPick3Items == null ||
                row3Pick3Items == null || row4Pick3Items == null ||
                row3NextPick3Items == null || row4NextPick3Items == null ||
                row3Pick4Items == null || row4Pick4Items == null ||
                pick3LinksCanvas == null || nextPick3LinksCanvas == null ||
                pick3Row3ToPick4Row4LinksCanvas == null || pick3Row4ToPick4Row3LinksCanvas == null)
            {
                return;
            }

            // Limpiar todos los canvas
            pick3LinksCanvas.Children.Clear();
            nextPick3LinksCanvas.Children.Clear();
            pick3Row3ToPick4Row4LinksCanvas.Children.Clear();
            pick3Row4ToPick4Row3LinksCanvas.Children.Clear();
            pick3Row1ToPick4Row2LinksCanvas.Children.Clear();
            pick3Row2ToPick4Row1LinksCanvas.Children.Clear();

            // Conectar Pick3: Fila 1 con Fila 2
            ConnectPick3Digits(row1Pick3Items, row2Pick3Items, pick3LinksCanvas);
            // Conectar NextPick3: Fila 1 con Fila 2
            ConnectPick3Digits(row1NextPick3Items, row2NextPick3Items, nextPick3LinksCanvas);
            // Conectar Pick3: Fila 3 con Fila 4
            ConnectPick3Digits(row3Pick3Items, row4Pick3Items, pick3LinksCanvas);
            // Conectar NextPick3: Fila 3 con Fila 4
            ConnectPick3Digits(row3NextPick3Items, row4NextPick3Items, nextPick3LinksCanvas);

            // Conectar Pick3 de fila 3 con Pick4 de fila 4
            ConnectPick3ToPick4Digits(row3Pick3Items, row4Pick4Items, pick3Row3ToPick4Row4LinksCanvas);
            ConnectPick3ToPick4Digits(row4Pick3Items, row3Pick4Items, pick3Row4ToPick4Row3LinksCanvas);

            ConnectPick3ToPick4DigitsWithRedLine(row1Pick3Items, row2Pick4Items, pick3Row1ToPick4Row2LinksCanvas);
            ConnectPick3ToPick4DigitsWithRedLine(row2Pick3Items, row1Pick4Items, pick3Row2ToPick4Row1LinksCanvas);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creando líneas de búsqueda: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    

    private void ConnectPick3ToPick4DigitsWithRedLine(ItemsControl pick3ItemsControl, ItemsControl pick4ItemsControl, Canvas canvas)
    {
        if (pick3ItemsControl == null || pick4ItemsControl == null || canvas == null)
            return;
        
        try
        {
            // Obtener los contenedores de los dígitos
            var pick3Digits = GetDigitContainers(pick3ItemsControl);
            var pick4Digits = GetDigitContainers(pick4ItemsControl);
            

            if (pick3Digits.Count != 3 || pick4Digits.Count != 4)
            {
                return;
            }

            // Limpiar canvas antes de dibujar
            canvas.Children.Clear();

            // Conectar dígitos que coincidan
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
                        // Dibujar línea ROJA conectando los dígitos
                        DrawRedConnectingLine(canvas, pick3Digit, pick4Digit);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error conectando Pick3 a Pick4 con línea roja: {ex.Message}");
        }
    }

    private void DrawRedConnectingLine(Canvas canvas, Border element1, Border element2)
    {
        try
        {
            // Verificar que los elementos tengan dimensiones válidas
            if (element1.ActualWidth == 0 || element1.ActualHeight == 0 ||
                element2.ActualWidth == 0 || element2.ActualHeight == 0)
                return;

            // Calcular centros absolutos
            var center1 = element1.TranslatePoint(new Point(element1.ActualWidth / 2, element1.ActualHeight / 2), canvas);
            var center2 = element2.TranslatePoint(new Point(element2.ActualWidth / 2, element2.ActualHeight / 2), canvas);
            
            // Calcular vector dirección de element1 a element2
            double dx = center2.X - center1.X;
            double dy = center2.Y - center1.Y;
            
            // Calcular distancia entre centros
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            // Si están en la misma posición, no dibujar línea
            if (distance == 0) return;
            
            // Calcular radio de cada círculo (mitad del ancho, ya que son círculos)
            double radius1 = element1.ActualWidth / 2;
            double radius2 = element2.ActualWidth / 2;
            
            // Normalizar vector dirección
            double unitX = dx / distance;
            double unitY = dy / distance;
            
            // Calcular puntos en los bordes
            Point startPoint = new Point(
                center1.X + (unitX * radius1),
                center1.Y + (unitY * radius1)
            );
            
            Point endPoint = new Point(
                center2.X - (unitX * radius2),
                center2.Y - (unitY * radius2)
            );
            
            // Crear línea CONTINUA ROJA
            var line = new Line
            {
                X1 = startPoint.X,
                Y1 = startPoint.Y,
                X2 = endPoint.X,
                Y2 = endPoint.Y,
                Stroke = Brushes.Red,  // Línea ROJA en lugar de negra
                StrokeThickness = 2
            };
            
            canvas.Children.Add(line);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error dibujando línea roja: {ex.Message}");
        }
    }

    private ItemsControl FindItemsControlByBinding(DependencyObject parent, string bindingPath)
    {
        try
        {
            var itemsControls = FindVisualChildren<ItemsControl>(parent);
            foreach (var itemsControl in itemsControls)
            {
                // Verificar si el ItemsControl tiene un binding en ItemsSource
                var bindingExpression = BindingOperations.GetBindingExpression(itemsControl, ItemsControl.ItemsSourceProperty);
                if (bindingExpression != null && bindingExpression.ParentBinding != null)
                {
                    var path = bindingExpression.ParentBinding.Path?.Path;
                    if (path == bindingPath)
                    {
                        return itemsControl;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error buscando ItemsControl por binding {bindingPath}: {ex.Message}");
        }
        
        return null;
    }

    private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;

        try
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                // Buscar por tipo
                if (child is T result)
                {
                    return result;
                }
                
                // Buscar recursivamente
                var foundChild = FindVisualChild<T>(child);
                if (foundChild != null) return foundChild;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en FindVisualChild: {ex.Message}");
        }
        
        return null;
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            CurrentSearchCard = SearchCards[_currentIndex];
            UpdateResultsCounter();
            UpdateNavigationButtons();
        }
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex < SearchCards.Count - 1)
        {
            _currentIndex++;
            CurrentSearchCard = SearchCards[_currentIndex];
            UpdateResultsCounter();
            UpdateNavigationButtons();
        }
    }

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        var filteredCards = new ObservableCollection<ThirdAnalysisCardVM>();
        
        foreach (var card in SearchCards)
        {
            int connection1Count = CountCommonDigits(card.Row3Pick3Digits, card.Row4Pick4Digits);
            int connection2Count = CountCommonDigits(card.Row4Pick3Digits, card.Row3Pick4Digits);

                    
            bool isValid = false;
            
            if (connection1Count == 1 && connection2Count == 0)
            {
                isValid = true;
            }
            else if (connection1Count == 0 && connection2Count == 1)
            {
                isValid = true;
            }

            if (isValid)
            {
                filteredCards.Add(card);
            }
        }
        
        // Actualizar la colección de tarjetas de búsqueda
        SearchCards.Clear();
        foreach (var card in filteredCards)
        {
            SearchCards.Add(card);
        }
        
        // Actualizar la interfaz
        if (SearchCards.Count > 0)
        {
            _currentIndex = 0;
            CurrentSearchCard = SearchCards[0];
            UpdateResultsCounter();
        }
        else
        {
            ResultsCounter.Text = "Resultados: 0 encontrados (filtro aplicado)";
            CurrentSearchCard = null;
        }
        
        UpdateNavigationButtons();
    }

    private void FilterButton2_Click(object sender, RoutedEventArgs e)
    {
        var filteredCards = new ObservableCollection<ThirdAnalysisCardVM>();
        
        foreach (var card in SearchCards)
        {
            int connection1Count = CountCommonDigits2(card.Row3Pick3Digits, card.Row4Pick4Digits);
            int connection2Count = CountCommonDigits2(card.Row4Pick3Digits, card.Row3Pick4Digits);

                    
            bool isValid = false;
            
            if (connection1Count == 1 && connection2Count == 0)
            {
                isValid = true;
            }
            else if (connection1Count == 0 && connection2Count == 1)
            {
                isValid = true;
            }

            if (isValid)
            {
                filteredCards.Add(card);
            }
        }
        
        // Actualizar la colección de tarjetas de búsqueda
        SearchCards.Clear();
        foreach (var card in filteredCards)
        {
            SearchCards.Add(card);
        }
        
        // Actualizar la interfaz
        if (SearchCards.Count > 0)
        {
            _currentIndex = 0;
            CurrentSearchCard = SearchCards[0];
            UpdateResultsCounter();
        }
        else
        {
            ResultsCounter.Text = "Resultados: 0 encontrados (filtro aplicado)";
            CurrentSearchCard = null;
        }
        
        UpdateNavigationButtons();
    }

    private int CountCommonDigits(ObservableCollection<DigitVM> pick3Digits, ObservableCollection<DigitVM> pick4Digits)
    {
        // Obtener los valores de dígitos del Pick3
        var pick3Values = pick3Digits
            .Select(d => d.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();
        
        // Obtener todos los dígitos del Pick4
        var allPick4Values = pick4Digits
            .Select(d => d.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();
        
        // Crear las dos listas separadas
        var pick4TwoFirstValues = new List<string>();
        var pick4TwoLastValues = new List<string>();
        
        // Asignar valores según la cantidad de dígitos disponibles
        pick4TwoFirstValues.Add(allPick4Values[0]); // Primer dígito
        pick4TwoFirstValues.Add(allPick4Values[1]); // Segundo dígito
        pick4TwoLastValues.Add(allPick4Values[2]);  // Tercer dígito
        pick4TwoLastValues.Add(allPick4Values[3]);  // Cuarto dígito 

        // PRIMERO: Verificar que NO hay coincidencias con pick4TwoFirstValues
        bool hasMatchesWithFirstTwo = false;
        foreach (var digit in pick3Values)
        {
            if (pick4TwoLastValues.Contains(digit))
            {
                hasMatchesWithFirstTwo = true;
                break; // Salir temprano si encontramos una coincidencia
            }
        }
        
        // Si hay coincidencias con los primeros 2 dígitos, retornar 0 inmediatamente
        if (hasMatchesWithFirstTwo)
        {
            return 3;
        }
        
        // SEGUNDO: Contar cuántos dígitos del Pick3 están en pick4TwoLastValues
        int commonCount = 0;
        foreach (var digit in pick3Values)
        {
            if (pick4TwoFirstValues.Contains(digit))
            {
                commonCount++;
            }
        }
        return commonCount;
    }

    private int CountCommonDigits2(ObservableCollection<DigitVM> pick3Digits, ObservableCollection<DigitVM> pick4Digits)
    {
        // Obtener los valores de dígitos del Pick3
        var pick3Values = pick3Digits
            .Select(d => d.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();
        
        // Obtener todos los dígitos del Pick4
        var allPick4Values = pick4Digits
            .Select(d => d.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();
        
        // Crear las dos listas separadas
        var pick4TwoFirstValues = new List<string>();
        var pick4TwoLastValues = new List<string>();
        
        // Asignar valores según la cantidad de dígitos disponibles
        pick4TwoFirstValues.Add(allPick4Values[0]); // Primer dígito
        pick4TwoFirstValues.Add(allPick4Values[1]); // Segundo dígito
        pick4TwoLastValues.Add(allPick4Values[2]);  // Tercer dígito
        pick4TwoLastValues.Add(allPick4Values[3]);  // Cuarto dígito 

        // PRIMERO: Verificar que NO hay coincidencias con pick4TwoFirstValues
        bool hasMatchesWithFirstTwo = false;
        foreach (var digit in pick3Values)
        {
            if (pick4TwoFirstValues.Contains(digit))
            {
                hasMatchesWithFirstTwo = true;
                break; // Salir temprano si encontramos una coincidencia
            }
        }
        
        // Si hay coincidencias con los primeros 2 dígitos, retornar 0 inmediatamente
        if (hasMatchesWithFirstTwo)
        {
            return 3;
        }
        
        // SEGUNDO: Contar cuántos dígitos del Pick3 están en pick4TwoLastValues
        int commonCount = 0;
        foreach (var digit in pick3Values)
        {
            if (pick4TwoLastValues.Contains(digit))
            {
                commonCount++;
            }
        }
        return commonCount;
    }

    




}