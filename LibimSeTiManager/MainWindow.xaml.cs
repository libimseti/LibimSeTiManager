using LibimSeTi.Core;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace LibimSeTiManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private LogWindow _logWindow;

        private readonly Model _model;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            _model = Model.Instance;

            ShowLog_Click(null, null);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            SetupRooms();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            Loaded += MainWindow_Loaded;
        }

        private async Task SetupRooms()
        {
            await _model.RetrieveAllRooms();

            roomsPanel.Children.Clear();

            roomsPanel.Children.Add(Helper.CreateHeaderButton("Rooms"));

            foreach (Room room in _model.AllRooms.OrderByDescending(room => room.Users != null ? room.Users.Length : 0))
            {
                AddRoomButton(room);
            }
        }

        private void AddRoomButton(Room room)
        {
            ToggleButton roomButton = new ToggleButton();
            roomButton.Content = room.Name;
            roomButton.Margin = new Thickness(2, 2, 0, 0);
            roomButton.ToolTip = string.Format("{0} users", room.Users != null ? room.Users.Length : 0);
            roomButton.Tag = room;

            roomButton.Checked += RoomButton_Checked;
            roomButton.Unchecked += RoomButton_Unchecked;

            roomsPanel.Children.Add(roomButton);
        }

        private void RoomButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton roomButton = (ToggleButton)sender;
            Tuple<Room, RoomWindow> tag = (Tuple<Room, RoomWindow>)roomButton.Tag;

            tag.Item2.Close();

            roomButton.Tag = tag.Item1;
        }

        private void RoomButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton roomButton = (ToggleButton)sender;
            Room room = (Room)roomButton.Tag;

            RoomWindow roomWindow = new RoomWindow(room);
            roomButton.Tag = new Tuple<Room, RoomWindow>(room, roomWindow);

            roomWindow.Closed += (sender2, e2) => { roomButton.IsChecked = false; };

            roomWindow.Show();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var ip = await Connector.GetIP();

            if (ip != null)
            {
                Title = ip.ToString();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
