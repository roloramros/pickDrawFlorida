using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FloridaLotteryApp;

public partial class PlotWindow : Window
{
    // Colecciones para la tabla de patrones
    public ObservableCollection<PatternRow> PatternRows { get; set; } = new();
    
    // Colecciones para FILA 1 (Tirada seleccionada)
    public List<string> Row1Pick3 { get; set; } = new();
    public List<string> Row1Pick4 { get; set; } = new();
    public List<string> Row1Fireball { get; set; } = new();
    public List<string> Row1Additional { get; set; } = new();
    
    // Colecciones para FILA 2
    public List<string> Row2Pick3 { get; set; } = new();
    public List<string> Row2Pick4 { get; set; } = new();
    public List<string> Row2Fireball { get; set; } = new();
    public List<string> Row2Additional { get; set; } = new();
    
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

    public PlotWindow(string dateText, string drawIcon, string pick3, string pick4)
    {
        InitializeComponent();
        
        // Configurar DataContext
        DataContext = this;
        
        // ==========================================
        // FILA 1: Datos de la tirada seleccionada
        // ==========================================
        Row1Pick3 = pick3.Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row1Pick4 = pick4.Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row1Fireball = new List<string> { "3", "6" }; // Ejemplo - luego lo calculas
        Row1Additional = new List<string> { "0", "1", "3", "5", "6", "8" }; // Ejemplo
        
        // ==========================================
        // FILA 2, 3, 4: Datos de ejemplo (luego los llenas con tu lógica)
        // ==========================================
        Row2Pick3 = new List<string> { "1", "2", "3" };
        Row2Pick4 = new List<string> { "4", "5", "6", "7" };
        Row2Fireball = new List<string> { "0", "8" };
        Row2Additional = new List<string> { "2", "4", "6", "9" };
        
        Row3Pick3 = new List<string> { "7", "8", "9" };
        Row3Pick4 = new List<string> { "0", "1", "2", "3" };
        Row3Fireball = new List<string> { "4", "5" };
        Row3Additional = new List<string> { "1", "3", "5", "7" };
        
        Row4Pick3 = new List<string> { "4", "5", "6" };
        Row4Pick4 = new List<string> { "7", "8", "9", "0" };
        Row4Fireball = new List<string> { "1", "2" };
        Row4Additional = new List<string> { "0", "2", "4", "6" };
        
        // Inicializar datos de ejemplo para la tabla superior
        InitializeSampleData();
        
        // Asignar ItemsSource a todos los ItemsControls
        // FILA 1
        Row1_Pick3Digits.ItemsSource = Row1Pick3;
        Row1_Pick4Digits.ItemsSource = Row1Pick4;
        Row1_FireballDigits.ItemsSource = Row1Fireball;
        Row1_AdditionalDigits.ItemsSource = Row1Additional;
        Row1_DrawIcon.Text = drawIcon;
        
        // FILA 2
        Row2_Pick3Digits.ItemsSource = Row2Pick3;
        Row2_Pick4Digits.ItemsSource = Row2Pick4;
        Row2_FireballDigits.ItemsSource = Row2Fireball;
        Row2_AdditionalDigits.ItemsSource = Row2Additional;
        
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
    }

    private void InitializeSampleData()
    {
        PatternRows.Clear();
        
        // Datos de ejemplo para la tabla
        PatternRows.Add(new PatternRow 
        { 
            ReferenceNumber = "6007630", 
            MatchNumber = "6002581",
            SimilarPatternNumber = "2664236",
            SimilarMatchNumber = "2661958"
        });
        
        PatternRows.Add(new PatternRow 
        { 
            ReferenceNumber = "6007630", 
            MatchNumber = "9002681",
            SimilarPatternNumber = "1227132",
            SimilarMatchNumber = "1229068"
        });
        
        PatternRows.Add(new PatternRow 
        { 
            ReferenceNumber = "6007630", 
            MatchNumber = "1002570",
            SimilarPatternNumber = "2664236",
            SimilarMatchNumber = "1665846"
        });
        
        PatternRows.Add(new PatternRow 
        { 
            ReferenceNumber = "6007630", 
            MatchNumber = "4008916",
            SimilarPatternNumber = "1227132",
            SimilarMatchNumber = "4226901"
        });
        
        PatternRows.Add(new PatternRow 
        { 
            ReferenceNumber = "6007630", 
            MatchNumber = "9009728",
            SimilarPatternNumber = "2664236",
            SimilarMatchNumber = "9669415"
        });
        
        PatternRows.Add(new PatternRow 
        { 
            ReferenceNumber = "6007630", 
            MatchNumber = "4009280",
            SimilarPatternNumber = "2664236",
            SimilarMatchNumber = "1660786"
        });
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
}