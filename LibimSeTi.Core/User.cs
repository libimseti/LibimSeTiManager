namespace LibimSeTi.Core
{
    public class User
    {
        public enum Sex
        {
            Male,
            Female
        }

        public User(string name, Sex sex)
        {
            Name = name;
            UserSex = sex;
        }

        public int Id { get; set; }

        public string Name { get; private set; }

        public Sex UserSex { get; private set; }
    }
}