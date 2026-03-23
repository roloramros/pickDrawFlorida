using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace FloridaLotteryApp;

public enum ExtendedAnalysis3Mode
{
    OneLine,
    CrossLine
}


public partial class AnalysisCardsWindow : Window
{
    public ObservableCollection<AnalysisPairCardVM> Cards { get; } = new();


    public AnalysisCardsWindow(GuideInfo guide, IEnumerable<AnalysisRow> resultRows)
    {
        InitializeComponent();
        DataContext = this;
        this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
        this.Top = 0;

        foreach (var card in resultRows.Select(r => AnalysisPairCardVM.Create(guide, r)))
            Cards.Add(card);
    }

    private void CardBorder_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement root)
        {
            return;
        }

        root.Dispatcher.BeginInvoke(new Action(() => Pick3LinksRenderer.DrawPick3Links(root)), DispatcherPriority.Loaded);
    }

    // Botón 1: Análisis de Línea (reemplaza clic izquierdo)
    private void LineAnalysisButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not AnalysisPairCardVM selectedCard)
            return;

        var detailWindow = new AnalysisLineMatchesWindow(selectedCard, Cards)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        detailWindow.Show();
    }

    // Botón 2: Análisis de Posición (reemplaza clic derecho)
    private void PositionAnalysisButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not AnalysisPairCardVM selectedCard)
            return;

        var detailWindow = new AnalysisPositionMatchesWindow(selectedCard, Cards)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        detailWindow.Show();
    }

    private void ThirdAnalysisButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not AnalysisPairCardVM selectedCard)
            return;

        var thirdAnalysisWindow = new ThirdAnalysisWindow(selectedCard)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        if (thirdAnalysisWindow.AnalysisCards.Count == 0)
        {
            MessageBox.Show("No se encontraron resultados para el tercer analisis.",
                            "Tercer analisis",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
            return;
        }

        thirdAnalysisWindow.Show();
    }

    private void ExtendedAnalysis3MenuItem_Click(object sender, RoutedEventArgs e)
    {
        var analysisMode = sender is MenuItem menuItem &&
                           menuItem.Header?.ToString()?.Contains("Cross Line", StringComparison.OrdinalIgnoreCase) == true
            ? ExtendedAnalysis3Mode.CrossLine
            : ExtendedAnalysis3Mode.OneLine;
        var filteredCards = Cards.Where(HasSingleStraightLine).ToList();

        if (filteredCards.Count == 0)
        {
            MessageBox.Show("No hay tarjetas que cumplan con el filtro de linea recta unica.",
                           "Analisis 3 Extendido",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);
            return;
        }

        var extendedAnalysisWindow = new Analisis3ExtendidoWindow(filteredCards, analysisMode)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        extendedAnalysisWindow.Show();
    }

    // Manejador para todos los items del menú
    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Contar cuántas tarjetas había originalmente
            int originalCount = Cards.Count;
            
            // Filtrar las tarjetas
            var filteredCards = new ObservableCollection<AnalysisPairCardVM>();
            
            foreach (var card in Cards)
            {
                if (HasSingleStraightLine(card))
                {
                    filteredCards.Add(card);
                }
            }
            
            // Actualizar la colección
            Cards.Clear();
            foreach (var card in filteredCards)
            {
                Cards.Add(card);
            }
            
            // Mostrar mensaje con el resultado del filtro
            MessageBox.Show($"Filtro aplicado: Línea recta única\n\n" +
                           $"Tarjetas originales: {originalCount}\n" +
                           $"Tarjetas después del filtro: {filteredCards.Count}\n\n" +
                           $"Mostrando solo tarjetas con exactamente UNA línea recta vertical.",
                           "Filtro aplicado",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al aplicar el filtro: {ex.Message}", 
                           "Error", 
                           MessageBoxButton.OK, 
                           MessageBoxImage.Error);
        }
    }

    
    // Método para verificar si una tarjeta tiene exactamente una línea recta vertical
