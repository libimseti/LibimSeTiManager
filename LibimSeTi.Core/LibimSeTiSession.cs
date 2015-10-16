using System;
using System.Collections.Generic;
using System.Net;

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace LibimSeTi.Core
{
    public class LibimSeTiSession
    {
        private string _userId;
        private string _logonToken;
        private string _hashId;
        private string _uid;
        private Bot _bot;

        public LibimSeTiSession(Bot bot)
        {
            _bot = bot;
        }

        public bool IsLoggedOn
        {
            get
            {
                return _logonToken != null && _userId != null;
            }
        }

        public async Task Logon()
        {
            Logger.Instance.Info(string.Format("[{0}] Logging on", _bot.Username));

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
                _bot.Username,
                _bot.Password),
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
                Logger.Instance.Info(string.Format("[{0}] Logon failed", _bot.Username));
                throw new UnauthorizedAccessException();
            }

            Logger.Instance.Info(string.Format("[{0}] Logged on", _bot.Username));
        }

        public async Task EnterRoom(Room room)
        {
            Logger.Instance.Info(string.Format("[{0}] Entering room {1}", _bot.Username, room.Name));

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
                Logger.Instance.Info(string.Format("[{0}] Room failed to enter", _bot.Username));
                throw new Exception("Not entered room.");
            }

            Logger.Instance.Info(string.Format("[{0}] Room entered", _bot.Username));
        }

        public async Task LeaveRoom(Room room)
        {
            Logger.Instance.Info(string.Format("[{0}] Leaving room {1}", _bot.Username, room.Name));

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
                Logger.Instance.Info(string.Format("[{0}] Room failed to leave", _bot.Username));
                throw new Exception("Not entered room.");
            }

            Logger.Instance.Info(string.Format("[{0}] Room left", _bot.Username));
        }

        public async Task ReadRoom(Room room)
        {
            Logger.Instance.Info(string.Format("[{0}] Reading room {1}", _bot.Username, room.Name));

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

        public async Task SendText(Room room, string text)
        {
            Logger.Instance.Info(string.Format("[{0}] [{1}] >> {2}", _bot.Username, room.Name, text));

            string response = await LibimSeTiConnector.Send(
                request =>
                {
                    request.Method = "POST";
                    request.Host = "chat.libimseti.cz";
                    request.KeepAlive = true;
                    request.Expect = string.Empty;
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                    request.CookieContainer = new CookieContainer();
                    request.CookieContainer.Add(new Cookie("hashId", _hashId, "/", ".libimseti.cz"));
                    request.CookieContainer.Add(new Cookie("id_user", _userId, "/", ".libimseti.cz"));
                    request.CookieContainer.Add(new Cookie("uid", _uid, "/", ".libimseti.cz"));
                },
                string.Format(
                "token={0}&act=text&room_ID={1}&mini=0&out=1&friend_ID_list=&keep=1&text={2}",
                _logonToken,
                room.Id,
                HttpUtility.UrlEncode(text)),
                new[]
                {
                new Tuple<string, string>("POST / ", "POST /room.py "),
                new Tuple<string, string>("Expect: 100-continue\r\n", string.Empty)
                });
         }

    }
}