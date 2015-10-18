using System;
using System.ComponentModel;
using System.Windows;

namespace LibimSeTiManager
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void AppendToLog(string line)
        {
            Log += line + Environment.NewLine;

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Log"));
                Dispatcher.Invoke(() => LogBox.ScrollToEnd());
            }
        }

        public string Log { get; set; }

        public LogWindow()
        {
            InitializeComponent();

            DataContext = this;
        }
    }
}
