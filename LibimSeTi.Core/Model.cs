using System.Collections.Generic;
using System.Linq;
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
            Room[] rooms = await Session.FindAllRooms();
            
            if (AllRooms == null)
            {
                AllRooms = rooms;
            }
            else
            {
                foreach (Room foundRoom in rooms)
                {
                    Room existingRoom = AllRooms.FirstOrDefault(room => room.Id == foundRoom.Id);

                    if (existingRoom != null)
                    {
                        if (foundRoom.Content != null)
                        {
                            existingRoom.Content = foundRoom.Content;
                        }

                        existingRoom.Users = foundRoom.Users;
                    }
                }
            }
        }
    }
}