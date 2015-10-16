using System.Collections.Generic;

namespace LibimSeTi.Core
{
    public class BotGroup
    {
        public BotGroup(string name)
        {
            Name = name;

            Bots = new List<Bot>();
        }

        public string Name { get; private set; }

        public IList<Bot> Bots { get; private set; }
    }
}