// y NINGUNA línea diagonal
    private bool HasSingleStraightLine(AnalysisPairCardVM card)
    {
        try
        {
            // Obtener los dígitos de la fila superior (guía) y fila inferior (resultado)
            var topDigits = card.GuidePick3Digits;
            var bottomDigits = card.ResPick3Digits;
            
            // Verificar que ambas colecciones tengan 3 dígitos
            if (topDigits.Count != 3 || bottomDigits.Count != 3)
                return false;
            
            // Contar coincidencias en la MISMA POSICIÓN (línea recta vertical)
            int straightLineMatches = 0;
            
            // Contar coincidencias en DIFERENTES POSICIONES (línea diagonal)
            int diagonalMatches = 0;
            
            for (int i = 0; i < 3; i++)
            {
                string topValue = topDigits[i].Value;
                
                // Si el dígito superior está vacío, continuar
                if (string.IsNullOrWhiteSpace(topValue))
                    continue;
                
                for (int j = 0; j < 3; j++)
                {
                    string bottomValue = bottomDigits[j].Value;
                    
                    // Si el dígito inferior está vacío, continuar
                    if (string.IsNullOrWhiteSpace(bottomValue))
                        continue;
                    
                    // Verificar si hay coincidencia
                    if (topValue == bottomValue)
                    {
                        if (i == j)
                        {
                            // Misma posición = línea recta vertical
                            straightLineMatches++;
                        }
                        else
                        {
                            // Diferente posición = línea diagonal
                            diagonalMatches++;
                        }
                    }
                }
            }
            
            // Una tarjeta válida debe tener:
            // 1. EXACTAMENTE UNA línea recta vertical
            // 2. CERO líneas diagonales
            return straightLineMatches == 1 && diagonalMatches == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en HasSingleStraightLine: {ex.Message}");
            return false;
        }
    }


    // Método auxiliar para obtener la ruta completa del menú
    private string GetMenuPath(MenuItem item)
    {
        string path = item.Header.ToString();
        var parent = item.Parent as MenuItem;
        
        while (parent != null)
        {
            path = parent.Header.ToString() + " → " + path;
            parent = parent.Parent as MenuItem;
        }
        
        return path;
    }
}

public class GuideInfo
{
    public string Pick3 { get; set; } = "";
    public string Pick4 { get; set; } = "";
    public string NextPick3 { get; set; } = "";
    public string Coding { get; set; } = "";
    public string DateText { get; set; } = "";     // yyyy-MM-dd
    public string DrawIcon { get; set; } = "";     // ☀️ / 🌙
    public int RepPosP3 { get; set; }
    public int RepPosP4 { get; set; }
}

public class AnalysisPairCardVM
{

    private static readonly Brush HighlightBrush =
    new SolidColorBrush(Color.FromRgb(255, 140, 0)); // amarillo tenue
    private static readonly Brush AlertBrush =
    new SolidColorBrush(Color.FromRgb(220, 53, 69)); // rojo
    private static readonly Brush InfoBrush =
    new SolidColorBrush(Color.FromRgb(13, 110, 253)); // azul
    private static readonly Brush SuccessBrush =
    new SolidColorBrush(Color.FromRgb(25, 135, 84)); // verde


    // GUÍA (igual en todas las cards)
    public string GuidePick3Value { get; set; } = "";
    public string GuidePick4Value { get; set; } = "";
    public string GuideCodingValue { get; set; } = "";
    public string GuideNextPick3Value { get; set; } = "";
    public string GuideDateText { get; set; } = "";
    public string GuideDrawIcon { get; set; } = "";
    public ObservableCollection<DigitVM> GuidePick3Digits { get; set; } = new();
    public ObservableCollection<DigitVM> GuidePick4Digits { get; set; } = new();
    public ObservableCollection<DigitVM> GuideNextPick3Digits { get; set; } = new();
    public ObservableCollection<DigitVM> GuideCodingDigits { get; set; } = new();
    

    // RESULTADO
    public string ResPick3Value { get; set; } = "";
    public string ResPick4Value { get; set; } = "";
    public string ResNextPick3Value { get; set; } = "";
    public string ResDateText { get; set; } = "";
    public string ResDrawIcon { get; set; } = "";
    public ObservableCollection<DigitVM> ResPick3Digits { get; set; } = new();
    public ObservableCollection<DigitVM> ResPick4Digits { get; set; } = new();
    public ObservableCollection<DigitVM> ResNextPick3Digits { get; set; } = new();
    public ObservableCollection<DigitVM> ResCodingDigits { get; set; } = new();

