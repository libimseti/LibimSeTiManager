using System;

namespace LibimSeTi.Core
{
    public class Logger
    {
        private static Logger _instance;

        public event Action<string> Message;

        private Logger()
        { }

        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Logger();
                }

                return _instance;
            }
        }

        public void Info(string message)
        {
            if (Message != null)
            {
                Message(message);
            }
        }

        public void Error(string message)
        {
            if (Message != null)
            {
                Message(message);
            }
        }
    }
}