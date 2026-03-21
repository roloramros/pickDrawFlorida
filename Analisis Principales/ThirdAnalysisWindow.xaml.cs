using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FloridaLotteryApp;

public partial class ThirdAnalysisWindow : Window
{
    public ObservableCollection<ThirdAnalysisCardVM> AnalysisCards { get; } = new ObservableCollection<ThirdAnalysisCardVM>();

    public ThirdAnalysisWindow(AnalysisPairCardVM selectedCard)
    {
        InitializeComponent();
        DataContext = this;

        // Obtener posiciones del d?gito repetido en las dos primeras filas
        var guidePositions = GetRepeatedDigitPositions(
            string.Join("", selectedCard.GuidePick3Digits.Select(d => d.Value)),
            string.Join("", selectedCard.GuidePick4Digits.Select(d => d.Value)));
        
        var resultPositions = GetRepeatedDigitPositions(
            string.Join("", selectedCard.ResPick3Digits.Select(d => d.Value)),
            string.Join("", selectedCard.ResPick4Digits.Select(d => d.Value)));

        // Crear m?ltiples tarjetas, una para cada resultado del tercer an?lisis
        var cards = ThirdAnalysisCardVM.CreateMultipleFrom(selectedCard, guidePositions, resultPositions);
        foreach (var card in cards)
        {
            AnalysisCards.Add(card);
        }
    }

    private void analysisButton1(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not ThirdAnalysisCardVM selectedCard) return;
        