    public static AnalysisPairCardVM Create(GuideInfo guide, AnalysisRow r)
    {
        var vm = new AnalysisPairCardVM
        {
            GuidePick3Value = guide.Pick3,
            GuidePick4Value = guide.Pick4,
            GuideNextPick3Value = guide.NextPick3,
            GuideCodingValue = guide.Coding,
            GuideDateText = guide.DateText,
            GuideDrawIcon = guide.DrawIcon,
            GuidePick3Digits = DigitsFrom(guide.Pick3, 3),
            GuidePick4Digits = DigitsFrom(guide.Pick4, 4),
            GuideNextPick3Digits = DigitsFrom(guide.NextPick3, 3, Brushes.White),
            GuideCodingDigits = DigitsFrom(guide.Coding, 6),

            ResPick3Value = r.Pick3,
            ResPick4Value = r.Pick4,
            ResNextPick3Value = r.NextPick3,
            ResDateText = r.Date,
            ResDrawIcon = r.DrawTime, // ☀️/🌙
            ResPick3Digits = DigitsFrom(r.Pick3, 3),
            ResPick4Digits = DigitsFrom(r.Pick4, 4),
            ResNextPick3Digits = DigitsFrom(r.NextPick3, 3),
            ResCodingDigits = DigitsFrom(r.Coding, 6),
        };

        // Colorear POSICIONES (no el valor): en Guía y Resultado
        HighlightPosition(vm.GuidePick3Digits, guide.RepPosP3);
        HighlightPosition(vm.GuidePick4Digits, guide.RepPosP4);
        HighlightPosition(vm.ResPick3Digits, guide.RepPosP3);
        HighlightPosition(vm.ResPick4Digits, guide.RepPosP4);

        HighlightNextPick3Digits(vm.ResNextPick3Digits, vm.ResPick3Digits, vm.ResPick4Digits, r.Pick3, r.Pick4);

        return vm;
    }

    public string BuildColorSignature()
    {
        var builder = new StringBuilder();
        AppendColors(builder, GuidePick3Digits);
        AppendColors(builder, GuidePick4Digits);
        AppendColors(builder, ResPick3Digits);
        AppendColors(builder, ResPick4Digits);
        AppendColors(builder, ResNextPick3Digits);
        return builder.ToString();
    }

    public string BuildResultColorSignature()
    {
        var builder = new StringBuilder();
        AppendColors(builder, ResPick3Digits);
        AppendColors(builder, ResPick4Digits);
        AppendColors(builder, ResNextPick3Digits);
        AppendColors(builder, ResCodingDigits);
        return builder.ToString();
    }

    public string BuildPick3LineSignature()
    {
        return BuildPick3LineSignature(GuidePick3Digits, ResPick3Digits);
    }

    public static string BuildPick3LineSignature(IReadOnlyList<DigitVM> topDigits, IReadOnlyList<DigitVM> bottomDigits)

    
    {
        var builder = new StringBuilder();
        var matches = topDigits
            .Select((digit, topIndex) => (digit.Value, topIndex))
            .Where(digit => !string.IsNullOrWhiteSpace(digit.Value))
            .SelectMany(top =>
                bottomDigits
                    .Select((digit, bottomIndex) => (digit.Value, bottomIndex))
                    .Where(bottom => string.Equals(bottom.Value, top.Value, StringComparison.Ordinal))
                    .Select(bottom => (TopIndex: top.topIndex, BottomIndex: bottom.bottomIndex)))
            .OrderBy(match => match.TopIndex)
            .ThenBy(match => match.BottomIndex);

        foreach (var match in matches)
        {
            builder.Append(match.TopIndex);
            builder.Append('-');
            builder.Append(match.BottomIndex);
            builder.Append('|');
        }

        return builder.ToString();
    }

    public static void HighlightRepeatedDigits(
        ObservableCollection<DigitVM> pick3Digits,
        ObservableCollection<DigitVM> pick4Digits,
        string pick3,
        string pick4)
    {
        var repeat = FindRepeatedDigit(pick3, pick4);
        if (repeat == null)
        {
            return;
        }

        HighlightMatchingDigits(pick3Digits, repeat.Value.ToString(), HighlightBrush);
        HighlightMatchingDigits(pick4Digits, repeat.Value.ToString(), HighlightBrush);
    }

