using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using WeatherNet;
using WeatherNet.Clients;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.System;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace LiveFrame
{
    public class WeatherResult
    {
        public string Temp { get; set; }
        public string IconUrl { get; set; }
        public DateTime Date { get; set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DateTime startTime;
        private DateTime alarmStartTime;
        private bool first = true;
        
        private DisplayRequest displayRequest = null; // class level to make it work!!
        private DispatcherTimer pageTimer;
        private DispatcherTimer pictureTimer;
        private DispatcherTimer backgroundPictureTimer;

        private List<string> picturesList = null;
        private int photoIndex = 0;
        private bool isPicturesFirstLoading = false;
        private bool isPicturesBackgroundLoading = false;

        private FeedNewsProvider NewsProvider = new FeedNewsProvider();

        private const int INITSET = 100;

        public MainPage()
        {
            this.InitializeComponent();

            startTime = DateTime.Now;

            // Set full screen mode
            ApplicationView view = ApplicationView.GetForCurrentView();
            if (!view.IsFullScreen)
            {
                //
            }

            displayRequest = new DisplayRequest();
            displayRequest.RequestActive(); //to request keep display on
            //displayRequest.RequestRelease(); //to release request of keep display on

            pageTimer = new DispatcherTimer();
            pageTimer.Tick += PageTimer_Tick;
            pageTimer.Interval = new TimeSpan(0, 0, 1);

            pictureTimer = new DispatcherTimer();
            pictureTimer.Tick += PictureTimer_Tick;
            pictureTimer.Interval = new TimeSpan(0, 0, 10);

            pageTimer.Start();

            pictureTimer.Start();

            SetPicturesFromLibrary();

            this.gridMain.PointerReleased += (s, e) =>
            {
                if (this.picturesList == null || this.picturesList.Count == 0)
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

            this.gridMain.KeyDown += gridMain_KeyDown;

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
                    StorageFile file = await StorageFile.GetFileFromPathAsync(this.picturesList[this.photoIndex]);
                    await file.DeleteAsync();

                    this.picturesList.RemoveAt(this.photoIndex);

                    //this.photoIndex;
                    await DisplayPhoto();
                }

                this.pictureTimer.Start();
            };

            this.gridMain.Holding += gridMain_Holding;
            this.gridMain.RightTapped += gridMain_RightTapped;
        }

        void gridMain_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Left)
            {
                this.pictureTimer.Stop();
                this.pictureTimer.Start();

                // Left
                this.photoIndex--;
                DisplayPhoto();
            }
            else if (e.Key == VirtualKey.Right)
            {
                this.photoIndex++;
                DisplayPhoto();

                this.pictureTimer.Stop();
                this.pictureTimer.Start();
            }
        }

        void gridMain_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);

            flyoutBase.ShowAt(senderElement);
        }

        void gridMain_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);

            flyoutBase.ShowAt(senderElement);

        }

        private void PageTimer_Tick(object sender, object e)
        {
            TimeSpan span = DateTime.Now - startTime;
            textTime.Text = DateTime.Now.ToString("h:mm");
            textTimer.Text = DateTime.Now.ToString("dddd, MMMM d");
            textRun.Text = span.ToString("d':'h':'m");

            int seconds = (int)span.TotalSeconds;

            if (first || seconds % 3600 == 0)
            {
                UpdateWeather();
            }

            if (first || seconds % 20 == 0)
            {
                GetNews(seconds);
            }

            if (first || seconds % 120 == 0)
            {
                GetQotes(first);
            }

            if (first)
            {
                first = false;
            }
        }

        private async void GetQotes(bool first)
        {
            if (!first)
            {
                int hour = DateTime.Now.Hour;
                bool isWeekend = DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday;

                // market open from 6-1
                if (hour < 6 || hour >= 14 || isWeekend)
                {
                    return;
                }
            }

            YahooStockQuoteProvider stockQuoteProvider = new YahooStockQuoteProvider();
            List<Tick> ticks = await stockQuoteProvider.GetQuotes();
            List<Ticker> tickers = new List<Ticker>();

            foreach (var t in ticks)
            {
                try
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

                    tickers.Add(new Ticker
                    {
                        Symbol = t.Symbol,
                        Price = price.ToString("N02", CultureInfo.InvariantCulture),
                        Up = string.Format("{0}%", change.ToString("N02", CultureInfo.InvariantCulture)),
                        Color = new SolidColorBrush(c)
                    });
                }
                catch (Exception)
                {
                    //Logger.Log(e.Message, Category.Exception, Priority.Medium);
                }

                if (tickers.Count == 4)
                {
                    textTName1.Text = tickers[0].Symbol;
                    textTName2.Text = tickers[1].Symbol;
                    textTName3.Text = tickers[2].Symbol;
                    textTName4.Text = tickers[3].Symbol;

                    textTPrice1.Text = tickers[0].Price;
                    textTPrice2.Text = tickers[1].Price;
                    textTPrice3.Text = tickers[2].Price;
                    textTPrice4.Text = tickers[3].Price;

                    textTUp1.Text = tickers[0].Up;
                    textTUp2.Text = tickers[1].Up;
                    textTUp3.Text = tickers[2].Up;
                    textTUp4.Text = tickers[3].Up;

                    textTUp1.Foreground = tickers[0].Color;
                    textTUp2.Foreground = tickers[1].Color;
                    textTUp3.Foreground = tickers[2].Color;
                    textTUp4.Foreground = tickers[3].Color;
                }
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

        private async Task GetNews(int seconds)
        {
            NewsProvider.MoveNext();
            
            if ((!NewsProvider.HasArticle || seconds % 7200 == 0) && !NewsProvider.IsLoading)
            {
                await NewsProvider.SearchAsync();
            }

            NewsArticle article = NewsProvider.Current;

            if (article != null)
            {
                ShowArticle(article);
            }
        }

        private void ShowArticle(NewsArticle article)
        {
            textTitle.Text = article.Title;
        }

        private async void UpdateWeather()
        {
            ClientSettings.ApiKey = "07fdb03f093152bf3aeb51952ae9a1e7";
            ClientSettings.ApiUrl = "http://api.openweathermap.org/data/2.5";

            string city = "Seattle";

            var result = await CurrentWeather.GetByCityNameAsync(city, "us", "en", "imperial");

            if (result.Success)
            {
                string temp = result.Item.Temp.ToString("N0");
                string iconUrl = string.Format("http://openweathermap.org/img/w/{0}.png", result.Item.Icon);

                textWeatherToday.Text = string.Format("{0}°", temp);
                Uri uri = null;
                if (Uri.TryCreate(iconUrl, UriKind.RelativeOrAbsolute, out uri) && uri.IsAbsoluteUri)
                {
                    imageWeatherToday.Source = new BitmapImage(uri);
                }
            }

            var fiveDays = await FiveDaysForecast.GetByCityNameAsync(city, "us", "en", "imperial");
            List<WeatherResult> wr = new List<WeatherResult>();
            if (fiveDays.Success)
            {
                if (fiveDays.Items.Count() == 0)
                {
                    return;
                }

                for (int i = 7; i < 40; i += 8)
                {
                    int index = i;
                    if (index >= fiveDays.Items.Count())
                    {
                        index = fiveDays.Items.Count() - 1;
                    }

                    var r = fiveDays.Items[index];
                    wr.Add(new WeatherResult
                    {
                        Temp = r.Temp.ToString("N0"),
                        IconUrl = string.Format("http://openweathermap.org/img/w/{0}.png", r.Icon),
                        Date = r.Date
                    });
                }

                Uri uri = null;

                if (wr != null && wr.Count >= 5)
                {
                    if (Uri.TryCreate(wr[0].IconUrl, UriKind.RelativeOrAbsolute, out uri) && uri.IsAbsoluteUri)
                        imageWeatherDay1.Source = new BitmapImage(uri);
                    textWeatherDay1.Text = string.Format("{0}°", wr[0].Temp);
                    if (Uri.TryCreate(wr[1].IconUrl, UriKind.RelativeOrAbsolute, out uri) && uri.IsAbsoluteUri)
                        imageWeatherDay2.Source = new BitmapImage(uri);
                    textWeatherDay2.Text = string.Format("{0}°", wr[1].Temp);
                    if (Uri.TryCreate(wr[2].IconUrl, UriKind.RelativeOrAbsolute, out uri) && uri.IsAbsoluteUri)
                        imageWeatherDay3.Source = new BitmapImage(uri);
                    textWeatherDay3.Text = string.Format("{0}°", wr[2].Temp);
                    if (Uri.TryCreate(wr[3].IconUrl, UriKind.RelativeOrAbsolute, out uri) && uri.IsAbsoluteUri)
                        imageWeatherDay4.Source = new BitmapImage(uri);
                    textWeatherDay4.Text = string.Format("{0}°", wr[3].Temp);
                    if (Uri.TryCreate(wr[4].IconUrl, UriKind.RelativeOrAbsolute, out uri) && uri.IsAbsoluteUri)
                        imageWeatherDay5.Source = new BitmapImage(uri);
                    textWeatherDay5.Text = string.Format("{0}°", wr[4].Temp);
                }
            }
        }

        private void PictureTimer_Tick(object sender, object e)
        {
            if (this.imgLocal.Visibility == Visibility.Visible)
            {
                ++this.photoIndex;
                SetPicturesFromLibrary();
            }
        }

        private async void SetPicturesFromLibrary()
        {
            if (this.isPicturesFirstLoading)
            {
                return;
            }

            if (picturesList == null)
            {
                this.isPicturesFirstLoading = true;

                await LoadPicturesFromStorage();

                this.isPicturesFirstLoading = false;

                //this.Logger.Log("Loaded photos: " + photoList.Count().ToString(), Category.Debug, Priority.Low);

                this.backgroundPictureTimer = new DispatcherTimer();
                this.backgroundPictureTimer.Tick += backgroundPictureTimer_Tick;
                this.backgroundPictureTimer.Interval = new TimeSpan(0, 0, 3);
                this.backgroundPictureTimer.Start();
            }

            int count = picturesList.Count();

            if (photoIndex >= count)
            {
                photoIndex = 0;
            }

            if (this.imgLocal.Visibility == Visibility.Visible)
            {
                await DisplayPhoto();
            }
        }

        private async void backgroundPictureTimer_Tick(object sender, object e)
        {
            if (this.isPicturesBackgroundLoading)
            {
                return;
            }

            this.isPicturesBackgroundLoading = true;
            await LoadPicturesFromStorage();
            this.isPicturesBackgroundLoading = false;
        }

        private async Task LoadPicturesFromStorage()
        {
            //var myPictures = await Windows.Storage.StorageLibrary.GetLibraryAsync
            //   (Windows.Storage.KnownLibraryId.Pictures);

            //IObservableVector<Windows.Storage.StorageFolder> myPictureFolders = myPictures.SaveFolder;
            // Get the user's Pictures folder.
            // Enable the corresponding capability in the app manifest file.
            StorageFolder picturesFolder = KnownFolders.PicturesLibrary;

            // Get the files in the current folder, sorted by date.
            IReadOnlyList<StorageFile> localPhotoList;

            if (this.picturesList == null)
            {
                localPhotoList = await picturesFolder.GetFilesAsync(CommonFileQuery.OrderBySearchRank, 0, INITSET);
            }
            else
            {
                localPhotoList = await picturesFolder.GetFilesAsync(CommonFileQuery.OrderBySearchRank, (uint)this.picturesList.Count, INITSET * 5);
            }

            if (this.picturesList != null && localPhotoList.Count == 0)
            {
                // Get all now
                this.backgroundPictureTimer.Stop();
                ShuffleList(this.picturesList);

                return;
            }

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
            var localList = new List<string>();

            //make a list of the index for list
            foreach (var file in localPhotoList)
            {
                //file.Path;
                localList.Add(file.Path);
            }

            ShuffleList(localList);

            if (this.picturesList == null)
            {
                this.picturesList = localList;
            }
            else
            {
                this.picturesList.AddRange(localList);
            }

            if ((this.picturesList.Count != INITSET) && ((this.picturesList.Count - INITSET) % (INITSET * 20) == 0))
            {
                ShuffleList(this.picturesList);
            }

            textImgIndex.Text = string.Format("{0}/{1}", this.photoIndex, this.picturesList.Count);
        }

        private static void ShuffleList(List<string> localList)
        {
            Random rng = new Random();
            int n = localList.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = localList[k];
                localList[k] = localList[n];
                localList[n] = value;
            }
        }

        private async Task DisplayPhoto()
        {
            if (this.picturesList == null || this.picturesList.Count == 0)
            {
                return;
            }

            if (this.photoIndex == this.picturesList.Count)
            {
                this.photoIndex = 0;
            }

            if (this.photoIndex < 0)
            {
                this.photoIndex = this.picturesList.Count - 1;
            }

            StorageFile file = await StorageFile.GetFileFromPathAsync(picturesList[this.photoIndex]);
            //imgLocal.Source = new BitmapImage(new Uri(file.Path));
            //imgLocal.Source = new BitmapImage(new Uri(@"D:/Pictures/googlelogo_color_272x92dp.png", UriKind.Absolute));

            //textImg.Text = file.DateCreated.ToString();

            //this.Logger.Log(string.Format("Show photo [{0}/{1}]: {2}", this.photoIndex, this.photoList.Count, file.Path), Category.Debug, Priority.Low);

            textImgIndex.Text = string.Format("{0}/{1}", this.photoIndex, this.picturesList.Count);

            using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                BitmapImage bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(fileStream);

                if (bitmapImage.PixelWidth > bitmapImage.PixelHeight)
                {
                    imgLocal.Width = (int)gridMain.RenderSize.Width;
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
                dateString = date.ToString("M/d/yyyy H:mm");
            }
            else
            {
                dateString = string.Empty;
            }
            textImg.Text = dateString;

            
            if (!string.IsNullOrEmpty(props.CameraModel))
            {
                var requests = new System.Collections.Generic.List<string>();
                requests.Add("System.Photo.FNumber");
                requests.Add("System.Photo.FocalLength");
                requests.Add("System.Photo.ExposureTime");

                IDictionary<string, object> retrievedProps = await props.RetrievePropertiesAsync(requests);

                string param = string.Empty;

                double fNumber;
                if (retrievedProps.ContainsKey("System.Photo.FNumber"))
                {
                    fNumber = (double)retrievedProps["System.Photo.FNumber"];
                    if (!string.IsNullOrEmpty(param))
                    {
                        param += " ,";
                    }

                    param += "F" + fNumber.ToString();
                }

                double focalLength;
                if (retrievedProps.ContainsKey("System.Photo.FocalLength"))
                {
                    focalLength = (double)retrievedProps["System.Photo.FocalLength"];
                    if (!string.IsNullOrEmpty(param))
                    {
                        param += ", ";
                    }

                    param += focalLength.ToString() + " mm";
                }

                double exposureTime;
                if (retrievedProps.ContainsKey("System.Photo.ExposureTime"))
                {
                    exposureTime = (double)retrievedProps["System.Photo.ExposureTime"];
                    if (exposureTime > 0)
                    {
                        if (!string.IsNullOrEmpty(param))
                        {
                            param += ", ";
                        }

                        double expo = 1 / exposureTime;
                        int time = 0;

                        if (expo < 100)
                        {
                            time = (int)Math.Ceiling(expo / 25) * 25;
                        }
                        else if (expo > 100 && expo < 1000)
                        {
                            time = (int)Math.Floor(expo / 100) * 100;
                        }
                        else
                        {
                            time = (int)Math.Floor(expo / 1000) * 1000;
                        }


                        param += "1/" + time.ToString() + " sec.";
                    }
                }

                string camera = props.CameraModel;

                if (!string.IsNullOrEmpty(param))
                {
                    //camera += " (" + param + ")";
                    textImgParam.Text = param;
                }

                if (!string.IsNullOrEmpty(camera))
                {
                    textImgCamera.Text = camera;
                }
            }

            /*
            Windows.Devices.Geolocation.Geoposition x;
            Windows.Devices.Geolocation.Geolocator locator = new Geolocator();
            x = await locator.GetGeopositionAsync();
            
            https://maps.googleapis.com/maps/api/geocode/json?latlng=40.714224,-73.961452&key=AIzaSyBK-dnlZDveYyXoddWCxcWygFMalPsmH_0
             
            if (props.Longitude != null && props.Latitude != null)
            {
                // Nearby location to use as a query hint.
                BasicGeoposition queryHint = new BasicGeoposition();
                queryHint.Latitude = props.Latitude.Value;
                queryHint.Longitude = props.Longitude.Value;
                Geopoint hintPoint = new Geopoint(queryHint);

                Windows.Devices.map

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

                    //props.Title = txt;
                    //props.SavePropertiesAsync();
                }
            }
            */
        }

        private void ToggleMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem item = sender as ToggleMenuFlyoutItem;

            if (item.IsChecked)
            {
                panelTime.Visibility = Windows.UI.Xaml.Visibility.Visible;
                panelMoreInfo.Visibility = Windows.UI.Xaml.Visibility.Visible;
                textRun.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                panelTime.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                panelMoreInfo.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                textRun.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }
    }
}
