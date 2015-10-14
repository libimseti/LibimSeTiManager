using System.Text;

namespace LibimSeTi.Core
{
    public class Configuration
    {
        private static Configuration _instance;

        private Configuration()
        {
            LibimSeTiHostName = "chat.libimseti.cz";
            LibimSeTiPort = 80;
            LibimSeTiEncoding = Encoding.UTF8;
            Socks5Server = "127.0.0.1";
            Socks5Port = 9150;
        }

        public static Configuration Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Configuration();
                }

                return _instance;
            }
        }

        public string LibimSeTiHostName { get; private set; }

        public int LibimSeTiPort { get; private set; }

        public string Socks5Server { get; private set; }

        public int Socks5Port { get; private set; }

        public Encoding LibimSeTiEncoding { get; private set; }
    }
}