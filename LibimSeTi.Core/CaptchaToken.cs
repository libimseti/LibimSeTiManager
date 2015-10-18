namespace LibimSeTi.Core
{
    public class CaptchaToken
    {
        public int Key { get; set; }

        public string ImageUrl { get { return string.Format("http://registrace.libimseti.cz/captcha/{0}", Key); } }
    }
}