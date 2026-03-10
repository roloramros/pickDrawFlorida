using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FloridaLotteryApp.Data;
using System.Windows.Threading;

namespace FloridaLotteryApp;

public partial class PlotWindow : Window
{
    // Colecciones para la tabla de patrones
    public ObservableCollection<PatternRow> PatternRows { get; set; } = new();

    // Colecciones para FILA 1 (Tirada seleccionada)
    public List<DigitCell> Row1Pick3 { get; set; } = new();
    public List<DigitCell> Row1Pick4 { get; set; } = new();
    public List<string> Row1Pick3Siguiente { get; set; } = new();
    public List<string> Row1Additional { get; set; } = new();
    public string Row1Date { get; set; } = " ";

    // Colecciones para FILA 2
    public List<DigitCell> Row2Pick3 { get; set; } = new();
    public List<DigitCell> Row2Pick4 { get; set; } = new();
    public List<string> Row2Fireball { get; set; } = new();
    public List<string> Row2Additional { get; set; } = new();
    public string Row2Date { get; set; } = " ";

    // Colecciones para FILA 3
    public List<DigitCell> Row3Pick3 { get; set; } = new();
    public List<DigitCell> Row3Pick4 { get; set; } = new();
    public List<string> Row3Fireball { get; set; } = new();
    public List<string> Row3Additional { get; set; } = new();
    public string Row3Date { get; set; } = " ";

    // Colecciones para FILA 4
    public List<DigitCell> Row4Pick3 { get; set; } = new();
    public List<DigitCell> Row4Pick4 { get; set; } = new();
    public List<string> Row4Fireball { get; set; } = new();
    public List<string> Row4Additional { get; set; } = new();
    public string Row4Date { get; set; } = " ";
    private string _row1Pick3Number = " ";
    private string _row1Pick4Number = " ";
    private string _row2Pick3Number = " ";
    private string _row2Pick4Number = " ";
    private string _row3Pick3Number = " ";
    private string _row3Pick4Number = " ";
    private string _row4Pick3Number = " ";
    private string _row4Pick4Number = " ";
    private readonly string _guideDateText;
    private readonly string _guidePick3;
    private readonly string _guidePick4;
    private bool _hasStartedLoading;
    private bool _hasLoaded = false;
    private bool _isLoading = false;
    private readonly double _originalWindowHeight;
    private readonly double _originalWindowWidth;
    private readonly double _expandedWindowHeight;
    private CancellationTokenSource? _cancellationTokenSource;
    private static readonly Brush[] RepeatPalette =
    {
        (Brush)new BrushConverter().ConvertFromString("#0000FF")!,
        (Brush)new BrushConverter().ConvertFromString("#006400")!,
        (Brush)new BrushConverter().ConvertFromString("#dc143c")!,
        (Brush)new BrushConverter().ConvertFromString("#daa520")!,
        (Brush)new BrushConverter().ConvertFromString("#9400d3")!,
        (Brush)new BrushConverter().ConvertFromString("#20b2aa")!
    };
    

    public PlotWindow(string dateText, string drawIcon, string pick3, string pick4, string pick3Siguiente)
    {
        InitializeComponent();
        _originalWindowHeight = Height;
        _originalWindowWidth = Width;
        _expandedWindowHeight = Height + 20;
        _guideDateText = dateText ?? " ";
        _guidePick3 = pick3 ?? " ";
        _guidePick4 = pick4 ?? " ";

        // Configurar DataContext
        DataContext = this;

        // ==========================================
        // FILA 1: Datos de la tirada seleccionada
        // ==========================================
        _row1Pick3Number = pick3 ?? " ";
        _row1Pick4Number = pick4 ?? " ";

        // Procesar Pick3 Siguiente con manejo de cadenas vacías o sin dígitos suficientes
        // Obtiene solo los dígitos de la cadena y los convierte a strings inmediatamente
        var pick3SiguienteDigits = pick3Siguiente.Where(char.IsDigit).Select(c => c.ToString()).ToList();
        // Rellena con espacios hasta tener 3 elementos o crea una lista de 3 espacios si no hay dígitos
        Row1Pick3Siguiente = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            if (i < pick3SiguienteDigits.Count)
            {
                Row1Pick3Siguiente.Add(pick3SiguienteDigits[i]);
            }
            else
            {
                Row1Pick3Siguiente.Add(" "); // Agrega un espacio en blanco
            }
        }

        Row1Additional = BuildCodificacionDigits(pick3, pick4);
        Row1Date = dateText; // Asignar la fecha

        // ==========================================
        // FILA 2,  3, 4: Datos de ejemplo (luego los llenas con tu lógica)
        // ==========================================
        Row2Pick3 = new List<DigitCell>();
        Row2Pick4 = new List<DigitCell>();
        Row2Fireball = new List<string>();
        Row2Additional = new List<string>();
        Row2Date = " ";

        Row3Pick3 = new List<DigitCell>();
        Row3Pick4 = new List<DigitCell>();
        Row3Fireball = new List<string>();
        Row3Additional = new List<string>();
        Row3Date = " ";

        Row4Pick3 = new List<DigitCell>();
        Row4Pick4 = new List<DigitCell>();
        Row4Fireball = new List<string>();
        Row4Additional = new List<string>();
        Row4Date = " ";

        // Asignar ItemsSource a todos los ItemsControls
        // FILA 1
        UpdateAllPickDigitCells();
        Row1_Pick3SiguienteDigits.ItemsSource = Row1Pick3Siguiente; // <-- Ahora siempre tiene 3 elementos
        Row1_AdditionalDigits.ItemsSource = Row1Additional;
        Row1_DrawIcon.Text = drawIcon;

        // FILA 2
        Row2_FireballDigits.ItemsSource = Row2Fireball;
        Row2_AdditionalDigits.ItemsSource = Row2Additional;
        Row2_DateText.Text = Row2Date;