        // Obtener el modo del Tag del bot?n
        if (button.Tag is AnalysisMode mode)
        {
            // Crear y mostrar la nueva ventana pasando el modo
            var thirdAnalysisOption1 = new ThirdAnalysisOption1(selectedCard, mode);
            thirdAnalysisOption1.Owner = this;
            thirdAnalysisOption1.Show();
        }
        else
        {
            // Si no hay Tag, usar Opcion1 por defecto
            var thirdAnalysisOption1 = new ThirdAnalysisOption1(selectedCard, AnalysisMode.Opcion1);
            thirdAnalysisOption1.Owner = this;
            thirdAnalysisOption1.Show();
        }
    }

    private void OpenAnalisis31MatchWindow(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not ThirdAnalysisCardVM selectedCard) return;

        var window = new Analisis_3_1_MatchWindow(selectedCard)
        {
            Owner = this
        };
        window.Show();
    }

    private (char digit, int pick3Position, int pick4Position)? GetRepeatedDigitPositions(string pick3, string pick4)
    {
        if (string.IsNullOrWhiteSpace(pick3) || pick3.Length != 3 ||
            string.IsNullOrWhiteSpace(pick4) || pick4.Length != 4)
            return null;

        // Buscar d?gito repetido y sus posiciones
        for (int i = 0; i < pick3.Length; i++)
        {
            char digit = pick3[i];
            int positionInPick4 = pick4.IndexOf(digit);
            
            if (positionInPick4 >= 0)
            {
                return (digit, i, positionInPick4);
            }
        }
        
        return null;
    }

    private void CardBorder_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement root)
        {
            return;
        }

        root.Dispatcher.BeginInvoke(new Action(() => 
        {
            // Conectar Pick3: Fila 1 con Fila 2 (usar el primer canvas)
            ConnectPick3Digits(root, "Row1Pick3Items", "Row2Pick3Items", "Pick3LinksCanvas");
            
            // Conectar NextPick3: Fila 1 con Fila 2 (usar el segundo canvas)
            ConnectPick3Digits(root, "Row1NextPick3Items", "Row2NextPick3Items", "NextPick3LinksCanvas");
            
            // CONECTAR Pick3: Fila 3 con Fila 4 (PERO NO LIMPIAR EL CANVAS EXISTENTE)
            // Buscar los ItemsControls de fila 3 y 4
            var row3Pick3Items = FindVisualChild<ItemsControl>(root, "Row3Pick3Items");
            var row4Pick3Items = FindVisualChild<ItemsControl>(root, "Row4Pick3Items");
            var pick3Canvas = FindVisualChild<Canvas>(root, "Pick3LinksCanvas");
            
            if (row3Pick3Items != null && row4Pick3Items != null && pick3Canvas != null)
            {
                // Obtener d?gitos de fila 3 y 4
                var row3Digits = GetDigitContainers(row3Pick3Items);
                var row4Digits = GetDigitContainers(row4Pick3Items);
                
                if (row3Digits.Count == 3 && row4Digits.Count == 3)
                {
                    // Conectar d?gitos que coincidan SIN LIMPIAR EL CANVAS
                    for (int i = 0; i < 3; i++)
                    {
                        var topDigit = row3Digits[i];
                        var topText = GetDigitText(topDigit);
                        
                        for (int j = 0; j < 3; j++)
                        {
                            var bottomDigit = row4Digits[j];
                            var bottomText = GetDigitText(bottomDigit);
                            
                            if (topText == bottomText)
                            {
                                // Dibujar l?nea conectando los d?gitos
                                DrawConnectingLine(pick3Canvas, topDigit, bottomDigit);
                            }
                        }
                    }
                }
            }
            
            // CONECTAR NextPick3: Fila 3 con Fila 4 (PERO NO LIMPIAR EL CANVAS EXISTENTE)
            var row3NextPick3Items = FindVisualChild<ItemsControl>(root, "Row3NextPick3Items");
            var row4NextPick3Items = FindVisualChild<ItemsControl>(root, "Row4NextPick3Items");
            var nextPick3Canvas = FindVisualChild<Canvas>(root, "NextPick3LinksCanvas");
            
            if (row3NextPick3Items != null && row4NextPick3Items != null && nextPick3Canvas != null)
            {
                // Obtener d?gitos de fila 3 y 4
                var row3Digits = GetDigitContainers(row3NextPick3Items);
                var row4Digits = GetDigitContainers(row4NextPick3Items);
                
                if (row3Digits.Count == 3 && row4Digits.Count == 3)
                {
                    // Conectar d?gitos que coincidan SIN LIMPIAR EL CANVAS
                    for (int i = 0; i < 3; i++)
                    {
                        var topDigit = row3Digits[i];
                        var topText = GetDigitText(topDigit);
                        
                        for (int j = 0; j < 3; j++)
                        {
                            var bottomDigit = row4Digits[j];
                            var bottomText = GetDigitText(bottomDigit);
                            
                            if (topText == bottomText)
                            {
                                // Dibujar l?nea conectando los d?gitos
                                DrawConnectingLine(nextPick3Canvas, topDigit, bottomDigit);
                            }
                        }
                    }
                }
            }
            // NUEVO: Conectar Pick3 de fila 3 con Pick4 de fila 4
            ConnectPick3ToPick4Digits(root, "Row3Pick3Items", "Row4Pick4Items", "Pick3Row3ToPick4Row4LinksCanvas");
            ConnectPick3ToPick4Digits(root, "Row4Pick3Items", "Row3Pick4Items", "Pick3Row4ToPick4Row3LinksCanvas"); 

            ConnectPick3ToPick4DigitsWithRedLine(root, "Row1Pick3Items", "Row2Pick4Items", "Pick3Row1ToPick4Row2LinksCanvas"); 
            ConnectPick3ToPick4DigitsWithRedLine(root, "Row2Pick3Items", "Row1Pick4Items", "Pick3Row2ToPick4Row1LinksCanvas"); 

        
        }), DispatcherPriority.Loaded);
    }

    private void ConnectPick3ToPick4Digits(FrameworkElement root, string pick3ItemsControlName, string pick4ItemsControlName, string canvasName)
    {
        // Buscar los ItemsControls
        var pick3ItemsControl = FindVisualChild<ItemsControl>(root, pick3ItemsControlName);
        var pick4ItemsControl = FindVisualChild<ItemsControl>(root, pick4ItemsControlName);
        var canvas = FindVisualChild<Canvas>(root, canvasName);

        if (pick3ItemsControl == null || pick4ItemsControl == null || canvas == null)
            return;


        // Obtener los contenedores de los d?gitos
        var pick3Digits = GetDigitContainers(pick3ItemsControl);
        var pick4Digits = GetDigitContainers(pick4ItemsControl);

        if (pick3Digits.Count != 3 || pick4Digits.Count != 4)
            return;
        // Limpiar canvas antes de dibujar
        canvas.Children.Clear();

        // Conectar d?gitos que coincidan
        for (int i = 0; i < 3; i++)
        {
            var pick3Digit = pick3Digits[i];
            var pick3Text = GetDigitText(pick3Digit);
            
            for (int j = 0; j < 4; j++)
            {
                var pick4Digit = pick4Digits[j];
                var pick4Text = GetDigitText(pick4Digit);
                
                if (pick3Text == pick4Text)
                {
                    // Dibujar l?nea conectando los d?gitos
                    DrawConnectingLine(canvas, pick3Digit, pick4Digit);
                }
            }
        }
    }

    private void ConnectPick3ToPick4DigitsWithRedLine(FrameworkElement root, string pick3ItemsControlName, string pick4ItemsControlName, string canvasName)
    {
        // Buscar los ItemsControls
        var pick3ItemsControl = FindVisualChild<ItemsControl>(root, pick3ItemsControlName);
        var pick4ItemsControl = FindVisualChild<ItemsControl>(root, pick4ItemsControlName);
        var canvas = FindVisualChild<Canvas>(root, canvasName);

        if (pick3ItemsControl == null || pick4ItemsControl == null || canvas == null)
            return;

        // Obtener los contenedores de los d?gitos
        var pick3Digits = GetDigitContainers(pick3ItemsControl);
        var pick4Digits = GetDigitContainers(pick4ItemsControl);

        if (pick3Digits.Count != 3 || pick4Digits.Count != 4)
            return;
        // Limpiar canvas antes de dibujar (opcional, dependiendo de si quieres borrar l?neas anteriores)
        // canvas.Children.Clear(); // Descomenta esta l?nea si quieres limpiar antes de dibujar las nuevas rojas

        // Conectar d?gitos que coincidan
        for (int i = 0; i < 3; i++)
        {
            var pick3Digit = pick3Digits[i];
            var pick3Text = GetDigitText(pick3Digit);

            for (int j = 0; j < 4; j++)
            {
                var pick4Digit = pick4Digits[j];
                var pick4Text = GetDigitText(pick4Digit);

                if (pick3Text == pick4Text)
                {
                    // Dibujar l?nea ROJA conectando los d?gitos
                    DrawRedConnectingLine(canvas, pick3Digit, pick4Digit); // Usamos la funci?n que ya existe
                }
            }
        }
    }

    private void DrawRedConnectingLine(Canvas canvas, Border element1, Border element2)
    {
        try
        {
            // Verificar que los elementos tengan dimensiones v?lidas
            if (element1.ActualWidth <= 0 || element1.ActualHeight <= 0 ||
                element2.ActualWidth <= 0 || element2.ActualHeight <= 0)
                return;

            // Calcular centros absolutos en el canvas
            var center1 = element1.TranslatePoint(new Point(element1.ActualWidth / 2, element1.ActualHeight / 2), canvas);
            var center2 = element2.TranslatePoint(new Point(element2.ActualWidth / 2, element2.ActualHeight / 2), canvas);

            // Calcular vector direcci?n de element1 a element2
            double dx = center2.X - center1.X;
            double dy = center2.Y - center1.Y;

            // Calcular distancia entre centros
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Si est?n en la misma posici?n, no dibujar l?nea
            if (distance == 0) return;

            // Calcular radio de cada c?rculo (mitad del ancho, ya que son c?rculos)
            double radius1 = element1.ActualWidth / 2;
            double radius2 = element2.ActualWidth / 2;

            // Normalizar vector direcci?n
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

            // Crear l?nea CONTINUA ROJA
            var line = new Line
            {
                X1 = startPoint.X,
                Y1 = startPoint.Y,
                X2 = endPoint.X,
                Y2 = endPoint.Y,
                Stroke = Brushes.Red, // <-- Ac? est? el cambio a Rojo
                StrokeThickness = 2
            };
            canvas.Children.Add(line);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error dibujando l?nea roja: {ex.Message}");
            // Opcional: Mostrar mensaje en UI o log detallado
        }
    }

    private void ConnectPick3Digits(FrameworkElement root, string topItemsControlName, string bottomItemsControlName, string canvasName)
    {
        // Buscar los ItemsControls
        var topItemsControl = FindVisualChild<ItemsControl>(root, topItemsControlName);
        var bottomItemsControl = FindVisualChild<ItemsControl>(root, bottomItemsControlName);
        var canvas = FindVisualChild<Canvas>(root, canvasName);

        if (topItemsControl == null || bottomItemsControl == null || canvas == null)
            return;

        // Obtener los contenedores de los d?gitos
        var topDigits = GetDigitContainers(topItemsControl);
        var bottomDigits = GetDigitContainers(bottomItemsControl);

        if (topDigits.Count != 3 || bottomDigits.Count != 3)
            return;

        // Limpiar canvas antes de dibujar
        canvas.Children.Clear();

        // Conectar d?gitos que coincidan
        for (int i = 0; i < 3; i++)
        {
            var topDigit = topDigits[i];
            var topText = GetDigitText(topDigit);
            
            for (int j = 0; j < 3; j++)
            {
                var bottomDigit = bottomDigits[j];
                var bottomText = GetDigitText(bottomDigit);
                
                if (topText == bottomText)
                {
                    // Dibujar l?nea conectando los d?gitos
                    DrawConnectingLine(canvas, topDigit, bottomDigit);
                }
            }
        }
    }

    private List<Border> GetDigitContainers(ItemsControl itemsControl)
    {
        var containers = new List<Border>();
        
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
        
        return containers;
    }

    private string GetDigitText(Border digitBorder)
    {
        var textBlock = FindVisualChild<TextBlock>(digitBorder);
        return textBlock?.Text ?? "";
    }

    private void DrawConnectingLine(Canvas canvas, Border element1, Border element2)
    {
        // Calcular centros absolutos
        var center1 = element1.TranslatePoint(new Point(element1.ActualWidth / 2, element1.ActualHeight / 2), canvas);
        var center2 = element2.TranslatePoint(new Point(element2.ActualWidth / 2, element2.ActualHeight / 2), canvas);
        
        // Calcular vector direcci?n de element1 a element2
        double dx = center2.X - center1.X;
        double dy = center2.Y - center1.Y;
        
        // Calcular distancia entre centros
        double distance = Math.Sqrt(dx * dx + dy * dy);
        
        // Si est?n en la misma posici?n, no dibujar l?nea
        if (distance == 0) return;
        
        // Calcular radio de cada c?rculo (mitad del ancho, ya que son c?rculos)
        double radius1 = element1.ActualWidth / 2;
        double radius2 = element2.ActualWidth / 2;
        
        // Normalizar vector direcci?n
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
        
        // Crear l?nea CONTINUA NEGRA
        var line = new Line
        {
            X1 = startPoint.X,
            Y1 = startPoint.Y,
            X2 = endPoint.X,
            Y2 = endPoint.Y,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        
        canvas.Children.Add(line);
    }

    private T? FindVisualChild<T>(DependencyObject parent, string? childName = null) where T : DependencyObject
    {
        if (parent == null) return null;

        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            
            // Buscar por nombre si se especifica
            if (child is FrameworkElement frameworkElement && 
                !string.IsNullOrEmpty(childName) && 
                frameworkElement.Name == childName)
            {
                return child as T;
            }
            
            // Buscar por tipo
            if (child is T result && (string.IsNullOrEmpty(childName) || childName == result.GetValue(FrameworkElement.NameProperty) as string))
            {
                return result;
            }
            
            // Buscar recursivamente
            var foundChild = FindVisualChild<T>(child, childName);
            if (foundChild != null) return foundChild;
        }
        
        return null;
    }

    private static string DrawTimeToIcon(string drawTime)
    {
        return drawTime switch
        {
            "M" => "\u2600\uFE0F",
            "E" => "\uD83C\uDF19",
            _ => ""
        };
    }
}







