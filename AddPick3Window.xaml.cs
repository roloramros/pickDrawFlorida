using System;
using System.Windows;
using FloridaLotteryApp.Data;
using System.Windows.Controls;

namespace FloridaLotteryApp;

public partial class AddPick3Window : Window
{
    public event EventHandler? RecordSaved;

    public AddPick3Window()
    {
        InitializeComponent();
        DatePickerDate.SelectedDate = DateTime.Today;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (DatePickerDate.SelectedDate == null)
        {
            MessageBox.Show("Selecciona una fecha", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var drawItem = ComboDrawTime.SelectedItem as ComboBoxItem;
        var drawTime = drawItem?.Tag?.ToString();
        if (drawTime == null)
        {
            MessageBox.Show("Selecciona el sorteo.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (TxtPick3Number.Text.Length != 3 || !int.TryParse(TxtPick3Number.Text, out _))
        {
            MessageBox.Show("El número de Pick 3 debe tener 3 dígitos", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (TxtPick4Number.Text.Length != 4 || !int.TryParse(TxtPick4Number.Text, out _))
        {
            MessageBox.Show("El número de Pick 4 debe tener 4 dígitos", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int? pick3Fireball = null;
        int? pick4Fireball = null;
        try
        {
            // Guardar el registro
            ManualInsertRepository.InsertPair(
                DatePickerDate.SelectedDate.Value,
                drawTime,
                TxtPick3Number.Text,
                pick3Fireball,
                TxtPick4Number.Text,
                pick4Fireball
            );

            // Reiniciar los campos (excepto fecha y sorteo)
            TxtPick3Number.Text = "";
            TxtPick4Number.Text = "";

            // Opcional: Mantener el foco en el primer campo para siguiente entrada
            TxtPick3Number.Focus();

            RecordSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
