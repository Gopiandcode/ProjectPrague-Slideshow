using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace NUISlideshow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }




        private void GenerateSlideshow(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(SubredditUrlTextBox.Text))
            {

                Window Slideshow = new Slideshow(this, SubredditUrlTextBox.Text);
                Slideshow.Owner = this;
                Slideshow.Show();
                this.Hide();

            }
            else
            {
                ShowMessageDialog("Could not generate Slideshow", "At least one valid reddit url is required to generate a slideshow.");
            }
        }






        private void subredditTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textbox = sender as TextBox;

            // First retrieve the text from the textbox
            string text = textbox.Text;

            // This regex expression captures the subreddit name in a subreddit url..
            Regex url_rx = new Regex(@"(?:^(?:https?:\/\/)?(?:www.)?(?:(?:np|np-dk).)?(?:reddit\.com)\/r\/([a-zA-Z0-9_]+)\/?|^(?:\/?r\/)?([a-zA-Z0-9_]+))", RegexOptions.Compiled);
            MatchCollection extracted = url_rx.Matches(text);

            if (extracted.Count != 0)
            {
                // If the url is valid, change the text in the textbox to visually indicate the change
                StringBuilder sb = new StringBuilder();
                Match match = extracted[0];
                sb.Append("r/");

                // The regex expression has two mutually exclusive capturing groups. We only want to append the non-empty one.
                if (!String.IsNullOrEmpty(match.Groups[1].ToString()))
                    sb.Append(match.Groups[1]);
                else if (!String.IsNullOrEmpty(match.Groups[2].ToString()))
                    sb.Append(match.Groups[2]);
                textbox.Text = sb.ToString();
            }
            else
            {
                // Show an error.
                ShowMessageDialog("Url Format Error", "The url submitted was not a valid reddit url.\n" +
                "Urls can be of the form reddit.com/r/[subreddit name] or even r/[subreddit name].");
                textbox.Text = "";
            }

            
        }

        private static void ShowMessageDialog(string title, string content)
        {
            string messageboxText = content;
            string caption = content;
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBox.Show(messageboxText, caption, button);
        }
    }
}
