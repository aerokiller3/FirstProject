using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Installer
{
    using System.IO;

    class Program
    {
        static void Main(string[] args)
        {
            var appFolders = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var revitAddinsDir = $@"{appFolders}\Autodesk\Revit\Addins\2019";
            const string moduleDir = "Module";
            CopyDirectory(moduleDir, revitAddinsDir);
        }

        private static void CopyDirectory(string directory, string outputDir)
        {
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
            foreach (var path in Directory.GetFiles(directory))
            {
                var next = GetNextName(path, outputDir);
                File.Copy(path, next, true);
            }

            foreach (var dir in Directory.GetDirectories(directory))
            {
                var next = GetNextName(dir, outputDir);
                CopyDirectory(dir, next);
            }
        }

        private static string GetNextName(string currentName, string outputDir)
        {
            var index = currentName.LastIndexOf('\\');
            var dirName = currentName.Substring(index, currentName.Length - index);
            return $@"{outputDir}{dirName}";
        }
    }
}
