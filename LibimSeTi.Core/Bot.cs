namespace LibimSeTi.Core
{
    public class Bot
    {
        private Session _session;

        public Bot(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string Username { get; private set; }
        public string Password { get; private set; }

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