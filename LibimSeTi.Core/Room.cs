using System;
using System.Collections.Generic;
using System.Linq;

namespace LibimSeTi.Core
{
    public class Room
    {
        public enum EventType
        {
            Enter,
            Leave,
            Text
        }

        public class Event
        {
            public EventType Type { get; set; }
            public string UserName { get; set; }
            public string Text { get; set; }
        }

        private Event[] _content;
        private User[] _users;

        public event Action<Room> ContentUpdated;
        public event Action<Room> UsersUpdated;

        public DateTime LastRead { get; set; }

        public Room(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; private set; }

        public string Name { get; private set; }

        public Event[] Content
        {
            get { return _content; }

            set
            {
                _content = value;

                if (ContentUpdated != null)
                {
                    ContentUpdated(this);
                }
            }
        }

        public User[] Users
        {
            get { return _users; }

            set
            {
                _users = value;

                if (UsersUpdated != null)
                {
                    UsersUpdated(this);
                }
            }
        }

        public BotGroup[] AssignedBotGroups { get; set; }

        public IEnumerable<Bot> AssignedBots { get { return AssignedBotGroups?.SelectMany(group => group.Bots); } }

        public Bot GetBotToMonitor(BotCommand doableCommand)
        {
            return AssignedBots?.FirstOrDefault(bot => bot.Session != null && bot.Session.RoomsEntered != null && bot.Session.RoomsEntered.Contains(this) && doableCommand.CanDo(bot, true)) ??
                Model.Instance.BotGroups.SelectMany(group => group.Bots).FirstOrDefault(bot => bot.Session != null && bot.Session.RoomsEntered != null && bot.Session.RoomsEntered.Contains(this) && doableCommand.CanDo(bot, true));
        }
    }
}