using System;
using System.Collections.Generic;
using System.Net;

using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LibimSeTi.Core
{
    public class LibimSeTiSession
    {
        private string _userId;
        private string _logonToken;
        private string _hashId;
        private string _uid;

        public LibimSeTiSession(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string Username { get; private set; }
        public string Password { get; private set; }

        public bool IsLoggedOn
        {
            get
            {
                return _logonToken != null && _userId != null;
            }
        }

        public async Task Logon()
        {
            Logger.Instance.Info(string.Format("[{0}] Logging on", Username));

            string response = await LibimSeTiConnector.Send(
                request =>
                {
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
                });

            var tokenMatch = Regex.Match(response, "token=([a-fA-F0-9]+)");

            if (tokenMatch.Success && tokenMatch.Groups.Count == 2)
            {
                _logonToken = tokenMatch.Groups[1].Value;
            }

            var hashMatch = Regex.Match(response, "hashId=([0-9]+)");

            if (hashMatch.Success && hashMatch.Groups.Count == 2)
            {
                _hashId = hashMatch.Groups[1].Value;
            }

            var userIdMatch = Regex.Match(response, "id_user=([0-9]+)");

            if (userIdMatch.Success && userIdMatch.Groups.Count == 2)
            {
                _userId = userIdMatch.Groups[1].Value;
            }

            var uidMatch = Regex.Match(response, "uid=([a-fA-F0-9]+)");

            if (uidMatch.Success && uidMatch.Groups.Count == 2)
            {
                _uid = uidMatch.Groups[1].Value;
            }

            if (!IsLoggedOn)
            {
                Logger.Instance.Info(string.Format("[{0}] Logon failed", Username));
                throw new UnauthorizedAccessException();
            }

            Logger.Instance.Info(string.Format("[{0}] Logged on", Username));
        }

        public async Task EnterRoom(Room room)
        {
            Logger.Instance.Info(string.Format("[{0}] Entering room {1}", Username, room.Name));

            string response = await LibimSeTiConnector.Send(
                request => {
                    request.Method = "GET";
                    request.Host = "chat.libimseti.cz";
                    request.KeepAlive = true;
                    request.Expect = string.Empty;
                    request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                    request.CookieContainer = new CookieContainer();
                    request.CookieContainer.Add(new Cookie("hashId", _hashId, "/", ".libimseti.cz"));
                    request.CookieContainer.Add(new Cookie("id_user", _userId, "/", ".libimseti.cz"));
                    request.CookieContainer.Add(new Cookie("uid", _uid, "/", ".libimseti.cz"));
                },
                null,
                new[]
                {
                    new Tuple<string, string>("GET / ",
                        string.Format("GET /room.py?act=enter&room_ID={0}&token={1} ", room.Id, _logonToken)),
                    new Tuple<string, string>("Expect: 100-continue\r\n", string.Empty)
                });

            if (!response.Contains("&act=text&token="))
            {
                Logger.Instance.Info(string.Format("[{0}] Room failed to enter", Username));
                throw new Exception("Not entered room.");
            }

            Logger.Instance.Info(string.Format("[{0}] Room entered", Username));
        }

        public async Task LeaveRoom(Room room)
        {
            Logger.Instance.Info(string.Format("[{0}] Leaving room {1}", Username, room.Name));

            string response = await LibimSeTiConnector.Send(
                request => {
                    request.Method = "GET";
                    request.Host = "chat.libimseti.cz";
                    request.KeepAlive = true;
                    request.Expect = string.Empty;
                    request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                    request.CookieContainer = new CookieContainer();
                    request.CookieContainer.Add(new Cookie("hashId", _hashId, "/", ".libimseti.cz"));
                    request.CookieContainer.Add(new Cookie("id_user", _userId, "/", ".libimseti.cz"));
                    request.CookieContainer.Add(new Cookie("uid", _uid, "/", ".libimseti.cz"));
                },
                null,
                new[]
                {
                    new Tuple<string, string>("GET / ",
                        string.Format("GET /room.py?act=leave&room_ID={0}&token={1} ", room.Id, _logonToken)),
                    new Tuple<string, string>("Expect: 100-continue\r\n", string.Empty)
                });

            if (response.Contains("&act=text&token="))
            {
                Logger.Instance.Info(string.Format("[{0}] Room failed to leave", Username));
                throw new Exception("Not entered room.");
            }

            Logger.Instance.Info(string.Format("[{0}] Room left", Username));
        }

        public async Task ReadRoom(Room room)
        {
            Logger.Instance.Info(string.Format("[{0}] Reading room {1}", Username, room.Name));

            string response = await LibimSeTiConnector.Send(
                request => {
                    request.Method = "GET";
                    request.Host = "chat.libimseti.cz";
                    request.KeepAlive = true;
                    request.Expect = string.Empty;
                    request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                    request.CookieContainer = new CookieContainer();
                    request.CookieContainer.Add(new Cookie("hashId", _hashId, "/", ".libimseti.cz"));
                    request.CookieContainer.Add(new Cookie("id_user", _userId, "/", ".libimseti.cz"));
                    request.CookieContainer.Add(new Cookie("uid", _uid, "/", ".libimseti.cz"));
                },
                null,
                new[]
                {
                    new Tuple<string, string>("GET / ",
                        string.Format("GET /room.py?act=read&room_ID={0}&token={1} ", room.Id, _logonToken)),
                    new Tuple<string, string>("Expect: 100-continue\r\n", string.Empty)
                });

            List<Room.Event> content = new List<Room.Event>();

            foreach (string line in response.Split(new[] {"\n"}, StringSplitOptions.None))
            {
                if (line.Contains("'whisper'"))
                {
                    continue;
                }

                if (line.StartsWith("addText"))
                {
                    string[] lineParts = line.Split(',');

                    if (lineParts.Length > 6 && lineParts[2] != " ''")
                    {
                        content.Add(new Room.Event()
                        {
                            Type = Room.EventType.Text,
                            UserName = lineParts[2].Substring(2, lineParts[2].Length - 3),
                            Text = lineParts[6].Substring(2, lineParts[6].Length - 3)
                        });
                    }
                    else if (lineParts.Length >= 14 && lineParts[2] == " ''" && line.Contains("'enterUser'"))
                    {
                        content.Add(new Room.Event()
                        {
                            Type = Room.EventType.Enter,
                            UserName = Regex.Match(lineParts[6], ">(.*)</b>").Groups[1].Value
                        });
                    }
                    else if (lineParts.Length >= 14 && lineParts[2] == " ''" && line.Contains("'leaveUser'"))
                    {
                        content.Add(new Room.Event()
                        {
                            Type = Room.EventType.Leave,
                            UserName = Regex.Match(lineParts[6], ">(.*)</b>").Groups[1].Value
                        });
                    }
                }
            }

            room.Content = content.ToArray();
        }
    }
}