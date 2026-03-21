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

public partial class Analisis_3_1_MatchWindow : Window
{
    public ThirdAnalysisCardVM GuideCard { get; }
    public ThirdAnalysisCardVM ResultCard { get; }

    public Analisis_3_1_MatchWindow(ThirdAnalysisCardVM selectedCard)
    {
        InitializeComponent();
        GuideCard = CloneCard(selectedCard);
        ResultCard = BuildFixedResultCard();
        DataContext = this;
    }

    private static ThirdAnalysisCardVM CloneCard(ThirdAnalysisCardVM source)
    {
        return new ThirdAnalysisCardVM
        {
            Row1Pick3Digits = CopyDigits(source.Row1Pick3Digits),
            Row1Pick4Digits = CopyDigits(source.Row1Pick4Digits),
            Row1NextPick3Digits = CopyDigits(source.Row1NextPick3Digits),
            Row1CodingDigits = CopyDigits(source.Row1CodingDigits),
            Row1DateText = source.Row1DateText,
            Row1DrawIcon = source.Row1DrawIcon,
            Row2Pick3Digits = CopyDigits(source.Row2Pick3Digits),
            Row2Pick4Digits = CopyDigits(source.Row2Pick4Digits),
            Row2NextPick3Digits = CopyDigits(source.Row2NextPick3Digits),
            Row2CodingDigits = CopyDigits(source.Row2CodingDigits),
            Row2DateText = source.Row2DateText,
            Row2DrawIcon = source.Row2DrawIcon,
            Row3Pick3Digits = CopyDigits(source.Row3Pick3Digits),
            Row3Pick4Digits = CopyDigits(source.Row3Pick4Digits),
            Row3NextPick3Digits = CopyDigits(source.Row3NextPick3Digits),
            Row3CodingDigits = CopyDigits(source.Row3CodingDigits),
            Row3DateText = source.Row3DateText,
            Row3DrawIcon = source.Row3DrawIcon,
            Row4Pick3Digits = CopyDigits(source.Row4Pick3Digits),
            Row4Pick4Digits = CopyDigits(source.Row4Pick4Digits),
            Row4NextPick3Digits = CopyDigits(source.Row4NextPick3Digits),
            Row4CodingDigits = CopyDigits(source.Row4CodingDigits),
            Row4DateText = source.Row4DateText,
            Row4DrawIcon = source.Row4DrawIcon,
            Pick4MatchesRow1Row2 = source.Pick4MatchesRow1Row2,
            CodingMatchesRow1Row2 = source.CodingMatchesRow1Row2,
            Pick4MatchesRow3Row4 = source.Pick4MatchesRow3Row4,
            CodingMatchesRow3Row4 = source.CodingMatchesRow3Row4,
            AnalysisSummary = source.AnalysisSummary
        };
    }

    private static ObservableCollection<DigitVM> CopyDigits(IEnumerable<DigitVM> digits)
    {
        return new ObservableCollection<DigitVM>(
            digits.Select(d => new DigitVM
            {
                Value = d.Value,
                Bg = d.Bg
            }));
    }

    private static ThirdAnalysisCardVM BuildFixedResultCard()
    {
        return new ThirdAnalysisCardVM
        {
            Row1DateText = "2011-05-06",
            Row1DrawIcon = "?",
            Row1Pick3Digits = Digits("230"),
            Row1Pick4Digits = Digits("5426", "5", Brushes.LightBlue),
            Row1NextPick3Digits = Digits("277"),
            Row1CodingDigits = Digits("023456", "0356", Brushes.LightPink),

            Row2DateText = "2005-11-06",
            Row2DrawIcon = "?",
            Row2Pick3Digits = Digits("860"),
            Row2Pick4Digits = Digits("5387", "5", Brushes.LightBlue),
            Row2NextPick3Digits = Digits("815"),
            Row2CodingDigits = Digits("035678", "0356", Brushes.LightPink),

            Row3DateText = "2026-03-13",
            Row3DrawIcon = "?",
            Row3Pick3Digits = Digits("450"),
            Row3Pick4Digits = Digits("1943", "3", Brushes.LightBlue),
            Row3NextPick3Digits = Digits("334"),
            Row3CodingDigits = Digits("013459", "035", Brushes.LightPink),

            Row4DateText = "2005-11-06",
            Row4DrawIcon = "?",
            Row4Pick3Digits = Digits("860", "8", Brushes.LightBlue),
            Row4Pick4Digits = Digits("5387", "38", Brushes.LightBlue),
            Row4NextPick3Digits = Digits("815", "8", Brushes.LightBlue),
            Row4CodingDigits = Digits("035678", "035", Brushes.LightPink),

            Pick4MatchesRow1Row2 = "1C",
            CodingMatchesRow1Row2 = "4C",
            Pick4MatchesRow3Row4 = "1C",
            CodingMatchesRow3Row4 = "3C",
            AnalysisSummary = "Resultado fijo"
        };
    }

    private static ObservableCollection<DigitVM> Digits(string value, string highlighted = "", Brush? highlightBrush = null)
    {
        highlightBrush ??= Brushes.Transparent;
        var highlightedSet = new HashSet<char>(highlighted ?? string.Empty);
        var result = new ObservableCollection<DigitVM>();

        foreach (char ch in value)
        {
            result.Add(new DigitVM
            {
                Value = ch.ToString(),
                Bg = highlightedSet.Contains(ch) ? highlightBrush : Brushes.White
            });
        }

        return result;
    }

