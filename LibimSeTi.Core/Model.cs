using System.Collections.Generic;
using System.Threading.Tasks;

namespace LibimSeTi.Core
{
    public class Model
    {
        private static Model _instance;

        private Model()
        {
        }

        public IEnumerable<Room> AllRooms { get; private set; }

        public IEnumerable<BotGroup> BotGroups { get { return Configuration.Instance.BotGroups; } }

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

        public async Task RetrieveAllRooms()
        {
           AllRooms = await Session.FindAllRooms();
        }
    }
}