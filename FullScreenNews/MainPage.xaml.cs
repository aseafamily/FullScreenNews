using ServiceHelpers;
using System;
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
        private DispatcherTimer pageTimer;
        private DispatcherTimer pictureTimer;

        private DateTime startTime;
        private DateTime alarmStartTime;
        private NewsArticles articles;
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

            // Full screen mode
            ApplicationView view = ApplicationView.GetForCurrentView();
            
            if (!view.IsFullScreenMode)
            {
                view.TryEnterFullScreenMode();
            }

            DateTimeOffset localTime = DateTimeOffset.Now;
            TimeZoneInfo hwZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            DateTimeOffset targetTime  = TimeZoneInfo.ConvertTime(localTime, hwZone);
            textChinaDate.Text = ChinaDate.GetChinaDate(targetTime.DateTime);

            tickers = new ObservableCollection<FullScreenNews.Ticker>();
            /*
            tickers.Add(new Ticker
            {
                Symbol = "aaa"
            });
            */

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
                    this.articles.MoveNext();
                }
                else
                {
                    // swiped right
                    this.articles.MovePrevious();
                }

                ShowArticle(this.articles.Current);

                pageTimer.Start();
            };

            startTime = DateTime.Now;
            alarmStartTime = startTime;

            // screen always on
            displayRequest = new DisplayRequest();
            displayRequest.RequestActive(); //to request keep display on
            //displayRequest.RequestRelease(); //to release request of keep display on

            pageTimer = new DispatcherTimer();
            pageTimer.Tick += PageTimer_Tick;
            pageTimer.Interval = new TimeSpan(0, 0, 1);
            pageTimer.Start();

            SetImageFromLibrary();
            pictureTimer = new DispatcherTimer();
            pictureTimer.Tick += PictureTimer_Tick;
            pictureTimer.Interval = new TimeSpan(0, 0, 25);
            pictureTimer.Start();

            
            articles = new FullScreenNews.NewsArticles();

            SetBackgroundFromBing();

            this.textTitle.DoubleTapped += (s, e) =>
            {
                if (this.articles.Current != null)
                {
                    this.Frame.Navigate(typeof(InternalBrowser), articles.Current.Url);
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
                    this.articles.MovePrevious();
                }
                else
                {
                    this.articles.MoveNext();
                }

                ShowArticle(this.articles.Current);
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

            List<FullScreenNews.Yahoo.Tick> ticks = await FullScreenNews.Yahoo.StockQuote.GetQuotes();

            textTickRefresh.Text = DateTime.Now.ToString("h:mm M/d");

            /*
            if (ticks.Count > 2)
            {
                FormatTick(textTick1, ticks[0]);
                FormatTick(textTick2, ticks[1]);
                FormatTick(textTick3, ticks[2]);
                FormatTick(textTick4, ticks[3]);
                FormatTick(textTick5, ticks[4]);
                FormatTick(textTick6, ticks[5]);
                FormatTick(textTick7, ticks[6]);
            }
            */

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

        private void FormatTick(TextBlock block, Yahoo.Tick tick)
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
        
        private async void PageTimer_Tick(object sender, object e)
        {
            TimeSpan span = DateTime.Now - startTime;
            textTime.Text = DateTime.Now.ToString("h:mm");
            textTimer.Text = DateTime.Now.ToString("dddd, MMMM d");

            //await SearchAsync("top stories");

            //pageTimer.Stop();
            //Debug.WriteLine(span.Seconds);

            UpdateAlarmBar();

            
            if (first || (int)span.TotalSeconds % 3600 == 0)
            {
                List<WeatherResult> wr = await WeatherProvider.GetWeather();
                if (wr != null && wr.Count > 6)
                {
                    textWeatherToday.Text = string.Format("{0}°", wr[0].Temp);
                    imageWeatherToday.Width = WeatherProvider.TodayWeatherIconWidth;
                    imageWeatherToday.Source = new BitmapImage(new Uri(wr[0].IconUrl));

                    imageWeatherDay1.Width = WeatherProvider.ForcastWeatherIconWidth;
                    imageWeatherDay2.Width = WeatherProvider.ForcastWeatherIconWidth;
                    imageWeatherDay3.Width = WeatherProvider.ForcastWeatherIconWidth;
                    imageWeatherDay4.Width = WeatherProvider.ForcastWeatherIconWidth;
                    imageWeatherDay5.Width = WeatherProvider.ForcastWeatherIconWidth;

                    imageWeatherDay1.Source = new BitmapImage(new Uri(wr[1].IconUrl));
                    imageWeatherDay2.Source = new BitmapImage(new Uri(wr[2].IconUrl));
                    imageWeatherDay3.Source = new BitmapImage(new Uri(wr[3].IconUrl));
                    imageWeatherDay4.Source = new BitmapImage(new Uri(wr[4].IconUrl));
                    imageWeatherDay5.Source = new BitmapImage(new Uri(wr[5].IconUrl));

                    textWeatherDay1.Text = string.Format("{0}°", wr[1].Temp);
                    textWeatherDay2.Text = string.Format("{0}°", wr[2].Temp);
                    textWeatherDay3.Text = string.Format("{0}°", wr[3].Temp);
                    textWeatherDay4.Text = string.Format("{0}°", wr[4].Temp);
                    textWeatherDay5.Text = string.Format("{0}°", wr[5].Temp);
                }
            }
            

            
            if (first || span.Seconds == 0)
            {
                GetQotes();
            }

            if (first || span.Seconds % 30 == 0)
            {
                if (!first)
                {
                    if (!skipNextArticle)
                    {
                        articles.MoveNext();
                    }
                    else
                    {
                        skipNextArticle = false;
                    }
                }

                if ((!articles.HasArticle || span.Minutes % 30 == 0) && !articles.IsLoading)
                {
                    await articles.SearchAsync();
                }

                NewsArticle article = articles.Current;

                if (article != null)
                {
                    ShowArticle(article);
                }
            }

            if (first)
            {
                first = false;
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
                articles.ArticleIndex);
            textInfo.Text = content;
        }

        private void buttonPrev_Click(object sender, RoutedEventArgs e)
        {
            this.articles.MovePrevious();

            ShowArticle(this.articles.Current);
        }

        private void buttonNext_Click(object sender, RoutedEventArgs e)
        {
            this.articles.MoveNext();

            ShowArticle(this.articles.Current);
        }

        private async Task SearchAsync(string query)
        {
            var news = await BingSearchHelper.GetNewsSearchResults(query, count: 50, offset: 0, market: "en-US");

            foreach (var article in news)
            {
                Uri uri = new Uri(article.ThumbnailUrl, UriKind.RelativeOrAbsolute);
                ImageSource imgSource = new BitmapImage(uri);
                //imgThumbnail.Width = article.Width;
                //imgThumbnail.Height = article.Height;
                imgThumbnail.Source = imgSource;

                textTitle.Text = article.Title;
                textDesc.Text = article.Description;

                break;
            }
        }
    }
}
