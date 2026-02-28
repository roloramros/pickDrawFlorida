using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FloridaLotteryApp.Data;

namespace FloridaLotteryApp;

public partial class PlotWindow : Window
{
    // Colecciones para la tabla de patrones
    public ObservableCollection<PatternRow> PatternRows { get; set; } = new();

    // Colecciones para FILA 1 (Tirada seleccionada)
    public List<string> Row1Pick3 { get; set; } = new();
    public List<string> Row1Pick4 { get; set; } = new();
    public List<string> Row1Pick3Siguiente { get; set; } = new();
    public List<string> Row1Additional { get; set; } = new();
    public string Row1Date { get; set; } = " ";

    // Colecciones para FILA 2
    public List<string> Row2Pick3 { get; set; } = new();
    public List<string> Row2Pick4 { get; set; } = new();
    public List<string> Row2Fireball { get; set; } = new();
    public List<string> Row2Additional { get; set; } = new();
    public string Row2Date { get; set; } = " ";

    // Colecciones para FILA 3
    public List<string> Row3Pick3 { get; set; } = new();
    public List<string> Row3Pick4 { get; set; } = new();
    public List<string> Row3Fireball { get; set; } = new();
    public List<string> Row3Additional { get; set; } = new();
    public string Row3Date { get; set; } = " ";

    // Colecciones para FILA 4
    public List<string> Row4Pick3 { get; set; } = new();
    public List<string> Row4Pick4 { get; set; } = new();
    public List<string> Row4Fireball { get; set; } = new();
    public List<string> Row4Additional { get; set; } = new();
    public string Row4Date { get; set; } = " ";

    public PlotWindow(string dateText, string drawIcon, string pick3, string pick4, string pick3Siguiente)
    {
        InitializeComponent();

        // Configurar DataContext
        DataContext = this;

        // ==========================================
        // FILA 1: Datos de la tirada seleccionada
        // ==========================================
        Row1Pick3 = pick3.Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row1Pick4 = pick4.Where(char.IsDigit).Select(c => c.ToString()).ToList();

        // Procesar Pick3 Siguiente con manejo de cadenas vacías o sin dígitos suficientes
        // Obtiene solo los dígitos de la cadena y los convierte a strings inmediatamente
        var pick3SiguienteDigits = pick3Siguiente.Where(char.IsDigit).Select(c => c.ToString()).ToList();
        // Rellena con espacios hasta tener 3 elementos o crea una lista de 3 espacios si no hay dígitos
        Row1Pick3Siguiente = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            if (i < pick3SiguienteDigits.Count)
            {
                Row1Pick3Siguiente.Add(pick3SiguienteDigits[i]);
            }
            else
            {
                Row1Pick3Siguiente.Add(" "); // Agrega un espacio en blanco
            }
        }

        Row1Additional = BuildCodificacionDigits(pick3, pick4);
        Row1Date = dateText; // Asignar la fecha

        // ==========================================
        // FILA 2,  3, 4: Datos de ejemplo (luego los llenas con tu lógica)
        // ==========================================
        Row2Pick3 = new List<string>();
        Row2Pick4 = new List<string>();
        Row2Fireball = new List<string>();
        Row2Additional = new List<string>();
        Row2Date = " ";

        Row3Pick3 = new List<string>();
        Row3Pick4 = new List<string>();
        Row3Fireball = new List<string>();
        Row3Additional = new List<string>();
        Row3Date = " ";

        Row4Pick3 = new List<string>();
        Row4Pick4 = new List<string>();
        Row4Fireball = new List<string>();
        Row4Additional = new List<string>();
        Row4Date = " ";

        // Cargar tabla superior según tirada guía
        LoadPatternRows(pick3, pick4, dateText); // <-- Se pasa la fecha guía

        // Asignar ItemsSource a todos los ItemsControls
        // FILA 1
        Row1_Pick3Digits.ItemsSource = Row1Pick3;
        Row1_Pick4Digits.ItemsSource = Row1Pick4;
        Row1_Pick3SiguienteDigits.ItemsSource = Row1Pick3Siguiente; // <-- Ahora siempre tiene 3 elementos
        Row1_AdditionalDigits.ItemsSource = Row1Additional;
        Row1_DrawIcon.Text = drawIcon;

