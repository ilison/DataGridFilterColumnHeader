using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace DataGridExtensions
{
    public class DataGridFilterColumnControl:Control, INotifyPropertyChanged
    {
        private static readonly BooleanToVisibilityConverter _booleanToVisibilityConverter = new BooleanToVisibilityConverter();
        private static readonly ControlTemplate _emptyControlTemplate = new ControlTemplate();

        //static DataGridFilterColumnControl()
        //{
        //    var templatePropertyDescriptor = DependencyPropertyDescriptor.FromProperty(TemplateProperty, typeof(Control));

        //    if (templatePropertyDescriptor != null)
        //        templatePropertyDescriptor.DesignerCoerceValueCallback = Template_CoerceValue;
        //}
        private static object Template_CoerceValue(DependencyObject sender, object baseValue)
        {
            if (baseValue != null)
                return baseValue;

            var control = sender as DataGridFilterColumnControl;

            // Just resolved the binding to the template property attached to the column, and the value has not been set on the column:
            // => try to find the default template based on the columns type.
            var columnType = control?.ColumnHeader?.Column?.GetType();
            if (columnType == null)
                return null;

         //   var resourceKey = new ComponentResourceKey(typeof(DataGridFilter), columnType);

            return control.FindResource(columnType.Name.ToString());
        }

        public DataGridFilterColumnControl()
        {
            this.Loaded += DataGridFilterColumnControl_Loaded;
        }

        private void DataGridFilterColumnControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (FilterHost == null)
            {
                // Find the ancestor column header and data grid controls.
                ColumnHeader = this.FindColumnHeader<DataGridColumnHeader>();
                if (ColumnHeader == null)
                    throw new InvalidOperationException("DataGridFilterColumnControl must be a child element of a DataGridColumnHeader.");

                 DataGrid = ColumnHeader.FindColumnHeader<DataGrid>();
                if (DataGrid == null)
                    throw new InvalidOperationException("DataGridColumnHeader must be a child element of a DataGrid");

                // Find our host and attach ourself.
                FilterHost = DataGrid.GetFilter();
            }
            FilterHost.AddColumn(this);

            Template = _emptyControlTemplate;

            var isFilterVisiblePropertyPath = new PropertyPath("Column.(0)", DataGridFilterColumn.IsFilterVisibleProperty);
            BindingOperations.SetBinding(this, VisibilityProperty, new Binding() { Path = isFilterVisiblePropertyPath, Source = ColumnHeader, Mode = BindingMode.OneWay, Converter = _booleanToVisibilityConverter });

            var templatePropertyPath = new PropertyPath("Column.(0)", DataGridFilterColumn.TemplateProperty);
            BindingOperations.SetBinding(this, TemplateProperty, new Binding() { Path = templatePropertyPath, Source = ColumnHeader, Mode = BindingMode.OneWay });

            var filterPropertyPath = new PropertyPath("Column.(0)", DataGridFilterColumn.FilterProperty);
            BindingOperations.SetBinding(this, FilterProperty, new Binding() { Path = filterPropertyPath, Source = ColumnHeader, Mode = BindingMode.TwoWay });


            //1.获取datagridcolumnheader中Column是什么类型
            //2.根据不同的类型修改自己的样式
            //ColumnHeader = this.FindColumnHeader<DataGridColumnHeader>();
            //if (ColumnHeader == null)
            //    throw new Exception();

            var template = ColumnHeader.Column.GetType();
            if (template == typeof(DataGridCheckBoxColumn))
            {
                var tem = this.FindResource("DataGridCheckBoxColumn") as ControlTemplate;
                var count = VisualTreeHelper.GetChildrenCount(new CheckBox());
                //  CheckBox c = VisualTreeHelper.GetChild(new CheckBox()) as CheckBox;
                //  CheckBox c = tem.c("_chebox_",new Grid()) as CheckBox;
                // BindingOperations.SetBinding(c, CheckBox.IsCheckedProperty, new Binding(nameof(FilterValues)) { Source = this });
                this.Template = tem;
            }
            else
            {
                this.Template = this.FindResource("DataGridTextColumn") as ControlTemplate;
            }
        }

        public object Filter
        {
            get { return GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }
        /// <summary>
        /// Identifies the Filter dependency property
        /// </summary>
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof(object), typeof(DataGridFilterColumnControl), new FrameworkPropertyMetadata(null, (sender, e) => ((DataGridFilterColumnControl)sender).Filter_Changed(e.NewValue)));

        internal void Filter_Changed(object newValue)
        {
            // Update the effective filter. If the filter is provided as content, the content filter will be recreated when needed.
            _filterValues = newValue;
            if (FilterHost == null)
                FilterHost = DataGrid.GetFilter();

            FilterHost.Filter();
        }
        protected DataGridFilterHost FilterHost { get;private set; }
        protected DataGrid DataGrid { get; private set; }
        protected DataGridColumnHeader ColumnHeader
        {
            get;
            private set;
        }

        private object _filterValues;

        /// <summary>
        /// 筛选方法
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Matches(object item)
        {
            if (FilterHost==null)
            {
                return true;
            }

            //if (item != _filterValues)
            //{
            //    return false;
            //}
            return IsMatch(GetCellContent(item));
        }
        public bool IsMatch(object value)
        {
            if (value == null)
                return false;
            if (_filterValues == null || String.IsNullOrWhiteSpace(_filterValues.ToString()))
            {
                return true;
            }
            return value.ToString().IndexOf(_filterValues.ToString(),StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        /// <summary>
        /// Identifies the CellValue dependency property, a private helper property used to evaluate the property path for the list items.
        /// </summary>
        private static readonly DependencyProperty _cellValueProperty =
            DependencyProperty.Register("_cellValue", typeof(object), typeof(DataGridFilterColumnControl));

        protected object GetCellContent(object item)
        {
            var propertyPath = ColumnHeader?.Column.SortMemberPath;

            if (string.IsNullOrEmpty(propertyPath))
                return null;

            // Since already the name "SortMemberPath" implies that this might be not only a simple property name but a full property path
            // we use binding for evaluation; this will properly handle even complex property paths like e.g. "SubItems[0].Name"
            BindingOperations.SetBinding(this, _cellValueProperty, new Binding(propertyPath) { Source = item });
            var propertyValue = GetValue(_cellValueProperty);
            BindingOperations.ClearBinding(this, _cellValueProperty);

            return propertyValue;
        }

        #region FilterValues Attached

        public static object GetFilterValues(DependencyObject obj)
        {
            return (object)obj.GetValue(FilterValuesProperty);
        }

        public static void SetFilterValues(DependencyObject obj, object value)
        {
            obj.SetValue(FilterValuesProperty, value);
        }

        // Using a DependencyProperty as the backing store for FilterValues.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilterValuesProperty =
            DependencyProperty.RegisterAttached("FilterValues", typeof(object), typeof(DataGridFilterColumnControl), new PropertyMetadata(null, (s, e) =>
             {
                 var parent = s.FindColumnHeader<DataGridFilterColumnControl>();
                // parent.Filter_Changed(e.NewValue);
             }
              ));

        public event PropertyChangedEventHandler PropertyChanged;


        #endregion
    }

    public static class Ext
    {
        internal static T FindColumnHeader<T>(this DependencyObject dependencyObject) where T : class
        {
            while (dependencyObject != null)
            {
                var target = dependencyObject as T;
                if (target != null)
                    return target;

                dependencyObject = LogicalTreeHelper.GetParent(dependencyObject) ?? VisualTreeHelper.GetParent(dependencyObject);
            }
            return null;
        }

        public static T GetValue<T>(this DependencyObject self, DependencyProperty property)
        {
            Contract.Requires(self != null);
            Contract.Requires(property != null);

            return self.GetValue(property).SafeCast<T>();
        }
        public static T SafeCast<T>(this object value)
        {
            return (value == null) ? default(T) : (T)value;
        }
    }
}
