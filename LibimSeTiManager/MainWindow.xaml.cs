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

            SetupBots();

            Loaded += MainWindow_Loaded;
        }

        private void SetupBots()
        {
            botsPanel.Children.Clear();

            botsPanel.Children.Add(Helper.CreateHeaderButton("Bot groups"));

            foreach (var botGroup in _model.BotGroups)
            {
                AddBotGroupButton(botGroup);
            }

            var newBotGroupButton = Helper.CreateButtonWithTextBox(string.Empty);

            newBotGroupButton.Margin = new Thickness(2, 2, 0, 0);

            botsPanel.Children.Add(newBotGroupButton);

            newBotGroupButton.Click += (s, e) =>
            {
                string newGroupName = Helper.ButtonText(newBotGroupButton);

                if (string.IsNullOrWhiteSpace(newGroupName) || _model.BotGroups.Any(group => group.Name == newGroupName))
                {
                    return;
                }

                Configuration.Instance.BotGroups.Add(new BotGroup(newGroupName));

                SetupBots();
            };
        }

        private void AddBotGroupButton(BotGroup botGroup)
        {
            ToggleButton botGroupButton = new ToggleButton();
            botGroupButton.Content = botGroup.Name;
            botGroupButton.Margin = new Thickness(2, 2, 0, 0);
            botGroupButton.ToolTip = string.Format("{0} bots", botGroup.Bots != null ? botGroup.Bots.Count : 0);
            botGroupButton.Tag = botGroup;

            botGroupButton.Checked += BotGroupButton_Checked;
            botGroupButton.Unchecked += BotGroupButton_Unchecked;

            botsPanel.Children.Add(botGroupButton);
        }

        private void BotGroupButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton botGroupButton = (ToggleButton)sender;
            BotGroup botGroup = (BotGroup)botGroupButton.Tag;

            RegisterWindow botGroupWindow = new RegisterWindow(botGroup);
            botGroupButton.Tag = new Tuple<BotGroup, RegisterWindow>(botGroup, botGroupWindow);

            botGroupWindow.Closed += (sender2, e2) => { botGroupButton.IsChecked = false; };

            botGroupWindow.Show();
        }

        private void BotGroupButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton botGroupButton = (ToggleButton)sender;
            Tuple<BotGroup, RegisterWindow> tag = (Tuple<BotGroup, RegisterWindow>)botGroupButton.Tag;

            tag.Item2.Close();

            botGroupButton.Tag = tag.Item1;
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
            try
            {
                var ip = await Connector.GetIP();

                if (ip != null)
                {
                    Title = ip.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(string.Format("Cannot grab IP - {0}", ex.Message));

                MessageBox.Show("Program will terminate");

                Close();
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

            Configuration.Instance.Save();

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Application.Current.Shutdown();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            _model.RetrieveAllRooms();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
    }
}
