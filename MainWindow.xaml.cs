﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using FloridaLotteryApp.Data;
using System.Linq;
using System.Windows.Input;

namespace FloridaLotteryApp;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public ObservableCollection<DayCard> DayCards { get; } = new();

    private const string SupabaseUrl = "https://lmhyfgagksvojfkbnygx.supabase.co";
    private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImxtaHlmZ2Fna3N2b2pma2JueWd4Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTA0MjA4MzksImV4cCI6MjA2NTk5NjgzOX0.bD-j6tXajDumkB7cuck_9aNMGkrAdnAJLzQACoRYaJo";

    private const int PageSize = 12;
    private int _pageIndex = 0;
    private int _totalDates = 0;
    private int _totalPages = 1;

    private bool _canPrev;
    public bool CanPrev { get => _canPrev; set { _canPrev = value; OnPropertyChanged(); } }

    private bool _canNext;
    public bool CanNext { get => _canNext; set { _canNext = value; OnPropertyChanged(); } }

    private string _pageText = "";
    public string PageText { get => _pageText; set { _pageText = value; OnPropertyChanged(); } }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        AnalysisDatePicker.SelectedDate = DateTime.Today;

        Loaded += MainWindow_Loaded;
        LoadPage(0);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        //if (!await IsMaintenanceModeEnabledAsync())
        //{
            //MessageBox.Show("La aplicación se va a Cerrar, contacte con su desarrollador.");
            //Close();
        //}
    }

    private static async Task<bool> IsMaintenanceModeEnabledAsync()
    {
        var requestUrl = $"{SupabaseUrl}/rest/v1/app_config?select=maintenance_mode&order=id.desc&limit=1";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("apikey", SupabaseKey);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SupabaseKey);

        using var response = await client.GetAsync(requestUrl);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
        {
            return false;
        }

        var row = document.RootElement[0];
        if (!row.TryGetProperty("maintenance_mode", out var maintenanceValue))
        {
            return false;
        }

        return maintenanceValue.GetBoolean();
    }

    private void LoadPage(int pageIndex)
    {
        _totalDates = DrawRepository.CountDistinctDatesOverall();

        _pageIndex = Math.Max(0, pageIndex);

        var dates = DrawRepository.GetDistinctDatesOverall(PageSize, _pageIndex * PageSize);

        DayCards.Clear();

        foreach (var d in dates)
        {
            var p3m = DrawRepository.GetResult("pick3", d, "M");
            var p3e = DrawRepository.GetResult("pick3", d, "E");
            var p4m = DrawRepository.GetResult("pick4", d, "M");
            var p4e = DrawRepository.GetResult("pick4", d, "E");

            DayCards.Add(DayCard.From(d, p3m, p3e, p4m, p4e));
        }

        CanPrev = _pageIndex > 0;
        CanNext = (_pageIndex + 1) * PageSize < _totalDates;

        _totalPages = Math.Max(1, (int)Math.Ceiling(_totalDates / (double)PageSize));
        PageText = $"Página {_pageIndex + 1} / {_totalPages}";
    }

    private void Prev_Click(object sender, RoutedEventArgs e) => LoadPage(_pageIndex - 1);
    private void Next_Click(object sender, RoutedEventArgs e) => LoadPage(_pageIndex + 1);
    private void GoToPage_Click(object sender, RoutedEventArgs e) => TryGoToPage();

    private void PageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TryGoToPage();
            e.Handled = true;
        }
    }

    private void TryGoToPage()
    {
        var rawText = PageInput.Text?.Trim() ?? "";
        if (!int.TryParse(rawText, out var page))
        {
            MessageBox.Show("Escribe un número de página válido.");
            return;
        }

        if (page < 1 || page > _totalPages)
        {
            MessageBox.Show($"La página debe estar entre 1 y {_totalPages}.");
            return;
        }

        LoadPage(page - 1);
    }

    private void AddManual_Click(object sender, RoutedEventArgs e)
    {
        var win = new AddPick3Window { Owner = this };
        win.RecordSaved += (_, __) => LoadPage(_pageIndex);
        if (win.ShowDialog() == true)
        {
            // refresca página actual (por si insertaste una fecha reciente)
            LoadPage(_pageIndex);
        }
    }

    private void Search_Click(object sender, RoutedEventArgs e)
    {
        var win = new SearchWindow { Owner = this };
        win.ShowDialog();
    }

    private void Analysis_Click(object sender, RoutedEventArgs e)
            => RunAnalysis(TxtManualP3.Text, TxtManualP4.Text, null, null);

    
    private void Analysis_Midday_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is DayCard card)
        {
            RunAnalysis(card.P3_M_Number, card.P4_M_Number, card.DateText, "M");
        }
    }

    private void Analysis_Night_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is DayCard card)
        {
            RunAnalysis(card.P3_E_Number, card.P4_E_Number, card.DateText, "E");
        }
    }

    private void Edit_Draw_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not DayCard card)
        {
            return;
        }

        if (!TryParseDrawTimeTag(element.Tag, out var drawTime))
        {
            MessageBox.Show("No se pudo determinar el registro a editar.");
            return;
        }

        if (!DateTime.TryParse(card.DateText, out var date))
        {
            MessageBox.Show("Fecha inválida para el registro.");
            return;
        }

        var pick3 = DrawRepository.GetResult("pick3", date, drawTime);
        var pick4 = DrawRepository.GetResult("pick4", date, drawTime);

        if (string.IsNullOrWhiteSpace(pick3.Number) || string.IsNullOrWhiteSpace(pick4.Number))
        {
            MessageBox.Show("No hay datos para editar en este registro.");
            return;
        }

        var win = new EditDrawWindow(
            date,
            drawTime,
            pick3.Number,
            pick3.Fireball,
            pick4.Number,
            pick4.Fireball)
        { Owner = this };
        if (win.ShowDialog() == true)
        {
            LoadPage(_pageIndex);
        }
    }

    private void Delete_Draw_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not DayCard card)
        {
            return;
        }

        if (!TryParseDrawTimeTag(element.Tag, out var drawTime))
        {
            MessageBox.Show("No se pudo determinar el registro a eliminar.");
            return;
        }

        if (!DateTime.TryParse(card.DateText, out var date))
        {
            MessageBox.Show("Fecha inválida para el registro.");
            return;
        }

        var drawLabel = drawTime == "M" ? "Mediodía" : "Noche";
        var confirm = MessageBox.Show(
            $"¿Seguro que deseas eliminar Pick 3 y Pick 4 ({drawLabel}) del {date:yyyy-MM-dd}?",
            "Confirmar eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        if (!ManualInsertRepository.DeleteLatestPair(date, drawTime))
        {
            MessageBox.Show("No se encontró un registro para eliminar.");
            return;
        }

        LoadPage(_pageIndex);
    }

    private static bool TryParseDrawTimeTag(object? tag, out string drawTime)
    {
        drawTime = "";
        if (tag is not string tagValue)
        {
            return false;
        }

        drawTime = tagValue;
        return drawTime is "M" or "E";
    }

    private static string DrawTimeToIcon(string drawTime)
    {
        return drawTime switch
        {
            "M" => "☀️",
            "E" => "🌙",
            _ => ""
        };
    }

    private void RunAnalysis(string? pick3Input, string? pick4Input, string? dateText, string? drawTime)
    {
        var p3 = (pick3Input ?? "").Trim();
        var p4 = (pick4Input ?? "").Trim();

        if (p3.Contains('-') || p4.Contains('-'))
        {
            MessageBox.Show("No hay resultados válidos para analizar en esta tirada.");
            return;
        }

        // Validación básica de longitud y numérico (para que no reviente después)
        if (p3.Length != 3 || !p3.All(char.IsDigit))
        {
            MessageBox.Show("Pick 3 inválido. Debe ser 3 dígitos (ej: 234).");
            return;
        }
        if (p4.Length != 4 || !p4.All(char.IsDigit))
        {
            MessageBox.Show("Pick 4 inválido. Debe ser 4 dígitos (ej: 2458).");
            return;
        }
        // Regla 1: NO repetir dígitos dentro de Pick3
        if (p3.Distinct().Count() != 3)
        {
            MessageBox.Show("Pick 3 inválido: no se pueden repetir dígitos.");
            return;
        }
        // Regla 1: NO repetir dígitos dentro de Pick4
        if (p4.Distinct().Count() != 4)
        {
            MessageBox.Show("Pick 4 inválido: no se pueden repetir dígitos.");
            return;
        }
        // Regla 2: debe repetirse EXACTAMENTE 1 dígito entre Pick3 y Pick4
        var comunes = p3.Intersect(p4).ToList();

        if (comunes.Count != 1)
        {
            MessageBox.Show($"Inválido: debe repetirse EXACTAMENTE 1 dígito entre Pick 3 y Pick 4. Ahora se repiten: {comunes.Count}");
            return;
        }

        char d = comunes[0];
        int posP3 = p3.IndexOf(d) + 1; // 1..3
        int posP4 = p4.IndexOf(d) + 1; // 1..4
        var analysisDate = AnalysisDatePicker.SelectedDate ?? DateTime.Today;
        var guideDateText = string.IsNullOrWhiteSpace(dateText)
            ? analysisDate.ToString("yyyy-MM-dd")
            : dateText;
        var guideDate = DateTime.TryParse(guideDateText, out var parsedGuideDate)
            ? parsedGuideDate
            : analysisDate;
        var guideDrawTime = drawTime ?? ResolvePick3DrawTime(guideDate, p3);
        // BÚSQUEDA EN BD
        var hits = FloridaLotteryApp.Data.DrawRepository.FindPositionMatches(posP3, posP4);
        var rows = hits.Where(h => !IsSamePattern(h.Pick3, h.Pick4, p3, p4))
            .Where(h => IsBeforeGuide(h.Date, h.DrawTime, guideDate, guideDrawTime))
            .Select(h => new
            {
                Hit = h,
                NextPick3 = DrawRepository.GetNextPick3Number(h.Date, h.DrawTime)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.NextPick3)) // <-- si no hay próxima válida, NO entra
            .Select(x => new AnalysisRow
            {
                IsChecked = false,
                Date = x.Hit.Date.ToString("yyyy-MM-dd"),
                DrawTime = x.Hit.DrawTime == "M" ? "☀️" : "🌙",
                Pick3 = x.Hit.Pick3,
                Pick4 = x.Hit.Pick4,

                NextPick3 = x.NextPick3!, // ya está validado
                Coding = BuildCoding(x.Hit.Pick3, x.Hit.Pick4)
            })
            .ToList();

        //var analysisDate = AnalysisDatePicker.SelectedDate ?? DateTime.Today;
        //var guideDateText = string.IsNullOrWhiteSpace(dateText)
        //    ? analysisDate.ToString("yyyy-MM-dd")
        //    : dateText;
        //var guideDate = DateTime.TryParse(guideDateText, out var parsedGuideDate)
        //    ? parsedGuideDate
        //    : analysisDate;
        //var guideDrawTime = drawTime ?? ResolvePick3DrawTime(guideDate, p3);
        var guideNextPick3 = guideDrawTime == null
            ? ""
            : DrawRepository.GetNextPick3Number(guideDate, guideDrawTime) ?? "";
        
        var guide = new GuideInfo
        {
            Pick3 = p3,
            Pick4 = p4,
            NextPick3 = guideNextPick3,
            Coding = BuildCoding(p3, p4),
            DateText = guideDateText,
            DrawIcon = DrawTimeToIcon(guideDrawTime), // si luego quieres elegir ☀️/🌙 en la entrada, aquí lo ponemos
            RepPosP3 = posP3,
            RepPosP4 = posP4
        };

        var win = new AnalysisCardsWindow(guide, rows) { Owner = this };
        win.ShowDialog();
    }

    private static string BuildCoding(string pick3, string pick4)
    {
        // Unimos dígitos, quitamos duplicados, ordenamos ascendente
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

    private static string? ResolvePick3DrawTime(DateTime date, string pick3)
    {
        if (string.IsNullOrWhiteSpace(pick3))
        {
            return null;
        }

        return DrawRepository.SearchPick3ByNumber(pick3)
            .FirstOrDefault(hit => hit.Date.Date == date.Date)
            ?.DrawTime;
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






    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}






public class DayCard
{
    public string DateText { get; set; } = "";

    public string P3_M_Number { get; set; } = "---";
    public string P3_M_FB { get; set; } = "FB";
    public string P3_E_Number { get; set; } = "---";
    public string P3_E_FB { get; set; } = "FB";
    public bool P3_M_Analyzable { get; set; }
    public bool P3_E_Analyzable { get; set; }
    public bool M_Exists { get; set; }
    public bool E_Exists { get; set; }

    public string P4_M_First2 { get; set; } = "--";
    public string P4_M_Last2  { get; set; } = "--";
    public string P4_M_FB     { get; set; } = "FB";
    public string P4_M_Number { get; set; } = "----";

    public string P4_E_First2 { get; set; } = "--";
    public string P4_E_Last2  { get; set; } = "--";
    public string P4_E_FB     { get; set; } = "FB";
    public string P4_E_Number { get; set; } = "----";

    public static DayCard From(
        DateTime date,
        (string? Number, int? Fireball) p3m,
        (string? Number, int? Fireball) p3e,
        (string? Number, int? Fireball) p4m,
        (string? Number, int? Fireball) p4e)
    {
        var card = new DayCard
        {
            DateText = date.ToString("yyyy-MM-dd")
        };

        // Pick3
        card.P3_M_Number = p3m.Number ?? "---";
        card.P3_M_FB = p3m.Fireball?.ToString() ?? "-";

        card.P3_E_Number = p3e.Number ?? "---";
        card.P3_E_FB = p3e.Fireball?.ToString() ?? "-";

        // Pick4 split
        (card.P4_M_First2, card.P4_M_Last2) = Split2(p4m.Number);
        card.P4_M_FB = p4m.Fireball?.ToString() ?? "-";
        card.P4_M_Number = p4m.Number ?? "----";

        (card.P4_E_First2, card.P4_E_Last2) = Split2(p4e.Number);
        card.P4_E_FB = p4e.Fireball?.ToString() ?? "-";
        card.P4_E_Number = p4e.Number ?? "----";

        card.P3_M_Analyzable = IsAnalyzable(card.P3_M_Number, card.P4_M_Number);
        card.P3_E_Analyzable = IsAnalyzable(card.P3_E_Number, card.P4_E_Number);
        card.M_Exists = !string.IsNullOrWhiteSpace(p3m.Number) && !string.IsNullOrWhiteSpace(p4m.Number);
        card.E_Exists = !string.IsNullOrWhiteSpace(p3e.Number) && !string.IsNullOrWhiteSpace(p4e.Number);

        return card; 
    }

    private static (string, string) Split2(string? number)
    {
        if (string.IsNullOrWhiteSpace(number) || number.Length < 4) return ("--", "--");
        return (number.Substring(0, 2), number.Substring(2, 2));
    }

    private static bool IsAnalyzable(string pick3, string pick4)
    {
        if (pick3.Length != 3 || pick4.Length != 4) return false;
        if (!pick3.All(char.IsDigit) || !pick4.All(char.IsDigit)) return false;
        if (pick3.Distinct().Count() != 3) return false;
        if (pick4.Distinct().Count() != 4) return false;
        return pick3.Intersect(pick4).Count() == 1;
    }

    
}
