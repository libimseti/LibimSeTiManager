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

        public event Action<Room> ContentUpdated;

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

        public User[] Users { get; set; }

        public BotGroup[] AssignedBotGroups { get; set; }

        public IEnumerable<Bot> AssignedBots { get { return AssignedBotGroups?.SelectMany(group => group.Bots); } }
    }
}