using AdHocMAC.Nodes;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AdHocMAC.GUI
{
    /// <summary>
    /// This will be a helper class that will draw all the nodes in the UI.
    /// </summary>
    class NodeVisualizer<T>
    {
        private readonly Dictionary<T, Ellipse> mNodes = new Dictionary<T, Ellipse>();
        private readonly Window mWindow;
        private readonly UIElementCollection mParent;

        private bool isDragging;
        private Point clickPosition;

        public NodeVisualizer(Window Window, UIElementCollection Parent)
        {
            mWindow = Window;
            mParent = Parent;
        }

        private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            clickPosition = e.GetPosition(mWindow);

            var draggableControl = sender as UIElement;
            var transform = draggableControl.RenderTransform as TranslateTransform;
            if (transform != null)
            {
                clickPosition.X -= transform.X;
                clickPosition.Y -= transform.Y;
            }
            draggableControl.CaptureMouse();
        }

        private void Control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            clickPosition = default;

            var draggableControl = sender as UIElement;
            draggableControl.ReleaseMouseCapture();
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            var draggableControl = sender as UIElement;
            if (isDragging && draggableControl != null)
            {
                var currentPosition = e.GetPosition(mWindow);
                var transform = draggableControl.RenderTransform as TranslateTransform;
                if (transform == null)
                {
                    transform = new TranslateTransform();
                    draggableControl.RenderTransform = transform;
                }

                transform.X = currentPosition.X - clickPosition.X;
                transform.Y = currentPosition.Y - clickPosition.Y;
            }
        }

        public void ResetNodes(List<T> Nodes)
        {
            CleanNodes();
            AddNodes(Nodes);
        }

        private void AddNodes(List<T> Nodes)
        {
            foreach (var node in Nodes)
            {
                var el = new Ellipse();
                el.MouseLeftButtonDown += Control_MouseLeftButtonDown;
                el.MouseLeftButtonUp += Control_MouseLeftButtonUp;
                el.MouseMove += Control_MouseMove;
                el.HorizontalAlignment = HorizontalAlignment.Left;
                el.VerticalAlignment = VerticalAlignment.Top;
                el.Width = 50;
                el.Height = 50;
                el.Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                el.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

                mNodes[node] = el;
                mParent.Add(el);
            }
        }

        private void CleanNodes()
        {
            foreach (var el in mNodes.Values)
            {
                mParent.Remove(el);
            }

            mNodes.Clear();
        }
    }
}