public class ThirdAnalysisCardVM
{
    // Propiedades para las 4 filas
    public ObservableCollection<DigitVM> Row1Pick3Digits { get; set; } = new ObservableCollection<DigitVM>();
    public ObservableCollection<DigitVM> Row1Pick4Digits { get; set; } = new ObservableCollection<DigitVM>();
    public ObservableCollection<DigitVM> Row1NextPick3Digits { get; set; } = new ObservableCollection<DigitVM>();
    public ObservableCollection<DigitVM> Row1CodingDigits { get; set; } = new ObservableCollection<DigitVM>();
    public string Row1DateText { get; set; } = "";
    public string Row1DrawIcon { get; set; } = "";

    public ObservableCollection<DigitVM> Row2Pick3Digits { get; set; } = new ObservableCollection<DigitVM>();
    public ObservableCollection<DigitVM> Row2Pick4Digits { get; set; } = new ObservableCollection<DigitVM>();
    public ObservableCollection<DigitVM> Row2NextPick3Digits { get; set; } = new ObservableCollection<DigitVM>();
    public ObservableCollection<DigitVM> Row2CodingDigits { get; set; } = new ObservableCollection<DigitVM>();
    public string Row2DateText { get; set; } = "";
    public string Row2DrawIcon { get; set; } = "";

