using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RevitOpening.Logic
{
    public static class Transactions
    {
        public static void Analysis(Document _currentDocument, IEnumerable<Document> _documents,
            double offset, double diameter)
        {
            using (var t = new Transaction(_currentDocument, "Анализ заданий"))
            {
                t.Start();
                BoxAnalyzer.ExecuteAnalysis(_documents, offset, diameter);
                t.Commit();
            }
        }
    }
}
