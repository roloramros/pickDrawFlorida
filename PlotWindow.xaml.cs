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
    public string Row1Date { get; set; } = "";
    
    // Colecciones para FILA 2
    public List<string> Row2Pick3 { get; set; } = new();
    public List<string> Row2Pick4 { get; set; } = new();
    public List<string> Row2Fireball { get; set; } = new();
    public List<string> Row2Additional { get; set; } = new();
    public string Row2Date { get; set; } = "";
    
    // Colecciones para FILA 3
    public List<string> Row3Pick3 { get; set; } = new();
    public List<string> Row3Pick4 { get; set; } = new();
    public List<string> Row3Fireball { get; set; } = new();
    public List<string> Row3Additional { get; set; } = new();
    public string Row3Date { get; set; } = "";
    
    // Colecciones para FILA 4
    public List<string> Row4Pick3 { get; set; } = new();
    public List<string> Row4Pick4 { get; set; } = new();
    public List<string> Row4Fireball { get; set; } = new();
    public List<string> Row4Additional { get; set; } = new();
    public string Row4Date { get; set; } = "";

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
        Row1Pick3Siguiente = pick3Siguiente.Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row1Additional = BuildCodificacionDigits(pick3, pick4);
        Row1Date = dateText;
        
        // ==========================================
        // FILA 2, 3, 4: Datos de ejemplo (luego los llenas con tu lógica)
        // ==========================================
        Row2Pick3 = new List<string>();
        Row2Pick4 = new List<string>();
        Row2Fireball = new List<string>();
        Row2Additional = new List<string>();
        Row2Date = "";
        
        Row3Pick3 = new List<string>();
        Row3Pick4 = new List<string>();
        Row3Fireball = new List<string>();
        Row3Additional = new List<string>();
        Row3Date = "";
        
        Row4Pick3 = new List<string>();
        Row4Pick4 = new List<string>();
        Row4Fireball = new List<string>();
        Row4Additional = new List<string>();
        Row4Date = "";
        
        // Cargar tabla superior según tirada guía
        LoadPatternRows(pick3, pick4);
        
        // Asignar ItemsSource a todos los ItemsControls
        // FILA 1
        Row1_Pick3Digits.ItemsSource = Row1Pick3;
        Row1_Pick4Digits.ItemsSource = Row1Pick4;
        Row1_Pick3SiguienteDigits.ItemsSource = Row1Pick3Siguiente;
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


    private void LoadPatternRows(string guidePick3, string guidePick4)
    {
        PatternRows.Clear();

        // Construir número guía de 7 dígitos
        var guideNumber = new string(
            (guidePick3 ?? "")
                .Where(char.IsDigit)
                .Concat((guidePick4 ?? "").Where(char.IsDigit))
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
                    Pos23 = GetPos23Key(combined) ?? "",
                    Pattern = BuildRepetitionPattern(combined)
                };
            })
            .Where(x => x != null)
            .Cast<CandidateRow>()
            .ToList();

        // ===== NUEVA LÓGICA =====
        
        // 1. TODOS los candidatos para Columna 2 (coinciden en Pos23 con guía)
        var col2Candidates = allHits
            .Where(x => x.Pos23 == referencePos23 &&    // ✓ Coincide Pos23 con guía
                    x.Number7 != guideNumber)         // ✓ Excluye solo el número idéntico
            .ToList();     
        
        // 2. TODOS los candidatos para Columna 3 (coinciden en patrón con guía, diferente número)
        var col3Candidates = allHits
            .Where(x => x.Pattern == referencePattern && x.Number7 != guideNumber)
            .ToList();

        // 3. Generar TODAS las combinaciones
        foreach (var col2 in col2Candidates)
        {
            foreach (var col3 in col3Candidates)
            {
                // Buscar TODOS los candidatos para Columna 4
                // que cumplan: Pos23 = Pos23 del col3 Y Patrón = Patrón del col2
                var col4Candidates = allHits
                    .Where(x => x.Pos23 == col3.Pos23 && 
                            x.Pattern == col2.Pattern &&
                            x.Number7 != col2.Number7 &&  // Opcional: evitar duplicados
                            x.Number7 != col3.Number7)    // con col2 y col3
                    .ToList();

                // Si no hay candidatos para col4, podemos:
                if (col4Candidates.Count > 0)
                {
                    foreach (var col4 in col4Candidates)
                    {
                        AddPatternRow(col2, col3, col4, guideNumber);
                    }
                }
            }
        }
    }

    // Método auxiliar para crear y añadir una fila
    private void AddPatternRow(CandidateRow col2, CandidateRow col3, CandidateRow? col4, string guideNumber)
    {
        var nextPick3 = DrawRepository.GetNextPick3Number(col2.Hit.Date, col2.Hit.DrawTime) ?? "";
        var col3NextPick3 = col3 == null ? "" : DrawRepository.GetNextPick3Number(col3.Hit.Date, col3.Hit.DrawTime) ?? "";
        var col4NextPick3 = col4 == null ? "" : DrawRepository.GetNextPick3Number(col4.Hit.Date, col4.Hit.DrawTime) ?? "";

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
            
            // Columna 4 (puede ser null)
            SimilarMatchNumber = col4?.Number7 ?? "",
            SimilarMatchPick3 = col4?.Hit.Pick3 ?? "",
            SimilarMatchPick4 = col4?.Hit.Pick4 ?? "",
            SimilarMatchNextPick3 = col4NextPick3,
            SimilarMatchDrawTime = col4?.Hit.DrawTime ?? "",
            SimilarMatchDate = col4?.Hit.Date.ToString("yyyy-MM-dd") ?? "",
            SimilarMatchCodificacion = col4 == null ? "" : string.Concat(BuildCodificacionDigits(col4.Hit.Pick3, col4.Hit.Pick4))
        });
    }

    private static string BuildSevenDigitNumber(string pick3, string pick4)
    {
        return new string(
            (pick3 ?? "")
                .Where(char.IsDigit)
                .Concat((pick4 ?? "").Where(char.IsDigit))
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
        return drawTime == "M" ? "\u2600\uFE0F" : drawTime == "E" ? "\U0001F319" : "";
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

        Row2Pick3 = (selected.MatchPick3 ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row2Pick4 = (selected.MatchPick4 ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row2Fireball = (selected.MatchNextPick3 ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row2Additional = (selected.MatchCodificacion ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row2Date = selected.MatchDate ?? "";

        Row2_Pick3Digits.ItemsSource = Row2Pick3;
        Row2_Pick4Digits.ItemsSource = Row2Pick4;
        Row2_FireballDigits.ItemsSource = Row2Fireball;
        Row2_AdditionalDigits.ItemsSource = Row2Additional;
        Row2_DrawIcon.Text = DrawIconFromTime(selected.MatchDrawTime);
        Row2_DateText.Text = Row2Date;

        Row3Pick3 = (selected.SimilarPick3 ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row3Pick4 = (selected.SimilarPick4 ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row3Fireball = (selected.SimilarNextPick3 ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row3Additional = (selected.SimilarCodificacion ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row3Date = selected.SimilarDate ?? "";

        Row3_Pick3Digits.ItemsSource = Row3Pick3;
        Row3_Pick4Digits.ItemsSource = Row3Pick4;
        Row3_FireballDigits.ItemsSource = Row3Fireball;
        Row3_AdditionalDigits.ItemsSource = Row3Additional;
        Row3_DrawIcon.Text = DrawIconFromTime(selected.SimilarDrawTime);
        Row3_DateText.Text = Row3Date;

        Row4Pick3 = (selected.SimilarMatchPick3 ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row4Pick4 = (selected.SimilarMatchPick4 ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row4Fireball = (selected.SimilarMatchNextPick3 ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row4Additional = (selected.SimilarMatchCodificacion ?? "").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row4Date = selected.SimilarMatchDate ?? "";

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





internal class CandidateRow
{
    public required ComboHit Hit { get; set; }
    public string Number7 { get; set; } = "";
    public string Pos23 { get; set; } = "";
    public string Pattern { get; set; } = "";
}

/// <summary>
/// Clase para las filas de la tabla de patrones (4 columnas)
/// </summary>
public class PatternRow
{
    public string ReferenceNumber { get; set; } = "";
    public string MatchNumber { get; set; } = "";
    public string SimilarNumber { get; set; } = "";
    public string SimilarPatternNumber { get; set; } = "";
    public string SimilarMatchNumber { get; set; } = "";
    public string MatchPick3 { get; set; } = "";
    public string MatchPick4 { get; set; } = "";
    public string MatchNextPick3 { get; set; } = "";
    public string MatchDrawTime { get; set; } = "";
    public string MatchDate { get; set; } = "";
    public string MatchCodificacion { get; set; } = "";
    public string SimilarPick3 { get; set; } = "";
    public string SimilarPick4 { get; set; } = "";
    public string SimilarNextPick3 { get; set; } = "";
    public string SimilarDrawTime { get; set; } = "";
    public string SimilarDate { get; set; } = "";
    public string SimilarCodificacion { get; set; } = "";
    public string SimilarMatchPick3 { get; set; } = "";
    public string SimilarMatchPick4 { get; set; } = "";
    public string SimilarMatchNextPick3 { get; set; } = "";
    public string SimilarMatchDrawTime { get; set; } = "";
    public string SimilarMatchDate { get; set; } = "";
    public string SimilarMatchCodificacion { get; set; } = "";
}

