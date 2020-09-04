using System;

namespace RevitOpening.ViewModels
{
    public interface IDataGridUpdater
    {
        void OnCurrentCellChanged(object sender, EventArgs e);
    }
}
