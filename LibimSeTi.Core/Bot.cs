namespace LibimSeTi.Core
{
    public class Bot
    {
        private Session _session;

        public Bot(string username, string password, string[] messages)
        {
            Username = username;
            Password = password;
            Messages = messages;
        }

        public string Username { get; private set; }
        public string Password { get; private set; }
        public string[] Messages { get; private set; }

        public Session Session
        {
            get
            {
                if (_session == null)
                {
                    _session = new Session(this);
                }

                return _session;
            }
        }
    }
}