// AnalisisSaveDialog.xaml.cs - Actualizado
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using FloridaLotteryApp.Data;

namespace FloridaLotteryApp;

public partial class AnalisisSaveDialog : Window, INotifyPropertyChanged
{
    private const string CREAR_NUEVA_CARPETA = "... crear nueva carpeta";
    
    private List<string> _foldersList = new();
    private Dictionary<string, string> _folderLabels = new();
    private bool _isNewFolderSelected;
    private bool _isLoadingFolders;
    private readonly string _tipoAnalisisFijo; // ✅ Tipo fijo recibido como argumento

    public string? SelectedFolder { get; private set; }
    
    public bool IsNewFolder => _isNewFolderSelected;
    public string NewFolderName => NewFolderTextBox.Text.Trim();
    public string NoteText => string.IsNullOrWhiteSpace(NoteTextBox.Text) 
        ? string.Empty 
        : NoteTextBox.Text.Trim();
    public string TipoAnalisis => _tipoAnalisisFijo; // ✅ Propiedad pública para acceder al tipo

    public bool IsNewFolderSelected
    {
        get => _isNewFolderSelected;
        set
        {
            _isNewFolderSelected = value;
            OnPropertyChanged();
            ValidarGuardado();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // ✅ Constructor recibe el tipo de análisis como parámetro
    public AnalisisSaveDialog(string tipoAnalisis)
    {
        InitializeComponent();
        DataContext = this;
        this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
        this.Top = 0;
        
        _tipoAnalisisFijo = tipoAnalisis;
        
        // ✅ Cargar folders para este tipo específico
        CargarFoldersParaTipo(tipoAnalisis);
        

    }

    private void CargarFoldersParaTipo(string tipoAnalisis)
    {
        try
        {
            var folders = AnalisisGuardadoRepository.GetFoldersByTipoAnalisis(tipoAnalisis);
            CargarFolders(folders);
        }
        catch
        {
            CargarFolders(new List<FolderInfo>());
        }
    }

    public void CargarFolders(List<FolderInfo> foldersInfo)
    {
        _isLoadingFolders = true;
        
        // ✅ Guardar folders y sus labels
        _foldersList = foldersInfo
            .Select(f => f.Folder!)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();
        
        _folderLabels = foldersInfo
            .Where(f => !string.IsNullOrWhiteSpace(f.Folder))
            .ToDictionary(f => f.Folder!, f => f.Label ?? string.Empty);
        
        // Construir la lista para el ComboBox
        var items = new List<string> { CREAR_NUEVA_CARPETA };
        items.AddRange(_foldersList);
        
        string? selectedItem = FolderComboBox.SelectedItem?.ToString();
        
        FolderComboBox.ItemsSource = items;
        
        if (!string.IsNullOrEmpty(selectedItem) && items.Contains(selectedItem))
        {
            FolderComboBox.SelectedItem = selectedItem;
            IsNewFolderSelected = selectedItem == CREAR_NUEVA_CARPETA;
        }
        else
        {
            FolderComboBox.SelectedIndex = 0;
            IsNewFolderSelected = true;
        }
        
        _isLoadingFolders = false;
        ValidarGuardado();
    }

    private void FolderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingFolders) return;
        
        if (FolderComboBox.SelectedItem == null)
        {
            IsNewFolderSelected = false;
            SelectedFolder = null;
            NoteTextBox.Text = string.Empty;
            NoteTextBox.IsEnabled = true;
            return;
        }

        string selected = FolderComboBox.SelectedItem.ToString()!;
        IsNewFolderSelected = selected == CREAR_NUEVA_CARPETA;
        
        if (!IsNewFolderSelected)
        {
            SelectedFolder = selected;
            NewFolderTextBox.Text = string.Empty;
            
            // ✅ Mostrar label de la carpeta seleccionada
            if (_folderLabels.TryGetValue(selected, out string? label))
            {
                NoteTextBox.Text = label ?? string.Empty;
            }
            else
            {
                NoteTextBox.Text = string.Empty;
            }
            
            // ✅ Bloquear el campo para que no se pueda modificar
            NoteTextBox.IsEnabled = false;
        }
        else
        {
            SelectedFolder = null;
            NoteTextBox.Text = string.Empty;
            NoteTextBox.IsEnabled = true;
        }
        
        ValidarGuardado();
    }

    private void NewFolderTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsNewFolderSelected)
        {
            ValidarGuardado();
        }
    }

    private void ValidarGuardado()
    {
        bool isValid = true;
        string errorMessage = string.Empty;

        // ✅ Ya no validamos SelectedTipoAnalisis porque es fijo

        // Validar folder
        if (IsNewFolderSelected)
        {
            if (string.IsNullOrWhiteSpace(NewFolderName))
            {
                isValid = false;
                errorMessage = "Ingrese el nombre de la nueva carpeta.";
            }
            else if (_foldersList.Contains(NewFolderName))
            {
                isValid = false;
                errorMessage = "Ya existe una carpeta con ese nombre.";
            }
        }
        else if (string.IsNullOrWhiteSpace(SelectedFolder))
        {
            isValid = false;
            errorMessage = "Seleccione una carpeta.";
        }

        GuardarButton.IsEnabled = isValid;
        
        if (!isValid && !string.IsNullOrEmpty(errorMessage))
        {
            GuardarButton.ToolTip = errorMessage;
        }
        else
        {
            GuardarButton.ToolTip = null;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!GuardarButton.IsEnabled)
        {
            return;
        }

        if (IsNewFolderSelected)
        {
            SelectedFolder = NewFolderName;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}