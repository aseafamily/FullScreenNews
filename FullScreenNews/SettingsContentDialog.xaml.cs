using Autofac;
using FullScreenNews.Logging;
using FullScreenNews.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace FullScreenNews
{
    public enum SaveResult
    {
        SaveOK,
        SaveFail,
        SaveCancel,
        Nothing
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsContentDialog : ContentDialog
    {
        public SaveResult Result { get; private set; }

        private ILoggerFacade Logger { get; set; }
        private IAppConfigurationLoader AppConfigurationLoader { get; set; }
        private IContainer Container { get; set; }

        public SettingsContentDialog()
        {
            this.InitializeComponent();

            this.Opened += SettingsContentDialog_Opened;
            this.Closing += SettingsContentDialog_Closing;
        }

        public SettingsContentDialog(IContainer container) : this()
        {
            this.Logger = container.Resolve<ILoggerFacade>();
            this.Logger.LogType<SettingsContentDialog>();

            this.AppConfigurationLoader = container.Resolve<IAppConfigurationLoader>();
            Container = container;
        }

        private void SettingsContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            
        }

        private void SettingsContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            this.textBoxFeeds.Text = string.Join("\n", this.AppConfigurationLoader.Configuration.FeedSources);
            this.textBoxVideos.Text = string.Join("\n", this.AppConfigurationLoader.Configuration.VideoChannels);
            this.textBoxSymbols.Text = string.Join(";", this.AppConfigurationLoader.Configuration.StockSymbols);
            this.textBoxTwitterList.Text = this.AppConfigurationLoader.Configuration.TwitterListUrl;

            this.toggleChina.IsOn = this.AppConfigurationLoader.Configuration.ShowChineseCalendar;
            this.textBoxClock1Name.Text = this.AppConfigurationLoader.Configuration.WorldClock1Name;
            this.textBoxClock1Timezone.Text = this.AppConfigurationLoader.Configuration.WorldClock1Timezone;
            this.textBoxClock2Name.Text = this.AppConfigurationLoader.Configuration.WorldClock2Name;
            this.textBoxClock2Timezone.Text = this.AppConfigurationLoader.Configuration.WorldClock2Timezone;
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            AppConfiguration newConfiguration = new AppConfiguration();

            // Feeds
            string[] feeds = this.textBoxFeeds.Text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (feeds.Length == 0)
            {
                args.Cancel = true;
                errorTextBlock.Text = "Please specify at least one feed.";

                return;
            }

            foreach (var str in feeds)
            {
                if (!(str.StartsWith("http://") || str.StartsWith("https://")))
                {
                    args.Cancel = true;
                    errorTextBlock.Text = "Not a valid URL for feed:" + str;

                    return;
                }
            }

            newConfiguration.FeedSources = feeds;

            // Videos
            string[] videos = this.textBoxVideos.Text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var str in videos)
            {
                if (!(str.StartsWith("http://") || str.StartsWith("https://")))
                {
                    args.Cancel = true;
                    errorTextBlock.Text = "Not a valid URL for video:" + str;

                    return;
                }
            }

            newConfiguration.VideoChannels = videos;

            // Symbols
            string[] symbols = this.textBoxSymbols.Text.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            newConfiguration.StockSymbols = symbols;

            // Twitter
            string twitter = textBoxTwitterList.Text;
            if (!(twitter.StartsWith("http://twitter.com") || twitter.StartsWith("https://twitter.com")))
            {
                args.Cancel = true;
                errorTextBlock.Text = "Not a valid twitter list:" + twitter;

                return;
            }

            newConfiguration.TwitterListUrl = twitter;

            // China calendar
            newConfiguration.ShowChineseCalendar = this.toggleChina.IsOn;

            // Word clock 
            newConfiguration.WorldClock1Name = this.textBoxClock1Name.Text;
            newConfiguration.WorldClock2Name = this.textBoxClock2Name.Text;

            try
            {
                TimeZoneInfo hwZone = TimeZoneInfo.FindSystemTimeZoneById(this.textBoxClock1Timezone.Text);
            }
            catch(Exception)
            {
                args.Cancel = true;
                errorTextBlock.Text = "Not a valid timezone for clock1";

                return;
            }

            try
            {
                TimeZoneInfo hwZone = TimeZoneInfo.FindSystemTimeZoneById(this.textBoxClock2Timezone.Text);
            }
            catch(Exception)
            {
                args.Cancel = true;
                errorTextBlock.Text = "Not a valid timezone for clock2";

                return;
            }

            newConfiguration.WorldClock1Timezone = this.textBoxClock1Timezone.Text;
            newConfiguration.WorldClock2Timezone = this.textBoxClock2Timezone.Text;

            // Save it now!!
            this.AppConfigurationLoader.Configuration = newConfiguration;

            ContentDialogButtonClickDeferral deferral = args.GetDeferral();
            if (await this.AppConfigurationLoader.Save())
            {
                this.Result = SaveResult.SaveOK;
            }
            else
            {
                this.Result = SaveResult.SaveFail;
            }

            deferral.Complete();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Result = SaveResult.SaveCancel;
        }

        // Handle the button clicks from the flyouts.
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(this).Hide();
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(this).Hide();
        }

        // When the flyout closes, hide the sign in dialog, too.
        private void Flyout_Closed(object sender, object e)
        {
            this.Hide();
        }
    }
}
