﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using FullScreenNews.Providers.Weather;
using Autofac;
using FullScreenNews.Providers.News;
using FullScreenNews.Logging;
using FullScreenNews.Settings;
using FullScreenNews.Providers.Stock;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FullScreenNews
{
    public class Ticker
    {
        public string Symbol { get; set; }

        public string Price { get; set; }

        public string Up { get; set; }

        public SolidColorBrush Color { get; set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Gets the <see cref="ILoggerFacade"/> for the application.
        /// </summary>
        /// <value>A <see cref="ILoggerFacade"/> instance.</value>
        private ILoggerFacade Logger { get; set; }

        private IContainer Container { get; set; }

        private IAppConfigurationLoader AppConfigurationLoader { get; set; }

        private INewsProvider NewsProvider;

        private IStockQuoteProvider StockQuoteProvider;

        private DispatcherTimer pageTimer;
        private DispatcherTimer pictureTimer;

        private DateTime startTime;
        private DateTime alarmStartTime;
        private bool first = true;
        private DisplayRequest displayRequest = null; // class level to make it work!!

        // for swipe
        int x1, x2;

        int backgroundIndex = 0;

        public ObservableCollection<Ticker> tickers;

        private List<StorageFile> photoList = null;

        private int photoIndex = 0;

        private bool isPhotoLoading = false;

        private string bingImageText;

        public object ReverseGeocodeQuery { get; private set; }

        private bool skipNextArticle = false;

        public MainPage()
        {
            this.InitializeComponent();

            // Set full screen mode
            ApplicationView view = ApplicationView.GetForCurrentView();
            if (!view.IsFullScreenMode)
            {
                view.TryEnterFullScreenMode();
            }

            // Set screen always on
            displayRequest = new DisplayRequest();
            displayRequest.RequestActive(); //to request keep display on
            //displayRequest.RequestRelease(); //to release request of keep display on
        }

        public void OnInitialized(IContainer container)
        {
            Container = container;

            // Start work now!
            this.Logger = container.Resolve<ILoggerFacade>();
            this.Logger.LogType<MainPage>();
            Logger.Log("MainPage OnOnInitialized", Category.Debug, Priority.Low);

            this.AppConfigurationLoader = container.Resolve<IAppConfigurationLoader>();
            this.NewsProvider = container.Resolve<INewsProvider>();
            this.StockQuoteProvider = container.Resolve<IStockQuoteProvider>();

            DateTimeOffset localTime = DateTimeOffset.Now;

            if (this.AppConfigurationLoader.Configuration.ShowChineseCalendar)
            {
                TimeZoneInfo hwZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                DateTimeOffset targetTime = TimeZoneInfo.ConvertTime(localTime, hwZone);
                textChinaDate.Text = ChinaDate.GetChinaDate(targetTime.DateTime);
            }

            TwitterList.Source = new Uri(string.Format(
                "http://aseafamily.azurewebsites.net/ticker.aspx?l={0}",
                this.AppConfigurationLoader.Configuration.TwitterListUrl));

            WorldClock1Name.Text = this.AppConfigurationLoader.Configuration.WorldClock1Name;
            WorldClock1.TimeZoneId = this.AppConfigurationLoader.Configuration.WorldClock1Timezone;
            WorldClock2Name.Text = this.AppConfigurationLoader.Configuration.WorldClock2Name;
            WorldClock2.TimeZoneId = this.AppConfigurationLoader.Configuration.WorldClock2Timezone;

            tickers = new ObservableCollection<FullScreenNews.Ticker>();

            // swipe
            this.NavigationCacheMode = NavigationCacheMode.Required;
            ManipulationMode = ManipulationModes.TranslateRailsX | ManipulationModes.TranslateRailsY;
            ManipulationStarted += (s, e) =>
            {
                pageTimer.Stop();

                x1 = (int)e.Position.X;
            };

            ManipulationCompleted += (s, e) =>
            {
                x2 = (int)e.Position.X;
                if (x1 > x2)
                {
                    // swiped left
                    this.NewsProvider.MoveNext();
                }
                else
                {
                    // swiped right
                    this.NewsProvider.MovePrevious();
                }

                ShowArticle(this.NewsProvider.Current);

                pageTimer.Start();
            };

            startTime = DateTime.Now;
            alarmStartTime = startTime;

            pageTimer = new DispatcherTimer();
            pageTimer.Tick += PageTimer_Tick;
            pageTimer.Interval = new TimeSpan(0, 0, 1);
            pageTimer.Start();

            SetImageFromLibrary();
            pictureTimer = new DispatcherTimer();
            pictureTimer.Tick += PictureTimer_Tick;
            pictureTimer.Interval = new TimeSpan(0, 0, this.AppConfigurationLoader.Configuration.UpdatePhotoInterval);
            pictureTimer.Start();


            SetBackgroundFromBing();

            this.textTitle.DoubleTapped += (s, e) =>
            {
                if (this.NewsProvider.Current != null)
                {
                    this.Frame.Navigate(typeof(InternalBrowser), NewsProvider.Current.Url);
                }
            };

            this.textImg.DoubleTapped += (s, e) =>
            {
                if (this.imgLocal.Visibility == Visibility.Visible)
                {
                    this.imgLocal.Visibility = Visibility.Collapsed;
                    this.gridLocalImage.Background = null;
                    this.gridLocalImage.Opacity = 1;
                    this.textImg.Text = bingImageText;
                }
                else
                {
                    this.imgLocal.Visibility = Visibility.Visible;
                    this.gridLocalImage.Background = new SolidColorBrush(Color.FromArgb(255, 10, 10, 10));
                    this.gridLocalImage.Opacity = 0.9;
                    this.textImg.Text = string.Empty;
                }
            };

            this.imgLocal.DoubleTapped += async (s, e) =>
            {
                this.pictureTimer.Stop();

                var dialog = new MessageDialog("Are you sure to delete this photo?");
                dialog.Title = "Really?";
                dialog.Commands.Add(new UICommand { Label = "Ok", Id = 0 });
                dialog.Commands.Add(new UICommand { Label = "Cancel", Id = 1 });
                var res = await dialog.ShowAsync();

                if ((int)res.Id == 0)
                {
                    StorageFile file = this.photoList[this.photoIndex];
                    await file.DeleteAsync();

                    this.photoList.RemoveAt(this.photoIndex);

                    ++this.photoIndex;
                    await DisplayPhoto();
                }

                this.pictureTimer.Start();
            };

            this.textTime.DoubleTapped += async (s, e) =>
            {
                this.alarmStartTime = DateTime.Now;
                await this.UpdateAlarmBar();
            };

            //this.imgLocal.ManipulationMode = ManipulationModes.TranslateRailsX | ManipulationModes.TranslateRailsY;
            this.gridLocalImage.PointerReleased += (s, e) =>
            {
                if (this.photoList == null || this.photoList.Count == 0)
                {
                    return;
                }

                PointerPoint point = e.GetCurrentPoint(this.imgLocal);
                if (point.Position.X < this.imgLocal.RenderSize.Width / 3)
                {
                    this.pictureTimer.Stop();
                    this.pictureTimer.Start();

                    // Left
                    this.photoIndex--;
                    DisplayPhoto();
                }
                else if (point.Position.X > this.imgLocal.RenderSize.Width * 0.67)
                {
                    // Right
                    this.photoIndex++;
                    DisplayPhoto();

                    this.pictureTimer.Stop();
                    this.pictureTimer.Start();
                }
            };

            this.textDesc.PointerReleased += (s, e) =>
            {
                this.skipNextArticle = true;
                PointerPoint point = e.GetCurrentPoint(this.textDesc);
                if (point.Position.X < this.textDesc.RenderSize.Width / 2)
                {
                    this.NewsProvider.MovePrevious();
                }
                else
                {
                    this.NewsProvider.MoveNext();
                }

                ShowArticle(this.NewsProvider.Current);
            };
        }

        private void PictureTimer_Tick(object sender, object e)
        {
            if (this.imgLocal.Visibility == Visibility.Visible)
            {
                ++this.photoIndex;
                SetImageFromLibrary();
            }
        }

        private async void SetImageFromLibrary()
        {
            if (this.isPhotoLoading)
            {
                return;
            }

            if (photoList == null)
            {
                this.isPhotoLoading = true;

                //var myPictures = await Windows.Storage.StorageLibrary.GetLibraryAsync
                //   (Windows.Storage.KnownLibraryId.Pictures);

                //IObservableVector<Windows.Storage.StorageFolder> myPictureFolders = myPictures.SaveFolder;
                // Get the user's Pictures folder.
                // Enable the corresponding capability in the app manifest file.
                StorageFolder picturesFolder = KnownFolders.PicturesLibrary;

                // Get the files in the current folder, sorted by date.
                IReadOnlyList<StorageFile> localPhotoList = await picturesFolder.GetFilesAsync(CommonFileQuery.OrderBySearchRank);

                // Iterate over the results and print the list of files
                // to the Visual Studio Output window.
                /*
                foreach (StorageFile file in sortedItems)
                {
                    string name = file.Name;
                    Debug.WriteLine(file.Name + ", " + file.DateCreated);
                }
                */

                // shuffle it
                photoList = new List<StorageFile>();

                //make a list of the index for list
                foreach (var file in localPhotoList)
                {
                    photoList.Add(file);
                }

                Random rng = new Random();
                int n = photoList.Count;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    StorageFile value = photoList[k];
                    photoList[k] = photoList[n];
                    photoList[n] = value;
                }

                this.isPhotoLoading = false;

                this.Logger.Log("Loaded photos: " + photoList.Count().ToString(), Category.Debug, Priority.Low);
            }

            int count = photoList.Count();

            if (photoIndex >= count)
            {
                photoIndex = 0;
            }

            await DisplayPhoto();
        }

        private async Task DisplayPhoto()
        {
            if (this.photoList == null || this.photoList.Count == 0)
            {
                return;
            }

            if (this.photoIndex == this.photoList.Count)
            {
                this.photoIndex = 0;
            }

            if (this.photoIndex < 0)
            {
                this.photoIndex = this.photoList.Count - 1;
            }

            StorageFile file = photoList[this.photoIndex];
            //imgLocal.Source = new BitmapImage(new Uri(file.Path));
            //imgLocal.Source = new BitmapImage(new Uri(@"D:/Pictures/googlelogo_color_272x92dp.png", UriKind.Absolute));

            //textImg.Text = file.DateCreated.ToString();

            this.Logger.Log(string.Format("Show photo [{0}/{1}]: {2}", this.photoIndex, this.photoList.Count, file.Path), Category.Debug, Priority.Low);

            using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                BitmapImage bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(fileStream);

                if (bitmapImage.PixelWidth > bitmapImage.PixelHeight)
                {
                    imgLocal.Width = (int)gridLocalImage.RenderSize.Width;
                    imgLocal.Stretch = Stretch.UniformToFill;
                }
                else
                {
                    imgLocal.Stretch = Stretch.Uniform;
                }

                imgLocal.Source = bitmapImage;
            }

            ImageProperties props = await file.Properties.GetImagePropertiesAsync();
            DateTimeOffset date = props.DateTaken;
            string dateString;
            if (date != null)
            {
                dateString = date.ToString("M/d/yyyy h:mm tt");
            }
            else
            {
                dateString = string.Empty;
            }
            textImg.Text = dateString;

            if (string.IsNullOrEmpty(props.Title))
            {
                if (props.Longitude != null && props.Latitude != null)
                {
                    // Nearby location to use as a query hint.
                    BasicGeoposition queryHint = new BasicGeoposition();
                    queryHint.Latitude = props.Latitude.Value;
                    queryHint.Longitude = props.Longitude.Value;
                    Geopoint hintPoint = new Geopoint(queryHint);

                    MapLocationFinderResult result =
                        await MapLocationFinder.FindLocationsAtAsync(hintPoint);

                    // If the query returns results, display the coordinates
                    // of the first result.
                    if (result.Status == MapLocationFinderStatus.Success)
                    {
                        string txt = string.Empty;
                        if (!string.IsNullOrWhiteSpace(result.Locations[0].Address.Town))
                        {
                            txt = result.Locations[0].Address.Town;
                        }

                        if (!string.IsNullOrWhiteSpace(result.Locations[0].Address.Region))
                        {
                            if (txt.Length > 0)
                            {
                                txt += ", ";
                            }
                            txt += result.Locations[0].Address.Region;
                        }

                        if (!string.IsNullOrWhiteSpace(dateString))
                        {
                            if (txt.Length > 0)
                            {
                                txt += " - ";
                            }
                            txt += dateString;
                        }

                        textImg.Text = txt;

                        props.Title = txt;
                        props.SavePropertiesAsync();
                    }
                }
            }
            else
            {
                textImg.Text = props.Title;
            }
        }

        private async void SetBackgroundFromBing()
        {
            this.Logger.Log("SetBackgroundFromBing", Category.Info, Priority.Low);

            const int backgroundCount = 1;    
            // We can specify the region we want for the Bing Image of the Day.
            string strRegion = "en-US";
            //string strRegion = "zh-CN";
            string strBingImageURL = string.Format("http://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n={0}&mkt={1}", backgroundCount, strRegion);
            string strJSONString = "";

            HttpClient client = new HttpClient();

            // Using an Async call makes sure the app is responsive during the time the response is fetched.
            // GetAsync sends an Async GET request to the Specified URI.
            HttpResponseMessage response = await client.GetAsync(new Uri(strBingImageURL));

            // Content property get or sets the content of a HTTP response message. 
            // ReadAsStringAsync is a method of the HttpContent which asynchronously 
            // reads the content of the HTTP Response and returns as a string.
            strJSONString = await response.Content.ReadAsStringAsync();

            // Parse using Windows.Data.Json.
            JsonObject jsonObject;
            bool boolParsed = JsonObject.TryParse(strJSONString, out jsonObject);

            if (boolParsed && backgroundIndex < jsonObject["images"].GetArray().Count())
            {
                string url = jsonObject["images"].GetArray()[backgroundIndex].GetObject()["url"].GetString();
                string text = jsonObject["images"].GetArray()[backgroundIndex].GetObject()["copyright"].GetString();
                if (text.IndexOf('(') > 0)
                {
                    text = text.Split(new string[] { "(" }, StringSplitOptions.RemoveEmptyEntries)[0];
                }

                if (this.imgLocal.Visibility == Visibility.Collapsed)
                {
                    textImg.Text = text;
                }
                bingImageText = text;

                if (!string.IsNullOrEmpty(url))
                {
                    imgBackground.ImageSource = new BitmapImage(new Uri("https://www.bing.com" + url));
                }

                /*
                backgroundIndex++;

                if (backgroundIndex >= jsonObject["images"].GetArray().Count())
                {
                    backgroundIndex = 0;
                }
                */
            }
        }

        private async void GetQotes()
        {
            this.Logger.Log("GetQotes", Category.Info, Priority.Low);

            if (this.tickers.Count > 0)
            {
                // Not first time, let's see if market is closed
                int hour = DateTime.Now.Hour;
                bool isWeekend = DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday;

                // market open from 6-1
                if (hour < 6 || hour >= 14 || isWeekend)
                {
                    return;
                }
            }

            List<Tick> ticks = await this.StockQuoteProvider.GetQuotes();

            textTickRefresh.Text = DateTime.Now.ToString("h:mm M/d");

            this.tickers.Clear();
            foreach (var t in ticks)
            {
                double price = Double.Parse(t.Price);
                double change = Double.Parse(t.Change.Replace("%", ""));

                int i = (int)Math.Abs(change);

                if (i > 5)
                {
                    i = 5;
                }

                i = 255 - i * 41;

                Color c;

                if (t.IsUp)
                {
                    c = Color.FromArgb(255, (byte)i, 255, (byte)i);
                }
                else
                {
                    c = Color.FromArgb(255, 255, (byte)i, (byte)i);
                }

                this.tickers.Add(new Ticker
                {
                    Symbol = t.Symbol,
                    Price = price.ToString("N02", CultureInfo.InvariantCulture),
                    Up = string.Format("{0}%", change.ToString("N02", CultureInfo.InvariantCulture)),
                    Color = new SolidColorBrush(c)
                });
            }
        }

        private void FormatTick(TextBlock block, Tick tick)
        {
            double price = Double.Parse(tick.Price);
            double change = Double.Parse(tick.Change.Replace("%", ""));
            block.Text = string.Format("{0} {1} {2}%", tick.Symbol, price.ToString("N02", CultureInfo.InvariantCulture), change.ToString("N02", CultureInfo.InvariantCulture));

            int i = (int)Math.Abs(change);

            if (i > 5)
            {
                i = 5;
            }

            i = 255 - i * 41;

            if (tick.IsUp)
            {
                block.Foreground = new SolidColorBrush(Color.FromArgb(255, (byte)i, 255, (byte)i));
            }
            else
            {
                block.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, (byte)i, (byte)i));
            }
        }

        private bool isAlarmOn = false;

        private async Task UpdateAlarmBar()
        {
            const int alarmSeconds = (int)(45 * 60);

            TimeSpan span = DateTime.Now - alarmStartTime;
            if (span.TotalSeconds < alarmSeconds)
            {
                gridAlarm.Margin = new Thickness(0, 0, gridLocalImage.RenderSize.Width - (span.TotalSeconds / alarmSeconds * gridLocalImage.RenderSize.Width), 0);
            }
            else
            {
                if (!isAlarmOn)
                {
                    gridAlarm.Margin = new Thickness(0, 0, 0, 0);

                    isAlarmOn = true;

                    // Play sound ms-appx:///Assets/Alarm03.wav
                    MediaElement mysong = new MediaElement();
                    Windows.Storage.StorageFolder folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Assets");
                    Windows.Storage.StorageFile file = await folder.GetFileAsync("Ring08.wav");
                    var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    mysong.SetSource(stream, file.ContentType);
                    for (int i = 0; i < 2; i++)
                    {
                        mysong.Play();
                    }

                    var dialog = new MessageDialog("Have a rest for a few minutes after long time work ;-)");
                    dialog.Title = "Hi buddy";
                    dialog.Commands.Add(new UICommand { Label = "Ok", Id = 0 });

                    var res = await dialog.ShowAsync();

                    if ((int)res.Id == 0)
                    {
                        alarmStartTime = DateTime.Now;
                        isAlarmOn = false;
                    }
                }
            }
        }
        
        private void PageTimer_Tick(object sender, object e)
        {
            TimeSpan span = DateTime.Now - startTime;
            textTime.Text = DateTime.Now.ToString("h:mm");
            textTimer.Text = DateTime.Now.ToString("dddd, MMMM d");

            UpdateAlarmBar();

            int seconds = (int)span.TotalSeconds;

            if (first || seconds % this.AppConfigurationLoader.Configuration.UpdateWeatherInterval == 0)
            {
                UpdateWeather();
            }


            if (first || seconds % this.AppConfigurationLoader.Configuration.UpdateStockInterval == 0)
            {
                GetQotes();
            }

            if (first || this.AppConfigurationLoader.Configuration.UpdateFeedInterval == 0)
            {
                GetNews();
            }

            if (first)
            {
                first = false;
            }
        }

        private async Task GetNews()
        {
            if (!first)
            {
                if (!skipNextArticle)
                {
                    NewsProvider.MoveNext();
                }
                else
                {
                    skipNextArticle = false;
                }
            }

            if ((!NewsProvider.HasArticle || this.AppConfigurationLoader.Configuration.UpdateFeedSourcesInterval == 0) && !NewsProvider.IsLoading)
            {
                await NewsProvider.SearchAsync();
            }

            NewsArticle article = NewsProvider.Current;

            if (article != null)
            {
                ShowArticle(article);
            }
        }

        private async Task UpdateWeather()
        {
            this.Logger.Log("UpdateWeather", Category.Info, Priority.Low);

            using (var scope = Container.BeginLifetimeScope())
            {
                var weatherProvider = scope.Resolve<IWeatherProvider>();
                List<WeatherResult> wr = await weatherProvider.GetWeather();
                if (wr != null && wr.Count >= 6)
                {
                    try
                    {
                        textWeatherToday.Text = string.Format("{0}°", wr[0].Temp);
                        imageWeatherToday.Width = weatherProvider.TodayWeatherIconWidth;

                        Uri uri = null;
                        if (Uri.TryCreate(wr[0].IconUrl, UriKind.RelativeOrAbsolute, out uri) && uri.IsAbsoluteUri)
                        {
                            imageWeatherToday.Source = new BitmapImage(uri);
                        }

                        imageWeatherDay1.Width = weatherProvider.ForcastWeatherIconWidth;
                        imageWeatherDay2.Width = weatherProvider.ForcastWeatherIconWidth;
                        imageWeatherDay3.Width = weatherProvider.ForcastWeatherIconWidth;
                        imageWeatherDay4.Width = weatherProvider.ForcastWeatherIconWidth;
                        imageWeatherDay5.Width = weatherProvider.ForcastWeatherIconWidth;

                        if (Uri.TryCreate(wr[1].IconUrl, UriKind.RelativeOrAbsolute, out uri) && uri.IsAbsoluteUri)
                            imageWeatherDay1.Source = new BitmapImage(uri);
                        if (Uri.TryCreate(wr[2].IconUrl, UriKind.RelativeOrAbsolute, out uri) && uri.IsAbsoluteUri)
                            imageWeatherDay2.Source = new BitmapImage(uri);
                        if (Uri.TryCreate(wr[3].IconUrl, UriKind.RelativeOrAbsolute, out uri) && uri.IsAbsoluteUri)
                            imageWeatherDay3.Source = new BitmapImage(uri);
                        if (Uri.TryCreate(wr[4].IconUrl, UriKind.RelativeOrAbsolute, out uri) && uri.IsAbsoluteUri)
                            imageWeatherDay4.Source = new BitmapImage(uri);
                        if (Uri.TryCreate(wr[5].IconUrl, UriKind.RelativeOrAbsolute, out uri) && uri.IsAbsoluteUri)
                            imageWeatherDay5.Source = new BitmapImage(uri);

                        textWeatherDay1.Text = string.Format("{0}°", wr[1].Temp);
                        textWeatherDay2.Text = string.Format("{0}°", wr[2].Temp);
                        textWeatherDay3.Text = string.Format("{0}°", wr[3].Temp);
                        textWeatherDay4.Text = string.Format("{0}°", wr[4].Temp);
                        textWeatherDay5.Text = string.Format("{0}°", wr[5].Temp);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("UpdateWeather UX exception\n" + ex.ToString(), Category.Exception, Priority.High);
                    }
                }
                else
                {
                    Logger.Log("Weather result is not expected. Count is " + wr.Count.ToString(), Category.Warn, Priority.Medium);
                }
            }
        }

        private void ShowArticle(NewsArticle article)
        {
            if (article.ThumbnailUrl != null)
            {
                Uri uri = new Uri(article.ThumbnailUrl, UriKind.RelativeOrAbsolute);
                ImageSource imgSource = new BitmapImage(uri);
                //imgThumbnail.Width = article.Width;
                //imgThumbnail.Height = article.Height;
                imgThumbnail.Source = imgSource;
            }
            else
            {
                imgThumbnail.Source = null;
            }

            textTitle.Text = article.Title;
            textDesc.Text = article.Description;

            string content = string.Format(
                "Published on {0} by {1}, refreshed on {2} ({3})",
                article.Published,
                article.Provider,
                article.Refreshed,
                NewsProvider.ArticleIndex);
            textInfo.Text = content;
        }

        private void buttonPrev_Click(object sender, RoutedEventArgs e)
        {
            this.NewsProvider.MovePrevious();

            ShowArticle(this.NewsProvider.Current);
        }

        private void buttonNext_Click(object sender, RoutedEventArgs e)
        {
            this.NewsProvider.MoveNext();

            ShowArticle(this.NewsProvider.Current);
        }
    }
}
