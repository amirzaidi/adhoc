using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AdHocMAC.GUI
{
    static class UIElementExtensions
    {
        public static TranslateTransform SetTransform(this UIElement UIElement, double X, double Y)
        {
            var transform = new TranslateTransform
            {
                X = X,
                Y = Y
            };

            UIElement.RenderTransform = transform;

            return transform;
        }

        public static TranslateTransform UpdateTransform(this UIElement UIElement, double X, double Y)
        {
            var transform = UIElement.RenderTransform as TranslateTransform;

            transform.X = X;
            transform.Y = Y;

            return transform;
        }

        public static void Remove(this UIElementCollection UIElements, params UIElement[] ToRemove)
        {
            foreach (var element in ToRemove)
            {
                UIElements.Remove(element);
            }
        }
    }
}
