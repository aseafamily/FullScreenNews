using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace FullScreenNews
{
    public static class UIElementExtensions
    {
        public static ContainerVisual GetVisual(this UIElement element)
        {
            var hostVisual = ElementCompositionPreview.GetElementVisual(element);
            var root = hostVisual.Compositor.CreateContainerVisual();
            ElementCompositionPreview.SetElementChildVisual(element, root);
            return root;
        }
    }

    /// <summary>
    /// From https://github.com/XamlBrewer/UWP-Composition-API-Clock
    /// </summary>
    public sealed partial class Clock : UserControl
    {
        private Compositor _compositor;
        private ContainerVisual _root;

        private SpriteVisual _hourhand;
        private SpriteVisual _minutehand;
        private SpriteVisual _secondhand;
        private CompositionScopedBatch _batch;

        private Brush _faceColor = new SolidColorBrush(Colors.Transparent);

        private DispatcherTimer _timer = new DispatcherTimer();

        public Clock()
        {
            this.InitializeComponent();

            this.Loaded += Clock_Loaded;

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }


        public bool ShowTicks { get; set; } = true;

        public bool ShowSecond { get; set; } = false;

        public Brush FaceColor
        {
            get { return _faceColor; }
            set
            {
                if (value != _faceColor)
                {
                    _faceColor = value;
                }
            }
        }
        public ImageSource BackgroundImage { get; set; }

        public string TimeZoneId { get; set; }

        private void Clock_Loaded(object sender, RoutedEventArgs e)
        {
            Face.Fill = FaceColor;

            _root = Container.GetVisual();
            _compositor = _root.Compositor;

            // Hour Ticks
            if (ShowTicks)
            {
                SpriteVisual tick;
                for (int i = 0; i < 12; i++)
                {
                    tick = _compositor.CreateSpriteVisual();
                    tick.Size = new Vector2(4.0f, 20.0f);
                    tick.Brush = _compositor.CreateColorBrush(Colors.White);
                    tick.Offset = new Vector3(98.0f, 0.0f, 0);
                    tick.CenterPoint = new Vector3(2.0f, 100.0f, 0);
                    tick.RotationAngleInDegrees = i * 30;
                    _root.Children.InsertAtTop(tick);
                }
            }

            if (this.ShowSecond)
            {
                // Second Hand
                _secondhand = _compositor.CreateSpriteVisual();
                _secondhand.Size = new Vector2(2.0f, 120.0f);
                _secondhand.Brush = _compositor.CreateColorBrush(Colors.Red);
                _secondhand.CenterPoint = new Vector3(1.0f, 100.0f, 0);
                _secondhand.Offset = new Vector3(99.0f, 0.0f, 0);
                _root.Children.InsertAtTop(_secondhand);
                _secondhand.RotationAngleInDegrees = (float)(int)DateTime.Now.TimeOfDay.TotalSeconds * 6;
            }

            // Hour Hand
            _hourhand = _compositor.CreateSpriteVisual();
            _hourhand.Size = new Vector2(4.0f, 100.0f);
            _hourhand.Brush = _compositor.CreateColorBrush(Colors.Black);
            _hourhand.CenterPoint = new Vector3(2.0f, 80.0f, 0);
            _hourhand.Offset = new Vector3(98.0f, 20.0f, 0);
            _root.Children.InsertAtTop(_hourhand);

            // Minute Hand
            _minutehand = _compositor.CreateSpriteVisual();
            _minutehand.Size = new Vector2(4.0f, 120.0f);
            _minutehand.Brush = _compositor.CreateColorBrush(Colors.Black);
            _minutehand.CenterPoint = new Vector3(2.0f, 100.0f, 0);
            _minutehand.Offset = new Vector3(98.0f, 0.0f, 0);
            _root.Children.InsertAtTop(_minutehand);

            SetHoursAndMinutes();

            // Add XAML element.
            if (BackgroundImage != null)
            {
                var xaml = new Image();
                xaml.Source = BackgroundImage;
                xaml.Height = 200;
                xaml.Width = 200;
                Container.Children.Add(xaml);
            }

            _timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            var now = DateTime.Now;

            _batch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

            if (ShowSecond)
            {
                var animation = _compositor.CreateScalarKeyFrameAnimation();
                var seconds = (float)(int)now.TimeOfDay.TotalSeconds;

                // This works:
                //animation.InsertKeyFrame(0.00f, seconds * 6);
                //animation.InsertKeyFrame(1.00f, (seconds + 1) * 6);

                // Just an example of using expressions:
                animation.SetScalarParameter("start", seconds * 6);
                animation.InsertExpressionKeyFrame(0.00f, "start");
                animation.SetScalarParameter("delta", 6.0f);
                animation.InsertExpressionKeyFrame(1.00f, "start + delta");

                animation.Duration = TimeSpan.FromMilliseconds(900);
                _secondhand.StartAnimation(nameof(_secondhand.RotationAngleInDegrees), animation);
            }

            _batch.End();
            _batch.Completed += Batch_Completed;
        }

        /// <summary>
        /// Fired at the end of the secondhand animation. 
        /// </summary>
        private void Batch_Completed(object sender, CompositionBatchCompletedEventArgs args)
        {
            _batch.Completed -= Batch_Completed;

            SetHoursAndMinutes();
        }

        private void SetHoursAndMinutes()
        {
            DateTimeOffset localTime = DateTimeOffset.Now;
            DateTimeOffset targetTime;

            if (!string.IsNullOrEmpty(this.TimeZoneId))
            {
                TimeZoneInfo hwZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
                targetTime = TimeZoneInfo.ConvertTime(localTime, hwZone);
            }
            else
            {
                targetTime = localTime;
            }

            _hourhand.RotationAngleInDegrees = (float)targetTime.TimeOfDay.TotalHours * 30;
            _minutehand.RotationAngleInDegrees = targetTime.Minute * 6;

            int by = 255;
            int gy = 175;
            int ry = 120;
            int bg = 44;
            int gg = 32;
            int rg = 26;

            double hour = targetTime.Hour + targetTime.Minute / 60;
            if (hour > 12)
            {
                hour = 24 - hour;
            }

            int b = (int)((by - bg) / 12 * hour + bg);
            int g = (int)((gy - gg) / 12 * hour + gg);
            int r = (int)((ry - rg) / 12 * hour + rg);

            Face.Fill = new SolidColorBrush(Color.FromArgb(255, (byte)r, (byte)g, (byte)b));
        }
    }
}
