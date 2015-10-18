using LibimSeTi.Core;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LibimSeTiManager
{
    /// <summary>
    /// Interaction logic for CaptchaChallenge.xaml
    /// </summary>
    public partial class CaptchaChallenge : Window
    {
        public string TypedText { get; private set; }

        public CaptchaChallenge(CaptchaToken token)
        {
            InitializeComponent();

            captchaImage.Source = new BitmapImage(new Uri(token.ImageUrl));
        }

        public void FocusTextBox()
        {
            captchaBox.Focus();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            TypedText = captchaBox.Text;
            Close();
        }
    }
}
