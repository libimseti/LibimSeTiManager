using Starksoft.Aspen.Proxy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LibimSeTi.Core
{
    public static class Connector
    {
        private static readonly Socks5ProxyClient _socksClient = new Socks5ProxyClient(
            Configuration.Instance.Socks5Server,
            Configuration.Instance.Socks5Port,
            Configuration.Instance.Socks5User,
            Configuration.Instance.Socks5Password);

        private static IPAddress _ipAddress;

        public static async Task<string> Send(Action<HttpWebRequest> requestSetter, string requestContent,
            Tuple<string, string>[] requestReplacements)
        {
            int port = 300;

            return await Task.Run(() => {
                lock (_socksClient)
                {
                    int attempt = 0;
                    string response;
                    do
                    {
                        HttpWebRequest request = CreateRequest(port);

                        requestSetter(request);

                        response = Forward(
                            request,
                            requestContent != null ? Encoding.ASCII.GetBytes(requestContent) : null,
                            port,
                            requestReplacements,
                            _socksClient);

                        attempt++;
                    } while (string.IsNullOrEmpty(response) && attempt < 5);

                    return response;
                }
            });
        }

        public static async Task<IPAddress> GetIP()
        {
            if (_ipAddress == null)
            {
                int attempt = 0;

                do
                {
                    _ipAddress = await GetPublicIP();
                    attempt++;
                }
                while (_ipAddress == null);

                Logger.Instance.Info(string.Format("IP: {0}", _ipAddress));
            }

            return _ipAddress;
        }

        private static async Task<IPAddress> GetPublicIP()
        {
            return await Task.Run(() =>
            {
                lock (_socksClient)
                {
                    TcpClient client = _socksClient.CreateConnection("checkip.dyndns.org", 80);

                    if (client != null)
                    {
                        client.Client.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: checkip.dyndns.org\r\nConnection: Keep-Alive\r\n\r\n"));

                        string response = Encoding.ASCII.GetString(ReadToEnd(client.Client));

                        var ipMatch = Regex.Match(response, "Current IP Address: (\\d+\\.\\d+\\.\\d+\\.\\d+)");

                        if (ipMatch.Success && ipMatch.Groups.Count == 2)
                        {
                            return IPAddress.Parse(ipMatch.Groups[1].Value);
                        }
                    }

                    return null;
                }
            });
        }


        private static HttpWebRequest CreateRequest(int port)
        {
            return WebRequest.CreateHttp(string.Format("http://localhost:{0}", port));
        }

        private static string Forward(HttpWebRequest request, byte[] requestContent, int localPort,
            Tuple<string, string>[] requestReplacements, Socks5ProxyClient socksClient)
        {
            if (requestContent != null)
            {
                request.ContentLength = requestContent.Length;
            }

            byte[] requestBytes = GetRequestContent(request, requestContent, localPort);

            string requestString = Encoding.ASCII.GetString(requestBytes);

            if (requestReplacements != null)
            {
                foreach (var replacement in requestReplacements)
                {
                    requestString = requestString.Replace(replacement.Item1, replacement.Item2);
                }
            }

            TcpClient forwarder = ConnectToLibimSeTi(socksClient);
            forwarder.Client.Send(Encoding.ASCII.GetBytes(requestString));
            byte[] responseBytes = ReadToEnd(forwarder.Client);
            forwarder.Close();

            return Configuration.Instance.LibimSeTiEncoding.GetString(responseBytes);
        }

        private static byte[] GetRequestContent(HttpWebRequest request, byte[] requestContent, int localPort)
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, localPort);
            listener.Start();

            if (requestContent != null)
            {
                Stream postStream = request.GetRequestStream();
                postStream.Write(requestContent, 0, requestContent.Length);
                postStream.Flush();
                postStream.Close();
            }

            request.GetResponseAsync();

            var localSocket = listener.AcceptSocket();
            byte[] requestBytes = ReadToEnd(localSocket);
            localSocket.Close();

            listener.Stop();

            if (requestContent != null && !Encoding.ASCII.GetString(requestBytes).EndsWith(Encoding.ASCII.GetString(requestContent)))
            {
                requestBytes = requestBytes.Concat(requestContent).ToArray();
            }

            return requestBytes;
        }

        public static byte[] ReadToEnd(Socket socket)
        {
            byte[] buffer = new byte[300000];
            List<byte> requestBytes = new List<byte>();

            socket.ReceiveTimeout = 1000;

            int read;

            try {
                do
                {
                    read = socket.Receive(buffer);

                    if (read > 0)
                    {
                        for (int i = 0; i < read; i++)
                        {
                            requestBytes.Add(buffer[i]);
                        }

                        Thread.Sleep(100);
                    }

                } while (read > 0);
            }
            catch (SocketException)
            {
            }

            return requestBytes.ToArray();
        }

        public static TcpClient ConnectToLibimSeTi(Socks5ProxyClient socksClient)
        {
            return socksClient.CreateConnection(Configuration.Instance.LibimSeTiHostName, Configuration.Instance.LibimSeTiPort);
            //return new TcpClient(Configuration.Instance.LibimSeTiHostName, Configuration.Instance.LibimSeTiPort);
        }

        //public static string Request(string content)
        //{
        //    var tcpClient = ConnectToLibimSeTi();

        //    tcpClient.Client.Send(Encoding.ASCII.GetBytes(content));

        //    byte[] buffer = new byte[1000];

        //    int read;
        //    string text = string.Empty;

        //    do
        //    {
        //        read = tcpClient.Client.Receive(buffer);

        //        text += Configuration.Instance.LibimSeTiEncoding.GetString(buffer, 0, read);
        //    } while (read > 0);

        //    tcpClient.Close();
        //    return text;
        //}

    }
}