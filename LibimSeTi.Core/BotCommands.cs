using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibimSeTi.Core
{
    public abstract class BotCommand
    {
        protected abstract string IntCanDo(Bot bot);
        public abstract Task Do(Bot bot);
        public abstract string Header { get; }

        public bool CanDo(Bot bot, bool suppressErrorMessage = false)
        {
            string errorMessage = IntCanDo(bot);

            if (errorMessage != null && !suppressErrorMessage)
            {
                Logger.Instance.Error(errorMessage);
            }

            return errorMessage == null;
        }

    }

    public class LogonCommand : BotCommand
    {
        protected override string IntCanDo(Bot bot)
        {
            return null;
        }

        public override async Task Do(Bot bot)
        {
            await bot.Session.Logon();
        }

        public override string Header
        {
            get
            {
                return "Log on";
            }
        }
    }

    public class EnterRoomCommand : BotCommand
    {
        public Room Room { get; set; }

        protected override string IntCanDo(Bot bot)
        {
            if (Room == null)
            {
                return "No room set";
            }

            return !bot.Session.IsLoggedOn ? string.Format("[{0}] Not logged on", bot.Username) : null;
        }

        public async override Task Do(Bot bot)
        {
            await bot.Session.EnterRoom(Room);
        }

        public override string Header
        {
            get
            {
                return string.Format("Enter {0}", Room != null ? Room.Name : "N/A");
            }
        }
    }

    public class LeaveRoomCommand : BotCommand
    {
        public Room Room { get; set; }

        protected override string IntCanDo(Bot bot)
        {
            if (Room == null)
            {
                return "No room set";
            }

            return !bot.Session.IsLoggedOn ? string.Format("[{0}] Not logged on", bot.Username) : null;
        }

        public async override Task Do(Bot bot)
        {
            await bot.Session.LeaveRoom(Room);
        }

        public override string Header
        {
            get
            {
                return string.Format("Leave {0}", Room != null ? Room.Name : "N/A");
            }
        }
    }

    public class ReadRoomCommand : BotCommand
    {
        public Room Room { get; set; }

        protected override string IntCanDo(Bot bot)
        {
            if (Room == null)
            {
                return "No room set";
            }

            if (!bot.Session.IsLoggedOn)
            {
                return string.Format("[{0}] Not logged on", bot.Username);
            }

            if (!bot.Session.RoomsEntered.Contains(Room))
            {
                return string.Format("Bot not in [{0}]", Room.Name);
            }

            return null;
        }

        public async override Task Do(Bot bot)
        {
            await bot.Session.ReadRoom(Room);
        }

        public override string Header
        {
            get
            {
                return string.Format("Read {0}", Room != null ? Room.Name : "N/A");
            }
        }
    }

    public class TextRoomCommand : BotCommand
    {
        public Room Room { get; set; }
        public Func<string> TextGetter { get; set; }

        protected override string IntCanDo(Bot bot)
        {
            if (Room == null)
            {
                return "No room set";
            }

            if (TextGetter == null)
            {
                return "No text function set";
            }

            if (string.IsNullOrEmpty(TextGetter()))
            {
                return "No text set";
            }

            if (!bot.Session.IsLoggedOn)
            {
                return string.Format("[{0}] Not logged on", bot.Username);
            }

            if (!bot.Session.RoomsEntered.Contains(Room))
            {
                return string.Format("Bot not in [{0}]", Room.Name);
            }

            return null;
        }

        public async override Task Do(Bot bot)
        {
            await bot.Session.SendText(Room, TextGetter());
        }

        public override string Header
        {
            get
            {
                return string.Format("[{0}] >> {1}", Room != null ? Room.Name : "N/A", TextGetter != null ? TextGetter() : "N/A");
            }
        }
    }

    public class PauseCommand : BotCommand
    {
        public Func<int> PauseAmountGetter { get; set; }

        protected override string IntCanDo(Bot bot)
        {
            if (PauseAmountGetter == null)
            {
                return "No pause function set";
            }

            return null;
        }

        public override Task Do(Bot bot)
        {
            Logger.Instance.Info(string.Format("[{0}] {1}", bot.Username, Header));

            return Task.Delay(PauseAmountGetter() * 1000);
        }

        public override string Header
        {
            get
            {
                return string.Format("Pause for {0} s", PauseAmountGetter != null ? PauseAmountGetter().ToString() : "N/A");
            }
        }
    }
}