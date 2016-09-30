using Autofac;
using FullScreenNews.Logging;
using FullScreenNews.Providers.News;
using FullScreenNews.Providers.Stock;
using FullScreenNews.Providers.Weather;
using FullScreenNews.Settings;
using MetroLog;
using MetroLog.Targets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace FullScreenNews
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Gets the <see cref="ILoggerFacade"/> for the application.
        /// </summary>
        /// <value>A <see cref="ILoggerFacade"/> instance.</value>
        private ILoggerFacade Logger { get; set; }

        private IContainer Container { get; set; }


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            DebugSettings.BindingFailed += DebugSettings_BindingFailed;

#if DEBUG
            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Debug, new StreamingFileTarget());
            //LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new SQLiteTarget());
#else
            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Info, LogLevel.Fatal, new StreamingFileTarget());
            //LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Info, LogLevel.Fatal, new SQLiteTarget());
#endif

            Logger = CreateLogger();
            if (Logger == null)
            {
                throw new InvalidOperationException("Logger Facade is null");
            }

            Logger.Log("Created Logger", Category.Debug, Priority.Low);

            CreateAndConfigureContainer();
        }

        private void CreateAndConfigureContainer()
        {
            Logger.Log("Creating and Configuring Container", Category.Debug, Priority.Low);

            var builder = new ContainerBuilder();
            ConfigureContainer(builder);
            Container = builder.Build();

        }

        private void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<MetroLogger>().As<ILoggerFacade>();

            //builder.RegisterType<SimpleAppConfigurationLoader>().As<IAppConfigurationLoader>().SingleInstance();
            builder.RegisterType<LocalAppConfigurationLoader>().As<IAppConfigurationLoader>().SingleInstance();

            //builder.RegisterType<NationalWeatherProvider>().As<IWeatherProvider>();
            builder.RegisterType<OpenWeatherProvider>().As<IWeatherProvider>();

            builder.RegisterType<FeedNewsProvider>().As<INewsProvider>();
            builder.RegisterType<YahooStockQuoteProvider>().As<IStockQuoteProvider>();
        }

        private void DebugSettings_BindingFailed(object sender, BindingFailedEventArgs e)
        {
            Logger.Log(e.Message, Category.Exception, Priority.Medium);
        }

        /// <summary>
        /// Create the <see cref="ILoggerFacade" /> used by the bootstrapper.
        /// </summary>
        /// <remarks>
        /// The base implementation returns a new DebugLogger.
        /// </remarks>
        private ILoggerFacade CreateLogger()
        {
            return new DebugLogger();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);

                    MainPage page = rootFrame.Content as MainPage;
                    page.OnInitialized(this.Container);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
