using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace LibimSeTi.Core
{
    public interface IIdentityProvider
    {
        string Name { get; }
        RegistrationData GetNextIdentity();
    }

    public class ExistingBotsProvider : IIdentityProvider
    {
        public string Name
        {
            get
            {
                return "Reregistration";
            }
        }

        public ExistingBotsProvider(BotGroup botGroup)
        {
            _botGroup = botGroup;
        }

        private BotGroup _botGroup;
        private int _groupIndex;

        public RegistrationData GetNextIdentity()
        {
            Bot currentBot = _botGroup.Bots.Skip(_groupIndex).FirstOrDefault();

            if (currentBot == null)
            {
                return null;
            }

            _groupIndex++;

            return new RegistrationData { UserName = currentBot.Username, Password = currentBot.Password, Messages = currentBot.Messages };
        }
    }

    public class WikiIdentityProvider : IIdentityProvider
    {
        public string Name { get { return "Wiki"; } }

        public RegistrationData GetNextIdentity()
        {
            HttpWebRequest wikiquery = WebRequest.CreateHttp("https://cs.wikipedia.org/wiki/Speci%C3%A1ln%C3%AD:N%C3%A1hodn%C3%A1_str%C3%A1nka");
            wikiquery.Method = "GET";

            var responseObject = wikiquery.GetResponse();

            string response = new StreamReader(responseObject.GetResponseStream()).ReadToEnd();

            string url = responseObject.ResponseUri.ToString();

            Match titleMatch = Regex.Match(response, "class=\"firstHeading\".*?>(.*?)</h1>", RegexOptions.Singleline);

            if (!titleMatch.Success || titleMatch.Groups.Count != 2)
            {
                return null;
            }

            string title = titleMatch.Groups[1].Value;

            title = title.Replace(" ", "");
            title = Regex.Replace(title, "\\(.*?\\)", string.Empty);
            title = Regex.Replace(title, "&#\\d+;", string.Empty);
            title = Regex.Replace(title, "&amp;", " and ");
            title = RemoveDiacritics(title);
            

            if (title.Length < 4 || title.Length > 20)
            {
                return null;
            }

            Match paragraphMatch = Regex.Match(response, "<p>(.*?)</p>", RegexOptions.Singleline);

            if (!paragraphMatch.Success || paragraphMatch.Groups.Count < 2)
            {
                return null;
            }

            string firstLine = paragraphMatch.Groups[1].Value;

            firstLine = Regex.Replace(firstLine, "<.*?>", string.Empty);
            firstLine = Regex.Replace(firstLine, "&#\\d+;", string.Empty);
            firstLine = Regex.Replace(firstLine, "&amp;", " and ");
            firstLine = RemoveDiacritics(firstLine);

            return new RegistrationData
            {
                UserName = title,
                Messages = new[] { url, firstLine }
            };
        }

        static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}