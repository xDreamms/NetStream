using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia;
using Avalonia.Xaml.Interactivity;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Input.Raw;
using Avalonia.VisualTree;

namespace NetStream
{
    /// <summary>
    /// Captures and eats MouseWheel events so that a nested ListBox does not
    /// prevent an outer scrollable control from scrolling.
    /// </summary>
    public class IgnoreMouseWheelBehavior : Behavior<Control>
    {
        protected override void OnAttachedToVisualTree()
        {
            base.OnAttachedToVisualTree();
            if (AssociatedObject != null)
            {
                AssociatedObject.AddHandler(Control.PointerWheelChangedEvent, AssociatedObject_PointerWheelChanged, RoutingStrategies.Tunnel);
            }
        }

        protected override void OnDetachedFromVisualTree()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.RemoveHandler(Control.PointerWheelChangedEvent, AssociatedObject_PointerWheelChanged);
            }
            base.OnDetachedFromVisualTree();
        }

        private void AssociatedObject_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            e.Handled = true;
            
            var scrollViewer = AssociatedObject?.FindAncestorOfType<ScrollViewer>();
            if (scrollViewer != null)
            {
                var currentOffset = scrollViewer.Offset;
                scrollViewer.Offset = new Vector(currentOffset.X, currentOffset.Y - (e.Delta.Y * 50));
            }
        }
    }
}
