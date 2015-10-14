using Starksoft.Aspen.Proxy;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;


namespace LibimSeTi.Core
{
    public class LibimSeTiSession
    {
        private readonly Socks5ProxyClient _socksClient;
        private IPAddress _ipAddress;

        public LibimSeTiSession(string username, string password)
        {
            Username = username;
            Password = password;

            _socksClient = new Socks5ProxyClient(Configuration.Instance.Socks5Server, Configuration.Instance.Socks5Port);
        }

        public string Username { get; private set; }
        public string Password { get; private set; }

        private IPAddress GetPublicIP()
        {
            TcpClient client = _socksClient.CreateConnection("checkip.dyndns.org", 80);

            client.Client.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: checkip.dyndns.org\r\nConnection: Keep-Alive\r\n\r\n"));

            string response = Encoding.ASCII.GetString(LibimSeTiConnector.ReadToEnd(client.Client));

            var ipMatch = Regex.Match(response, "Current IP Address: (\\d+\\.\\d+\\.\\d+\\.\\d+)");

            if (ipMatch.Success && ipMatch.Groups.Count == 2)
            {
                return IPAddress.Parse(ipMatch.Groups[1].Value);
            }

            return null;
        }

        public IPAddress IP
        {
            get
            {
                if (_ipAddress == null)
                {
                    _ipAddress = GetPublicIP();
                }

                return _ipAddress;
            }
        }

        public void Logon()
        {
            string s = LibimSeTiConnector.Send(
                request => {
                    request.Method = "POST";
                    request.Host = "libimseti.cz";
                    request.KeepAlive = true;
                    request.Expect = string.Empty;
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                },
                string.Format(
                "e_login={0}&e_pass={1}&a=l&urlCrc=49a355a5b043ab8ccd316747cff2c735&targetUrl=http%3A%2F%2Fchat.libimseti.cz%2Findex.py%3F",
                Username,
                Password),
                new[]
                {
                    new Tuple<string, string>("POST / ", "POST /login "),
                    new Tuple<string, string>("Expect: 100-continue\r\n", string.Empty)
                },
                _socksClient);
        }
    }
}