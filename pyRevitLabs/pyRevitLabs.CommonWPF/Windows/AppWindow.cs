using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Controls;

// packages
using MahApps.Metro;
using MahApps.Metro.Controls;

namespace pyRevitLabs.CommonWPF.Windows {
    /// <summary>
    /// Interaction logic for AppWindow.xaml
    /// </summary>
    public class AppWindow : MetroWindow, INotifyPropertyChanged {
        public AppWindow() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            // setting up user name and app version buttons
            var winButtons = new WindowCommands() {
                Items = {
                    new Button() { Content = CurrentUser },
                    new Button() { Content = AppVersion },
                }
            };
            RightWindowCommands = winButtons;

            TitleCharacterCasing = CharacterCasing.Normal;
            SaveWindowPosition = true;
        }

        // property updates
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName) {
            if (null != this.PropertyChanged) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // app version
        public string AppVersion { get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); } }

        // current user id
        public string CurrentUser { get { return System.Security.Principal.WindowsIdentity.GetCurrent().Name; } }

    }
}
