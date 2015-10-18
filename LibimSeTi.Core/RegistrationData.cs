using System;

namespace LibimSeTi.Core
{
    public class RegistrationData
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public User.Sex Sex { get; set; }
        public DateTime BirthDate { get; set; }
        public string[] Messages { get; set; }
    }
}
