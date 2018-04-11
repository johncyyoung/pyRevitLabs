using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace pyRevitInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isInstalling = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isInstalling)
            {
                if (e.Key == Key.Escape)
                    Close();
            }
            else
            {

            }
        }

        private void ProgressBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            InstallButton.IsIndeterminate = true;
            InstallButton.Tag = "Installing...";
            _isInstalling = true;
            CustomizeButton.Visibility = Visibility.Collapsed;
        }
    }
}
