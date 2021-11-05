using AdHocMAC.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private const double GRID_PANEL_CENTER_Y = 15.0;
        private const int ROW_PANEL_COUNT = 10;
        private const double NODE_CIRCLE_DIAMETER = 30.0;
        private const double BLOB_CIRCLE_DIAMETER = NODE_CIRCLE_DIAMETER / 3.0;

        // Two-way linking for fast lookup.
        private readonly Dictionary<UIElement, T> mNodes = new Dictionary<UIElement, T>();
        private readonly Dictionary<T, (UIElement, Ellipse, Label)> mNodeUIElements = new Dictionary<T, (UIElement, Ellipse, Label)>();

        private readonly Dictionary<(T, T), (Line, Ellipse, Ellipse)> mLines = new Dictionary<(T, T), (Line, Ellipse, Ellipse)>();

        private readonly Window mWindow;
        private readonly UIElementCollection mUIElements;
        private readonly Action<T, double, double> mOnNodeMoved;
        private readonly Func<T, int> mGetNodeID;

        // This breaks when the object is not created on the UI thread!
        private readonly SynchronizationContext mContext;

        private bool mIsDragging;
        private Point mClickPosition;

        public NodeVisualizer(Window Window, UIElementCollection UIElements,
            Action<T, double, double> OnNodeMoved, Func<T, int> GetNodeID)
        {
            mWindow = Window;
            mUIElements = UIElements;
            mOnNodeMoved = OnNodeMoved;
            mGetNodeID = GetNodeID;

            mContext = SynchronizationContext.Current;
        }

        public void RunOnUIThread(Action Runnable)
        {
            mContext.Post(_ => Runnable(), null);
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
                    var (n1, n2) = line.Key;
                    var (lineObj, blob1, blob2) = line.Value;

                    if (Equals(draggedNode, n1))
                    {
                        // Update X1, Y1
                        lineObj.X1 = transform.X + GRID_PANEL_CENTER_X;
                        lineObj.Y1 = transform.Y + GRID_PANEL_CENTER_Y;
                    }
                    else if (Equals(draggedNode, n2))
                    {
                        // Update X2, Y2
                        lineObj.X2 = transform.X + GRID_PANEL_CENTER_X;
                        lineObj.Y2 = transform.Y + GRID_PANEL_CENTER_Y;
                    }

                    UpdateBlobPositioning(lineObj, blob1, blob2);
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

                int elementId = mGetNodeID(node);
                var transform = new TranslateTransform
                {
                    X = GRID_MARGIN + GRID_PANEL_SIZE * (elementId % ROW_PANEL_COUNT),
                    Y = GRID_MARGIN + GRID_PANEL_SIZE * (elementId / ROW_PANEL_COUNT),
                };
                panel.RenderTransform = transform;
                var c = UIElementCreator.CreateCircle(
                    NODE_CIRCLE_DIAMETER,
                    Color.FromArgb(255, 0, 0, 0),
                    Color.FromArgb(255, 0, 0, 0),
                    HorizontalAlignment.Center
                );
                panel.Children.Add(c);

                var l = new Label
                {
                    Content = elementId,
                    Padding = new Thickness(0, 5, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                };
                panel.Children.Add(l);

                // Double link.
                mNodes[panel] = node;
                mNodeUIElements[node] = (panel, c, l);

                // Add to UI.
                mUIElements.Add(panel);
                Panel.SetZIndex(panel, 1);

                // Immediately trigger the move event.
                mOnNodeMoved(node, transform.X + GRID_PANEL_CENTER_X, transform.Y + GRID_PANEL_CENTER_Y);
            }
        }

        /// <summary>
        /// Returns true if n1 and n2 have been swapped, false otherwise.
        /// </summary>
        private bool OrderNodes(ref T n1, ref T n2)
        {
            var nId1 = mGetNodeID(n1);
            var nId2 = mGetNodeID(n2);

            if (nId1 == nId2)
            {
                throw new ArgumentException("Cannot connect the same two nodes.");
            }

            if (nId1 > nId2)
            {
                var nTemp = n2;
                n2 = n1;
                n1 = nTemp;

                return true;
            }

            return false;
        }

        // We need to remove and add lines during runtime because rendering 900 lines all the time is a bad idea.
        public void ConnectNodes(T n1, T n2)
        {
            OrderNodes(ref n1, ref n2);

            var e1 = mNodeUIElements[n1].Item1;
            var t1 = e1.RenderTransform as TranslateTransform;

            var e2 = mNodeUIElements[n2].Item1;
            var t2 = e2.RenderTransform as TranslateTransform;

            var line = new Line
            {
                X1 = t1.X + GRID_PANEL_CENTER_X,
                Y1 = t1.Y + GRID_PANEL_CENTER_Y,
                X2 = t2.X + GRID_PANEL_CENTER_X,
                Y2 = t2.Y + GRID_PANEL_CENTER_Y,
                Stroke = new SolidColorBrush(Color.FromArgb(255, 127, 127, 255)),
                StrokeThickness = 2,
            };

            // Add to UI.
            mUIElements.Add(line);
            Panel.SetZIndex(line, 0);

            Ellipse createAndAddBlob()
            {
                var blob = UIElementCreator.CreateCircle(
                    BLOB_CIRCLE_DIAMETER,
                    Color.FromArgb(255, 0, 0, 0),
                    Color.FromArgb(255, 0, 0, 0)
                );

                // Add to UI.
                Panel.SetZIndex(blob, 1);
                mUIElements.Add(blob);

                return blob;
            };

            var blob1 = createAndAddBlob();
            var blob2 = createAndAddBlob();

            UpdateBlobPositioning(line, blob1, blob2);
            mLines.Add((n1, n2), (line, blob1, blob2));
        }

        public void UpdateBlobPositioning(Line Line, Ellipse Blob1, Ellipse Blob2)
        {
            const double NODE_CIRCLE_RADIUS = NODE_CIRCLE_DIAMETER / 2.0;
            const double BLOB_CIRCLE_RADIUS = BLOB_CIRCLE_DIAMETER / 2.0;

            var dir = new Vector2D
            {
                X = Line.X2 - Line.X1,
                Y = Line.Y2 - Line.Y1
            }.Normalize();

            Blob1.SetTransform(
                Line.X1 - BLOB_CIRCLE_RADIUS + dir.X * NODE_CIRCLE_RADIUS,
                Line.Y1 - BLOB_CIRCLE_RADIUS + dir.Y * NODE_CIRCLE_RADIUS
            );

            Blob2.SetTransform(
                Line.X2 - BLOB_CIRCLE_RADIUS - dir.X * NODE_CIRCLE_RADIUS,
                Line.Y2 - BLOB_CIRCLE_RADIUS - dir.Y * NODE_CIRCLE_RADIUS
            );
        }

        // These three methods are not synchronized and can be invoked on non-existent nodes.
        // However, they are always running on the UI thread.
        public void ChangeNodeColor(T n, byte r, byte g, byte b, byte a = 255)
        {
            if (mNodeUIElements.TryGetValue(n, out (UIElement, Ellipse, Label) value))
            {
                var c = value.Item2;
                c.Fill = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }
        }

        public void ChangeNodeText(T n, string Text)
        {
            if (mNodeUIElements.TryGetValue(n, out (UIElement, Ellipse, Label) value))
            {
                var l = value.Item3;
                l.Content = $"{mGetNodeID(n)}: {Text}";
            }
        }

        public void ChangeLineColor(T n1, T n2, byte r, byte g, byte b, byte a = 255)
        {
            OrderNodes(ref n1, ref n2);
            if (mLines.TryGetValue((n1, n2), out var value))
            {
                var line = value.Item1;
                line.Stroke = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }
        }

        public void ChangeBlobColor(T n1, T n2, byte r, byte g, byte b, byte a = 255)
        {
            var swapped = OrderNodes(ref n1, ref n2);
            if (mLines.TryGetValue((n1, n2), out var value))
            {
                var blob = swapped ? value.Item3 : value.Item2;
                blob.Fill = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }
        }

        public void DisconnectNodes(T n1, T n2)
        {
            OrderNodes(ref n1, ref n2);
            var lineNodes = (n1, n2);
            var (line, blob1, blob2) = mLines[lineNodes];
            mUIElements.Remove(line, blob1, blob2);
            mLines.Remove(lineNodes);
        }

        private void ClearUI()
        {
            // Remove everything from the UI then clear lists.
            foreach (var el in mNodeUIElements.Values)
            {
                mUIElements.Remove(el.Item1);
            }

            mNodes.Clear();
            mNodeUIElements.Clear();

            foreach (var (line, blob1, blob2) in mLines.Values)
            {
                mUIElements.Remove(line, blob1, blob2);
            }

            mLines.Clear();
        }
    }
}
