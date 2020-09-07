namespace RevitOpening.ViewModels
{
    using System;

    public interface IDataGridUpdater
    {
        void OnCurrentCellChanged(object sender, EventArgs e);
    }
}