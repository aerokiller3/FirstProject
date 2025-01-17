﻿namespace RevitOpening.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Controls;

    internal static class DataGridExtensions
    {
        public static IEnumerable<T> GetSelectedItemsFromGrid<T>(this DataGrid grid)
        {
            return grid.SelectedItems.Cast<T>();
        }
    }
}