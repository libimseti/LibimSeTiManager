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
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Input;

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

        private ToggleButton _actionsModeButton;
        private ToggleButton _isComposingButton;

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
            _room.UsersUpdated += Update;

            StartRoomWatching();
        }

        private bool IsComposing { get { return _isComposingButton != null && _isComposingButton.IsChecked == true; } }

        private bool IsAllBotsAtOnceMode { get { return _actionsModeButton != null && _actionsModeButton.IsChecked == true; } }

        private object GetComposingButtonContent(string name)
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            panel.VerticalAlignment = VerticalAlignment.Bottom;
            panel.Margin = new Thickness(0, 0, 10, 0);

            TextBlock nameBlock = new TextBlock();
            nameBlock.Text = name;
            nameBlock.Margin = new Thickness(0, 0, 5, 0);

            panel.Children.Add(nameBlock);

            _actionsModeButton = new ToggleButton();
            _actionsModeButton.Margin = new Thickness(2, 2, 0, 0);
            _actionsModeButton.Background = Brushes.Black;
            _actionsModeButton.Foreground = Brushes.White;
            _actionsModeButton.Checked += (s, e) => { _actionsModeButton.Content = " All at once "; e.Handled = true; };
            _actionsModeButton.Unchecked += (s, e) => { _actionsModeButton.Content = " Bot by bot "; e.Handled = true; };
			_actionsModeButton.IsChecked = true;
			_actionsModeButton.IsChecked = false;

            panel.Children.Add(_actionsModeButton);

            return panel;
        }

        private void SetupActions()
        {
            actionsPanel.Children.Clear();

            actionsPanel.Children.Add(Helper.CreateHeaderButton("Actions"));

            _isComposingButton = new ToggleButton();
            _isComposingButton.Margin = new Thickness(2, 2, 0, 0);
            _isComposingButton.Background = Brushes.Black;
            _isComposingButton.Foreground = Brushes.White;
            _isComposingButton.Checked += (s, e) => { _isComposingButton.Content = GetComposingButtonContent(" Composing mode "); };
            _isComposingButton.Unchecked += (s, e) => { _isComposingButton.Content = GetComposingButtonContent(" Direct mode "); };
            _isComposingButton.IsChecked = true;
			_isComposingButton.IsChecked = false;
			_isComposingButton.IsChecked = true;

			actionsPanel.Children.Add(_isComposingButton);

            AddActionButton("Logon", new LogonCommand());
            AddActionButton("Enter", new EnterRoomCommand { Room = _room });
            AddActionButton("Leave", new LeaveRoomCommand { Room = _room });

            var textContent = CreateTextActionContent("Text");
            AddActionButton(textContent.Item1, new TextRoomCommand {
                Room = _room,
                TextGetter = bot => textContent.Item2.Text },
                cmd =>
                {
                    TextRoomCommand textCmd = (TextRoomCommand)cmd;

                    string text = textContent.Item2.Text;

                    return new TextRoomCommand { Room = textCmd.Room, TextGetter = bot => text };
                });

			var messageContent = CreateTwoTextsActionContent("Message");
			AddActionButton(messageContent.Item1, new MessageSendCommand
			{
				UserNameGetter = bot => messageContent.Item2.Text,
				MessageGetter = bot => messageContent.Item3.Text },
				cmd =>
				{
					MessageSendCommand messageCmd = (MessageSendCommand)cmd;
					string userName = messageContent.Item2.Text;
					string text = messageContent.Item3.Text;
					return new MessageSendCommand { UserNameGetter = bot => userName, MessageGetter = bot => text };
				});

			var botMessageContent = CreateTwoTextsActionContent("BotMessage (optional nick)");
			botMessageContent.Item2.Text = "2";
            AddActionButton(botMessageContent.Item1, new TextRoomCommand
            {
                Room = _room,
                TextGetter = bot => bot != null ? bot.Messages.Length >= int.Parse(botMessageContent.Item2.Text) ?
					((string.IsNullOrWhiteSpace(botMessageContent.Item3.Text)? string.Empty : string.Format("/m {0} ", botMessageContent.Item3.Text)) + bot.Messages[int.Parse(botMessageContent.Item2.Text) - 1])
					:
					null
					:
					string.Format("Message {0} {1}", botMessageContent.Item3.Text, botMessageContent.Item2.Text)
            },
            cmd =>
            {
                TextRoomCommand textCmd = (TextRoomCommand)cmd;

                int messageNumber = int.Parse(botMessageContent.Item2.Text);

				return new TextRoomCommand
				{
					Room = textCmd.Room,
					TextGetter = bot => bot != null ? bot.Messages.Length >= int.Parse(botMessageContent.Item2.Text) ?
					((string.IsNullOrWhiteSpace(botMessageContent.Item3.Text) ? string.Empty : string.Format("/m {0} ", botMessageContent.Item3.Text)) + bot.Messages[int.Parse(botMessageContent.Item2.Text) - 1])
					:
					null
					:
					string.Format("Message {0} {1}", botMessageContent.Item3.Text, botMessageContent.Item2.Text)
				};
            });

			var botMessageAsMessageContent = CreateTwoTextsActionContent("BotMessage as Message");
			botMessageAsMessageContent.Item3.Text = "2";
			AddActionButton(botMessageAsMessageContent.Item1, new MessageSendCommand
			{
				UserNameGetter = bot => botMessageAsMessageContent.Item2.Text,
				MessageGetter = bot => bot != null ? bot.Messages.Length >= int.Parse(botMessageAsMessageContent.Item3.Text) ? bot.Messages[int.Parse(botMessageAsMessageContent.Item3.Text) - 1] : null : string.Format("Message {0}", botMessageAsMessageContent.Item3.Text)
			},
			cmd =>
			{
				MessageSendCommand messageCmd = (MessageSendCommand)cmd;
				string userName = botMessageAsMessageContent.Item2.Text;
				int messageNumber = int.Parse(botMessageAsMessageContent.Item3.Text);
				return new MessageSendCommand {
					UserNameGetter = bot => userName,
					MessageGetter = bot => bot != null ? bot.Messages.Length >= messageNumber ? bot.Messages[messageNumber - 1] : null : string.Format("Message {0}", messageNumber) };
			});

			var pauseContent = CreateTextActionContent("Pause");
            pauseContent.Item2.Text = "3";
            AddActionButton(pauseContent.Item1, new PauseCommand {
                PauseAmountGetter = () => int.Parse(pauseContent.Item2.Text) },
                cmd =>
                {
                    PauseCommand pauseCmd = (PauseCommand)cmd;

                    int pauseSeconds = int.Parse(pauseContent.Item2.Text);

                    return new PauseCommand { PauseAmountGetter = () => pauseSeconds };
                });

            Button composedActionButton = new Button();
            composedActionButton.Content = "Run composed action";
            composedActionButton.Margin = new Thickness(2, 2, 0, 0);

            composedActionButton.Click += (s, e) =>
            {
                RunComposedAction();
            };

            actionsPanel.Children.Add(composedActionButton);

			Button cancelActionButton = new Button();
			cancelActionButton.Content = "Cancel";
			cancelActionButton.Margin = new Thickness(2, 2, 0, 0);

			cancelActionButton.Click += (s, e) =>
			{
				_cancel = true;
			};

			actionsPanel.Children.Add(cancelActionButton);
		}

		private bool _cancel;

        private async void RunComposedAction()
        {
            if (composedActionBox.Items.Count == 0)
            {
                Logger.Instance.Error("No action in composed action");
                return;
            }

            if (_room.AssignedBots == null || !_room.AssignedBots.Any())
            {
                Logger.Instance.Error("No assigned bots");
                return;
            }

            Bot[] assignedBots = _room.AssignedBots.ToArray();
            BotCommand[] commands = composedActionBox.Items.OfType<BotCommand>().ToArray();
            bool isAllBotsAtOnce = IsAllBotsAtOnceMode;

            if (isAllBotsAtOnce)
            {
                foreach (BotCommand command in commands)
                {
                    List<Task> commandTasks = new List<Task>();

                    foreach (Bot assignedBot in assignedBots)
                    {
                        if (command.CanDo(assignedBot))
                        {
                            commandTasks.Add(command.Do(assignedBot));
                        }
                    }

                    try
                    {
                        await Task.Run(() => Task.WaitAll(commandTasks.ToArray()));
                    }
                    catch
                    {
                        Logger.Instance.Error(string.Format("Command {0} cannot be waited, bailing out", command.Header));
                    }

					if (_cancel)
					{
						break;
					}
                }
            }
            else
            {
                foreach (Bot assignedBot in assignedBots)
                {
                    foreach (BotCommand command in commands)
                    {
                        if (command.CanDo(assignedBot))
                        {
                            try
                            {
                                await command.Do(assignedBot);
                            }
                            catch
                            {
                                Logger.Instance.Error(string.Format("[{0}] command {1} failed", assignedBot.Username, command.Header));
                            }
                        }

						if (_cancel)
						{
							break;
						}
                    }

					if (_cancel)
					{
						break;
					}
                }
            }

			_cancel = false;
		}

		private Tuple<StackPanel, TextBox, TextBox> CreateTwoTextsActionContent(string name)
		{
			StackPanel panel = new StackPanel();
			panel.Orientation = Orientation.Horizontal;
			panel.VerticalAlignment = VerticalAlignment.Bottom;
			panel.Margin = new Thickness(0, 0, 10, 0);

			TextBlock nameBlock = new TextBlock();
			nameBlock.Text = name;
			nameBlock.Margin = new Thickness(0, 0, 5, 0);

			panel.Children.Add(nameBlock);

			TextBox textBox = new TextBox();
			textBox.MinWidth = 10;

			panel.Children.Add(textBox);

			TextBox textBox2 = new TextBox();
			textBox2.MinWidth = 10;
			textBox2.Margin = new Thickness(5, 0, 0, 0);

			panel.Children.Add(textBox2);

			return new Tuple<StackPanel, TextBox, TextBox>(panel, textBox, textBox2);
		}

		private Tuple<StackPanel, TextBox> CreateTextActionContent(string name)
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            panel.VerticalAlignment = VerticalAlignment.Bottom;
            panel.Margin = new Thickness(0, 0, 10, 0);

            TextBlock nameBlock = new TextBlock();
            nameBlock.Text = name;
            nameBlock.Margin = new Thickness(0, 0, 5, 0);

            panel.Children.Add(nameBlock);

            TextBox textBox = new TextBox();
            textBox.MinWidth = 10;
            
            panel.Children.Add(textBox);

            return new Tuple<StackPanel, TextBox>(panel, textBox);
        }

        private Button AddActionButton(object content, BotCommand command, Func<BotCommand, BotCommand> onInstantiating = null)
        {
            Button actionButton = new Button();
            actionButton.Content = content;
            actionButton.Margin = new Thickness(2, 2, 0, 0);
            actionButton.Tag = command;

            actionButton.Click += (s, e) =>
            {
                if (IsComposing)
                {
                    BotCommand instantiatedCommand;

                    if (onInstantiating != null)
                    {
                        instantiatedCommand = onInstantiating(command);
                    }
                    else
                    {
                        instantiatedCommand = command;
                    }

                    composedActionBox.Items.Add(instantiatedCommand);

                    return;
                }

                if (_room.AssignedBots == null || !_room.AssignedBots.Any())
                {
                    Logger.Instance.Error("No assigned bots");
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

            return actionButton;
        }

        private void StartRoomWatching()
        {
            Thread watcher = new Thread(new ThreadStart(() =>
            {
                ReadRoomCommand readingCommand = new ReadRoomCommand();
                readingCommand.Room = _room;

                while (!_stopWatcherEvent.WaitOne(TimeSpan.FromSeconds(3)))
                {
                    Bot watchingBot = _room.GetBotToMonitor(readingCommand);

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

                roomContentScroller.ScrollToEnd();
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
            _room.UsersUpdated -= Update;

            _stopWatcherEvent.Set();
        }

        private void composedActionBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (composedActionBox.SelectedItem == null)
                {
                    composedActionBox.Items.Clear();
                }
                else
                {
                    composedActionBox.Items.Remove(composedActionBox.SelectedItem);
                }
            }
        }
    }
}
