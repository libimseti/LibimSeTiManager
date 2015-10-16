using LibimSeTi.Core;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace LibimSeTiManager
{
    /// <summary>
    /// Interaction logic for RoomWindow.xaml
    /// </summary>
    public partial class RoomWindow : Window
    {
        private readonly Room _room;

        public RoomWindow(Room room)
        {
            InitializeComponent();

            Title = string.Format("Room {0}", room.Name);
        }

        private void ShowRoom(Room room)
        {
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
    }
}
