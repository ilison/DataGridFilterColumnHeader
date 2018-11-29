using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DataGridExtensions
{
    public static class DataGridFilter
    {
        #region Filtered Attached
        
        public static bool? GetFiltered(this DataGrid obj)
        {
            return (bool)obj.GetValue(FilteredProperty);
        }

        public static void SetFiltered(this DataGrid obj, bool? value)
        {
            obj.SetValue(FilteredProperty, value);
        }

        // Using a DependencyProperty as the backing store for Filtered.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilteredProperty =
            DependencyProperty.RegisterAttached("Filtered", typeof(bool?), typeof(DataGridFilter), new PropertyMetadata(null,OnFilteredChanged));

        public static void OnFilteredChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var datagrid = sender as DataGrid;
            datagrid?.GetFilter()?.Enable(true.Equals(args.NewValue==null?false: args.NewValue));
        }
        #endregion

        #region Filter Attached


        public static DataGridFilterHost GetFilter(this DataGrid dataGrid)
        {
            var value = (DataGridFilterHost)dataGrid.GetValue(FilterProperty);
            if(value==null)
            {
                value = new DataGridFilterHost(dataGrid);
                dataGrid.SetValue(FilterProperty, value);
            }
            return value;
        }

        // Using a DependencyProperty as the backing store for Filter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.RegisterAttached("Filter", typeof(DataGridFilterHost), typeof(DataGridFilterHost));


        #endregion

    }
}
