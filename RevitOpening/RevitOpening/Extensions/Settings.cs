using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitOpening.Extensions
{
    using System.Configuration;
    using System.Globalization;

    public static class Settings
    {
        private const string offsetStr = "1.5";
        private const string diameterStr = "200";

        public static double Offset
        {
            get =>
                double.Parse(GetParameterFromSettings(nameof(Offset), offsetStr),
                    NumberStyles.Any, CultureInfo.InvariantCulture);
            set => throw new NotImplementedException();
        }

        public static double Diameter
        {
            get =>
                double.Parse(GetParameterFromSettings(nameof(Diameter), diameterStr),
                    NumberStyles.Any, CultureInfo.InvariantCulture);
            set => throw new NotImplementedException();
        }

        public static bool IsCombineAll
        {
            get => bool.Parse(GetParameterFromSettings(nameof(IsCombineAll), false));
            set => SetParameterToSettings(nameof(IsCombineAll), value);
        }

        public static bool IsAnalysisOnStart
        {
            get => bool.Parse(GetParameterFromSettings(nameof(IsAnalysisOnStart), true));
            set => SetParameterToSettings(nameof(IsAnalysisOnStart), value);
        }

        public static string OffsetStr
        {
            get => GetParameterFromSettings(nameof(Offset), offsetStr);
            set
            {
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    SetParameterToSettings(nameof(Offset), value);
            }
        }

        public static string DiameterStr
        {
            get => GetParameterFromSettings(nameof(Diameter), diameterStr);
            set
            {
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    SetParameterToSettings(nameof(Diameter), value);
            }
        }

        public static string GetParameterFromSettings(string parameterName, object defaultValue = null)
        {
            return ConfigurationManager.AppSettings[parameterName] ??
                (ConfigurationManager.AppSettings[parameterName] = defaultValue?.ToString());
        }

        public static void SetParameterToSettings(string parameterName, object value)
        {
            ConfigurationManager.AppSettings[parameterName] = value.ToString();
        }
    }
}
