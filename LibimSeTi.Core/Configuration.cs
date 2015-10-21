using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace LibimSeTi.Core
{
    public class Configuration
    {
        private static Configuration _instance;

        private Configuration()
        {
            Load();
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
        public string Socks5User { get; private set; }
        public string Socks5Password { get; private set; }

        public Encoding LibimSeTiEncoding { get; private set; }

        public IList<BotGroup> BotGroups { get; private set; }

        private string ConfigFilePath { get { return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.xml"); } }

        private void Load()
        {
            if (!File.Exists(ConfigFilePath))
            {
                return;
            }

            XmlDocument config = new XmlDocument();
            config.Load(ConfigFilePath);

            LibimSeTiHostName = config.SelectSingleNode("Configuration/@HostName")?.Value ?? "chat.libimseti.cz";
            LibimSeTiPort = int.Parse(config.SelectSingleNode("Configuration/@Port")?.Value ?? "80");
            Socks5Server = config.SelectSingleNode("Configuration/@SocksServer")?.Value ?? "127.0.0.1";
            Socks5Port = int.Parse(config.SelectSingleNode("Configuration/@SocksPort")?.Value ?? "9150");
            Socks5User = config.SelectSingleNode("Configuration/@SocksUser")?.Value ?? "user";
            Socks5Password = config.SelectSingleNode("Configuration/@SocksPassword")?.Value ?? "password";
            LibimSeTiEncoding = Encoding.GetEncoding(int.Parse(config.SelectSingleNode("Configuration/@WebResponseEncoding")?.Value ?? "65001"));

            BotGroups = new List<BotGroup>();

            foreach (XmlNode botGroupNode in config.SelectNodes("Configuration/Bots/Group"))
            {
                BotGroup botGroup = new BotGroup(botGroupNode.Attributes["Name"].Value);

                foreach (XmlNode botNode in botGroupNode.SelectNodes("Bot"))
                {
                    List<string> messages = new List<string>();

                    foreach (XmlNode messageNode in botNode.SelectNodes("Message"))
                    {
                        messages.Add(messageNode.InnerText);
                    }

                    Bot bot = new Bot(botNode.Attributes["UserName"].Value, botNode.Attributes["Password"].Value, messages.ToArray());

                    botGroup.Bots.Add(bot);
                }

                BotGroups.Add(botGroup);
            }
        }

        public void Save()
        {
            XmlDocument config = new XmlDocument();
            XmlNode root = config.AppendChild(config.CreateElement("Configuration"));

            var attr = config.CreateAttribute("HostName");
            attr.Value = LibimSeTiHostName;
            root.Attributes.Append(attr);

            attr = config.CreateAttribute("Port");
            attr.Value = LibimSeTiPort.ToString();
            root.Attributes.Append(attr);

            attr = config.CreateAttribute("SocksServer");
            attr.Value = Socks5Server;
            root.Attributes.Append(attr);

            attr = config.CreateAttribute("SocksPort");
            attr.Value = Socks5Port.ToString();
            root.Attributes.Append(attr);

            attr = config.CreateAttribute("SocksUser");
            attr.Value = Socks5User;
            root.Attributes.Append(attr);

            attr = config.CreateAttribute("SocksPassword");
            attr.Value = Socks5Password;
            root.Attributes.Append(attr);

            attr = config.CreateAttribute("WebResponseEncoding");
            attr.Value = LibimSeTiEncoding.CodePage.ToString();
            root.Attributes.Append(attr);

            if (BotGroups != null)
            {
                XmlNode botsRoot = root.AppendChild(config.CreateElement("Bots"));

                foreach (BotGroup botGroup in BotGroups)
                {
                    XmlNode groupRoot = botsRoot.AppendChild(config.CreateElement("Group"));

                    attr = config.CreateAttribute("Name");
                    attr.Value = botGroup.Name;
                    groupRoot.Attributes.Append(attr);

                    foreach (Bot bot in botGroup.Bots)
                    {
                        XmlNode botNode = groupRoot.AppendChild(config.CreateElement("Bot"));

                        attr = config.CreateAttribute("UserName");
                        attr.Value = bot.Username;
                        botNode.Attributes.Append(attr);

                        attr = config.CreateAttribute("Password");
                        attr.Value = bot.Password;
                        botNode.Attributes.Append(attr);

                        if (bot.Messages != null)
                        {
                            foreach (string message in bot.Messages)
                            {
                                botNode.AppendChild(config.CreateElement("Message")).InnerText = message;
                            }
                        }
                    }
                }
            }

            config.Save(ConfigFilePath);
        }
    }
}