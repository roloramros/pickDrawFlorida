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

public partial class Analisis3ExtendidoWindow : Window
{
    public ObservableCollection<ThirdAnalysisCardVM> AnalysisCards { get; } = new();
    private readonly ExtendedAnalysis3Mode _analysisMode;

    public Analisis3ExtendidoWindow(IEnumerable<AnalysisPairCardVM> sourceCards, ExtendedAnalysis3Mode analysisMode)
    {
        InitializeComponent();
        DataContext = this;
        this.Loaded += (s, e) =>
        {
            // Centrar horizontalmente
            this.Left = (SystemParameters.PrimaryScreenWidth - this.ActualWidth) / 2;
            this.Top = 0;
        };
        _analysisMode = analysisMode;
        Title = analysisMode == ExtendedAnalysis3Mode.CrossLine
            ? "Analisis 3 Extendido - Cross Line"
            : "Analisis 3 Extendido - Single Line";

        foreach (var sourceCard in sourceCards)
        {
            var guidePositions = GetRepeatedDigitPositions(
                string.Join("", sourceCard.GuidePick3Digits.Select(d => d.Value)),
                string.Join("", sourceCard.GuidePick4Digits.Select(d => d.Value)));

            var resultPositions = GetRepeatedDigitPositions(
                string.Join("", sourceCard.ResPick3Digits.Select(d => d.Value)),
                string.Join("", sourceCard.ResPick4Digits.Select(d => d.Value)));

            var cards = ThirdAnalysisCardVM.CreateMultipleFrom(sourceCard, guidePositions, resultPositions);
            foreach (var card in cards)
            {
                if (MatchesExtendedFilter(card, _analysisMode))
                {
                    AnalysisCards.Add(card);
                }
            }
        }
    }

    private static bool MatchesExtendedFilter(ThirdAnalysisCardVM card, ExtendedAnalysis3Mode analysisMode)
    {
        return analysisMode switch
        {
            ExtendedAnalysis3Mode.CrossLine => MatchesExtendedCrossLineFilter(card),
            _ => MatchesExtendedOneLineFilter(card)
        };
    }

    private static bool MatchesExtendedOneLineFilter(ThirdAnalysisCardVM card)
    {
        int upperConnections = CountBlockConnections(
            card.Row1Pick3Digits,
            card.Row2Pick4Digits,
            card.Row2Pick3Digits,
            card.Row1Pick4Digits);

        int lowerConnections = CountBlockConnections(
            card.Row3Pick3Digits,
            card.Row4Pick4Digits,
            card.Row4Pick3Digits,
            card.Row3Pick4Digits);

        return upperConnections == 1 && (lowerConnections == 1 || lowerConnections == 0);
    }

    private static bool MatchesExtendedCrossLineFilter(ThirdAnalysisCardVM card)
    {
        var upperForwardConnections = GetPick3ToPick4Matches(card.Row1Pick3Digits, card.Row2Pick4Digits);
        var upperReverseConnections = GetPick3ToPick4Matches(card.Row2Pick3Digits, card.Row1Pick4Digits);
        int lowerConnections = CountBlockConnections(
            card.Row3Pick3Digits,
            card.Row4Pick4Digits,
            card.Row4Pick3Digits,
            card.Row3Pick4Digits);

        if (upperForwardConnections.Count != 1 || upperReverseConnections.Count != 1)
        {
            return false;
        }

        return LinesCross(upperForwardConnections[0], upperReverseConnections[0])
            && (lowerConnections == 1 || lowerConnections == 0);
    }

    private static int CountBlockConnections(
        ObservableCollection<DigitVM> forwardPick3Digits,
        ObservableCollection<DigitVM> forwardPick4Digits,
        ObservableCollection<DigitVM> reversePick3Digits,
        ObservableCollection<DigitVM> reversePick4Digits)
    {
        return GetPick3ToPick4Matches(forwardPick3Digits, forwardPick4Digits).Count
             + GetPick3ToPick4Matches(reversePick3Digits, reversePick4Digits).Count;
    }

    private static List<(int Pick3Position, int Pick4Position)> GetPick3ToPick4Matches(
        ObservableCollection<DigitVM> pick3Digits,
        ObservableCollection<DigitVM> pick4Digits)
    {
        var matches = new List<(int Pick3Position, int Pick4Position)>();

        if (pick3Digits.Count != 3 || pick4Digits.Count != 4)
        {
            return matches;
        }

        for (int i = 0; i < pick3Digits.Count; i++)
        {
            string pick3Value = pick3Digits[i].Value;
            if (string.IsNullOrWhiteSpace(pick3Value))
            {
                continue;
            }

            for (int j = 0; j < pick4Digits.Count; j++)
            {
                string pick4Value = pick4Digits[j].Value;
                if (string.IsNullOrWhiteSpace(pick4Value))
                {
                    continue;
                }

                if (pick3Value == pick4Value)
                {
                    matches.Add((i, j));
                }
            }
        }

        return matches;
    }

