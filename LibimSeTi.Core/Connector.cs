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

		public static bool UseSocks { private get; set; } = true;

        private static IPAddress _ipAddress;

        public static async Task<string> Send(string resource, Action<HttpWebRequest> requestSetter, string requestContent)
        {
            int port = 300;

            return await Task.Run(() => {
                lock (_socksClient)
                {
                    int attempt = 0;
                    string response;
                    do
                    {
						HttpWebRequest request = CreateSocksOriginalRequest(resource, port);

                        requestSetter(request);

						response = Forward(
							request,
							resource,
							requestContent != null ? Encoding.ASCII.GetBytes(requestContent) : null,
							port,
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
					try
					{
						_ipAddress = await GetPublicIP();
					}
					catch { };

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


        private static HttpWebRequest CreateSocksOriginalRequest(string resource, int port)
        {
            return WebRequest.CreateHttp(string.Format("http://localhost:{0}", port, resource));
        }

        private static string Forward(HttpWebRequest request, string resource, byte[] requestContent, int localPort,
            Socks5ProxyClient socksClient)
        {
			// uncomment to debug request text
			//if (requestContent != null)
			//{
			//	request.ContentLength = requestContent.Length;
			//}

			//byte[] requestBytes = GetRequestContent(request, requestContent, localPort);

			//string requestString = Encoding.ASCII.GetString(requestBytes);

			//if (requestReplacements != null)
			//{
			//	foreach (var replacement in requestReplacements)
			//	{
			//		requestString = requestString.Replace(replacement.Item1, replacement.Item2);
			//	}
			//}

			string requestString2 = request.GetRequestString(resource, requestContent);

			using (TcpClient libimsetiClient = ConnectToLibimSeTi(socksClient))
			{
				libimsetiClient.Client.Send(Encoding.ASCII.GetBytes(requestString2));

				byte[] responseBytes = ReadToEnd(libimsetiClient.Client);

				return Configuration.Instance.LibimSeTiEncoding.GetString(responseBytes);
			}
        }


		private static TcpListener _listener;

		private static byte[] GetRequestContent(HttpWebRequest request, byte[] requestContent, int localPort)
        {
			if (_listener == null)
			{
				_listener = new TcpListener(IPAddress.Loopback, localPort);
				_listener.Start();
			}

			if (requestContent != null)
			{
				Stream postStream = request.GetRequestStream();
				postStream.Write(requestContent, 0, requestContent.Length);
				postStream.Close();
			}

            request.GetResponseAsync();

            var localSocket = _listener.AcceptSocket();
            byte[] requestBytes = ReadToEnd(localSocket);
            localSocket.Close();

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

            socket.ReceiveTimeout = 10000;

			DateTime start = DateTime.Now;

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

					if ((DateTime.Now - start).TotalMilliseconds > socket.ReceiveTimeout)
					{
						throw new SocketException();
					}

                } while (read > 0 || requestBytes.Count == 0);
            }
            catch (SocketException e)
            {
            }

            return requestBytes.ToArray();
        }

        private static TcpClient ConnectToLibimSeTi(Socks5ProxyClient socksClient)
        {
            return UseSocks ?
				socksClient.CreateConnection(Configuration.Instance.LibimSeTiHostName, Configuration.Instance.LibimSeTiPort)
				:
				new TcpClient(Configuration.Instance.LibimSeTiHostName, Configuration.Instance.LibimSeTiPort);
        }
    }
}