    private void AnalysisCard_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement root)
        {
            return;
        }

        root.Dispatcher.BeginInvoke(new Action(() =>
        {
            ConnectPick3Digits(root, "Row1Pick3Items", "Row2Pick3Items", "Pick3LinksCanvas");
            ConnectPick3Digits(root, "Row1NextPick3Items", "Row2NextPick3Items", "NextPick3LinksCanvas");
            ConnectPick3Digits(root, "Row3Pick3Items", "Row4Pick3Items", "Pick3LinksCanvas", false);
            ConnectPick3Digits(root, "Row3NextPick3Items", "Row4NextPick3Items", "NextPick3LinksCanvas", false);
            ConnectPick3ToPick4Digits(root, "Row3Pick3Items", "Row4Pick4Items", "Pick3Row3ToPick4Row4LinksCanvas");
            ConnectPick3ToPick4Digits(root, "Row4Pick3Items", "Row3Pick4Items", "Pick3Row4ToPick4Row3LinksCanvas");
            ConnectPick3ToPick4DigitsWithRedLine(root, "Row1Pick3Items", "Row2Pick4Items", "Pick3Row1ToPick4Row2LinksCanvas");
            ConnectPick3ToPick4DigitsWithRedLine(root, "Row2Pick3Items", "Row1Pick4Items", "Pick3Row2ToPick4Row1LinksCanvas");
        }), DispatcherPriority.Loaded);
    }

    private void ConnectPick3Digits(FrameworkElement root, string topItemsControlName, string bottomItemsControlName, string canvasName, bool clearCanvas = true)
    {
        var topItemsControl = FindVisualChild<ItemsControl>(root, topItemsControlName);
        var bottomItemsControl = FindVisualChild<ItemsControl>(root, bottomItemsControlName);
        var canvas = FindVisualChild<Canvas>(root, canvasName);

        if (topItemsControl == null || bottomItemsControl == null || canvas == null)
        {
            return;
        }

        var topDigits = GetDigitContainers(topItemsControl);
        var bottomDigits = GetDigitContainers(bottomItemsControl);

        if (topDigits.Count != 3 || bottomDigits.Count != 3)
        {
            return;
        }

        if (clearCanvas)
        {
            canvas.Children.Clear();
        }

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
                    DrawConnectingLine(canvas, topDigit, bottomDigit, Brushes.Black);
                }
            }
        }
    }

    private void ConnectPick3ToPick4Digits(FrameworkElement root, string pick3ItemsControlName, string pick4ItemsControlName, string canvasName)
    {
        var pick3ItemsControl = FindVisualChild<ItemsControl>(root, pick3ItemsControlName);
        var pick4ItemsControl = FindVisualChild<ItemsControl>(root, pick4ItemsControlName);
        var canvas = FindVisualChild<Canvas>(root, canvasName);

        if (pick3ItemsControl == null || pick4ItemsControl == null || canvas == null)
        {
            return;
        }

        var pick3Digits = GetDigitContainers(pick3ItemsControl);
        var pick4Digits = GetDigitContainers(pick4ItemsControl);

        if (pick3Digits.Count != 3 || pick4Digits.Count != 4)
        {
            return;
        }

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
                    DrawConnectingLine(canvas, pick3Digit, pick4Digit, Brushes.Black);
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
        {
            return;
        }

        var pick3Digits = GetDigitContainers(pick3ItemsControl);
        var pick4Digits = GetDigitContainers(pick4ItemsControl);

        if (pick3Digits.Count != 3 || pick4Digits.Count != 4)
        {
            return;
        }

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
                    DrawConnectingLine(canvas, pick3Digit, pick4Digit, Brushes.Red);
                }
            }
        }
    }

    private static List<Border> GetDigitContainers(ItemsControl itemsControl)
    {
        var containers = new List<Border>();

        for (int i = 0; i < itemsControl.Items.Count; i++)
        {
            if (itemsControl.ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter container)
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

    private static string GetDigitText(Border digitBorder)
    {
        var textBlock = FindVisualChild<TextBlock>(digitBorder);
        return textBlock?.Text ?? string.Empty;
    }

    private static void DrawConnectingLine(Canvas canvas, Border element1, Border element2, Brush stroke)
    {
        if (element1.ActualWidth <= 0 || element1.ActualHeight <= 0 || element2.ActualWidth <= 0 || element2.ActualHeight <= 0)
        {
            return;
        }

        var center1 = element1.TranslatePoint(new Point(element1.ActualWidth / 2, element1.ActualHeight / 2), canvas);
        var center2 = element2.TranslatePoint(new Point(element2.ActualWidth / 2, element2.ActualHeight / 2), canvas);
        double dx = center2.X - center1.X;
        double dy = center2.Y - center1.Y;
        double distance = Math.Sqrt((dx * dx) + (dy * dy));

        if (distance == 0)
        {
            return;
        }

        double radius1 = element1.ActualWidth / 2;
        double radius2 = element2.ActualWidth / 2;
        double unitX = dx / distance;
        double unitY = dy / distance;

        var line = new Line
        {
            X1 = center1.X + (unitX * radius1),
            Y1 = center1.Y + (unitY * radius1),
            X2 = center2.X - (unitX * radius2),
            Y2 = center2.Y - (unitY * radius2),
            Stroke = stroke,
            StrokeThickness = 2
        };

        canvas.Children.Add(line);
    }

    private static T? FindVisualChild<T>(DependencyObject parent, string? childName = null) where T : DependencyObject
    {
        if (parent == null)
        {
            return null;
        }

        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is FrameworkElement frameworkElement && !string.IsNullOrEmpty(childName) && frameworkElement.Name == childName)
            {
                return child as T;
            }

            if (child is T result && (string.IsNullOrEmpty(childName) || childName == result.GetValue(FrameworkElement.NameProperty) as string))
            {
                return result;
            }

            var foundChild = FindVisualChild<T>(child, childName);
            if (foundChild != null)
            {
                return foundChild;
            }
        }

        return null;
    }
}
