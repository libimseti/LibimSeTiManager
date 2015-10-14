using LibimSeTi.Core;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace LibimSeTiManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            LibimSeTiSession session = new LibimSeTiSession("helicobacter2", "123456789");
            LibimSeTiSession session2 = new LibimSeTiSession("helicobacter2", "123456789");

            AppendToLog("Start");
            AppendToLog(session.IP.ToString());
            AppendToLog(session2.IP.ToString());

            //session.Logon();
        }

        private void AppendToLog(string line)
        {
            Log += line + Environment.NewLine;

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Log"));
            }
        }

        public string Log { get; set; }
    }
}
