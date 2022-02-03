using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace SlovoedCheat
{
    public class ScrollToSelectedListBoxItemBehaviour : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += AssociatedObjectOnSelectionChanged;
            AssociatedObject.IsVisibleChanged += AssociatedObjectOnIsVisibleChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged -= AssociatedObjectOnSelectionChanged;
            AssociatedObject.IsVisibleChanged -= AssociatedObjectOnIsVisibleChanged;
            base.OnDetaching();
        }

        private static void AssociatedObjectOnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ScrollIntoFirstSelectedItem(sender);
        }

        private static void AssociatedObjectOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScrollIntoFirstSelectedItem(sender);
        }

        private static void ScrollIntoFirstSelectedItem(object sender)
        {
            if (!(sender is DataGrid listBox))
                return;
            var selectedItems = listBox.SelectedItems;
            if (selectedItems.Count > 0)
                listBox.ScrollIntoView(selectedItems[0]);
        }
    }
}
