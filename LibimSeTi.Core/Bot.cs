namespace LibimSeTi.Core
{
    public class Bot
    {
        private LibimSeTiSession _session;

        public Bot(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string Username { get; private set; }
        public string Password { get; private set; }

        public LibimSeTiSession Session
        {
            get
            {
                if (_session == null)
                {
                    _session = new LibimSeTiSession(this);
                }

                return _session;
            }
        }
    }
}