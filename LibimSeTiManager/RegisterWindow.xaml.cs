using LibimSeTi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LibimSeTiManager
{
    /// <summary>
    /// Interaction logic for RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        private readonly BotGroup _botGroup;
        private readonly Model _model;

        private class RegistrationItem
        {
            public RegistrationData RegistrationData { get; set; }

            public Brush Color
            {
                get
                {
                    return RoomWindow.UserItem.SexToColor(RegistrationData.Sex);
                }
            }

            public string BirthDateString
            {
                get
                {
                    return RegistrationData.BirthDate.ToLongDateString();
                }
            }
        }

        public RegisterWindow(BotGroup botGroup)
        {
            InitializeComponent();

            _botGroup = botGroup;
            _model = Model.Instance;

            Title = string.Format("Bot group {0}", botGroup.Name);

            SetupIdentityProviders();

            SetupBots();
        }

        private void SetupIdentityProviders()
        {
            identityProviderBox.Items.Add(new WikiIdentityProvider());
            identityProviderBox.Items.Add(new ExistingBotsProvider(_botGroup));

            identityProviderBox.SelectedIndex = 0;
        }

        private void SetupBots()
        {
            botBox.Items.Clear();

            foreach (Bot bot in _botGroup.Bots)
            {
                botBox.Items.Add(bot);
            }
        }

        private void patternGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int i = int.Parse(patternFromBox.Text); i <= int.Parse(patternToBox.Text); i++)
                {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    GeneratePatternedIdentity(i);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.Message);
            }
        }

        private async Task GeneratePatternedIdentity(int i)
        {
            Random rnd = new Random();

            string userName = string.Format(patternBox.Text, i);

            User existingUser = checkExistence.IsChecked == true ? await Session.GetUserInfo(userName) : null;

            if (existingUser == null)
            {
                registerBox.Items.Add(new RegistrationItem
                {
                    RegistrationData = new RegistrationData
                    {
                        UserName = userName,
                        Password = GetPassword(),
                        Email = GetEmail(),
                        BirthDate = new DateTime(rnd.Next(1950, 1980), rnd.Next(1, 12), rnd.Next(1, 28)),
                        Sex = GetSex()
                    }
                });
            }
            else
            {
                Logger.Instance.Error(string.Format("User {0} already exists", userName));
            }
        }

        private User.Sex GetSex()
        {
            Thread.Sleep(10);
            return sexBox.IsChecked == true || (sexBox.IsChecked == null && new Random((int)(DateTime.Now.Ticks & uint.MaxValue)).Next(0, 2) == 0) ? User.Sex.Male : User.Sex.Female;
        }

        private string GetEmail()
        {
            if (!string.IsNullOrWhiteSpace(emailBox.Text))
            {
                return emailBox.Text;
            }

            string[] emailServers = new[] { "seznam", "gmail", "centrum", "atlas" };

            return string.Format("{0}@{1}.cz", GetRandomPassword(), emailServers[new Random().Next(0, emailServers.Length - 1)]);
        }

        private string GetPassword()
        {
            if (!string.IsNullOrWhiteSpace(passwordBox.Text))
            {
                return passwordBox.Text;
            }

            return GetRandomPassword();
        }

        private static string GetRandomPassword()
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            Thread.Sleep(20);

            Random rnd = new Random((int)(DateTime.Now.Ticks & uint.MaxValue));

            int length = rnd.Next(4, 20);

            StringBuilder pwdBuilder = new StringBuilder(length);

            for (int i = 0; i < length - 1; i++)
            {
                pwdBuilder.Append(validChars[rnd.Next(0, validChars.Length - 1)]);
            }

            pwdBuilder.Append(rnd.Next(0, 9).ToString());

            return pwdBuilder.ToString();
        }

        private async void registerButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (RegistrationItem registrationItem in registerBox.Items.OfType<RegistrationItem>().ToArray())
            {
                if (createOneByOne.IsChecked != true)
                {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    CreateBot(registrationItem);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed                
                }
                else
                {
                    await CreateBot(registrationItem);
                }
            }
        }

        private List<CaptchaChallenge> _pendingChanllenges = new List<CaptchaChallenge>();

        private async Task CreateBot(RegistrationItem registrationItem)
        {
            RegistrationData registrationData = registrationItem.RegistrationData;

            CaptchaToken captchaToken = await Session.GetRegistrationCaptcha();

            if (captchaToken == null)
            {
                Logger.Instance.Error(string.Format("Cannot get captcha for [{0}]", registrationData.UserName));
                return;
            }

            CaptchaChallenge captchaChallenge = new CaptchaChallenge(captchaToken);

            if (_pendingChanllenges.Count > 0)
            {
                captchaChallenge.WindowState = WindowState.Minimized;
            }

            captchaChallenge.ShowActivated = false;
            captchaChallenge.Show();

            captchaChallenge.FocusTextBox();

            _pendingChanllenges.Add(captchaChallenge);

            captchaChallenge.Closed += async (s, e) =>
            {
                _pendingChanllenges.Remove(captchaChallenge);

                if (_pendingChanllenges.Count > 0)
                {
                    _pendingChanllenges[0].WindowState = WindowState.Normal;
                    _pendingChanllenges[0].Activate();
                    _pendingChanllenges[0].FocusTextBox();
                }

                string typedCaptcha = captchaChallenge.TypedText;

                if (string.IsNullOrWhiteSpace(typedCaptcha))
                {
                    return;
                }

                bool isRegistered;

                try
                {
                    await Session.Register(registrationData, captchaToken, typedCaptcha);
                    isRegistered = true;
                }
                catch
                {
                    isRegistered = false;
                }

                Bot newBot = new Bot(registrationData.UserName, registrationData.Password, registrationData.Messages);

                try
                {
                    await newBot.Session.Logon();
                }
                catch
                {
                    if (!isRegistered)
                    {
                        return;
                    }

                    if (MessageBox.Show(string.Format("{0}/{1} looks registered but cannot logon. Add to bots ?", newBot.Username, newBot.Password),
                        string.Empty, MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                lock (Configuration.Instance)
                {
                    if (!_botGroup.Bots.Any(bot => bot.Username == newBot.Username))
                    {
                        _botGroup.Bots.Add(newBot);

                        Configuration.Instance.Save();
                    }
                }

                SetupBots();

                registerBox.Items.Remove(registrationItem);
            };
        }

        private void clearGenerate_Click(object sender, RoutedEventArgs e)
        {
            registerBox.Items.Clear();
        }

        private void providerGenerate_Click(object sender, RoutedEventArgs e)
        {
            IIdentityProvider identityProvider = identityProviderBox.SelectedItem as IIdentityProvider;

            int numberOfNicks = int.Parse(botsNumberBox.Text);

            if (identityProvider != null)
            {
                try
                {
                    for (int i = 0; i < numberOfNicks; i++)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        GenerateProvidedIdentity(identityProvider);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex.Message);
                }
            }
        }

        private async Task GenerateProvidedIdentity(IIdentityProvider identityProvider)
        {
            Random rnd = new Random();

            RegistrationData identity = identityProvider.GetNextIdentity();

            if (identity == null)
            {
                return;
            }

            User existingUser = checkExistence.IsChecked == true ? await Session.GetUserInfo(identity.UserName) : null;

            if (existingUser == null)
            {
                identity.Password = identity.Password ?? GetPassword();
                identity.Email = identity.Email ?? GetEmail();
                identity.BirthDate = new DateTime(rnd.Next(1950, 1980), rnd.Next(1, 12), rnd.Next(1, 28));
                identity.Sex = GetSex();

                registerBox.Items.Add(new RegistrationItem
                {
                    RegistrationData = identity
                });
            }
            else
            {
                Logger.Instance.Error(string.Format("User {0} already exists", identity.UserName));
            }
        }

        private void registerBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && registerBox.SelectedItem != null)
            {
                registerBox.Items.Remove(registerBox.SelectedItem);
            }
        }
    }
}
