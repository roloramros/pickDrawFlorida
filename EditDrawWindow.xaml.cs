using System.Windows;

namespace FloridaLotteryApp;

public partial class EditDrawWindow : Window
{
    private readonly DateTime _date;
    private readonly string _drawTime;

    public EditDrawWindow(
        DateTime date,
        string drawTime,
        string pick3Number,
        int? pick3Fireball,
        string pick4Number,
        int? pick4Fireball)
    {
        InitializeComponent();

        _date = date;
        _drawTime = drawTime;

        TxtDate.Text = date.ToString("yyyy-MM-dd");
        TxtDrawTime.Text = drawTime == "M" ? "Mediodía" : "Noche";
        TxtPick3Number.Text = pick3Number;
        TxtPick3Fireball.Text = pick3Fireball?.ToString() ?? "";
        TxtPick4Number.Text = pick4Number;
        TxtPick4Fireball.Text = pick4Fireball?.ToString() ?? "";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (TxtPick3Number.Text.Length != 3 || !int.TryParse(TxtPick3Number.Text, out _))
        {
            MessageBox.Show("El número de Pick 3 debe tener 3 dígitos");
            return;
        }

        if (TxtPick4Number.Text.Length != 4 || !int.TryParse(TxtPick4Number.Text, out _))
        {
            MessageBox.Show("El número de Pick 4 debe tener 4 dígitos");
            return;
        }

        int? pick3Fireball = null;
        if (!string.IsNullOrWhiteSpace(TxtPick3Fireball.Text))
        {
            if (!int.TryParse(TxtPick3Fireball.Text, out var fb))
            {
                MessageBox.Show("Fireball de Pick 3 inválido");
                return;
            }

            pick3Fireball = fb;
        }

        int? pick4Fireball = null;
        if (!string.IsNullOrWhiteSpace(TxtPick4Fireball.Text))
        {
            if (!int.TryParse(TxtPick4Fireball.Text, out var fb))
            {
                MessageBox.Show("Fireball de Pick 4 inválido");
                return;
            }

            pick4Fireball = fb;
        }

        if (!Data.ManualInsertRepository.UpdateLatestPair(
            _date,
            _drawTime,
            TxtPick3Number.Text,
            pick3Fireball,
            TxtPick4Number.Text,
            pick4Fireball))
        {
            MessageBox.Show("No se pudo actualizar el registro.");
            return;
        }

        DialogResult = true;
        Close();
    }
}
