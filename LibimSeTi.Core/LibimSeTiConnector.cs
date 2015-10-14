using Starksoft.Aspen.Proxy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LibimSeTi.Core
{
    public static class LibimSeTiConnector
    {
        public static string Send(Action<HttpWebRequest> requestSetter, string requestContent,
            Tuple<string, string>[] requestReplacements, Socks5ProxyClient socksClient)
        {
            int port = 300;

            HttpWebRequest request = CreateRequest(port);

            requestSetter(request);

            return Forward(request, Encoding.ASCII.GetBytes(requestContent), port, requestReplacements, socksClient);
        }

        private static HttpWebRequest CreateRequest(int port)
        {
            return WebRequest.CreateHttp(string.Format("http://localhost:{0}", port));
        }

        private static string Forward(HttpWebRequest request, byte[] requestContent, int localPort,
            Tuple<string, string>[] requestReplacements, Socks5ProxyClient socksClient)
        {
            request.ContentLength = requestContent.Length;
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

            Stream postStream = request.GetRequestStream();
            postStream.Write(requestContent, 0, requestContent.Length);
            postStream.Flush();
            postStream.Close();

            request.GetResponseAsync();

            var localSocket = listener.AcceptSocket();
            byte[] requestBytes = ReadToEnd(localSocket);
            localSocket.Close();

            listener.Stop();

            if (!Encoding.ASCII.GetString(requestBytes).EndsWith(Encoding.ASCII.GetString(requestContent)))
            {
                requestBytes = requestBytes.Concat(requestContent).ToArray();
            }

            return requestBytes;
        }

        public static byte[] ReadToEnd(Socket socket)
        {
            byte[] buffer = new byte[300000];
            List<byte> requestBytes = new List<byte>();

            socket.ReceiveTimeout = 100;

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
            return new TcpClient(Configuration.Instance.LibimSeTiHostName, Configuration.Instance.LibimSeTiPort);
        }

        public static TcpClient Connect(string host, int port)
        {
            Socks5ProxyClient socksClient = new Socks5ProxyClient(Configuration.Instance.Socks5Server, Configuration.Instance.Socks5Port);
            return socksClient.CreateConnection(host, port);
            return new TcpClient(Configuration.Instance.LibimSeTiHostName, Configuration.Instance.LibimSeTiPort);
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