    public ObservableCollection<DigitVM> Row3Pick3Digits { get; set; } = new ObservableCollection<DigitVM>();
    public ObservableCollection<DigitVM> Row3Pick4Digits { get; set; } = new ObservableCollection<DigitVM>();
    public ObservableCollection<DigitVM> Row3NextPick3Digits { get; set; } = new ObservableCollection<DigitVM>();
    public ObservableCollection<DigitVM> Row3CodingDigits { get; set; } = new ObservableCollection<DigitVM>();
    public string Row3DateText { get; set; } = "";
    public string Row3DrawIcon { get; set; } = "";

    public ObservableCollection<DigitVM> Row4Pick3Digits { get; set; } = new ObservableCollection<DigitVM>();
    public ObservableCollection<DigitVM> Row4Pick4Digits { get; set; } = new ObservableCollection<DigitVM>();
    public ObservableCollection<DigitVM> Row4NextPick3Digits { get; set; } = new ObservableCollection<DigitVM>();
    public ObservableCollection<DigitVM> Row4CodingDigits { get; set; } = new ObservableCollection<DigitVM>();
    public string Row4DateText { get; set; } = "";
    public string Row4DrawIcon { get; set; } = "";

    // NUEVAS propiedades para los contadores
    public string CodingMatchesRow1Row2 { get; set; } = "";
    public string CodingMatchesRow3Row4 { get; set; } = "";
    public string Pick4MatchesRow1Row2 { get; set; } = "";
    public string Pick4MatchesRow3Row4 { get; set; } = "";

    public string AnalysisSummary { get; set; } = "";

    private static string DrawTimeToIcon(string drawTime)
    {
        return drawTime switch
        {
            "M" => "\u2600\uFE0F",
            "E" => "\uD83C\uDF19",
            _ => ""
        };
    }

