using System;
using System.Linq;
using System.Net;
using System.Text;


namespace LibimSeTi.Core
{
	public static class HttpWebRequestExtensions
	{
		public static string GetRequestString(this HttpWebRequest request, string resource, byte[] requestContent)
		{
			StringBuilder contentBuilder = new StringBuilder();

			contentBuilder.AppendFormat("{0} /{1} HTTP/1.1", request.Method, resource);
			contentBuilder.AppendLine();

			if (!string.IsNullOrWhiteSpace(request.ContentType))
			{
				contentBuilder.AppendFormat("Content-Type: {0}", request.ContentType);
				contentBuilder.AppendLine();
			}

			if (!string.IsNullOrWhiteSpace(request.UserAgent))
			{
				contentBuilder.AppendFormat("User-Agent: {0}", request.UserAgent);
				contentBuilder.AppendLine();
			}

			if (!string.IsNullOrWhiteSpace(request.Host))
			{
				contentBuilder.AppendFormat("Host: {0}", request.Host);
				contentBuilder.AppendLine();
			}

			if (request.KeepAlive)
			{
				contentBuilder.AppendLine("Connection: Keep-Alive");
			}

			if (request.CookieContainer != null && request.CookieContainer.Count > 0)
			{
				contentBuilder.Append("Cookie: ");

				contentBuilder.Append(string.Join("; ", request.CookieContainer.GetCookies(new Uri("http://libimseti.cz", UriKind.RelativeOrAbsolute))
					.OfType<Cookie>()
					.Select(cookie => string.Format("{0}={1}", cookie.Name, cookie.Value))));

				contentBuilder.AppendLine();
			}

			if (requestContent != null)
			{
				contentBuilder.AppendFormat("Content-Length: {0}", requestContent.Length);
				contentBuilder.AppendLine();
			}

			contentBuilder.AppendLine();

			if (requestContent != null)
			{
				contentBuilder.Append(Encoding.ASCII.GetString(requestContent));
			}

			return contentBuilder.ToString();
		}
	}
}
