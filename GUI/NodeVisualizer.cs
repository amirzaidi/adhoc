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
        private const double GRID_PANEL_CENTER_X = GRID_PANEL_SIZE / 2.0;
        private const double GRID_PANEL_CENTER_Y = 15;
        private const int ROW_PANEL_COUNT = 10;

        // Two-way linking for fast lookup.
        private readonly Dictionary<UIElement, T> mNodes = new Dictionary<UIElement, T>();
        private readonly Dictionary<T, UIElement> mNodeUIElements = new Dictionary<T, UIElement>();

        private readonly Dictionary<Line, (T, T)> mLines = new Dictionary<Line, (T, T)>();

        private readonly Window mWindow;
        private readonly UIElementCollection mUIElements;
        private readonly Action<T, double, double> mOnNodeMoved;

        private bool mIsDragging;
        private Point mClickPosition;

        public NodeVisualizer(Window Window, UIElementCollection UIElements, Action<T, double, double> OnNodeMoved)
        {
            mWindow = Window;
            mUIElements = UIElements;
            mOnNodeMoved = OnNodeMoved;
        }

        private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mIsDragging = true;
            mClickPosition = e.GetPosition(mWindow);

            var draggableControl = sender as UIElement;
            var transform = draggableControl.RenderTransform as TranslateTransform;
            mClickPosition.X -= transform.X;
            mClickPosition.Y -= transform.Y;
            draggableControl.CaptureMouse();
        }

        private void Control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mIsDragging = false;
            mClickPosition = default;

            var draggableControl = sender as UIElement;
            draggableControl.ReleaseMouseCapture();
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            var draggableControl = sender as UIElement;
            if (mIsDragging && draggableControl != null)
            {
                var draggedNode = mNodes[draggableControl];
                var currentPosition = e.GetPosition(mWindow);
                var transform = draggableControl.RenderTransform as TranslateTransform;

                transform.X = currentPosition.X - mClickPosition.X;
                transform.Y = currentPosition.Y - mClickPosition.Y;

                foreach (var line in mLines)
                {
                    var (n1, n2) = line.Value;
                    if (Equals(draggedNode, n1))
                    {
                        // Update X1, Y1
                        line.Key.X1 = transform.X + GRID_PANEL_CENTER_X;
                        line.Key.Y1 = transform.Y + GRID_PANEL_CENTER_Y;
                    }
                    else if (Equals(draggedNode, n2))
                    {
                        // Update X2, Y2
                        line.Key.X2 = transform.X + GRID_PANEL_CENTER_X;
                        line.Key.Y2 = transform.Y + GRID_PANEL_CENTER_Y;
                    }
                }

                mOnNodeMoved(mNodes[draggableControl], transform.X + GRID_PANEL_CENTER_X, transform.Y + GRID_PANEL_CENTER_Y);
            }
        }

        public void ResetNodes(List<T> Nodes)
        {
            ClearUI();
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
                mNodeUIElements[node] = panel;

                // Add to UI.
                mUIElements.Add(panel);
                Panel.SetZIndex(panel, 1);

                // Immediately trigger the move event.
                mOnNodeMoved(node, transform.X + GRID_PANEL_CENTER_X, transform.Y + GRID_PANEL_CENTER_Y);
            }

            // To-Do: Remove this placeholder.
            if (Nodes.Count >= 3)
            {
                AddLine(Nodes[0], Nodes[1]);
                AddLine(Nodes[1], Nodes[2]);
                AddLine(Nodes[2], Nodes[0]);
            }
        }

        private void AddLine(T n1, T n2)
        {
            var e1 = mNodeUIElements[n1];
            var t1 = e1.RenderTransform as TranslateTransform;

            var e2 = mNodeUIElements[n2];
            var t2 = e2.RenderTransform as TranslateTransform;

            var line = new Line
            {
                X1 = t1.X + GRID_PANEL_CENTER_X,
                Y1 = t1.Y + GRID_PANEL_CENTER_Y,
                X2 = t2.X + GRID_PANEL_CENTER_X,
                Y2 = t2.Y + GRID_PANEL_CENTER_Y,
                Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 200)),
                StrokeThickness = 2,
            };

            mLines.Add(line, (n1, n2));

            // Add to UI.
            mUIElements.Add(line);
            Panel.SetZIndex(line, 0);
        }

        private void ClearUI()
        {
            // Remove everything from the UI then clear lists.
            foreach (var el in mNodeUIElements.Values)
            {
                mUIElements.Remove(el);
            }

            mNodes.Clear();
            mNodeUIElements.Clear();

            foreach (var line in mLines.Keys)
            {
                mUIElements.Remove(line);
            }

            mLines.Clear();
        }
    }
}
