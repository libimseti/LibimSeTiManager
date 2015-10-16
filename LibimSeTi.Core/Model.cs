using System.Collections.Generic;

namespace LibimSeTi.Core
{
    public class Model
    {
        private static Model _instance;

        private Model()
        {
            AllRooms = new[] { new Room(351818, "Cela do naha") };


            var bots = new BotGroup("helicobacters");
            bots.Bots.Add(new Bot("helicobacter2", "123456789"));

            BotGroups = new[] { bots };
        }

        public IEnumerable<Room> AllRooms { get; private set; }

        public IEnumerable<BotGroup> BotGroups { get; private set; }

        public static Model Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Model();
                }

                return _instance;
            }
        }
    }
}