    private static bool LinesCross((int Pick3Position, int Pick4Position) forwardConnection,
                                   (int Pick3Position, int Pick4Position) reverseConnection)
    {
        var a1 = (X: (double)forwardConnection.Pick3Position, Y: 0d);
        var a2 = (X: 4d + forwardConnection.Pick4Position, Y: 1d);
        var b1 = (X: (double)reverseConnection.Pick3Position, Y: 1d);
        var b2 = (X: 4d + reverseConnection.Pick4Position, Y: 0d);

        double o1 = Cross(a1, a2, b1);
        double o2 = Cross(a1, a2, b2);
        double o3 = Cross(b1, b2, a1);
        double o4 = Cross(b1, b2, a2);

        return o1 * o2 < 0 && o3 * o4 < 0;
    }

    private static double Cross((double X, double Y) a, (double X, double Y) b, (double X, double Y) c)
    {
        return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    }

    private void analysisButton1(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not ThirdAnalysisCardVM selectedCard) return;

        if (button.Tag is AnalysisMode mode)
        {
            var thirdAnalysisOption1 = new ThirdAnalysisOption1(selectedCard, mode);
            thirdAnalysisOption1.Show();
        }
        else
        {
            var thirdAnalysisOption1 = new ThirdAnalysisOption1(selectedCard, AnalysisMode.Opcion1);
            thirdAnalysisOption1.Show();
        }
    }

    private void OpenAnalisis31MatchWindow(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not ThirdAnalysisCardVM selectedCard) return;

        var window = new Analisis_3_1_MatchWindow(selectedCard)
        {
            
        };
        window.Show();
    }

    private (char digit, int pick3Position, int pick4Position)? GetRepeatedDigitPositions(string pick3, string pick4)
    {
        if (string.IsNullOrWhiteSpace(pick3) || pick3.Length != 3 ||
            string.IsNullOrWhiteSpace(pick4) || pick4.Length != 4)
            return null;

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
            ConnectPick3Digits(root, "Row1Pick3Items", "Row2Pick3Items", "Pick3LinksCanvas");
            ConnectPick3Digits(root, "Row1NextPick3Items", "Row2NextPick3Items", "NextPick3LinksCanvas");

            var row3Pick3Items = FindVisualChild<ItemsControl>(root, "Row3Pick3Items");
            var row4Pick3Items = FindVisualChild<ItemsControl>(root, "Row4Pick3Items");
            var pick3Canvas = FindVisualChild<Canvas>(root, "Pick3LinksCanvas");

            if (row3Pick3Items != null && row4Pick3Items != null && pick3Canvas != null)
            {
                var row3Digits = GetDigitContainers(row3Pick3Items);
                var row4Digits = GetDigitContainers(row4Pick3Items);

                if (row3Digits.Count == 3 && row4Digits.Count == 3)
                {
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
                                DrawConnectingLine(pick3Canvas, topDigit, bottomDigit);
                            }
                        }
                    }
                }
            }

            var row3NextPick3Items = FindVisualChild<ItemsControl>(root, "Row3NextPick3Items");
            var row4NextPick3Items = FindVisualChild<ItemsControl>(root, "Row4NextPick3Items");
            var nextPick3Canvas = FindVisualChild<Canvas>(root, "NextPick3LinksCanvas");

            if (row3NextPick3Items != null && row4NextPick3Items != null && nextPick3Canvas != null)
            {
                var row3Digits = GetDigitContainers(row3NextPick3Items);
                var row4Digits = GetDigitContainers(row4NextPick3Items);

                if (row3Digits.Count == 3 && row4Digits.Count == 3)
                {
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
                                DrawConnectingLine(nextPick3Canvas, topDigit, bottomDigit);
                            }
                        }
                    }
                }
            }

            ConnectPick3ToPick4Digits(root, "Row3Pick3Items", "Row4Pick4Items", "Pick3Row3ToPick4Row4LinksCanvas");
            ConnectPick3ToPick4Digits(root, "Row4Pick3Items", "Row3Pick4Items", "Pick3Row4ToPick4Row3LinksCanvas");
            ConnectPick3ToPick4DigitsWithRedLine(root, "Row1Pick3Items", "Row2Pick4Items", "Pick3Row1ToPick4Row2LinksCanvas");
            ConnectPick3ToPick4DigitsWithRedLine(root, "Row2Pick3Items", "Row1Pick4Items", "Pick3Row2ToPick4Row1LinksCanvas");
        }), DispatcherPriority.Loaded);
    }

    private void ConnectPick3ToPick4Digits(FrameworkElement root, string pick3ItemsControlName, string pick4ItemsControlName, string canvasName)
    {
        var pick3ItemsControl = FindVisualChild<ItemsControl>(root, pick3ItemsControlName);
        var pick4ItemsControl = FindVisualChild<ItemsControl>(root, pick4ItemsControlName);
        var canvas = FindVisualChild<Canvas>(root, canvasName);

        if (pick3ItemsControl == null || pick4ItemsControl == null || canvas == null)
            return;

        var pick3Digits = GetDigitContainers(pick3ItemsControl);
        var pick4Digits = GetDigitContainers(pick4ItemsControl);

        if (pick3Digits.Count != 3 || pick4Digits.Count != 4)
            return;

        canvas.Children.Clear();

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
                    DrawConnectingLine(canvas, pick3Digit, pick4Digit);
                }
            }
        }
    }

    private void ConnectPick3ToPick4DigitsWithRedLine(FrameworkElement root, string pick3ItemsControlName, string pick4ItemsControlName, string canvasName)
    {
        var pick3ItemsControl = FindVisualChild<ItemsControl>(root, pick3ItemsControlName);
        var pick4ItemsControl = FindVisualChild<ItemsControl>(root, pick4ItemsControlName);
        var canvas = FindVisualChild<Canvas>(root, canvasName);

        if (pick3ItemsControl == null || pick4ItemsControl == null || canvas == null)
            return;

        var pick3Digits = GetDigitContainers(pick3ItemsControl);
        var pick4Digits = GetDigitContainers(pick4ItemsControl);

        if (pick3Digits.Count != 3 || pick4Digits.Count != 4)
            return;

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
                    DrawRedConnectingLine(canvas, pick3Digit, pick4Digit);
                }
            }
        }
    }

    private void DrawRedConnectingLine(Canvas canvas, Border element1, Border element2)
    {
        try
        {
            if (element1.ActualWidth <= 0 || element1.ActualHeight <= 0 ||
                element2.ActualWidth <= 0 || element2.ActualHeight <= 0)
                return;

            var center1 = element1.TranslatePoint(new Point(element1.ActualWidth / 2, element1.ActualHeight / 2), canvas);
            var center2 = element2.TranslatePoint(new Point(element2.ActualWidth / 2, element2.ActualHeight / 2), canvas);

            double dx = center2.X - center1.X;
            double dy = center2.Y - center1.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance == 0) return;

            double radius1 = element1.ActualWidth / 2;
            double radius2 = element2.ActualWidth / 2;
            double unitX = dx / distance;
            double unitY = dy / distance;

            Point startPoint = new(
                center1.X + (unitX * radius1),
                center1.Y + (unitY * radius1)
            );
            Point endPoint = new(
                center2.X - (unitX * radius2),
                center2.Y - (unitY * radius2)
            );

            var line = new Line
            {
                X1 = startPoint.X,
                Y1 = startPoint.Y,
                X2 = endPoint.X,
                Y2 = endPoint.Y,
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };
            canvas.Children.Add(line);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error dibujando línea roja: {ex.Message}");
        }
    }

    private void ConnectPick3Digits(FrameworkElement root, string topItemsControlName, string bottomItemsControlName, string canvasName)
    {
        var topItemsControl = FindVisualChild<ItemsControl>(root, topItemsControlName);
        var bottomItemsControl = FindVisualChild<ItemsControl>(root, bottomItemsControlName);
        var canvas = FindVisualChild<Canvas>(root, canvasName);

        if (topItemsControl == null || bottomItemsControl == null || canvas == null)
            return;

        var topDigits = GetDigitContainers(topItemsControl);
        var bottomDigits = GetDigitContainers(bottomItemsControl);

        if (topDigits.Count != 3 || bottomDigits.Count != 3)
            return;

        canvas.Children.Clear();

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
        var center1 = element1.TranslatePoint(new Point(element1.ActualWidth / 2, element1.ActualHeight / 2), canvas);
        var center2 = element2.TranslatePoint(new Point(element2.ActualWidth / 2, element2.ActualHeight / 2), canvas);

        double dx = center2.X - center1.X;
        double dy = center2.Y - center1.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance == 0) return;

        double radius1 = element1.ActualWidth / 2;
        double radius2 = element2.ActualWidth / 2;
        double unitX = dx / distance;
        double unitY = dy / distance;

        Point startPoint = new(
            center1.X + (unitX * radius1),
            center1.Y + (unitY * radius1)
        );
        Point endPoint = new(
            center2.X - (unitX * radius2),
            center2.Y - (unitY * radius2)
        );

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

        return null;
    }
}












