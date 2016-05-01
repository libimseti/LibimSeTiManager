using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace LibimSeTi.Core
{
    public class Session
    {
        private string _userId;
        private string _logonToken;
        private string _hashId;
        private string _uid;
        private Bot _bot;

        private List<Room> _roomsEntered;

        public Session(Bot bot)
        {
            _bot = bot;
            _roomsEntered = new List<Room>();
        }

        public bool IsLoggedOn
        {
            get
            {
                return _logonToken != null && _userId != null;
            }
        }

        public IEnumerable<Room> RoomsEntered
        {
            get
            {
                return _roomsEntered;
            }
        }

        public async Task Logon()
        {
            Logger.Instance.Info(string.Format("[{0}] Logging on", _bot.Username));

            string response = await Connector.Send(
				"login",
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
                _bot.Password));

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
                Logger.Instance.Error(string.Format("[{0}] Logon failed", _bot.Username));
                throw new UnauthorizedAccessException();
            }

            Logger.Instance.Info(string.Format("[{0}] Logged on", _bot.Username));
        }

        public async Task EnterRoom(Room room)
        {
            Logger.Instance.Info(string.Format("[{0}] Entering room {1}", _bot.Username, room.Name));

            string response = await Connector.Send(
				string.Format("room.py?act=enter&room_ID={0}&token={1}", room.Id, _logonToken),
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
                null);

            if (!response.Contains("&act=text&token="))
            {
                _roomsEntered.Remove(room);
                Logger.Instance.Error(string.Format("[{0}] Room failed to enter", _bot.Username));
                throw new Exception("Not entered room.");
            }

            _roomsEntered.Add(room);

            Logger.Instance.Info(string.Format("[{0}] Room entered", _bot.Username));

            if ((DateTime.Now - room.LastRead).TotalMinutes > 1)
            {
                Logger.Instance.Info(string.Format("[{0}] Room entered - initiating reading", _bot.Username));

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                ReadRoom(room);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        public async Task LeaveRoom(Room room)
        {
            Logger.Instance.Info(string.Format("[{0}] Leaving room {1}", _bot.Username, room.Name));

            string response = await Connector.Send(
				string.Format("room.py?act=leave&room_ID={0}&token={1} ", room.Id, _logonToken),
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
                null);

            if (response.Contains("&act=text&token="))
            {
                Logger.Instance.Error(string.Format("[{0}] Room failed to leave", _bot.Username));
                throw new Exception("Not entered room.");
            }

            _roomsEntered.Remove(room);

            Logger.Instance.Info(string.Format("[{0}] Room left", _bot.Username));
        }

        public async Task ReadRoom(Room room)
        {
            Logger.Instance.Info(string.Format("[{0}] Reading room {1}", _bot.Username, room.Name));

			room.LastRead = DateTime.Now;

			List<Room.Event> content = new List<Room.Event>();
            int attemptCounter = 0;

            do
            {
                if (attemptCounter > 0)
                {
                    Logger.Instance.Info("Re-reading");
                }

                string response = await Connector.Send(
					string.Format("room.py?act=read&room_ID={0}&token={1} ", room.Id, _logonToken),
					request =>
                    {
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
                    null);

                foreach (string line in response.Split(new[] { "\n" }, StringSplitOptions.None))
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

                attemptCounter++;
            } while (content.Count == 0 && attemptCounter < 3);

            if (content.Count > 0)
            {
                room.Content = content.ToArray();
            }
            else
            {
                _roomsEntered.Remove(room);
                Logger.Instance.Info(string.Format("[{0}] Reading failed", _bot.Username));
            }
        }

        public async Task SendText(Room room, string text)
        {
            Logger.Instance.Info(string.Format("[{0}] [{1}] >> {2}", _bot.Username, room.Name, text));

            await Connector.Send(
				"room.py",
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
                HttpUtility.UrlEncode(text)));

            Logger.Instance.Info(string.Format("[{0}] [{1}] text sent", _bot.Username, room.Name));
        }

		public async Task SendMessage(string username, string text)
		{
			Logger.Instance.Info(string.Format("[{0}] >> {1} >> {2}", _bot.Username, username, text));

			await Connector.Send(
				string.Format("zpravy/{0}?msg=sent", HttpUtility.UrlEncode(username)),
				request =>
				{
					request.Method = "POST";
					request.Host = "vzkazy.libimseti.cz";
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
				"token={0}&username={1}&message={2}",
				_logonToken,
				HttpUtility.UrlEncode(username),
				HttpUtility.UrlEncode(text)));

			Logger.Instance.Info(string.Format("[{0}] message sent", _bot.Username));
		}

		public static async Task<Room[]> FindAllRooms()
        {
            return await Task.Run(() =>
            {
                Logger.Instance.Info("Retrieving rooms");

                List<Room> result = new List<Room>();

                HttpWebRequest chatPageRequest = WebRequest.CreateHttp(string.Format("http://{0}", Configuration.Instance.LibimSeTiHostName));

                var response = chatPageRequest.GetResponse();

                string content = new StreamReader(response.GetResponseStream()).ReadToEnd();

                foreach (Match categoryMatch in Regex.Matches(content, "act=room_list&category_ID=(\\d+)\">.*? title=\"(.*?)\"></a>"))
                {
                    if (categoryMatch.Groups.Count != 3)
                    {
                        continue;
                    }

                    HttpWebRequest categoryPageRequest = WebRequest.CreateHttp(string.Format("http://{0}/index.py?act=room_list&category_ID={1}", Configuration.Instance.LibimSeTiHostName, categoryMatch.Groups[1].Value));

                    response = categoryPageRequest.GetResponse();
                    content = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    foreach (Match roomMatch in Regex.Matches(content, "/room.py\\?act=enter&room_ID=(\\d+)&.*?>(.*?)</a>(.*?)</tr>", RegexOptions.Singleline))
                    {
                        if (roomMatch.Groups.Count != 4)
                        {
                            continue;
                        }

                        Room room = new Room(int.Parse(roomMatch.Groups[1].Value), roomMatch.Groups[2].Value);

                        List<User> roomUsers = new List<User>();

                        foreach (Match userMatch in Regex.Matches(roomMatch.Groups[3].Value, "sex_(.) card\".*?>(.*?)</a>", RegexOptions.Singleline))
                        {
                            if (userMatch.Groups.Count != 3)
                            {
                                continue;
                            }

                            User.Sex sex;
                            
                            switch (userMatch.Groups[1].Value)
                            {
                                case "m":
                                    sex = User.Sex.Male;
                                    break;
                                case "f":
                                    sex = User.Sex.Female;
                                    break;
                                default:
                                    continue;
                            }

                            roomUsers.Add(new User(userMatch.Groups[2].Value, sex));
                        }

                        room.Users = roomUsers.ToArray();

                        result.Add(room);
                    }
                }

                Logger.Instance.Info(string.Format("{0} rooms found", result.Count));

                return result.ToArray();
            });
        }

        public static async Task<User> GetUserInfo(string username)
        {
            Logger.Instance.Info(string.Format("Reading user info for [{0}]", username));

            string response = await Connector.Send(
				string.Format("user/info?callback=jQuery1705234449510280471_1445100660724&username={0} ", username),
				request => {
                    request.Method = "GET";
                    request.Host = "ajaxapi.libimseti.cz";
                    request.KeepAlive = true;
                    request.Expect = string.Empty;
                    request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                },
                null);

            int? userId;

            Match userIdMatch = Regex.Match(response, "\"userId\":(\\d+)");

            if (userIdMatch.Success && userIdMatch.Groups.Count == 2)
            {
                userId = int.Parse(userIdMatch.Groups[1].Value);
            }
            else
            {
                userId = null;
            }

            User.Sex? sex;

            Match sexMatch = Regex.Match(response, "\"sex\":\"(.)\"");

            if (sexMatch.Success && sexMatch.Groups.Count == 2)
            {
                switch(sexMatch.Groups[1].Value)
                {
                    case "m":
                        sex = User.Sex.Male;
                        break;
                    case "f":
                        sex = User.Sex.Female;
                        break;
                    default:
                        sex = null;
                        break;
                }
            }
            else
            {
                sex = null;
            }

            if (userId != null && sex != null)
            {
                return new User(username, sex.Value) { Id = userId.Value };
            }

            return null;
        }

        public static async Task<CaptchaToken> GetRegistrationCaptcha()
        {
            Logger.Instance.Info("Getting registration captcha");

            string response = await Connector.Send(
				string.Empty,
                request => {
                    request.Method = "GET";
                    request.Host = "registrace.libimseti.cz";
                    request.KeepAlive = true;
                    request.Expect = string.Empty;
                    request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                },
                null);

            Match captchaMatch = Regex.Match(response, "http://registrace\\.libimseti\\.cz/captcha/(\\d+)");

            if (captchaMatch.Success && captchaMatch.Groups.Count == 2)
            {
                return new CaptchaToken { Key = int.Parse(captchaMatch.Groups[1].Value) };
            }

            return null;
        }

        public static async Task Register(RegistrationData registrationData, CaptchaToken captchaToken, string captchaTyped)
        {
            Logger.Instance.Info(string.Format("[{0}] Registering", registrationData.UserName));

            string response = await Connector.Send(
				string.Empty,
                request =>
                {
                    request.Method = "POST";
                    request.Host = "registrace.libimseti.cz";
                    request.KeepAlive = true;
                    request.Expect = string.Empty;
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                },
                string.Format(
                "name={0}&pass1={1}&pass2={1}&email={2}&sex={3}&birthDay={4}&birthMonth={5}&birthYear={6}&okres=o3&captcha_val={7}&captcha_key={8}&podminky=on&action=save&referer=",
                registrationData.UserName,
                registrationData.Password,
                registrationData.Email,
                registrationData.Sex == User.Sex.Male ? "m" : "f",
                registrationData.BirthDate.Day,
                registrationData.BirthDate.Month,
                registrationData.BirthDate.Year,
                captchaTyped,
                captchaToken.Key));

            if (!response.Contains(string.Format("e_login={0}&pswdhash", registrationData.UserName)))
            {
                Logger.Instance.Error(string.Format("[{0}] Not registered", registrationData.UserName));
                throw new Exception("Not registered.");
            }

            Logger.Instance.Info(string.Format("[{0}] Registered", registrationData.UserName));
        }

    }
}