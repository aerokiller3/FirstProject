using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using RevitOpening.Models;

namespace RevitOpening.Extensions
{
    public static class DataGridExtensions
    {
        public static IEnumerable<T> GetSelectedItemsFromGrid<T>(this DataGrid grid)
        {
            return grid.SelectedItems.Cast<T>();
        }
    }
}
