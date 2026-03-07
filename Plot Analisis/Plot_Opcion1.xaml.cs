using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text;

namespace FloridaLotteryApp
{
    public partial class Plot_Opcion1 : Window
    {
        // Colecciones para la tabla
        public ObservableCollection<PatternRow> PatternRows { get; set; } = new();

        // Colecciones para FILA 1
        public List<DigitCell> Row1Pick3 { get; set; } = new();
        public List<DigitCell> Row1Pick4 { get; set; } = new();
        public List<string> Row1Pick3Siguiente { get; set; } = new();
        public List<string> Row1Additional { get; set; } = new();
        
        // Colecciones para FILA 2
        public List<DigitCell> Row2Pick3 { get; set; } = new();
        public List<DigitCell> Row2Pick4 { get; set; } = new();
        public List<string> Row2Fireball { get; set; } = new();
        public List<string> Row2Additional { get; set; } = new();

        // Colecciones para FILA 3
        public List<DigitCell> Row3Pick3 { get; set; } = new();
        public List<DigitCell> Row3Pick4 { get; set; } = new();
        public List<string> Row3Fireball { get; set; } = new();
        public List<string> Row3Additional { get; set; } = new();

        // Colecciones para FILA 4
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

            // Cargar datos de la FILA 1
            _row1Pick3Number = pick3 ?? " ";
            _row1Pick4Number = pick4 ?? " ";

            // Procesar Pick3 Siguiente
            var pick3SiguienteDigits = pick3Siguiente.Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row1Pick3Siguiente = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                if (i < pick3SiguienteDigits.Count)
                    Row1Pick3Siguiente.Add(pick3SiguienteDigits[i]);
                else
                    Row1Pick3Siguiente.Add(" ");
            }

            Row1Additional = BuildCodificacionDigits(pick3, pick4);
            Row1_DateText.Text = dateText;
            Row1_DrawIcon.Text = drawIcon;

            // Cargar datos de la fila seleccionada en FILAS 2, 3 y 4
            LoadSelectedRow(selectedRow);

            // ===== NUEVO: Agregar la fila seleccionada como primer registro en la tabla =====
            PatternRows.Add(selectedRow);
            UpdateResultsCounter(); // <-- Agregar esta línea

            // Asignar ItemsSource
            PatternsTable.ItemsSource = PatternRows;
            
            // Seleccionar automáticamente el primer registro (la fila que acabamos de agregar)
            if (PatternRows.Count > 0)
            {
                PatternsTable.SelectedIndex = 0;
            }
            
            // Inicializar las celdas de dígitos
            UpdateAllPickDigitCells();
            
            // Asignar ItemsSources de los ItemsControls
            AssignItemsSources();

            // ===== MENSAJE DE DEPURACIÓN =====
            MostrarPatronesDepuracion();
        }

        private void LoadSelectedRow(PatternRow selected)
        {
            if (selected == null) return;

            // FILA 2 (Match)
            _row2Pick3Number = selected.MatchPick3 ?? " ";
            _row2Pick4Number = selected.MatchPick4 ?? " ";
            Row2Fireball = (selected.MatchNextPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row2Additional = (selected.MatchCodificacion ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row2_DateText.Text = selected.MatchDate ?? " ";
            Row2_DrawIcon.Text = DrawIconFromTime(selected.MatchDrawTime);

            // FILA 3 (Similar)
            _row3Pick3Number = selected.SimilarPick3 ?? " ";
            _row3Pick4Number = selected.SimilarPick4 ?? " ";
            Row3Fireball = (selected.SimilarNextPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row3Additional = (selected.SimilarCodificacion ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row3_DateText.Text = selected.SimilarDate ?? " ";
            Row3_DrawIcon.Text = DrawIconFromTime(selected.SimilarDrawTime);

            // FILA 4 (SimilarMatch)
            _row4Pick3Number = selected.SimilarMatchPick3 ?? " ";
            _row4Pick4Number = selected.SimilarMatchPick4 ?? " ";
            Row4Fireball = (selected.SimilarMatchNextPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row4Additional = (selected.SimilarMatchCodificacion ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
            Row4_DateText.Text = selected.SimilarMatchDate ?? " ";
            Row4_DrawIcon.Text = DrawIconFromTime(selected.SimilarMatchDrawTime);
        }

        private void AssignItemsSources()
        {
            // FILA 1
            Row1_Pick3SiguienteDigits.ItemsSource = Row1Pick3Siguiente;
            Row1_AdditionalDigits.ItemsSource = Row1Additional;

            // FILA 2
            Row2_FireballDigits.ItemsSource = Row2Fireball;
            Row2_AdditionalDigits.ItemsSource = Row2Additional;

            // FILA 3
            Row3_FireballDigits.ItemsSource = Row3Fireball;
            Row3_AdditionalDigits.ItemsSource = Row3Additional;

            // FILA 4
            Row4_FireballDigits.ItemsSource = Row4Fireball;
            Row4_AdditionalDigits.ItemsSource = Row4Additional;
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


        //////////////////////////////////////////////////////////////////////
        /// Esto es para depuracion, luego borrar/////////////////////////////
        //////////////////////////////////////////////////////////////////////
        // ===== NUEVO MÉTODO PARA MOSTRAR PATRONES DE DEPURACIÓN =====
        private void MostrarPatronesDepuracion()
        {
            try
            {
                string mensaje = "=== PATRONES AABCDEFF (Pick3+Pick4+Pick3Sgte) POR FILA ===\n\n";

                // FILA 1: Pick3 + Pick4 + Pick3Siguiente
                string fila1Completa = _row1Pick3Number + _row1Pick4Number + string.Concat(Row1Pick3Siguiente);
                string patronFila1 = BuildRepetitionPattern(fila1Completa);
                mensaje += $"FILA 1 (Referencia):\n";
                mensaje += $"Completo: {fila1Completa}\n";
                mensaje += $"Patrón: {patronFila1}\n\n";

                // FILA 2: Pick3 + Pick4 + Fireball (Pick3Siguiente)
                string fila2Completa = _row2Pick3Number + _row2Pick4Number + string.Concat(Row2Fireball);
                string patronFila2 = BuildRepetitionPattern(fila2Completa);
                mensaje += $"FILA 2 (Match):\n";
                mensaje += $"Completo: {fila2Completa}\n";
                mensaje += $"Patrón: {patronFila2}\n\n";

                // FILA 3: Pick3 + Pick4 + Fireball (Pick3Siguiente)
                string fila3Completa = _row3Pick3Number + _row3Pick4Number + string.Concat(Row3Fireball);
                string patronFila3 = BuildRepetitionPattern(fila3Completa);
                mensaje += $"FILA 3 (Similar):\n";
                mensaje += $"Completo: {fila3Completa}\n";
                mensaje += $"Patrón: {patronFila3}\n\n";

                // FILA 4: Pick3 + Pick4 + Fireball (Pick3Siguiente)
                string fila4Completa = _row4Pick3Number + _row4Pick4Number + string.Concat(Row4Fireball);
                string patronFila4 = BuildRepetitionPattern(fila4Completa);
                mensaje += $"FILA 4 (SimilarMatch):\n";
                mensaje += $"Completo: {fila4Completa}\n";
                mensaje += $"Patrón: {patronFila4}\n";

                MessageBox.Show(mensaje, "Patrones de Depuración", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mostrar patrones: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Necesitas agregar este método si no existe en Plot_Opcion1
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
         //////////////////////////////////////////////////////////////////////
         ///  //////////////////////////////////////////////////////////////////////

    }     
}