        // FILA 3
        Row3_FireballDigits.ItemsSource = Row3Fireball;
        Row3_AdditionalDigits.ItemsSource = Row3Additional;
        Row3_DateText.Text = Row3Date;

        // FILA 4
        Row4_FireballDigits.ItemsSource = Row4Fireball;
        Row4_AdditionalDigits.ItemsSource = Row4Additional;
        Row4_DateText.Text = Row4Date;

        PatternsTable.ItemsSource = PatternRows;
        UpdateResultsCounter();
        
        // Cambiamos el estado de carga inicial
        SetLoadingState(true, "Preparando an�lisis...", 0, 1, false);
        
        // Botón de cancelar
        CancelButton.Visibility = Visibility.Collapsed;
        CancelButton.Click += (s, e) => CancelLoading();
        
        Loaded += PlotWindow_Loaded;
        // También suscribirse al evento de cambio de selección para redibujar
        PatternsTable.SelectionChanged += (s, e) => QueueDrawConnectingLines();
    }

    private void CancelLoading()
    {
        _cancellationTokenSource?.Cancel();
        CancelButton.IsEnabled = false;
        SetLoadingState(true, "Cancelando analisis...", 0, Math.Max(1, (int)Math.Ceiling(AnalysisProgressBar.Maximum)), false);
    }

    
    private async void PlotWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_hasStartedLoading)
        {
            return;
        }

        _hasStartedLoading = true;
        
        // Limpiar la tabla antes de comenzar
        PatternRows.Clear();
        
        // Iniciar la carga en tiempo real
        await LoadPatternRowsRealtimeAsync();
        
        // Dibujar líneas después de cargar los datos
        Dispatcher.BeginInvoke(new Action(() =>
        {
            Dispatcher.BeginInvoke(new Action(DrawConnectingLines), DispatcherPriority.Render);
        }), DispatcherPriority.Loaded);
    }

    // Método para encolar el dibujo de líneas
    private void QueueDrawConnectingLines()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            Dispatcher.BeginInvoke(new Action(DrawConnectingLines), DispatcherPriority.Render);
        }), DispatcherPriority.Loaded);
    }

    // Método principal para dibujar las líneas
    private void DrawConnectingLines()
    {
        try
        {
            var canvas = ConnectionCanvas;
            if (canvas == null) return;

            // Limpiar canvas
            canvas.Children.Clear();

            // Obtener los ItemsControl de los dígitos
            var row1NextPick3 = FindVisualChild<ItemsControl>(this, "Row1_Pick3SiguienteDigits");
            var row2Fireball = FindVisualChild<ItemsControl>(this, "Row2_FireballDigits");
            var row3Fireball = FindVisualChild<ItemsControl>(this, "Row3_FireballDigits");
            var row4Fireball = FindVisualChild<ItemsControl>(this, "Row4_FireballDigits");

            if (row1NextPick3 == null || row2Fireball == null || 
                row3Fireball == null || row4Fireball == null)
            {
                Console.WriteLine("No se encontraron todos los ItemsControl necesarios");
                return;
            }

            // Conectar Fila 1 → Fila 2 (Pick3 Siguiente)
            ConnectPick3Digits(row1NextPick3, row2Fireball, canvas, Brushes.Black);

            // Conectar Fila 3 → Fila 4 (Fireball)
            ConnectPick3Digits(row3Fireball, row4Fireball, canvas, Brushes.Black);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error dibujando líneas: {ex.Message}");
        }
    }


    // Adaptación de ConnectPick3Digits de ThirdAnalysisOption1
    private void ConnectPick3Digits(ItemsControl topItemsControl, ItemsControl bottomItemsControl, Canvas canvas, Brush lineColor)
    {
        if (topItemsControl == null || bottomItemsControl == null || canvas == null)
            return;

        try
        {
            // Obtener los contenedores de los dígitos
            var topDigits = GetDigitContainers(topItemsControl);
            var bottomDigits = GetDigitContainers(bottomItemsControl);

            if (topDigits.Count == 0 || bottomDigits.Count == 0)
            {
                return;
            }

            // Conectar dígitos que coincidan
            for (int i = 0; i < topDigits.Count; i++)
            {
                var topDigit = topDigits[i];
                var topText = GetDigitText(topDigit);
                
                if (string.IsNullOrWhiteSpace(topText)) continue;

                for (int j = 0; j < bottomDigits.Count; j++)
                {
                    var bottomDigit = bottomDigits[j];
                    var bottomText = GetDigitText(bottomDigit);
                    
                    if (string.IsNullOrWhiteSpace(bottomText)) continue;

                    if (topText == bottomText)
                    {
                        // Dibujar línea conectando los dígitos
                        DrawConnectingLine(canvas, topDigit, bottomDigit, lineColor);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error conectando dígitos: {ex.Message}");
        }
    }

    // Método para obtener contenedores de dígitos (adaptado de ThirdAnalysisOption1)
    private List<Border> GetDigitContainers(ItemsControl itemsControl)
    {
        var containers = new List<Border>();
        
        try
        {
            if (itemsControl == null) return containers;

            // 1. Intentar usar ItemContainerGenerator
            if (itemsControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                for (int i = 0; i < itemsControl.Items.Count; i++)
                {
                    var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                    if (container != null)
                    {
                        var border = FindVisualChild<Border>(container);
                        if (border != null)
                        {
                            containers.Add(border);
                        }
                    }
                }
            }

            // 2. Si no encontró contenedores, buscar en el árbol visual
            if (containers.Count == 0)
            {
                var contentPresenters = FindVisualChildren<ContentPresenter>(itemsControl).ToList();
                foreach (var cp in contentPresenters)
                {
                    var border = FindVisualChild<Border>(cp);
                    if (border != null)
                    {
                        containers.Add(border);
                    }
                }
            }

            // 3. Si aún no encontró, buscar Borders directamente
            if (containers.Count == 0)
            {
                var allBorders = FindVisualChildren<Border>(itemsControl).ToList();
                foreach (var border in allBorders)
                {
                    var textBlock = FindVisualChild<TextBlock>(border);
                    if (textBlock != null)
                    {
                        containers.Add(border);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetDigitContainers: {ex.Message}");
        }
        
        return containers;
    }

    // Método para obtener el texto de un dígito
    private string GetDigitText(Border digitBorder)
    {
        try
        {
            var textBlock = FindVisualChild<TextBlock>(digitBorder);
            return textBlock?.Text?.Trim() ?? "";
        }
        catch
        {
            return "";
        }
    }

    // Método para dibujar una línea (adaptado de ThirdAnalysisOption1)
    private void DrawConnectingLine(Canvas canvas, Border element1, Border element2, Brush lineColor)
    {
        try
        {
            // Verificar que los elementos tengan dimensiones válidas
            if (element1.ActualWidth == 0 || element1.ActualHeight == 0 ||
                element2.ActualWidth == 0 || element2.ActualHeight == 0)
                return;

            // Calcular centros absolutos
            var center1 = element1.TranslatePoint(new Point(element1.ActualWidth / 2, element1.ActualHeight / 2), canvas);
            var center2 = element2.TranslatePoint(new Point(element2.ActualWidth / 2, element2.ActualHeight / 2), canvas);
            
            // Calcular vector dirección
            double dx = center2.X - center1.X;
            double dy = center2.Y - center1.Y;
            
            // Calcular distancia entre centros
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            // Si están en la misma posición, no dibujar línea
            if (distance == 0) return;
            
            // Calcular radio de cada círculo
            double radius1 = element1.ActualWidth / 2;
            double radius2 = element2.ActualWidth / 2;
            
            // Normalizar vector dirección
            double unitX = dx / distance;
            double unitY = dy / distance;
            
            // Calcular puntos en los bordes
            Point startPoint = new Point(
                center1.X + (unitX * radius1),
                center1.Y + (unitY * radius1)
            );
            
            Point endPoint = new Point(
                center2.X - (unitX * radius2),
                center2.Y - (unitY * radius2)
            );
            
            // Crear línea
            var line = new System.Windows.Shapes.Line
            {
                X1 = startPoint.X,
                Y1 = startPoint.Y,
                X2 = endPoint.X,
                Y2 = endPoint.Y,
                Stroke = lineColor,
                StrokeThickness = 2
            };
            
            canvas.Children.Add(line);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error dibujando línea: {ex.Message}");
        }
    }

    // Métodos auxiliares para buscar en el árbol visual (copiados de ThirdAnalysisOption1)
    private T FindVisualChild<T>(DependencyObject parent, string childName = null) where T : DependencyObject
    {
        if (parent == null) return null;

        try
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is FrameworkElement frameworkElement && 
                    !string.IsNullOrEmpty(childName) && 
                    frameworkElement.Name == childName)
                {
                    return child as T;
                }
                
                if (child is T result && (string.IsNullOrEmpty(childName) || childName == result.GetValue(FrameworkElement.NameProperty) as string))
                {
                    return result;
                }
                
                var foundChild = FindVisualChild<T>(child, childName);
                if (foundChild != null) return foundChild;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en FindVisualChild: {ex.Message}");
        }
        
        return null;
    }

    private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        var results = new List<T>();
        
        if (parent == null) return results;

        try
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T childOfType)
                {
                    results.Add(childOfType);
                }

                foreach (var descendant in FindVisualChildren<T>(child))
                {
                    results.Add(descendant);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en FindVisualChildren: {ex.Message}");
        }
        
        return results;
    }

    // Sobrescribir OnRender si es necesario
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        QueueDrawConnectingLines();
    }

    private void PatternsTable_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        DependencyObject? source = e.OriginalSource as DependencyObject;
        while (source != null && source is not DataGridRow)
        {
            source = VisualTreeHelper.GetParent(source);
        }

        if (source is DataGridRow row)
        {
            row.IsSelected = true;
            row.Focus();
        }
    }
    private void EliminarDobles_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Verificar si hay una fila seleccionada
            if (PatternsTable.SelectedItem is not PatternRow selectedRow)
            {
                MessageBox.Show("Por favor, selecciona una fila en la tabla primero.", 
                    "Eliminar Dobles", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Verificar si la FILA 1 (fila de referencia/guía) tiene dígitos repetidos
            bool fila1TieneRepetidos = HasRepeatedDigitsInFirstRow();
            
            if (fila1TieneRepetidos)
            {
                MessageBox.Show(
                    "La fila de referencia (guía) tiene dígitos repetidos.\n" +
                    "No se puede aplicar el filtro 'Eliminar Dobles' porque vaciaría toda la tabla.\n\n" +
                    "Se mantendrá la tabla sin cambios.",
                    "No se puede filtrar", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                return; // Salir sin hacer cambios
            }

            // Confirmar con el usuario
            var result = MessageBox.Show(
                "¿Estás seguro de que quieres eliminar los patrones que tienen dígitos repetidos?\n\n" +
                "Se eliminarán los resultados que tengan:\n" +
                "• Dígitos repetidos dentro del Pick3\n" +
                "• Dígitos repetidos dentro del Pick4\n" +
                "• Dígitos repetidos dentro del Pick3 Siguiente", 
                "Confirmar Eliminación", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // Mostrar progreso
            int totalRows = Math.Max(1, PatternRows.Count);
            SetLoadingState(true, $"Filtrando patrones... 0 de {totalRows}", 0, totalRows, false);

            // Crear una nueva colección con los patrones que NO tienen dígitos repetidos
            var filteredRows = new ObservableCollection<PatternRow>();
            int processed = 0;
            int eliminatedCount = 0;

            foreach (var row in PatternRows)
            {
                processed++;
                
                // Verificar si el patrón tiene dígitos repetidos internamente
                bool hasRepeatedDigits = HasRepeatedDigits(row);
                
                // Si NO tiene dígitos repetidos, lo mantenemos
                if (!hasRepeatedDigits)
                {
                    filteredRows.Add(row);
                }
                else
                {
                    eliminatedCount++;
                }

                // Actualizar progreso (opcional)
                if (processed % 1 == 0)
                {
                    SetLoadingState(true, $"Filtrando patrones... {processed} de {totalRows}", processed, totalRows, false);
                }
            }

            // Actualizar la colección
            PatternRows.Clear();
            foreach (var row in filteredRows)
            {
                PatternRows.Add(row);
            }

            // Si había una fila seleccionada, intentar mantener la selección
            if (filteredRows.Count > 0)
            {
                // Buscar si la fila seleccionada original aún existe
                var matchingRow = filteredRows.FirstOrDefault(r => 
                    r.MatchNumber == selectedRow.MatchNumber && 
                    r.SimilarNumber == selectedRow.SimilarNumber &&
                    r.SimilarMatchNumber == selectedRow.SimilarMatchNumber);

                if (matchingRow != null)
                {
                    PatternsTable.SelectedItem = matchingRow;
                }
                else
                {
                    PatternsTable.SelectedIndex = 0;
                }
            }

            // Ocultar progreso
            SetLoadingState(false, "", 0, false);

            // Mostrar mensaje con resultados
            if (eliminatedCount > 0)
            {
                MessageBox.Show(
                    $"Se eliminaron {eliminatedCount} patrones con dígitos repetidos.\n" +
                    $"Patrones restantes: {filteredRows.Count}", 
                    "Eliminar Dobles", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    "No se encontraron patrones con dígitos repetidos para eliminar.", 
                    "Eliminar Dobles", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }

            UpdateResultsCounter();
        }
        catch (Exception ex)
        {
            SetLoadingState(false, "", 0, false);
            MessageBox.Show($"Error al eliminar dobles: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool HasRepeatedDigitsInFirstRow()
    {
        // Función auxiliar para verificar si un string TIENE DÍGITOS REPETIDOS DENTRO DE SÍ MISMO
        bool StringHasInternalRepeats(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var digits = input.Where(char.IsDigit).ToList();
            if (digits.Count < 2)
                return false;
            
            return digits.Count != digits.Distinct().Count();
        }

        // Verificar los campos de la primera fila (fila de referencia)
        // Usamos los valores guardados en las variables privadas
        if (StringHasInternalRepeats(_row1Pick3Number) ||
            StringHasInternalRepeats(_row1Pick4Number))
        {
            return true;
        }
        return false;
    }


    private bool HasRepeatedDigits(PatternRow row)
    {
        // Función auxiliar para verificar si un string TIENE DÍGITOS REPETIDOS DENTRO DE SÍ MISMO
        bool StringHasInternalRepeats(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var digits = input.Where(char.IsDigit).ToList();
            if (digits.Count < 2)
                return false;
            
            return digits.Count != digits.Distinct().Count();
        }

        // Verificar PICK3 (debe tener 3 dígitos diferentes)
        if (StringHasInternalRepeats(row.MatchPick3) ||
            StringHasInternalRepeats(row.SimilarPick3) ||
            StringHasInternalRepeats(row.SimilarMatchPick3))
        {
            return true;
        }

        // Verificar PICK4 (debe tener 4 dígitos diferentes)
        if (StringHasInternalRepeats(row.MatchPick4) ||
            StringHasInternalRepeats(row.SimilarPick4) ||
            StringHasInternalRepeats(row.SimilarMatchPick4))
        {
            return true;
        }

        // Verificar PICK3 SIGUIENTE (debe tener 3 dígitos diferentes)
        if (StringHasInternalRepeats(row.MatchNextPick3) ||
            StringHasInternalRepeats(row.SimilarNextPick3) ||
            StringHasInternalRepeats(row.SimilarMatchNextPick3))
        {
            return true;
        }

        return false;
    }


    private async Task LoadPatternRowsRealtimeAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        _isLoading = true;
        UpdateResultsCounterWithHighlight();
        SetLoadingState(true, "Analizando combinaciones...", 0, 1, false);
        CancelButton.IsEnabled = true; // Actualizar contador con fondo amarillo

        var progress = new Progress<PatternRow>(patternRow =>
        {
            PatternRows.Add(patternRow);
            UpdateResultsCounter();
        });

        try
        {
            await Task.Run(() => LoadPatternRowsRealtime(_guidePick3, _guidePick4, _guideDateText, progress, token), token);
        }
        catch (OperationCanceledException)
        {
            // El usuario canceló la operación
            MessageBox.Show("Análisis cancelado por el usuario.", "Cancelado", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error durante el análisis: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isLoading = false;
            UpdateResultsCounterWithHighlight(); // Quitar fondo amarillo
            
            SetLoadingState(false, "", 0, false);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void UpdateResultsCounterWithHighlight()
    {
        int total = PatternRows.Count;
        int selected = 0;

        if (total > 0 && PatternsTable.SelectedIndex >= 0)
        {
            selected = PatternsTable.SelectedIndex + 1;
        }

        // Actualizar el texto
        ResultsCounterText.Text = $"{selected} de {total}";
        
    }


    private void LoadPatternRowsRealtime(
        string guidePick3,
        string guidePick4,
        string guideDateText,
        IProgress<PatternRow> progress,
        CancellationToken cancellationToken)
    {
        // Convertir la fecha guía de texto a DateTime para comparaciones
        if (!DateTime.TryParseExact(guideDateText, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime guideDateTime))
        {
            // Manejar error si la fecha no es válida
            Console.WriteLine($"Error: Fecha guía '{guideDateText}' no es válida.");
            return;
        }

        // Construir número guía de 7 dígitos
        var guideNumber = new string(
            (guidePick3 ?? " ")
                .Where(char.IsDigit)
                .Concat((guidePick4 ?? " ").Where(char.IsDigit))
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
                    Pos23 = GetPos23Key(combined) ?? " ",
                    Pattern = BuildRepetitionPattern(combined),
                    Date = hit.Date
                };
            })
            .Where(x => x != null)
            .Cast<CandidateRow>()
            .ToList();

        // 1. Candidatos para Columna 2
        var col2Candidates = allHits
            .Where(x => x.Pos23 == referencePos23 && x.Number7 != guideNumber)
            .ToList();

        // 2. Candidatos para Columna 3
        var col3Candidates = allHits
            .Where(x => x.Pattern == referencePattern && x.Number7 != guideNumber)
            .ToList();

        // Extraer el primer dígito de la referencia
        char? refFirstDigit = null;
        if (!string.IsNullOrEmpty(guidePick3) && char.IsDigit(guidePick3[0]))
        {
             refFirstDigit = guidePick3[0];
        }

        int patternsFound = 0;
        int totalIterations = Math.Max(1, col2Candidates.Count);
        int currentIteration = 0;
        int updateFrequency = Math.Max(1, totalIterations / 200);

        Dispatcher.BeginInvoke(() =>
        {
            if (!_isLoading) return;
            SetLoadingState(true, "Analizando combinaciones... 0 de " + totalIterations, 0, totalIterations, false);
        });

        // 3. Generar combinaciones
        for (int col2Index = 0; col2Index < col2Candidates.Count; col2Index++)
        {
            // Verificar cancelaci�n
            cancellationToken.ThrowIfCancellationRequested();

            var col2 = col2Candidates[col2Index];
            currentIteration++;

            // Actualizar progreso visible sin saturar el hilo UI
            if (currentIteration % updateFrequency == 0 || currentIteration == totalIterations)
            {
                int progressCompleted = currentIteration;
                int progressPatterns = patternsFound;
                Dispatcher.BeginInvoke(() =>
                {
                    if (!_isLoading) return;
                    SetLoadingState(true,
                        $"Analizando combinaciones... {progressCompleted} de {totalIterations} | Patrones: {progressPatterns}",
                        progressCompleted,
                        totalIterations,
                        false);
                });
            }

            // Filtrar: Fecha Col2 < Fecha Referencia
            if (col2.Date >= guideDateTime) continue;

            // Extraer el primer d�gito de Col2
            char? col2FirstDigit = null;
            if (!string.IsNullOrEmpty(col2.Hit.Pick3) && char.IsDigit(col2.Hit.Pick3[0]))
            {
                 col2FirstDigit = col2.Hit.Pick3[0];
            }

            bool refAndCol2AreEqual = refFirstDigit.HasValue && col2FirstDigit.HasValue && refFirstDigit.Value == col2FirstDigit.Value;
            bool refAndCol2AreDifferent = refFirstDigit.HasValue && col2FirstDigit.HasValue && refFirstDigit.Value != col2FirstDigit.Value;

            foreach (var col3 in col3Candidates)
            {
                // Verificar cancelaci�n
                cancellationToken.ThrowIfCancellationRequested();

                // Extraer el primer d�gito de Col3
                char? col3FirstDigit = null;
                if (!string.IsNullOrEmpty(col3.Hit.Pick3) && char.IsDigit(col3.Hit.Pick3[0]))
                {
                     col3FirstDigit = col3.Hit.Pick3[0];
                }

                // Buscar candidatos para Columna 4
                var col4Candidates = allHits
                    .Where(x =>
                    {
                        bool condition1 = x.Pos23 == col3.Pos23;
                        bool condition2 = x.Pattern == col2.Pattern;
                        bool condition3 = x.Number7 != col2.Number7;
                        bool condition4 = x.Number7 != col3.Number7;
                        bool condition5 = x.Date < col3.Date;
                        bool condition6 = HasSameCrossPositionEqualityPattern(guideNumber, col2.Number7, col3.Number7, x.Number7);

                        if (!(condition1 && condition2 && condition3 && condition4 && condition5 && condition6))
                        {
                            return false;
                        }

                        char? col4FirstDigit = null;
                        if (!string.IsNullOrEmpty(x.Hit.Pick3) && char.IsDigit(x.Hit.Pick3[0]))
                        {
                             col4FirstDigit = x.Hit.Pick3[0];
                        }

                        if (col3FirstDigit.HasValue && col4FirstDigit.HasValue)
                        {
                            bool col3AndCol4AreEqual = col3FirstDigit.Value == col4FirstDigit.Value;
                            bool col3AndCol4AreDifferent = col3FirstDigit.Value != col4FirstDigit.Value;

                            if (refAndCol2AreDifferent && col3AndCol4AreDifferent)
                            {
                                return true;
                            }
                            else if (refAndCol2AreEqual && col3AndCol4AreEqual)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    })
                    .ToList();

                if (col4Candidates.Count > 0)
                {
                    foreach (var col4 in col4Candidates)
                    {
                        // Verificar cancelaci�n
                        cancellationToken.ThrowIfCancellationRequested();

                        // Crear el patr�n y reportarlo inmediatamente
                        var patternRow = CreatePatternRow(col2, col3, col4, guideNumber, guideDateTime);
                        if (patternRow != null)
                        {
                            patternsFound++;
                            progress.Report(patternRow);
                        }
                    }
                }
            }
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (!_isLoading) return;
            SetLoadingState(true,
                $"An�lisis completado. Se encontraron {patternsFound} patrones.",
                totalIterations,
                totalIterations,
                false);
        });
    }


    private PatternRow? CreatePatternRow(
        CandidateRow col2,
        CandidateRow col3,
        CandidateRow col4,
        string guideNumber,
        DateTime guideDate)
    {
        try
        {
            var nextPick3 = DrawRepository.GetNextPick3Number(col2.Hit.Date, col2.Hit.DrawTime) ?? " ";
            var col3NextPick3 = col3 == null ? " " : DrawRepository.GetNextPick3Number(col3.Hit.Date, col3.Hit.DrawTime) ?? " ";
            var col4NextPick3 = col4 == null ? " " : DrawRepository.GetNextPick3Number(col4.Hit.Date, col4.Hit.DrawTime) ?? " ";

            return new PatternRow
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

                // Columna 4
                SimilarMatchNumber = col4?.Number7 ?? " ",
                SimilarMatchPick3 = col4?.Hit.Pick3 ?? " ",
                SimilarMatchPick4 = col4?.Hit.Pick4 ?? " ",
                SimilarMatchNextPick3 = col4NextPick3,
                SimilarMatchDrawTime = col4?.Hit.DrawTime ?? " ",
                SimilarMatchDate = col4?.Hit.Date.ToString("yyyy-MM-dd") ?? " ",
                SimilarMatchCodificacion = col4 == null ? " " : string.Concat(BuildCodificacionDigits(col4.Hit.Pick3, col4.Hit.Pick4)),

                ReferenceDate = guideDate.ToString("yyyy-MM-dd")
            };
        }
        catch
        {
            return null;
        }
    }


    private void Analisis_Opcion1_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Verificar si hay una fila seleccionada
            if (PatternsTable.SelectedItem is not PatternRow selectedRow)
            {
                MessageBox.Show("Por favor, selecciona una fila en la tabla primero.", 
                    "Análisis Opción 1", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool filaSeleccionadaTieneRepetidos = HasRepeatedDigits(selectedRow);
            if (filaSeleccionadaTieneRepetidos)
            {
                MessageBox.Show(
                    "La fila seleccionada tiene dígitos repetidos en Pick3, Pick4 o Pick3 Siguiente.\n" +
                    "No se puede realizar el análisis.",
                    "Patrón no válido", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                return;
            }

            // Obtener los datos de la FILA 1 actual
            string dateText = Row1Date;
            string drawIcon = Row1_DrawIcon.Text;
            string pick3 = _row1Pick3Number;
            string pick4 = _row1Pick4Number;
            string pick3Siguiente = string.Concat(Row1Pick3Siguiente);

            // Crear y mostrar la nueva ventana
            Plot_Opcion1 nuevaVentana = new Plot_Opcion1(
                dateText,
                drawIcon,
                pick3,
                pick4,
                pick3Siguiente,
                selectedRow
            );

            nuevaVentana.Owner = this;
            nuevaVentana.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al abrir nueva ventana: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
   




    private class PatronRepeticion
    {
        // Para FILA 2 (Match)
        public bool[] Fila2_CoincidenciasPick3SigConPick3 { get; set; } = new bool[3]; // Pos1,2,3
        public bool[] Fila2_CoincidenciasPick3SigConPick4 { get; set; } = new bool[3]; // Pos1,2,3
        public bool[] Fila2_RepeticionesInternasPick3Sig { get; set; } = new bool[3]; // Si el dígito se repite dentro del mismo Pick3Sig
        
        // Para FILA 4 (SimilarMatch)
        public bool[] Fila4_CoincidenciasPick3SigConPick3 { get; set; } = new bool[3];
        public bool[] Fila4_CoincidenciasPick3SigConPick4 { get; set; } = new bool[3];
        public bool[] Fila4_RepeticionesInternasPick3Sig { get; set; } = new bool[3];
    }

    private PatronRepeticion ExtraerPatronRepeticion(PatternRow row)
    {
        try
        {
            var patron = new PatronRepeticion();

            // ===== PROCESAR FILA 2 (Match) =====
            string pick3SigFila2 = row.MatchNextPick3 ?? "";
            string pick3Fila2 = row.MatchPick3 ?? "";
            string pick4Fila2 = row.MatchPick4 ?? "";

            // Analizar cada posición del Pick3 Siguiente en FILA 2
            for (int i = 0; i < 3 && i < pick3SigFila2.Length; i++)
            {
                char digito = pick3SigFila2[i];
                
                // Coincidencia con Pick3 de la misma fila
                patron.Fila2_CoincidenciasPick3SigConPick3[i] = pick3Fila2.Contains(digito);
                
                // Coincidencia con Pick4 de la misma fila
                patron.Fila2_CoincidenciasPick3SigConPick4[i] = pick4Fila2.Contains(digito);
                
                // Repetición interna en Pick3 Siguiente (el mismo dígito aparece en otra posición)
                patron.Fila2_RepeticionesInternasPick3Sig[i] = 
                    pick3SigFila2.Count(c => c == digito) > 1;
            }

            // ===== PROCESAR FILA 4 (SimilarMatch) =====
            string pick3SigFila4 = row.SimilarMatchNextPick3 ?? "";
            string pick3Fila4 = row.SimilarMatchPick3 ?? "";
            string pick4Fila4 = row.SimilarMatchPick4 ?? "";

            // Analizar cada posición del Pick3 Siguiente en FILA 4
            for (int i = 0; i < 3 && i < pick3SigFila4.Length; i++)
            {
                char digito = pick3SigFila4[i];
                
                patron.Fila4_CoincidenciasPick3SigConPick3[i] = pick3Fila4.Contains(digito);
                patron.Fila4_CoincidenciasPick3SigConPick4[i] = pick4Fila4.Contains(digito);
                patron.Fila4_RepeticionesInternasPick3Sig[i] = 
                    pick3SigFila4.Count(c => c == digito) > 1;
            }

            return patron;
        }
        catch
        {
            return null;
        }
    }

    private bool SonPatronesIguales(PatronRepeticion p1, PatronRepeticion p2)
    {
        // Comparar FILA 2
        for (int i = 0; i < 3; i++)
        {
            if (p1.Fila2_CoincidenciasPick3SigConPick3[i] != p2.Fila2_CoincidenciasPick3SigConPick3[i])
                return false;
            if (p1.Fila2_CoincidenciasPick3SigConPick4[i] != p2.Fila2_CoincidenciasPick3SigConPick4[i])
                return false;
            if (p1.Fila2_RepeticionesInternasPick3Sig[i] != p2.Fila2_RepeticionesInternasPick3Sig[i])
                return false;
        }

        // Comparar FILA 4
        for (int i = 0; i < 3; i++)
        {
            if (p1.Fila4_CoincidenciasPick3SigConPick3[i] != p2.Fila4_CoincidenciasPick3SigConPick4[i])
                return false;
            if (p1.Fila4_CoincidenciasPick3SigConPick4[i] != p2.Fila4_CoincidenciasPick3SigConPick4[i])
                return false;
            if (p1.Fila4_RepeticionesInternasPick3Sig[i] != p2.Fila4_RepeticionesInternasPick3Sig[i])
                return false;
        }

        return true;
    }

    // --- Modificar CandidateRow para incluir la fecha ---
    internal class CandidateRow
    {
        public required ComboHit Hit { get; set; }
        public string Number7 { get; set; } = "";
        public string Pos23 { get; set; } = "";
        public string Pattern { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.MinValue;
    }

    private sealed class PatternLoadProgress
    {
        public PatternLoadProgress(int completed, int total, string status)
        {
            Completed = completed;
            Total = total;
            Status = status;
        }

        public int Completed { get; }
        public int Total { get; }
        public string Status { get; }
    }

    private static string BuildSevenDigitNumber(string pick3, string pick4)
    {
        return new string(
            (pick3 ?? " ")
                .Where(char.IsDigit)
                .Concat((pick4 ?? " ").Where(char.IsDigit))
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

    private static bool HasSameCrossPositionEqualityPattern(string referenceTop,string referenceBottom,string candidateTop,string candidateBottom)
    {
        if (string.IsNullOrWhiteSpace(referenceTop) ||
            string.IsNullOrWhiteSpace(referenceBottom) ||
            string.IsNullOrWhiteSpace(candidateTop) ||
            string.IsNullOrWhiteSpace(candidateBottom))
        {
            return false;
        }

        if (referenceTop.Length != referenceBottom.Length ||
            candidateTop.Length != candidateBottom.Length ||
            referenceTop.Length != candidateTop.Length)
        {
            return false;
        }

        int len = referenceTop.Length;
        for (int i = 0; i < len; i++)
        {
            for (int j = 0; j < len; j++)
            {
                bool referenceEqual = referenceTop[i] == referenceBottom[j];
                bool candidateEqual = candidateTop[i] == candidateBottom[j];
                if (referenceEqual != candidateEqual)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static string DrawIconFromTime(string drawTime)
    {
        return drawTime == "M" ? "\u2600\uFE0F" : drawTime == "E" ? "\U0001F319" : " ";
    }

    private void PatternsTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PatternsTable.SelectedItem is not PatternRow selected)
        {
            UpdateResultsCounter();
            return;
        }

        ApplySelectionToRows(selected);
        UpdateResultsCounter();
    }

    private void ApplySelectionToRows(PatternRow selected)
    {
        if (selected == null)
        {
            return;
        }

        _row2Pick3Number = selected.MatchPick3 ?? " ";
        _row2Pick4Number = selected.MatchPick4 ?? " ";
        Row2Fireball = (selected.MatchNextPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row2Additional = (selected.MatchCodificacion ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row2Date = selected.MatchDate ?? " ";

        Row2_FireballDigits.ItemsSource = Row2Fireball;
        Row2_AdditionalDigits.ItemsSource = Row2Additional;
        Row2_DrawIcon.Text = DrawIconFromTime(selected.MatchDrawTime);
        Row2_DateText.Text = Row2Date;

        _row3Pick3Number = selected.SimilarPick3 ?? " ";
        _row3Pick4Number = selected.SimilarPick4 ?? " ";
        Row3Fireball = (selected.SimilarNextPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row3Additional = (selected.SimilarCodificacion ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row3Date = selected.SimilarDate ?? " ";

        Row3_FireballDigits.ItemsSource = Row3Fireball;
        Row3_AdditionalDigits.ItemsSource = Row3Additional;
        Row3_DrawIcon.Text = DrawIconFromTime(selected.SimilarDrawTime);
        Row3_DateText.Text = Row3Date;

        _row4Pick3Number = selected.SimilarMatchPick3 ?? " ";
        _row4Pick4Number = selected.SimilarMatchPick4 ?? " ";
        Row4Fireball = (selected.SimilarMatchNextPick3 ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row4Additional = (selected.SimilarMatchCodificacion ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
        Row4Date = selected.SimilarMatchDate ?? " ";

        Row4_FireballDigits.ItemsSource = Row4Fireball;
        Row4_AdditionalDigits.ItemsSource = Row4Additional;
        Row4_DrawIcon.Text = DrawIconFromTime(selected.SimilarMatchDrawTime);
        Row4_DateText.Text = Row4Date;

        UpdateAllPickDigitCells();
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

    private void UpdateAllPickDigitCells()
    {
        var topA = ExtractDigits(_row1Pick3Number).Concat(ExtractDigits(_row1Pick4Number)).ToList();
        var topB = ExtractDigits(_row2Pick3Number).Concat(ExtractDigits(_row2Pick4Number)).ToList();
        var bottomA = ExtractDigits(_row3Pick3Number).Concat(ExtractDigits(_row3Pick4Number)).ToList();
        var bottomB = ExtractDigits(_row4Pick3Number).Concat(ExtractDigits(_row4Pick4Number)).ToList();

        var topCounts = topA.Concat(topB).GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
        var repeatedTopDigits = topA.Concat(topB)
            .Where(d => topCounts.TryGetValue(d, out var count) && count > 1)
            .Distinct()
            .ToList();

        var topBrushByDigit = new Dictionary<string, Brush>();
        for (int i = 0; i < repeatedTopDigits.Count; i++)
        {
            topBrushByDigit[repeatedTopDigits[i]] = RepeatPalette[i % RepeatPalette.Length];
        }

        var topProfiles = BuildDigitProfiles(topA, topB);
        var topBrushByProfile = topProfiles
            .Where(kv => topBrushByDigit.ContainsKey(kv.Key))
            .ToDictionary(kv => kv.Value, kv => topBrushByDigit[kv.Key]);

        var bottomCounts = bottomA.Concat(bottomB).GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
        var bottomProfiles = BuildDigitProfiles(bottomA, bottomB);
        var bottomBrushByDigit = new Dictionary<string, Brush>();

        var repeatedBottomDigits = bottomA.Concat(bottomB)
            .Where(d => bottomCounts.TryGetValue(d, out var count) && count > 1)
            .Distinct()
            .ToList();

        int nextPaletteIndex = repeatedTopDigits.Count;
        foreach (var digit in repeatedBottomDigits)
        {
            if (bottomProfiles.TryGetValue(digit, out var profile) && topBrushByProfile.TryGetValue(profile, out var mappedBrush))
            {
                bottomBrushByDigit[digit] = mappedBrush;
            }
            else
            {
                bottomBrushByDigit[digit] = RepeatPalette[nextPaletteIndex % RepeatPalette.Length];
                nextPaletteIndex++;
            }
        }

        Row1Pick3 = BuildDigitCells(_row1Pick3Number, topCounts, topBrushByDigit);
        Row1Pick4 = BuildDigitCells(_row1Pick4Number, topCounts, topBrushByDigit);
        Row2Pick3 = BuildDigitCells(_row2Pick3Number, topCounts, topBrushByDigit);
        Row2Pick4 = BuildDigitCells(_row2Pick4Number, topCounts, topBrushByDigit);

        Row3Pick3 = BuildDigitCells(_row3Pick3Number, bottomCounts, bottomBrushByDigit);
        Row3Pick4 = BuildDigitCells(_row3Pick4Number, bottomCounts, bottomBrushByDigit);
        Row4Pick3 = BuildDigitCells(_row4Pick3Number, bottomCounts, bottomBrushByDigit);
        Row4Pick4 = BuildDigitCells(_row4Pick4Number, bottomCounts, bottomBrushByDigit);

        Row1_Pick3Digits.ItemsSource = Row1Pick3;
        Row1_Pick4Digits.ItemsSource = Row1Pick4;
        Row2_Pick3Digits.ItemsSource = Row2Pick3;
        Row2_Pick4Digits.ItemsSource = Row2Pick4;
        Row3_Pick3Digits.ItemsSource = Row3Pick3;
        Row3_Pick4Digits.ItemsSource = Row3Pick4;
        Row4_Pick3Digits.ItemsSource = Row4Pick3;
        Row4_Pick4Digits.ItemsSource = Row4Pick4;
    }

    private static List<string> ExtractDigits(string? value)
    {
        return (value ?? " ").Where(char.IsDigit).Select(c => c.ToString()).ToList();
    }

    private static Dictionary<string, string> BuildDigitProfiles(IReadOnlyList<string> rowA, IReadOnlyList<string> rowB)
    {
        var profiles = new Dictionary<string, string>();
        var digits = rowA.Concat(rowB).Distinct().ToList();

        foreach (var digit in digits)
        {
            var aPositions = Enumerable.Range(0, rowA.Count).Where(i => rowA[i] == digit);
            var bPositions = Enumerable.Range(0, rowB.Count).Where(i => rowB[i] == digit);
            var profile = $"A:{string.Join(",", aPositions)}|B:{string.Join(",", bPositions)}";
            profiles[digit] = profile;
        }

        return profiles;
    }

    private static List<DigitCell> BuildDigitCells(
        string? value,
        IReadOnlyDictionary<string, int> counts,
        IReadOnlyDictionary<string, Brush> brushesByDigit)
    {
        return ExtractDigits(value)
            .Select(d => new DigitCell
            {
                Value = d,
                Background = counts.TryGetValue(d, out var c) && c > 1
                    ? (brushesByDigit.TryGetValue(d, out var brush) ? brush : RepeatPalette[0])
                    : Brushes.White
            })
            .ToList();
    }

    public sealed class DigitCell
    {
        public string Value { get; set; } = " ";
        public Brush Background { get; set; } = Brushes.White;
    }

        private void SetLoadingState(bool isLoading, string status, int completed, int total, bool isIndeterminate)
    {
        UpdateWindowSize(isLoading);
        LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        AnalysisProgressText.Text = status;

        AnalysisProgressBar.IsIndeterminate = false;
        AnalysisProgressBar.Maximum = Math.Max(1, total);
        AnalysisProgressBar.Value = Math.Min(Math.Max(0, completed), AnalysisProgressBar.Maximum);
    }

    private void SetLoadingState(bool isLoading, string status, int completed, bool isIndeterminate)
    {
        int total = Math.Max(1, (int)Math.Ceiling(AnalysisProgressBar.Maximum));
        SetLoadingState(isLoading, status, completed, total, false);
    }

    private void UpdateWindowSize(bool isLoading)
    {
        Height = isLoading ? _expandedWindowHeight : _originalWindowHeight;
        Width = _originalWindowWidth;
    }
    private void UpdateResultsCounter()
    {
        int total = PatternRows.Count;
        int selected = 0;

        if (total > 0 && PatternsTable.SelectedIndex >= 0)
        {
            selected = PatternsTable.SelectedIndex + 1;
        }

        ResultsCounterText.Text = $"{selected} de {total}";
    }
}
