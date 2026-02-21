using System;
using System.Linq;
using System.Windows;
using FloridaLotteryApp.Data;

namespace FloridaLotteryApp;

public partial class SearchWindow : Window
{
    public SearchWindow()
    {
        InitializeComponent();
        DpDate.SelectedDate = DateTime.Today;
        ClearDateResults();
    }

    // ================= BUSCAR POR FECHA =================
    private void SearchByDate_Click(object sender, RoutedEventArgs e)
    {
        if (DpDate.SelectedDate == null)
        {
            MessageBox.Show("Selecciona una fecha");
            return;
        }

        var date = DpDate.SelectedDate.Value;

        // Pick 3
        TxtP3Night.Text  = Format(DrawRepository.GetResult("pick3", date, "E"));
        TxtP3Midday.Text = Format(DrawRepository.GetResult("pick3", date, "M"));

        // Pick 4
        TxtP4Night.Text  = Format(DrawRepository.GetResult("pick4", date, "E"));
        TxtP4Midday.Text = Format(DrawRepository.GetResult("pick4", date, "M"));
    }

    // ================= BUSCAR POR COMBINACIÓN =================
    private void SearchByNumber_Click(object sender, RoutedEventArgs e)
    {
        var pick3 = TxtPick3.Text.Trim();
        var pick4 = TxtPick4.Text.Trim();

        if (string.IsNullOrWhiteSpace(pick3) && string.IsNullOrWhiteSpace(pick4))
        {
            MessageBox.Show("Escribe un Pick3, un Pick4, o ambos.");
            return;
        }

        if (!IsValidPick3(pick3) || !IsValidPick4(pick4))
        {
            MessageBox.Show("Pick3 debe tener 3 dígitos y Pick4 4 dígitos.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(pick3) && !string.IsNullOrWhiteSpace(pick4))
        {
            var comboHits = DrawRepository.SearchPick3Pick4Combo(pick3, pick4);
            TxtCount.Text = $"Veces: {comboHits.Count}";
            LvHits.ItemsSource = comboHits.Select(h => new
            {
                Date = h.Date.ToString("yyyy-MM-dd"),
                Game = "P3+P4",
                Icon = h.DrawTime == "M" ? "☀️" : "🌙",
                Number = $"P3 {h.Pick3} | P4 {h.Pick4}",
                Fireball = $"FB3 {(h.Pick3Fireball?.ToString() ?? "-")} / FB4 {(h.Pick4Fireball?.ToString() ?? "-")}"
            }).ToList();
            return;
        }

        var number = !string.IsNullOrWhiteSpace(pick3) ? pick3 : pick4;
        var hits = DrawRepository.SearchByNumberBoth(number);
        TxtCount.Text = $"Veces: {hits.Count}";

        LvHits.ItemsSource = hits.Select(h => new
        {
            Date = h.Date.ToString("yyyy-MM-dd"),
            Game = h.Game,
            Icon = h.DrawTime == "M" ? "☀️" : "🌙",
            h.Number,
            Fireball = h.Fireball?.ToString() ?? "-"
        }).ToList();
    }

    // ================= HELPERS =================
    private static string Format((string? Number, int? Fireball) r)
    {
        return r.Number == null
            ? "---"
            : $"{r.Number} | FB {(r.Fireball?.ToString() ?? "-")}";
    }

    private static bool IsValidPick3(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            || (value.Length == 3 && int.TryParse(value, out _));
    }

    private static bool IsValidPick4(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            || (value.Length == 4 && int.TryParse(value, out _));
    }

    private void ClearDateResults()
    {
        TxtP3Night.Text = "---";
        TxtP3Midday.Text = "---";
        TxtP4Night.Text = "---";
        TxtP4Midday.Text = "---";
    }
}
