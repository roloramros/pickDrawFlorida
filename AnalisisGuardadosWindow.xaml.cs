// AnalisisGuardadosWindow.xaml.cs - Actualizado
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FloridaLotteryApp.Data;

namespace FloridaLotteryApp;

public partial class AnalisisGuardadosWindow : Window
{
    private List<string> _tiposAnalisis = new();
    private List<FolderInfo> _foldersActuales = new();
    private FolderInfo? _selectedFolder;

    public AnalisisGuardadosWindow()
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        LoadTiposAnalisis();
    }

    private void LoadTiposAnalisis()
    {
        try
        {
            _tiposAnalisis = AnalisisGuardadoRepository.GetTiposAnalisisUnicos();
            
            AnalisisComboBox.ItemsSource = _tiposAnalisis;
            
            if (_tiposAnalisis.Any())
            {
                ContadorTextBlock.Text = $"{_tiposAnalisis.Count} tipos de análisis";
                AnalisisComboBox.SelectedIndex = 0;
            }
            else
            {
                ContadorTextBlock.Text = "No hay análisis guardados";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar los tipos de análisis: {ex.Message}", 
                          "Error", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private void AnalisisComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AnalisisComboBox.SelectedItem == null)
        {
            FoldersDataGrid.ItemsSource = null;
            _selectedFolder = null;
            ContadorTextBlock.Text = "Seleccione un tipo de análisis";
            return;
        }

        string tipoSeleccionado = AnalisisComboBox.SelectedItem.ToString()!;
        
        try
        {
            _foldersActuales = AnalisisGuardadoRepository.GetFoldersByTipoAnalisis(tipoSeleccionado);
            
            if (_foldersActuales.Any())
            {
                FoldersDataGrid.ItemsSource = _foldersActuales;
                ContadorTextBlock.Text = $"{tipoSeleccionado} - {_foldersActuales.Count} folders";
                
                // Seleccionar el primer folder por defecto
                if (FoldersDataGrid.Items.Count > 0)
                {
                    FoldersDataGrid.SelectedIndex = 0;
                    _selectedFolder = _foldersActuales[0];
                }
            }
            else
            {
                FoldersDataGrid.ItemsSource = null;
                ContadorTextBlock.Text = $"{tipoSeleccionado} - No hay folders";
                _selectedFolder = null;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar los folders: {ex.Message}", 
                          "Error", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
            FoldersDataGrid.ItemsSource = null;
        }
    }

    private void FoldersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedFolder = FoldersDataGrid.SelectedItem as FolderInfo;
    }

    private void FoldersDataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        DependencyObject? source = e.OriginalSource as DependencyObject;
        while (source != null && source is not DataGridRow)
        {
            source = VisualTreeHelper.GetParent(source);
        }

        if (source is DataGridRow row)
        {
            row.IsSelected = true;
            FoldersDataGrid.SelectedItem = row.Item;
            _selectedFolder = row.Item as FolderInfo;
        }
    }

    private void Abrir_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFolder == null)
        {
            MessageBox.Show("Seleccione un folder para abrir.", 
                          "Abrir análisis", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information);
            return;
        }

        try
        {
            string tipoActual = AnalisisComboBox.SelectedItem?.ToString() ?? string.Empty;

            if (string.Equals(tipoActual, "Analisis 3 Match", StringComparison.OrdinalIgnoreCase))
            {
                var window = new Analisis_3_1_MatchWindow(new ThirdAnalysisCardVM(), Analisis31MatchWindowMode.SavedResults, _selectedFolder.Folder)
                {
                    Owner = Owner ?? Application.Current.MainWindow
                };
                window.Show();
                return;
            }

            MessageBox.Show($"La apertura para '{tipoActual}' aún no está implementada.", 
                          "Abrir análisis", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al abrir: {ex.Message}", 
                          "Error", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private void Borrar_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFolder == null)
        {
            MessageBox.Show("Seleccione un folder para borrar.", 
                          "Borrar análisis", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information);
            return;
        }

        int cantidadRegistros = _selectedFolder.Ids.Count;
        string mensaje = cantidadRegistros > 1
            ? $"El folder '{_selectedFolder.Folder}' contiene {cantidadRegistros} registros.\n\n" +
              $"¿Está seguro que desea borrar TODOS los registros de este folder?\n\n" +
              $"Esta acción no se puede deshacer."
            : $"¿Está seguro que desea borrar el folder '{_selectedFolder.Folder}'?\n\n" +
              $"Esta acción no se puede deshacer.";

        var result = MessageBox.Show(
            mensaje,
            "Confirmar borrado",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            string tipoActual = AnalisisComboBox.SelectedItem.ToString()!;
            
            if (AnalisisGuardadoRepository.DeleteByFolder(tipoActual, _selectedFolder.Folder))
            {
                // Actualizar la lista
                _foldersActuales = AnalisisGuardadoRepository.GetFoldersByTipoAnalisis(tipoActual);
                
                if (_foldersActuales.Any())
                {
                    FoldersDataGrid.ItemsSource = _foldersActuales;
                    ContadorTextBlock.Text = $"{tipoActual} - {_foldersActuales.Count} folders";
                    
                    if (FoldersDataGrid.Items.Count > 0)
                    {
                        FoldersDataGrid.SelectedIndex = 0;
                        _selectedFolder = _foldersActuales[0];
                    }
                    else
                    {
                        _selectedFolder = null;
                    }
                }
                else
                {
                    FoldersDataGrid.ItemsSource = null;
                    ContadorTextBlock.Text = $"{tipoActual} - No hay folders";
                    _selectedFolder = null;
                }
                
                MessageBox.Show($"Folder '{_selectedFolder?.Folder}' borrado correctamente.", 
                              "Borrado exitoso", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No se pudo borrar el folder.", 
                              "Error", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al borrar: {ex.Message}", 
                          "Error", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}


