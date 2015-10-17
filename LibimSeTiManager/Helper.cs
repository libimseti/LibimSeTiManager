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
    }
}