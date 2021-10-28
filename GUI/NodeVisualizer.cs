using AdHocMAC.Nodes;
using System;
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
        private const int GRID_MARGIN = 20;
        private const int GRID_PANEL_SIZE = 70;
        private const double GRID_PANEL_CENTER = GRID_PANEL_SIZE / 2.0;
        private const int ROW_PANEL_COUNT = 10;

        private readonly Dictionary<UIElement, T> mNodes = new Dictionary<UIElement, T>();
        private readonly Dictionary<T, UIElement> mElements = new Dictionary<T, UIElement>();

        private readonly Window mWindow;
        private readonly UIElementCollection mParent;
        private readonly Action<T, double, double> mOnNodeMoved;

        private bool isDragging;
        private Point clickPosition;

        public NodeVisualizer(Window Window, UIElementCollection Parent, Action<T, double, double> OnNodeMoved)
        {
            mWindow = Window;
            mParent = Parent;
            mOnNodeMoved = OnNodeMoved;
        }

        private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            clickPosition = e.GetPosition(mWindow);

            var draggableControl = sender as UIElement;
            var transform = draggableControl.RenderTransform as TranslateTransform;
            clickPosition.X -= transform.X;
            clickPosition.Y -= transform.Y;
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

                transform.X = currentPosition.X - clickPosition.X;
                transform.Y = currentPosition.Y - clickPosition.Y;

                mOnNodeMoved(mNodes[draggableControl], transform.X + GRID_PANEL_CENTER, transform.Y + GRID_PANEL_CENTER);
            }
        }

        public void ResetNodes(List<T> Nodes)
        {
            CleanNodes();
            AddNodes(Nodes);
        }

        private void AddNodes(List<T> Nodes)
        {
            int elementNumber = 0;
            foreach (var node in Nodes)
            {
                var panel = new StackPanel
                {
                    Width = GRID_PANEL_SIZE,
                    Height = GRID_PANEL_SIZE,
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                };

                // Handle movement of the entire node.
                panel.MouseLeftButtonDown += Control_MouseLeftButtonDown;
                panel.MouseLeftButtonUp += Control_MouseLeftButtonUp;
                panel.MouseMove += Control_MouseMove;

                int elementId = elementNumber++;
                var transform = new TranslateTransform
                {
                    X = GRID_MARGIN + GRID_PANEL_SIZE * (elementId % ROW_PANEL_COUNT),
                    Y = GRID_MARGIN + GRID_PANEL_SIZE * (elementId / ROW_PANEL_COUNT),
                };

                panel.RenderTransform = transform;

                panel.Children.Add(new Ellipse
                {
                    Width = 30,
                    Height = 30,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)),
                    Fill = new SolidColorBrush(Color.FromArgb(255, 255, 127, 0)),
                });

                panel.Children.Add(new Label
                {
                    Content = "Node #" + elementNumber,
                    Padding = new Thickness(0, 5, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                });

                // Double link.
                mNodes[panel] = node;
                mElements[node] = panel;

                // Add to UI.
                mParent.Add(panel);

                // Immediately trigger the move event.
                mOnNodeMoved(node, transform.X + GRID_PANEL_CENTER, transform.Y + GRID_PANEL_CENTER);
            }
        }

        private void CleanNodes()
        {
            foreach (var el in mElements.Values)
            {
                // Remove from UI.
                mParent.Remove(el);
            }

            mNodes.Clear();
            mElements.Clear();
        }
    }
}
