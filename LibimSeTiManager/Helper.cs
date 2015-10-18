using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LibimSeTiManager
{
    public static class Helper
    {
        public static Button CreateHeaderButton(string text)
        {
            Button button = new Button();
            button.Content = text;
            button.Padding = new Thickness(5);
            button.Margin = new Thickness(-5, -5, 0, 0);
            button.FontWeight = FontWeights.Bold;
            button.Foreground = Brushes.ForestGreen;
            button.Background = Brushes.WhiteSmoke;
            button.IsEnabled = false;
            return button;
        }

        public static Button CreateButtonWithTextBox(string text)
        {
            TextBox textBox = new TextBox();
            textBox.Text = text;
            textBox.Margin = new Thickness(0, 0, 10, 0);
            textBox.MinWidth = 20;

            Button button = new Button();
            button.HorizontalContentAlignment = HorizontalAlignment.Left;
            button.Content = textBox;
            button.MinWidth = 40;
            return button;
        }

        public static string ButtonText(Button buttonWithTextBox)
        {
            return ((TextBox)buttonWithTextBox.Content).Text;
        }
    }
}