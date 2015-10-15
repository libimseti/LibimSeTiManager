using LibimSeTi.Core;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace LibimSeTiManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private LogWindow _logWindow;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            object o = LibimSeTiConnector.IP;

            LibimSeTiSession session = new LibimSeTiSession("helicobacter2", "123456789");
           
            await session.Logon();

            Room room = new Room(351818, "Cela do naha");

            for (int i = 0; i < 10; i++)
            {
                await session.EnterRoom(room);
                await session.ReadRoom(room);

                ShowRoom(room);

                await session.LeaveRoom(room);
            }
        }

        private void ShowRoom(Room room)
        {
            roomLabel.Content = room.Name;

            if (room.Content == null)
            {
                return;
            }

            roomContentBlock.Inlines.Clear();

            foreach (Room.Event roomEvent in room.Content)
            {
                switch (roomEvent.Type)
                {
                    case Room.EventType.Enter:
                        roomContentBlock.Inlines.Add(new Run(roomEvent.UserName) { Foreground = Brushes.DarkRed, FontWeight = FontWeights.Bold });
                        roomContentBlock.Inlines.Add(new Run(" entered\n") { FontStyle = FontStyles.Italic });
                        break;
                    case Room.EventType.Leave:
                        roomContentBlock.Inlines.Add(new Run(roomEvent.UserName) { Foreground = Brushes.DarkRed, FontWeight = FontWeights.Bold });
                        roomContentBlock.Inlines.Add(new Run(" left\n") { FontStyle = FontStyles.Italic });
                        break;
                    case Room.EventType.Text:
                        roomContentBlock.Inlines.Add(new Run(roomEvent.UserName) { Foreground = Brushes.DarkRed, FontWeight = FontWeights.Bold });
                        roomContentBlock.Inlines.Add(new Run(string.Format(": {0}\n", roomEvent.Text)));
                        break;
                }
            }
        }

        private void AppendToLog(string message)
        {
            if (_logWindow != null)
            {
                _logWindow.AppendToLog(message);
            }
        }

        private void ShowLog_Click(object sender, RoutedEventArgs e)
        {
            if (_logWindow == null)
            {
                _logWindow = new LogWindow();
                _logWindow.Show();
                Logger.Instance.Message += AppendToLog;
            }
            else
            {
                Logger.Instance.Message -= AppendToLog;
                _logWindow.Close();
                _logWindow = null;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_logWindow != null)
            {
                _logWindow.Close();
            }

            base.OnClosing(e);
        }
    }
}
