using LibimSeTi.Core;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls;

namespace LibimSeTiManager
{
    /// <summary>
    /// Interaction logic for RoomWindow.xaml
    /// </summary>
    public partial class RoomWindow : Window
    {
        public class UserItem
        {
            public UserItem(User user)
            {
                User = user;
            }

            public User User { get; private set; }

            public Brush Color
            {
                get
                {
                    return SexToColor(User.UserSex);
                }
            }

            public static Brush SexToColor(User.Sex sex)
            {
                switch (sex)
                {
                    case User.Sex.Male:
                        return Brushes.Blue;
                    case User.Sex.Female:
                        return Brushes.DeepPink;
                    default:
                        return Brushes.Black;
                }
            }
        }

        private class BotItem
        {
            public BotItem(Bot bot)
            {
                Bot = bot;
            }

            public Bot Bot { get; private set; }
        }

        private readonly Room _room;
        private readonly Model _model;
        private readonly AutoResetEvent _stopWatcherEvent = new AutoResetEvent(false);

        public RoomWindow(Room room)
        {
            InitializeComponent();

            _room = room;
            _model = Model.Instance;

            Title = string.Format("Room {0}", room.Name);

            SetupBotGroups();
            SetupActions();

            Update(room);

            _room.ContentUpdated += Update;

            StartRoomWatching();
        }

        private void SetupActions()
        {
            actionsPanel.Children.Clear();

            actionsPanel.Children.Add(Helper.CreateHeaderButton("Actions"));

            AddActionButton("Logon", new LogonCommand());
            AddActionButton("Enter", new EnterRoomCommand { Room = _room });
            AddActionButton("Leave", new LeaveRoomCommand { Room = _room });
        }

        private void AddActionButton(string name, BotCommand command)
        {
            Button actionButton = new Button();
            actionButton.Content = name;
            actionButton.Margin = new Thickness(2, 2, 0, 0);
            actionButton.Tag = command;

            actionButton.Click += (s, e) =>
            {
                if (_room.AssignedBots == null || !_room.AssignedBots.Any())
                {
                    Logger.Instance.Info("No assigned bots");
                    return;
                }

                foreach (Bot assignedBot in _room.AssignedBots)
                {
                    if (command.CanDo(assignedBot))
                    {
                        command.Do(assignedBot);
                    }
                }
            };

            actionsPanel.Children.Add(actionButton);
        }

        private void StartRoomWatching()
        {
            Thread watcher = new Thread(new ThreadStart(() =>
            {
                ReadRoomCommand readingCommand = new ReadRoomCommand();
                readingCommand.Room = _room;

                while (!_stopWatcherEvent.WaitOne(TimeSpan.FromSeconds(3)))
                {
                    Bot watchingBot = _room.AssignedBots?.FirstOrDefault(bot => readingCommand.CanDo(bot, true));

                    if (watchingBot != null)
                    {
                        var readingTask = readingCommand.Do(watchingBot);

                        try
                        {
                            readingTask.Wait();
                        }
                        catch
                        {
                            Logger.Instance.Error(string.Format("[{0}] Cannot watch", _room.Name));
                        }

                        if (_stopWatcherEvent.WaitOne(TimeSpan.FromSeconds(30)))
                        {
                            return;
                        }
                    }
                }
            }));

            watcher.IsBackground = true;
            watcher.Start();
        }

        private void SetupBotGroups()
        {
            botGroupsPanel.Children.Clear();

            botGroupsPanel.Children.Add(Helper.CreateHeaderButton("Bot groups"));

            foreach (BotGroup group in _model.BotGroups)
            {
                AddBotGroupButton(group);
            }
        }

        private void AddBotGroupButton(BotGroup botGroup)
        {
            ToggleButton botGroupButton = new ToggleButton();
            botGroupButton.Content = botGroup.Name;
            botGroupButton.Margin = new Thickness(2, 2, 0, 0);
            botGroupButton.ToolTip = string.Format("{0} bots", botGroup.Bots != null ? botGroup.Bots.Count : 0);
            botGroupButton.Tag = botGroup;

            botGroupButton.Checked += BotGroupButton_Toggled;
            botGroupButton.Unchecked += BotGroupButton_Toggled;

            botGroupsPanel.Children.Add(botGroupButton);
        }

        private void BotGroupButton_Toggled(object sender, RoutedEventArgs e)
        {
            RefreshBots();
        }

        private void RefreshBots()
        {
            _room.AssignedBotGroups = botGroupsPanel.Children
                .OfType<ToggleButton>()
                .Where(button => button.IsChecked == true)
                .Select(button => button.Tag as BotGroup)
                .Where(botGroup => botGroup != null)
                .ToArray();

            botBox.Items.Clear();

            foreach (var botItem in _room.AssignedBotGroups.SelectMany(botGroup => botGroup.Bots).Select(bot => new BotItem(bot)))
            {
                botBox.Items.Add(botItem);
            }
        }

        private void Update(Room room)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<Room>(Update), room);
                return;
            }

            if (room.Content != null)
            {
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

            if (room.Users != null)
            {
                userBox.Items.Clear();

                foreach (User user in room.Users)
                {
                    userBox.Items.Add(new UserItem(user));
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _room.ContentUpdated -= Update;

            _stopWatcherEvent.Set();
        }
    }
}
