using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    
    // Colecciones para FILA 4
    public List<string> Row4Pick3 { get; set; } = new();
    public List<string> Row4Pick4 { get; set; } = new();
    public List<string> Row4Fireball { get; set; } = new();
    public List<string> Row4Additional { get; set; } = new();

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
        
        Row3Pick3 = new List<string> { "7", "8", "9" };
        Row3Pick4 = new List<string> { "0", "1", "2", "3" };
        Row3Fireball = new List<string> { "4", "5" };
        Row3Additional = new List<string> { "1", "3", "5", "7" };
        
        Row4Pick3 = new List<string> { "4", "5", "6" };
        Row4Pick4 = new List<string> { "7", "8", "9", "0" };
        Row4Fireball = new List<string> { "1", "2" };
        Row4Additional = new List<string> { "0", "2", "4", "6" };
        
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
        
        // FILA 4
        Row4_Pick3Digits.ItemsSource = Row4Pick3;
        Row4_Pick4Digits.ItemsSource = Row4Pick4;
        Row4_FireballDigits.ItemsSource = Row4Fireball;
        Row4_AdditionalDigits.ItemsSource = Row4Additional;
        
        PatternsTable.ItemsSource = PatternRows;
        if (PatternRows.Count > 0)
        {
            PatternsTable.SelectedIndex = 0;
            PatternsTable.ScrollIntoView(PatternRows[0]);
            ApplySelectionToRow2(PatternRows[0]);
        }
    }

    private void LoadPatternRows(string guidePick3, string guidePick4)
    {
        PatternRows.Clear();

        var suffix = new string((guidePick3 ?? "").Where(char.IsDigit).TakeLast(2).ToArray());
        if (suffix.Length != 2)
        {
            return;
        }

        var guideNumber = new string(
            (guidePick3 ?? "")
                .Where(char.IsDigit)
                .Concat((guidePick4 ?? "").Where(char.IsDigit))
                .ToArray());

        var matches = DrawRepository.SearchPick3BySuffixWithPick4(suffix);
        foreach (var hit in matches)
        {
            var nextPick3 = DrawRepository.GetNextPick3Number(hit.Date, hit.DrawTime) ?? "";
            PatternRows.Add(new PatternRow
            {
                ReferenceNumber = guideNumber,
                MatchNumber = $"{hit.Pick3}{hit.Pick4}",
                SimilarPatternNumber = "",
                SimilarMatchNumber = "",
                MatchPick3 = hit.Pick3,
                MatchPick4 = hit.Pick4,
                MatchNextPick3 = nextPick3,
                MatchDrawTime = hit.DrawTime,
                MatchDate = hit.Date.ToString("yyyy-MM-dd"),
                MatchCodificacion = string.Concat(BuildCodificacionDigits(hit.Pick3, hit.Pick4))
            });
        }
    }

    private void PatternsTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PatternsTable.SelectedItem is not PatternRow selected)
        {
            return;
        }

        ApplySelectionToRow2(selected);
    }

    private void ApplySelectionToRow2(PatternRow selected)
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
        Row2_DrawIcon.Text = selected.MatchDrawTime == "M" ? "☀️" : "🌙";
        Row2_DateText.Text = Row2Date;
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

/// <summary>
/// Clase para las filas de la tabla de patrones (4 columnas)
/// </summary>
public class PatternRow
{
    public string ReferenceNumber { get; set; } = "";
    public string MatchNumber { get; set; } = "";
    public string SimilarPatternNumber { get; set; } = "";
    public string SimilarMatchNumber { get; set; } = "";
    public string MatchPick3 { get; set; } = "";
    public string MatchPick4 { get; set; } = "";
    public string MatchNextPick3 { get; set; } = "";
    public string MatchDrawTime { get; set; } = "";
    public string MatchDate { get; set; } = "";
    public string MatchCodificacion { get; set; } = "";
}