    public static ObservableCollection<ThirdAnalysisCardVM> CreateMultipleFrom(
        AnalysisPairCardVM originalCard,
        (char digit, int pick3Position, int pick4Position)? guidePositions,
        (char digit, int pick3Position, int pick4Position)? resultPositions)
    {
        var resultCollection = new ObservableCollection<ThirdAnalysisCardVM>();
        
        // Ejecutar an?lisis para obtener resultados del tercer an?lisis
        var analysisResults = ExecuteThirdAnalysis(
            string.Join("", originalCard.GuidePick3Digits.Select(d => d.Value)),
            string.Join("", originalCard.ResPick3Digits.Select(d => d.Value)),
            guidePositions,
            resultPositions);
        if (analysisResults.Count == 0)
        {
            return resultCollection;
        }

        // Crear una tarjeta para cada resultado del an?lisis
        foreach (var result in analysisResults)
        {
            // Obtener la tercera fecha del resultado del an?lisis
            DateTime thirdDate = DateTime.Parse(result.Date);
            DateTime guideDate = DateTime.Parse(originalCard.GuideDateText);
            DateTime resultDate = DateTime.Parse(originalCard.ResDateText);
            
            // Crear lista de todas las fechas
            var allDates = new[]
            {
                (Date: guideDate, IsGuide: true, IsResult: false, IsThird: false),
                (Date: resultDate, IsGuide: false, IsResult: true, IsThird: false),
                (Date: thirdDate, IsGuide: false, IsResult: false, IsThird: true)
            };

            // Ordenar fechas
            var orderedDates = allDates.OrderBy(d => d.Date).ToList();
            
            // Identificar qu? fecha es cu?l
            DateTime minDate = orderedDates[0].Date;
            DateTime middleDate = orderedDates[1].Date;
            DateTime maxDate = orderedDates[2].Date;

            // Crear tarjeta
            var card = new ThirdAnalysisCardVM
            {
                AnalysisSummary = $"Resultado {analysisResults.IndexOf(result) + 1} de {analysisResults.Count}"
            };

            // ASIGNAR FILA 1: FECHA INTERMEDIA (Resultado Original)
            if (Math.Abs((middleDate - resultDate).TotalDays) < 1)
            {
                // La fecha intermedia es el resultado original
                card.Row1DateText = originalCard.ResDateText;
                card.Row1DrawIcon = originalCard.ResDrawIcon;
                card.Row1Pick3Digits = CopyDigitCollection(originalCard.ResPick3Digits);
                card.Row1Pick4Digits = CopyDigitCollection(originalCard.ResPick4Digits);
                card.Row1NextPick3Digits = CopyDigitCollection(originalCard.ResNextPick3Digits);
                card.Row1CodingDigits = CopyDigitCollection(originalCard.ResCodingDigits);
            }
            else if (Math.Abs((middleDate - guideDate).TotalDays) < 1)
            {
                // La fecha intermedia es la gu?a original
                card.Row1DateText = originalCard.GuideDateText;
                card.Row1DrawIcon = originalCard.GuideDrawIcon;
                card.Row1Pick3Digits = CopyDigitCollection(originalCard.GuidePick3Digits);
                card.Row1Pick4Digits = CopyDigitCollection(originalCard.GuidePick4Digits);
                card.Row1NextPick3Digits = CopyDigitCollection(originalCard.GuideNextPick3Digits);
                card.Row1CodingDigits = CopyDigitCollection(originalCard.GuideCodingDigits);
            }
            else
            {
                // La fecha intermedia es el resultado del tercer an?lisis
                card.Row1DateText = result.Date;
                card.Row1DrawIcon = DrawTimeToIcon(result.DrawTime);
                AssignThirdResultDigits(card.Row1Pick3Digits, result.Pick3Number);
                AssignThirdResultDigits(card.Row1Pick4Digits, result.Pick4Number);
                AssignThirdResultDigits(card.Row1NextPick3Digits, result.NextPick3Number);
                AssignCodingDigits(card.Row1CodingDigits, result.Coding);
            }

            // ASIGNAR FILA 2: FECHA MENOR
            if (Math.Abs((minDate - guideDate).TotalDays) < 1)
            {
                card.Row2DateText = originalCard.GuideDateText;
                card.Row2DrawIcon = originalCard.GuideDrawIcon;
                card.Row2Pick3Digits = CopyDigitCollection(originalCard.GuidePick3Digits);
                card.Row2Pick4Digits = CopyDigitCollection(originalCard.GuidePick4Digits);
                card.Row2NextPick3Digits = CopyDigitCollection(originalCard.GuideNextPick3Digits);
                card.Row2CodingDigits = CopyDigitCollection(originalCard.GuideCodingDigits);
            }
            else if (Math.Abs((minDate - resultDate).TotalDays) < 1)
            {
                card.Row2DateText = originalCard.ResDateText;
                card.Row2DrawIcon = originalCard.ResDrawIcon;
                card.Row2Pick3Digits = CopyDigitCollection(originalCard.ResPick3Digits);
                card.Row2Pick4Digits = CopyDigitCollection(originalCard.ResPick4Digits);
                card.Row2NextPick3Digits = CopyDigitCollection(originalCard.ResNextPick3Digits);
                card.Row2CodingDigits = CopyDigitCollection(originalCard.ResCodingDigits);
            }
            else
            {
                card.Row2DateText = result.Date;
                card.Row2DrawIcon = DrawTimeToIcon(result.DrawTime);
                AssignThirdResultDigits(card.Row2Pick3Digits, result.Pick3Number);
                AssignThirdResultDigits(card.Row2Pick4Digits, result.Pick4Number);
                AssignThirdResultDigits(card.Row2NextPick3Digits, result.NextPick3Number);
                AssignCodingDigits(card.Row2CodingDigits, result.Coding);
            }

            // ASIGNAR FILA 3: FECHA MAYOR
            if (Math.Abs((maxDate - guideDate).TotalDays) < 1)
            {
                card.Row3DateText = originalCard.GuideDateText;
                card.Row3DrawIcon = originalCard.GuideDrawIcon;
                card.Row3Pick3Digits = CopyDigitCollection(originalCard.GuidePick3Digits);
                card.Row3Pick4Digits = CopyDigitCollection(originalCard.GuidePick4Digits);
                card.Row3NextPick3Digits = CopyDigitCollection(originalCard.GuideNextPick3Digits);
                card.Row3CodingDigits = CopyDigitCollection(originalCard.GuideCodingDigits);
            }
            else if (Math.Abs((maxDate - resultDate).TotalDays) < 1)
            {
                card.Row3DateText = originalCard.ResDateText;
                card.Row3DrawIcon = originalCard.ResDrawIcon;
                card.Row3Pick3Digits = CopyDigitCollection(originalCard.ResPick3Digits);
                card.Row3Pick4Digits = CopyDigitCollection(originalCard.ResPick4Digits);
                card.Row3NextPick3Digits = CopyDigitCollection(originalCard.ResNextPick3Digits);
                card.Row3CodingDigits = CopyDigitCollection(originalCard.ResCodingDigits);
            }
            else
            {
                card.Row3DateText = result.Date;
                card.Row3DrawIcon = DrawTimeToIcon(result.DrawTime);
                AssignThirdResultDigits(card.Row3Pick3Digits, result.Pick3Number);
                AssignThirdResultDigits(card.Row3Pick4Digits, result.Pick4Number);
                AssignThirdResultDigits(card.Row3NextPick3Digits, result.NextPick3Number);
                AssignCodingDigits(card.Row3CodingDigits, result.Coding);
            }

            // ASIGNAR FILA 4: REPETIR FECHA MENOR (igual que fila 2)
            card.Row4DateText = card.Row2DateText;
            card.Row4DrawIcon = card.Row2DrawIcon;
            card.Row4Pick3Digits = CopyDigitCollection(card.Row2Pick3Digits);
            card.Row4Pick4Digits = CopyDigitCollection(card.Row2Pick4Digits);
            card.Row4NextPick3Digits = CopyDigitCollection(card.Row2NextPick3Digits);
            card.Row4CodingDigits = CopyDigitCollection(card.Row2CodingDigits);

            // NUEVO FILTRO: No permitir digitos repetidos en NextPick3 de fila 1 y 2
            if (HasRepeatedDigits(card.Row1NextPick3Digits) || HasRepeatedDigits(card.Row2NextPick3Digits))
            {
                continue;
            }

            resultCollection.Add(card);
        }

        // NUEVO: Procesar colores de codificaci?n despu?s de crear todas las tarjetas
        foreach (var card in resultCollection)
        {
            ProcessCodingColors(card);
        }

        return resultCollection;
    }

