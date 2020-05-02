using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BTLabelPrint.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

/*            var desc = DependencyPropertyDescriptor.FromProperty(DataGridColumn.SortDirectionProperty, typeof(DataGridColumn));
            foreach(var col in dataGrid.Columns)
            {
                desc.AddValueChanged(col, (s, e) => 
                {
                    if(col.SortDirection != null && !setSorting)
                        sortDirections[col.Header.ToString()] = (col, col.SortDirection);
                });
            }

            desc = DependencyPropertyDescriptor.FromProperty(DataGrid.ItemsSourceProperty, typeof(DataGrid));
            desc.AddValueChanged(dataGrid, (s, e) =>
            {
                setSorting = true;
                foreach (var sd in sortDirections.Values)
                {
                    sd.column.SortDirection = sd.dir;
                }
                setSorting = false;
            });*/
        }

        private Dictionary<string, (DataGridColumn column, ListSortDirection? dir)> sortDirections = new Dictionary<string, (DataGridColumn column, ListSortDirection? dir)>();
        private bool setSorting;

        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                ((Control)sender).GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            }
        }
    }
}
