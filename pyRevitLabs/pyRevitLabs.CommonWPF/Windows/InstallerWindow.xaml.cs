using System.ComponentModel;
using System.Windows;

namespace pyRevitLabs.CommonWPF.Windows {
    /// <summary>
    /// Interaction logic for InstallerWindow.xaml
    /// </summary>
    public partial class InstallerWindow : Window, INotifyPropertyChanged {
        public InstallerWindow()
        {
            InitializeComponent();
        }

        public bool Updating {
            get => "Updating" == (string)InstallButton.Tag;
            set {
                if (value) {
                    InstallButton.Tag = "Updating";
                    InstallButton.IsIndeterminate = true;
                }
                else {
                    InstallButton.Tag = "";
                    InstallButton.IsIndeterminate = false;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