    private static void AssignThirdResultDigits(ObservableCollection<DigitVM> collection, string number)
    {
        if (!string.IsNullOrWhiteSpace(number) && number.All(char.IsDigit))
        {
            foreach (char digit in number)
            {
                collection.Add(new DigitVM
                {
                    Value = digit.ToString(),
                    Bg = CreateFrozenBrush(Colors.White)
                });
            }
        }
    }

    private static void AssignCodingDigits(ObservableCollection<DigitVM> collection, string coding)
    {
        if (!string.IsNullOrWhiteSpace(coding))
        {
            foreach (char digit in coding.OrderBy(c => c))
            {
                collection.Add(new DigitVM
                {
                    Value = digit.ToString(),
                    Bg = CreateFrozenBrush(Colors.White)
                });
            }
        }
    }

    // NUEVO M?TODO: Procesar colores de d?gitos repetidos en codificaci?n
    private static void ProcessCodingColors(ThirdAnalysisCardVM card)
    {
        // Comparar Fila 1 vs Fila 2 - Codificaci?n
        int codingMatches1 = ColorRepeatedDigitsInCollections(
            card.Row1CodingDigits, 
            card.Row2CodingDigits,
            Colors.LightPink);  // Color para codificaci?n
        
        card.CodingMatchesRow1Row2 = $"{codingMatches1}C";
        
        // Comparar Fila 1 vs Fila 2 - Pick4
        int pick4Matches1 = ColorRepeatedDigitsInCollections(
            card.Row1Pick4Digits, 
            card.Row2Pick4Digits,
            Colors.LightBlue);  // Color diferente para Pick4
        
        card.Pick4MatchesRow1Row2 = $"{pick4Matches1}C";
        
        // Comparar Fila 3 vs Fila 4 - Codificaci?n
        int codingMatches2 = ColorRepeatedDigitsInCollections(
            card.Row3CodingDigits, 
            card.Row4CodingDigits,
            Colors.LightPink);
        
        card.CodingMatchesRow3Row4 = $"{codingMatches2}C";
        
        // Comparar Fila 3 vs Fila 4 - Pick4
        int pick4Matches2 = ColorRepeatedDigitsInCollections(
            card.Row3Pick4Digits, 
            card.Row4Pick4Digits,
            Colors.LightBlue);
        
        card.Pick4MatchesRow3Row4 = $"{pick4Matches2}C";
        
        // NUEVO: Colorear d?gito repetido entre Pick3 y Pick4 en la fila 4 que tambi?n aparece en NextPick3
        ColorRepeatedDigitInRow4(card);
    }

