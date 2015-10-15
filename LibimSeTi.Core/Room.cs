using System;

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

        public Room(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; private set; }

        public string Name { get; private set; }

        public Event[] Content { get; set; }
    }
}