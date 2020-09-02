using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RevitOpening.Annotations;

namespace RevitOpening.ViewModels
{
    public class LoginVM : INotifyPropertyChanged
    {

        public LoginVM()
        {
        }

        private Window _window;

        public LoginVM(Window window)
        {
            _window = window;
        }

        private RelayCommand _signInCommand;

        public string Login { get; set; } = GetParameterFromSettings(nameof(Login), "a");
        public string Password { get; set; } = GetParameterFromSettings(nameof(Password), "a");

        public RelayCommand SignInCommand
        {
            get
            {
                return _signInCommand ?? (_signInCommand = new RelayCommand(obj =>
                {
                    SetParameterToSettings(nameof(Login), Login);
                    SetParameterToSettings(nameof(Password), Password);
                    _window.Close();
                }));
            }
        }

        private static string GetParameterFromSettings(string parameterName, object defaultValue = null)
        {
            return ConfigurationManager.AppSettings[parameterName] ??
                   (ConfigurationManager.AppSettings[parameterName] = defaultValue?.ToString());
        }

        private static void SetParameterToSettings(string parameterName, object value)
        {
            ConfigurationManager.AppSettings[parameterName] = value.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