    // NUEVO M?TODO: Colorear d?gito repetido en fila 4
    private static void ColorRepeatedDigitInRow4(ThirdAnalysisCardVM card)
    {
        // Encontrar d?gito repetido entre Pick3 y Pick4 en la fila 4
        var pick3Digits = card.Row4Pick3Digits.Select(d => d.Value).ToList();
        var pick4Digits = card.Row4Pick4Digits.Select(d => d.Value).ToList();
        
        // Buscar d?gitos repetidos entre Pick3 y Pick4
        var repeatedDigits = pick3Digits.Intersect(pick4Digits).ToList();
        
        // Si hay exactamente 1 d?gito repetido (como se espera en el an?lisis)
        if (repeatedDigits.Count == 1)
        {
            string repeatedDigit = repeatedDigits[0];
            
            // Verificar si este d?gito tambi?n est? en el NextPick3 de la misma fila
            var nextPick3Digits = card.Row4NextPick3Digits.Select(d => d.Value).ToList();
            
            if (nextPick3Digits.Contains(repeatedDigit))
            {
                // Colorear el d?gito en Pick3
                foreach (var digitVM in card.Row4Pick3Digits)
                {
                    if (digitVM.Value == repeatedDigit)
                    {
                        digitVM.Bg = CreateFrozenBrush(Colors.LightBlue);
                    }
                }
                
                // Colorear el d?gito en Pick4
                foreach (var digitVM in card.Row4Pick4Digits)
                {
                    if (digitVM.Value == repeatedDigit)
                    {
                        digitVM.Bg = CreateFrozenBrush(Colors.LightBlue);
                    }
                }
                
                // Colorear el d?gito en NextPick3
                foreach (var digitVM in card.Row4NextPick3Digits)
                {
                    if (digitVM.Value == repeatedDigit)
                    {
                        digitVM.Bg = CreateFrozenBrush(Colors.LightBlue);
                    }
                }
            }
        }
    }

    // NUEVO M?TODO: Colorear d?gitos repetidos y devolver el conteo
    private static int ColorRepeatedDigitsInCollections(
        ObservableCollection<DigitVM> collection1, 
        ObservableCollection<DigitVM> collection2,
        Color highlightColor)
    {
        // Obtener valores ?nicos de cada colecci?n
        var values1 = collection1.Select(d => d.Value).ToList();
        var values2 = collection2.Select(d => d.Value).ToList();
        
        // Encontrar d?gitos que se repiten
        var repeatedValues = values1.Intersect(values2).ToList();
        int matchCount = repeatedValues.Count;
        
        // Colorear solo los d?gitos repetidos
        ColorRepeatedDigits(collection1, repeatedValues, highlightColor);
        ColorRepeatedDigits(collection2, repeatedValues, highlightColor);
        
        return matchCount;
    }

    // NUEVO M?TODO: Aplicar color a d?gitos repetidos
    private static void ColorRepeatedDigits(
        ObservableCollection<DigitVM> collection, 
        List<string> repeatedValues, 
        Color highlightColor)
    {
        foreach (var digitVM in collection)
        {
            if (repeatedValues.Contains(digitVM.Value))
            {
                // Usar el color especificado
                digitVM.Bg = CreateFrozenBrush(highlightColor);
            }
            else
            {
                // Mantener blanco para no repetidos
                digitVM.Bg = CreateFrozenBrush(Colors.White);
            }
        }
    }

    private static bool HasRepeatedDigits(ObservableCollection<DigitVM> collection)
    {
        if (collection == null || collection.Count == 0)
        {
            return false;
        }

        var values = collection
            .Select(d => d.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        if (values.Count != 3)
        {
            return false;
        }

        return values.Distinct().Count() != 3;
    }

    private static ThirdAnalysisCardVM CreateEmptyCard(AnalysisPairCardVM originalCard)
    {
        return new ThirdAnalysisCardVM
        {
            // Fila 1: Gu?a original
            Row1DateText = originalCard.GuideDateText,
            Row1DrawIcon = originalCard.GuideDrawIcon,
            Row1Pick3Digits = CopyDigitCollection(originalCard.GuidePick3Digits),
            Row1Pick4Digits = CopyDigitCollection(originalCard.GuidePick4Digits),
            Row1NextPick3Digits = CopyDigitCollection(originalCard.GuideNextPick3Digits),
            Row1CodingDigits = CopyDigitCollection(originalCard.GuideCodingDigits),

            // Fila 2: Resultado original
            Row2DateText = originalCard.ResDateText,
            Row2DrawIcon = originalCard.ResDrawIcon,
            Row2Pick3Digits = CopyDigitCollection(originalCard.ResPick3Digits),
            Row2Pick4Digits = CopyDigitCollection(originalCard.ResPick4Digits),
            Row2NextPick3Digits = CopyDigitCollection(originalCard.ResNextPick3Digits),
            Row2CodingDigits = CopyDigitCollection(originalCard.ResCodingDigits),

            // Fila 3: Sin resultados
            Row3DateText = "Sin resultados",
            Row3DrawIcon = "",
            
            // Fila 4: Repetir resultado original
            Row4DateText = originalCard.ResDateText,
            Row4DrawIcon = originalCard.ResDrawIcon,
            Row4Pick3Digits = CopyDigitCollection(originalCard.ResPick3Digits),
            Row4Pick4Digits = CopyDigitCollection(originalCard.ResPick4Digits),
            Row4NextPick3Digits = CopyDigitCollection(originalCard.ResNextPick3Digits),
            Row4CodingDigits = CopyDigitCollection(originalCard.ResCodingDigits),

            // NUEVO: Contadores vac?os
            CodingMatchesRow1Row2 = "0C",
            CodingMatchesRow3Row4 = "0C",
            Pick4MatchesRow1Row2 = "0C",
            Pick4MatchesRow3Row4 = "0C",  

            AnalysisSummary = "No se encontraron resultados que cumplan con el filtro de posici?n"
        };
    }

    private static ObservableCollection<DigitVM> CopyDigitCollection(ObservableCollection<DigitVM> source)
    {
        var copy = new ObservableCollection<DigitVM>();
        foreach (var digit in source)
        {
            copy.Add(new DigitVM
            {
                Value = digit.Value,
                Bg = CreateFrozenBrush(Colors.White)  
            });
        }
        return copy;
    }

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        if (brush.CanFreeze)
        {
            brush.Freeze();
        }
        return brush;
    }

