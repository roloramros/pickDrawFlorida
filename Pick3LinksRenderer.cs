using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FloridaLotteryApp;

internal static class Pick3LinksRenderer
{
    public static void DrawPick3Links(FrameworkElement cardRoot)
    {
        if (cardRoot.DataContext is not AnalysisPairCardVM viewModel)
        {
            return;
        }

        if (cardRoot.FindName("Pick3LinksCanvas") is not Canvas canvas)
        {
            return;
        }

        if (cardRoot.FindName("GuidePick3Items") is not ItemsControl guideItems)
        {
            return;
        }

        if (cardRoot.FindName("ResPick3Items") is not ItemsControl resultItems)
        {
            return;
        }

        canvas.Width = cardRoot.ActualWidth;
        canvas.Height = cardRoot.ActualHeight;
        canvas.Children.Clear();

        DrawMatchingLinks(canvas, guideItems, resultItems, viewModel.GuidePick3Digits, viewModel.ResPick3Digits);

        if (cardRoot.FindName("GuideNextPick3Items") is ItemsControl guideNextItems
            && cardRoot.FindName("ResNextPick3Items") is ItemsControl resultNextItems)
        {
            DrawMatchingLinks(canvas, guideNextItems, resultNextItems, viewModel.GuideNextPick3Digits, viewModel.ResNextPick3Digits);
        }
    }

    private static void DrawMatchingLinks(
        Canvas canvas,
        ItemsControl guideItems,
        ItemsControl resultItems,
        IReadOnlyList<DigitVM> guideDigits,
        IReadOnlyList<DigitVM> resultDigits)
    {
        for (int i = 0; i < guideDigits.Count; i++)
        {
            var guideDigit = guideDigits[i].Value;
            if (string.IsNullOrWhiteSpace(guideDigit))
            {
                continue;
            }

            for (int j = 0; j < resultDigits.Count; j++)
            {
                var resultDigit = resultDigits[j].Value;
                if (!string.Equals(guideDigit, resultDigit, StringComparison.Ordinal))
                {
                    continue;
                }

                var guideElement = GetDigitElement(guideItems, i);
                var resultElement = GetDigitElement(resultItems, j);
                if (guideElement == null || resultElement == null)
                {
                    continue;
                }

                var start = GetCenterPoint(guideElement, canvas);
                var end = GetCenterPoint(resultElement, canvas);
                var startRadius = GetRadius(guideElement);
                var endRadius = GetRadius(resultElement);
                var (startEdge, endEdge) = GetEdgePoints(start, end, startRadius, endRadius);

                var line = new Line
                {
                    X1 = startEdge.X,
                    Y1 = startEdge.Y,
                    X2 = endEdge.X,
                    Y2 = endEdge.Y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 5,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };

                canvas.Children.Add(line);
            }
        }
    }

    private static FrameworkElement? GetDigitElement(ItemsControl itemsControl, int index)
    {
        if (itemsControl.ItemContainerGenerator.ContainerFromIndex(index) is not FrameworkElement container)
        {
            return null;
        }

        return FindVisualChild<Border>(container);
    }

    private static double GetRadius(FrameworkElement element)
    {
        return Math.Min(element.ActualWidth, element.ActualHeight) / 2;
    }

    private static (Point Start, Point End) GetEdgePoints(Point start, Point end, double startRadius, double endRadius)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt((dx * dx) + (dy * dy));
        if (length <= double.Epsilon)
        {
            return (start, end);
        }

        var unitX = dx / length;
        var unitY = dy / length;
        var startEdge = new Point(start.X + (unitX * startRadius), start.Y + (unitY * startRadius));
        var endEdge = new Point(end.X - (unitX * endRadius), end.Y - (unitY * endRadius));

        return (startEdge, endEdge);
    }

    private static Point GetCenterPoint(FrameworkElement element, Visual relativeTo)
    {
        var transform = element.TransformToVisual(relativeTo);
        return transform.Transform(new Point(element.ActualWidth / 2, element.ActualHeight / 2));
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var result = FindVisualChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
} 