    private static ObservableCollection<DigitVM> DigitsFrom(string s, int count)
        => DigitsFrom(s, count, Brushes.Transparent);

    private static ObservableCollection<DigitVM> DigitsFrom(string s, int count, Brush background)
    {
        s = (s ?? "").Trim();
        var list = new ObservableCollection<DigitVM>();
        for (int i = 0; i < count; i++)
        {
            var val = (i < s.Length) ? s[i].ToString() : "";
            list.Add(new DigitVM { Value = val, Bg = background });
        }
        return list;
    }


    private static void HighlightPosition(ObservableCollection<DigitVM> list, int pos1Based)
    {
        int idx = pos1Based - 1;
        if (idx < 0 || idx >= list.Count) return;
        list[idx].Bg = HighlightBrush;
    }

    private static void HighlightNextPick3Digits(
        ObservableCollection<DigitVM> list,
        ObservableCollection<DigitVM> pick3Digits,
        ObservableCollection<DigitVM> pick4Digits,
        string pick3,
        string pick4)
    {
        var repeat = FindRepeatedDigit(pick3, pick4);
        if (repeat != null)
        {
            foreach (var digit in list)
            {
                if (digit.Value == repeat.ToString())
                    digit.Bg = HighlightBrush;
            }
        }

        HighlightNextPick3Position(list, pick3Digits, pick4Digits, pick3, pick4, repeat, 0, AlertBrush);
        HighlightNextPick3Position(list, pick3Digits, pick4Digits, pick3, pick4, repeat, 1, InfoBrush);
        HighlightNextPick3Position(list, pick3Digits, pick4Digits, pick3, pick4, repeat, 2, SuccessBrush);
    }

    private static char? FindRepeatedDigit(string pick3, string pick4)
    {
        if (string.IsNullOrWhiteSpace(pick3) || string.IsNullOrWhiteSpace(pick4)) return null;

        var repeats = pick3.Intersect(pick4).ToList();
        return repeats.Count == 1 ? repeats[0] : null;
    }

    private static void HighlightNextPick3Position(
        ObservableCollection<DigitVM> list,
        ObservableCollection<DigitVM> pick3Digits,
        ObservableCollection<DigitVM> pick4Digits,
        string pick3,
        string pick4,
        char? repeat,
        int index,
        Brush highlight)
    {
        if (index < 0 || index >= list.Count) return;

        var digit = list[index];
        if (string.IsNullOrWhiteSpace(digit.Value)) return;
        if (repeat != null && digit.Value == repeat.ToString()) return;

        if (pick3.Contains(digit.Value) || pick4.Contains(digit.Value))
        {
            digit.Bg = highlight;
            HighlightMatchingDigits(pick3Digits, digit.Value, highlight);
            HighlightMatchingDigits(pick4Digits, digit.Value, highlight);
        }
    }

    private static void HighlightMatchingDigits(
        ObservableCollection<DigitVM> list,
        string value,
        Brush highlight)
    {
        foreach (var digit in list)
        {
            if (digit.Value == value)
                digit.Bg = highlight;
        }
    }

    private static void AppendColors(StringBuilder builder, ObservableCollection<DigitVM> digits)
    {
        foreach (var digit in digits)
        {
            builder.Append(GetBrushCode(digit.Bg));
        }
    }

    private static char GetBrushCode(Brush brush)
    {
        if (IsSameBrush(brush, HighlightBrush)) return 'O';
        if (IsSameBrush(brush, AlertBrush)) return 'R';
        if (IsSameBrush(brush, InfoBrush)) return 'B';
        if (IsSameBrush(brush, SuccessBrush)) return 'G';
        return 'N';
    }

    private static bool IsSameBrush(Brush? left, Brush? right)
    {
        if (left == null || right == null) return false;
        if (ReferenceEquals(left, right)) return true;
        if (left is SolidColorBrush leftSolid && right is SolidColorBrush rightSolid)
        {
            return leftSolid.Color.Equals(rightSolid.Color);
        }
        return false;
    }
}

public class DigitVM
{
    public string Value { get; set; } = "";
    public Brush Bg { get; set; } = Brushes.Transparent;
}