    private static ObservableCollection<ThirdAnalysisResultVM> ExecuteThirdAnalysis(
        string guidePick3, 
        string resultPick3,
        (char digit, int pick3Position, int pick4Position)? guidePositions,
        (char digit, int pick3Position, int pick4Position)? resultPositions)
    {
        var results = new ObservableCollection<ThirdAnalysisResultVM>();

        if (string.IsNullOrWhiteSpace(guidePick3) || guidePick3.Length != 3 ||
            string.IsNullOrWhiteSpace(resultPick3) || resultPick3.Length != 3)
        {
            return results;
        }

        try
        {
            // 1. Buscar coincidencias en la base de datos
            var matches = Data.DrawRepository.FindThirdAnalysisMatches(guidePick3, resultPick3, resultPick3);

            // 2. Procesar resultados con FILTRO ADICIONAL
            if (matches.Count == 0)
            {
                return results;
            }

            // 3. Aplicar filtros y crear resultados
            foreach (var match in matches)
            {
                // Obtener el Pick4 correspondiente
                var pick4Number = GetCorrespondingPick4(match.Date, match.DrawTime);
                
                // Si no hay Pick4, saltar este resultado
                if (string.IsNullOrWhiteSpace(pick4Number) || pick4Number == "----" || pick4Number.Length != 4)
                {
                    continue;
                }
                
                // APLICAR FILTRO: Solo 1 d?gito repetido entre Pick3 y Pick4
                var pick3Digits = match.Number.ToCharArray();
                var pick4Digits = pick4Number.ToCharArray();
                
                var commonDigits = pick3Digits.Intersect(pick4Digits).ToList();
                
                // Solo aceptar si hay EXACTAMENTE 1 d?gito repetido
                if (commonDigits.Count != 1)
                {
                    continue;
                }
                
                // Tambi?n verificar que dentro de cada Pick no haya d?gitos repetidos
                if (pick3Digits.Distinct().Count() != 3 || pick4Digits.Distinct().Count() != 4)
                {
                    continue;
                }

                // NUEVO FILTRO: Verificar posici?n del d?gito repetido
                char repeatedDigit = commonDigits[0];
                int pick3Position = Array.IndexOf(pick3Digits, repeatedDigit);
                int pick4Position = Array.IndexOf(pick4Digits, repeatedDigit);

                // Verificar si las posiciones coinciden con las de las dos primeras filas
                bool positionMatchesGuide = guidePositions.HasValue && 
                                           pick3Position == guidePositions.Value.pick3Position && 
                                           pick4Position == guidePositions.Value.pick4Position;
                
                bool positionMatchesResult = resultPositions.HasValue && 
                                            pick3Position == resultPositions.Value.pick3Position && 
                                            pick4Position == resultPositions.Value.pick4Position;

                // Solo aceptar si la posici?n del d?gito repetido coincide con AL MENOS UNA de las dos primeras filas
                if (!positionMatchesGuide && !positionMatchesResult)
                {
                    continue;
                }
                
                var resultVM = new ThirdAnalysisResultVM
                {
                    Pick3Number = match.Number,
                    Date = match.Date.ToString("yyyy-MM-dd"),
                    DrawTime = match.DrawTime,
                    Pick4Number = pick4Number,
                    NextPick3Number = Data.DrawRepository.GetNextPick3Number(match.Date, match.DrawTime) ?? "",
                    Coding = CalculateCoding(match.Number, pick4Number)
                };

                results.Add(resultVM);
            }
        }
        catch (Exception ex)
        {
            // En caso de error, devolver colecci?n vac?a
            Console.WriteLine($"Error en an?lisis: {ex.Message}");
        }

        return results;
    }

    private static string GetCorrespondingPick4(DateTime date, string drawTime)
    {
        try
        {
            var pick4Result = Data.DrawRepository.GetResult("pick4", date, drawTime);
            return pick4Result.Number ?? "----";
        }
        catch
        {
            return "----";
        }
    }

    private static string CalculateCoding(string pick3, string pick4)
    {
        if (string.IsNullOrWhiteSpace(pick3) || string.IsNullOrWhiteSpace(pick4))
            return "";

        try
        {
            return new string((pick3 + pick4)
                .Where(char.IsDigit)
                .Distinct()
                .OrderBy(c => c)
                .ToArray());
        }
        catch
        {
            return "";
        }
    }
}

public class ThirdAnalysisResultVM
{
    public string Pick3Number { get; set; } = "";
    public string Pick4Number { get; set; } = "";
    public string NextPick3Number { get; set; } = "";
    public string Coding { get; set; } = "";
    public string Date { get; set; } = "";
    public string DrawTime { get; set; } = "";
    public string Fireball { get; set; } = "";
}







