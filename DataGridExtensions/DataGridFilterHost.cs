using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DataGridExtensions
{
    /// <summary>
    /// 此类为DataGrid的所有扩展操作类,
    /// </summary>
    public sealed class DataGridFilterHost
    {
        private readonly DataGrid _dataGrid;

        //DataGrid中的所有列
        private readonly List<DataGridFilterColumnControl> _filterColumnControls = new List<DataGridFilterColumnControl>();

        //控件是否显示
        internal bool _isFilteringVisibility { set; get; }

        public DataGridFilterHost(DataGrid dataGrid)
        {
            this._dataGrid = dataGrid;
            this._dataGrid.Columns.CollectionChanged += Columns_Changed;
        }

        internal void Columns_Changed(object sender, NotifyCollectionChangedEventArgs args)
        {
            var resource = _dataGrid.TryFindResource("ColumnHeaderTemplate");

            var headerTemplate = (DataTemplate)resource;
            var filteredColumnsWithEmptyHeaderTemplate = args.NewItems
                .Cast<DataGridColumn>()
                .Where(column => (column.HeaderTemplate == null))
                .ToArray();
            foreach (var column in filteredColumnsWithEmptyHeaderTemplate)
            {
                column.HeaderTemplate = headerTemplate;
            }
        }

        //添加个新列
        internal void AddColumn(DataGridFilterColumnControl dataGridFilterColumn)
        {
            //dataGridFilterColumn.Visibility = _isFilteringVisibility?Visibility.Visible:Visibility.Hidden;
            _filterColumnControls.Add(dataGridFilterColumn);
        }

        //显示或隐藏控件来启动筛选
        internal void Enable(bool value)
        {
            _isFilteringVisibility = value;
            _filterColumnControls.ForEach(column => column.Visibility = value ? Visibility.Visible : Visibility.Hidden);
            //筛选DataGrid
            if(_filterColumnControls?.Any()==true)
                    Filter();
        }

        internal void Filter()
        {
            var filterColumnControls = _filterColumnControls.Where(col => col.Visibility == Visibility.Visible);
            _dataGrid.Items.Filter = CreatePredicate(filterColumnControls?.ToList());
        }

        private Predicate<object> CreatePredicate(List<DataGridFilterColumnControl> columnControls)
        {
            
            if (columnControls?.Any() != true)
            {
                return item => columnControls.All(filter => true);
            }
            return item => columnControls.All(filter => filter.Matches(item));
        }
    }
}
