using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AdHocMAC.GUI
{
    class UIElementCreator
    {
        public static Ellipse CreateCircle(double Radius, Color Outline, Color Fill,
            HorizontalAlignment HorizontalAlign = HorizontalAlignment.Left,
            VerticalAlignment VerticalAlign = VerticalAlignment.Top)
        {
            return new Ellipse
            {
                Width = Radius,
                Height = Radius,
                HorizontalAlignment = HorizontalAlign,
                VerticalAlignment = VerticalAlign,
                Stroke = new SolidColorBrush(Outline),
                Fill = new SolidColorBrush(Fill),
            };
        }
    }
}