        // FILA 2
        Row2_Pick3Digits.ItemsSource = Row2Pick3;
        Row2_Pick4Digits.ItemsSource = Row2Pick4;
        Row2_FireballDigits.ItemsSource = Row2Fireball;
        Row2_AdditionalDigits.ItemsSource = Row2Additional;
        Row2_DateText.Text = Row2Date;

        // FILA 3
        Row3_Pick3Digits.ItemsSource = Row3Pick3;
        Row3_Pick4Digits.ItemsSource = Row3Pick4;
        Row3_FireballDigits.ItemsSource = Row3Fireball;
        Row3_AdditionalDigits.ItemsSource = Row3Additional;
        Row3_DateText.Text = Row3Date;

        // FILA 4
        Row4_Pick3Digits.ItemsSource = Row4Pick3;
        Row4_Pick4Digits.ItemsSource = Row4Pick4;
        Row4_FireballDigits.ItemsSource = Row4Fireball;
        Row4_AdditionalDigits.ItemsSource = Row4Additional;
        Row4_DateText.Text = Row4Date;

        PatternsTable.ItemsSource = PatternRows;
        if (PatternRows.Count > 0)
        {
            PatternsTable.SelectedIndex = 0;
            PatternsTable.ScrollIntoView(PatternRows[0]);
            ApplySelectionToRows(PatternRows[0]);
        }
    }


    private void LoadPatternRows(string guidePick3, string guidePick4, string guideDateText) // <-- Recibe la fecha guía
    {
        PatternRows.Clear();

        // Convertir la fecha guía de texto a DateTime para comparaciones
        if (!DateTime.TryParseExact(guideDateText, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime guideDateTime))
        {
            // Manejar error si la fecha no es válida
            Console.WriteLine($"Error: Fecha guía '{guideDateText}' no es válida.");
            return;
        }

        // Construir número guía de 7 dígitos
        var guideNumber = new string(
            (guidePick3 ?? " ")
                .Where(char.IsDigit)
                .Concat((guidePick4 ?? " ").Where(char.IsDigit))
                .ToArray());

        if (guideNumber.Length != 7)
        {
            return;
        }

        // Obtener criterios del número guía
        var referencePos23 = GetPos23Key(guideNumber);
        var referencePattern = BuildRepetitionPattern(guideNumber);

        if (referencePos23 == null)
        {
            return;
        }

        // Obtener todos los hits con su número de 7 dígitos
        var allHits = DrawRepository.GetAllPick3WithPick4()
            .Select(hit =>
            {
                var combined = BuildSevenDigitNumber(hit.Pick3, hit.Pick4);
                if (combined.Length != 7)
                {
                    return null;
                }

                return new CandidateRow
                {
                    Hit = hit,
                    Number7 = combined,
                    Pos23 = GetPos23Key(combined) ?? " ",
                    Pattern = BuildRepetitionPattern(combined),
                    Date = hit.Date // <-- Asignar la fecha original del hit al CandidateRow
                };
            })
            .Where(x => x != null)
            .Cast<CandidateRow>()
            .ToList();


        // ===== LÓGICA MODIFICADA CON FILTROS: FECHA (Col2<Ref, Col4<Col3) Y PATRÓN DE PRIMER DÍGITO =====

        // 1. Candidatos para Columna 2 (coinciden en Pos23 con guía)
        //    Se itera sobre todos, pero se filtra dentro del bucle principal.
        var col2Candidates = allHits
            .Where(x => x.Pos23 == referencePos23 && x.Number7 != guideNumber)
            .ToList();

        // 2. Candidatos para Columna 3 (coinciden en patrón con guía)
        //    Se itera sobre todos, pero se filtra dentro del bucle principal.
        var col3Candidates = allHits
            .Where(x => x.Pattern == referencePattern && x.Number7 != guideNumber)
            .ToList();

        // Extraer el primer dígito de la referencia (Pick3)
        char? refFirstDigit = null;
        if (!string.IsNullOrEmpty(guidePick3) && char.IsDigit(guidePick3[0]))
        {
             refFirstDigit = guidePick3[0];
        }

        // 3. Generar combinaciones aplicando los filtros temporales:
        //    - Fecha Col2 < Fecha Referencia
        //    - Fecha Col4 < Fecha Col3 (Col3 puede ser cualquiera)
        //    - Patrón de primer dígito: (Ref1 vs Col2) implica (Col3 vs Col4)
        foreach (var col2 in col2Candidates)
        {
            // Filtrar: Fecha Col2 < Fecha Referencia
            if (col2.Date >= guideDateTime) continue;

            // Extraer el primer dígito de Col2 (Pick3)
            char? col2FirstDigit = null;
            if (!string.IsNullOrEmpty(col2.Hit.Pick3) && char.IsDigit(col2.Hit.Pick3[0]))
            {
                 col2FirstDigit = col2.Hit.Pick3[0];
            }

            // Determinar el patrón de igualdad/desigualdad entre Ref1 y Col2
            bool refAndCol2AreEqual = refFirstDigit.HasValue && col2FirstDigit.HasValue && refFirstDigit.Value == col2FirstDigit.Value;
            bool refAndCol2AreDifferent = refFirstDigit.HasValue && col2FirstDigit.HasValue && refFirstDigit.Value != col2FirstDigit.Value;

            foreach (var col3 in col3Candidates) // <-- Ahora itera sobre todos los candidatos de Col3 sin filtrar por fecha contra Col2
            {
                // NO HAY FILTRO: Fecha Col3 < Fecha Col2

                // Extraer el primer dígito de Col3 (Pick3)
                char? col3FirstDigit = null;
                if (!string.IsNullOrEmpty(col3.Hit.Pick3) && char.IsDigit(col3.Hit.Pick3[0]))
                {
                     col3FirstDigit = col3.Hit.Pick3[0];
                }

                // Buscar candidatos para Columna 4
                // Cumplen: Pos23 = Pos23 del col3 Y Patrón = Patrón del col2
                // Filtros temporales (Col4 < Col3) y de patrón de primer dígito
                var col4Candidates = allHits
                    .Where(x =>
                    {
                        // Condiciones originales
                        bool condition1 = x.Pos23 == col3.Pos23;
                        bool condition2 = x.Pattern == col2.Pattern;
                        bool condition3 = x.Number7 != col2.Number7;
                        bool condition4 = x.Number7 != col3.Number7;
                        bool condition5 = x.Date < col3.Date; // <-- Filtro temporal: Col4 < Col3

                        if (!(condition1 && condition2 && condition3 && condition4 && condition5))
                        {
                            return false;
                        }

                        // Nueva condición: Patrón de primer dígito
                        char? col4FirstDigit = null;
                        if (!string.IsNullOrEmpty(x.Hit.Pick3) && char.IsDigit(x.Hit.Pick3[0]))
                        {
                             col4FirstDigit = x.Hit.Pick3[0];
                        }

                        // Solo aplicar el filtro si todos los dígitos relevantes están disponibles
                        if (col3FirstDigit.HasValue && col4FirstDigit.HasValue)
                        {
                            bool col3AndCol4AreEqual = col3FirstDigit.Value == col4FirstDigit.Value;
                            bool col3AndCol4AreDifferent = col3FirstDigit.Value != col4FirstDigit.Value;

                            // Si Ref1 y Col2 son diferentes, Col3 y Col4 deben ser diferentes
                            if (refAndCol2AreDifferent && col3AndCol4AreDifferent)
                            {
                                return true;
                            }
                            // Si Ref1 y Col2 son iguales, Col3 y Col4 deben ser iguales
                            else if (refAndCol2AreEqual && col3AndCol4AreEqual)
                            {
                                return true;
                            }
                            // En cualquier otro caso, no cumple el patrón
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            // Si no se puede determinar el primer dígito de Col4 o Col3, no se puede aplicar el filtro.
                            // Opcional: retornar false si se requiere que ambos tengan dígito.
                            // Por ahora, permitimos que pase si otros filtros lo permiten.
                            // Para ser estrictos, podríamos retornar false aquí si se considera que faltar dígito incumple el patrón.
                            // Suponiendo que Pick3 debe tener dígitos, este caso sería raro.
                            return true; // O retornar false si se considera inválido no tener dígito.
                        }
                    })
                    .ToList();

                if (col4Candidates.Count > 0)
                {
                    foreach (var col4 in col4Candidates)
                    {
                        AddPatternRow(col2, col3, col4, guideNumber, guideDateTime); // <-- Pasamos la fecha guía
                    }
                }
            }
        }
    }

    // --- Modificar CandidateRow para incluir la fecha ---
    internal class CandidateRow
    {
        public required ComboHit Hit { get; set; }
        public string Number7 { get; set; } = "";
        public string Pos23 { get; set; } = "";
        public string Pattern { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.MinValue; // <-- Nueva propiedad para la fecha del hit
    }

    // --- Modificar AddPatternRow para recibir la fecha guía ---
    private void AddPatternRow(CandidateRow col2, CandidateRow col3, CandidateRow col4, string guideNumber, DateTime guideDate) // <-- Recibe la fecha guía
    {
        var nextPick3 = DrawRepository.GetNextPick3Number(col2.Hit.Date, col2.Hit.DrawTime) ?? " ";
        var col3NextPick3 = col3 == null ? " " : DrawRepository.GetNextPick3Number(col3.Hit.Date, col3.Hit.DrawTime) ?? " ";
        var col4NextPick3 = col4 == null ? " " : DrawRepository.GetNextPick3Number(col4.Hit.Date, col4.Hit.DrawTime) ?? " ";

        PatternRows.Add(new PatternRow
        {
            ReferenceNumber = guideNumber,

            // Columna 2
            MatchNumber = col2.Number7,
            MatchPick3 = col2.Hit.Pick3,
            MatchPick4 = col2.Hit.Pick4,
            MatchNextPick3 = nextPick3,
            MatchDrawTime = col2.Hit.DrawTime,
            MatchDate = col2.Hit.Date.ToString("yyyy-MM-dd"),
            MatchCodificacion = string.Concat(BuildCodificacionDigits(col2.Hit.Pick3, col2.Hit.Pick4)),

            // Columna 3
            SimilarNumber = col3.Number7,
            SimilarPick3 = col3.Hit.Pick3,
            SimilarPick4 = col3.Hit.Pick4,
            SimilarNextPick3 = col3NextPick3,
            SimilarDrawTime = col3.Hit.DrawTime,
            SimilarDate = col3.Hit.Date.ToString("yyyy-MM-dd"),
            SimilarCodificacion = string.Concat(BuildCodificacionDigits(col3.Hit.Pick3, col3.Hit.Pick4)),

            // Columna 4
            SimilarMatchNumber = col4?.Number7 ?? " ",
            SimilarMatchPick3 = col4?.Hit.Pick3 ?? " ",
            SimilarMatchPick4 = col4?.Hit.Pick4 ?? " ",
            SimilarMatchNextPick3 = col4NextPick3,
            SimilarMatchDrawTime = col4?.Hit.DrawTime ?? " ",
            SimilarMatchDate = col4?.Hit.Date.ToString("yyyy-MM-dd") ?? " ",
            SimilarMatchCodificacion = col4 == null ? " " : string.Concat(BuildCodificacionDigits(col4.Hit.Pick3, col4.Hit.Pick4)),

            // Fecha Referencia (opcional, para mostrarla en la UI si es útil)
            ReferenceDate = guideDate.ToString("yyyy-MM-dd") // <-- Añadir propiedad a PatternRow y asignar aquí
        });
    }

    // --- Modificar PatternRow para incluir la fecha de referencia ---
    public class PatternRow
    {
        // ... (otras propiedades existentes)
        public string ReferenceDate { get; set; } = " "; // <-- Nueva propiedad para almacenar la fecha de la tirada de referencia

        // Mantener las propiedades originales...
        public string ReferenceNumber { get; set; } = " ";
        public string MatchNumber { get; set; } = " ";
        public string SimilarNumber { get; set; } = " ";
        public string SimilarPatternNumber { get; set; } = " ";
        public string SimilarMatchNumber { get; set; } = " ";
        public string MatchPick3 { get; set; } = " ";
        public string MatchPick4 { get; set; } = " ";
        public string MatchNextPick3 { get; set; } = " ";
        public string MatchDrawTime { get; set; } = " ";
        public string MatchDate { get; set; } = " ";
        public string MatchCodificacion { get; set; } = " ";
        public string SimilarPick3 { get; set; } = " ";
        public string SimilarPick4 { get; set; } = " ";
        public string SimilarNextPick3 { get; set; } = " ";
        public string SimilarDrawTime { get; set; } = " ";
        public string SimilarDate { get; set; } = " ";
        public string SimilarCodificacion { get; set; } = " ";
        public string SimilarMatchPick3 { get; set; } = " ";
        public string SimilarMatchPick4 { get; set; } = " ";
        public string SimilarMatchNextPick3 { get; set; } = " ";
        public string SimilarMatchDrawTime { get; set; } = " ";
        public string SimilarMatchDate { get; set; } = " ";
        public string SimilarMatchCodificacion { get; set; } = " ";
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

    private static string DrawIconFromTime(string drawTime)
    {
        return drawTime == "M" ? "\u2600\uFE0F" : drawTime == "E" ? "\U0001F319" : " ";
    }

    private void PatternsTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PatternsTable.SelectedItem is not PatternRow selected)
        {
            return;
        }

        ApplySelectionToRows(selected);
    }

    private void ApplySelectionToRows(PatternRow selected)
    {
        if (selected == null)
        {
            return;
        }

        Row2Pick3 = (selected.MatchPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row2Pick4 = (selected.MatchPick4 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row2Fireball = (selected.MatchNextPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row2Additional = (selected.MatchCodificacion ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row2Date = selected.MatchDate ?? " ";

        Row2_Pick3Digits.ItemsSource = Row2Pick3;
        Row2_Pick4Digits.ItemsSource = Row2Pick4;
        Row2_FireballDigits.ItemsSource = Row2Fireball;
        Row2_AdditionalDigits.ItemsSource = Row2Additional;
        Row2_DrawIcon.Text = DrawIconFromTime(selected.MatchDrawTime);
        Row2_DateText.Text = Row2Date;

        Row3Pick3 = (selected.SimilarPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row3Pick4 = (selected.SimilarPick4 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row3Fireball = (selected.SimilarNextPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row3Additional = (selected.SimilarCodificacion ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row3Date = selected.SimilarDate ?? " ";

        Row3_Pick3Digits.ItemsSource = Row3Pick3;
        Row3_Pick4Digits.ItemsSource = Row3Pick4;
        Row3_FireballDigits.ItemsSource = Row3Fireball;
        Row3_AdditionalDigits.ItemsSource = Row3Additional;
        Row3_DrawIcon.Text = DrawIconFromTime(selected.SimilarDrawTime);
        Row3_DateText.Text = Row3Date;

        Row4Pick3 = (selected.SimilarMatchPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row4Pick4 = (selected.SimilarMatchPick4 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row4Fireball = (selected.SimilarMatchNextPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row4Additional = (selected.SimilarMatchCodificacion ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row4Date = selected.SimilarMatchDate ?? " ";

        Row4_Pick3Digits.ItemsSource = Row4Pick3;
        Row4_Pick4Digits.ItemsSource = Row4Pick4;
        Row4_FireballDigits.ItemsSource = Row4Fireball;
        Row4_AdditionalDigits.ItemsSource = Row4Additional;
        Row4_DrawIcon.Text = DrawIconFromTime(selected.SimilarMatchDrawTime);
        Row4_DateText.Text = Row4Date;
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
}