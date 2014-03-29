using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using Awesomium.Core;
using GoogleMusicWrapper.Properties;
using Lpfm.LastFmScrobbler;
using MessageBox = System.Windows.Forms.MessageBox;
using Timer = System.Timers.Timer;
using System.Threading;
using System.ComponentModel;

namespace GoogleMusicWrapper
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string LASTFM_API_KEY = "cb46a0c5eea4592f36f8877ad1e7458f";
        private const string LASTFM_SECRET = "4f46a32528437d209db58aa5176d90d4";
        private static QueuingScrobbler scrobbler;
        private static Track currentTrack = new Track();
        private static bool currentlyPlaying;
        private static bool scrobbled;
        private static Timer delayedOffTimer = new Timer();

        private delegate void NoArgDelegate();

        private readonly SynchronizationContext _syncContext;

        public MainWindow()
        {
            InitializeComponent();
            InitLastFM();

            webControl.ViewType = WebViewType.Window;

            _syncContext = SynchronizationContext.Current;

            delayedOffTimer.AutoReset = false;
            delayedOffTimer.Interval = 2700000;
            delayedOffTimer.Elapsed += delayedOffTimer_Elapsed;
        }

        void delayedOffTimerElapsed()
        {
            this.webControl.ExecuteJavascript("SJBpost('playPause');");
            OnPlayPause();
        }

        void delayedOffTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _syncContext.Post(o => delayedOffTimerElapsed(), null);
        }

        private void InitLastFM()
        {
            var settings = Settings.Default;
            scrobbler = new QueuingScrobbler(LASTFM_API_KEY, 
                                             LASTFM_SECRET,
                                             (settings.LastFmSession != "") ? settings.LastFmSession : null);

            if (settings.LastFmSession != "") return;

            var authScrobbler = new Scrobbler(LASTFM_API_KEY, LASTFM_SECRET);

            Process.Start(authScrobbler.GetAuthorisationUri());

            MessageBox.Show("Click OK when Application authenticated");

            settings.LastFmSession = authScrobbler.GetSession();
            settings.Save();
        }

        private void webControl_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            if (e.Message.Contains("Unsafe JavaScript")) return;

            Debug.WriteLine(e.Message);
            Debug.WriteLine(e.LineNumber);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            GlobalHotkeys.RegisterHotKey(Keys.MediaPlayPause, 
                                        this,
                                        () =>
                                        {
                                            webControl.ExecuteJavascript("SJBpost('playPause');");
                                            OnPlayPause();
                                        });
            GlobalHotkeys.RegisterHotKey(Keys.MediaNextTrack,
                                        this,
                                        () => webControl.ExecuteJavascript("$('.flat-button[data-id=forward]').click()"));
            GlobalHotkeys.RegisterHotKey(Keys.MediaPreviousTrack,
                                         this,
                                         () =>
                                         {
                                             delayedOffTimer.Start();
                                         });
        }

        private void ProcessScrobbles()
        {
            scrobbler.Process();
        }

        private void WebControl_OnLoadingFrameComplete(object sender, FrameEventArgs e)
        {
            if (webControl == null || !webControl.IsLive || !e.IsMainFrame || !webControl.IsDocumentReady) return;

            webControl.ExecuteJavascript(@"(function() {
                                               var script = document.createElement('script');
                                               script.src = '//ajax.googleapis.com/ajax/libs/jquery/1.10.2/jquery.min.js';
                                               script.onload = script.onreadystatechange = function(){external.app.injectJs();};
                                               document.body.appendChild( script );
                                           })()");

        }

        private void InjectJavaScript()
        {
            webControl.ExecuteJavascript(File.ReadAllText("js/attrmonitor.js"));
            webControl.ExecuteJavascript(File.ReadAllText("js/Scrobbler.js"));
            webControl.ExecuteJavascript("window.scrobbler.init()");
        }

        private void WebControl_OnNativeViewInitialized(object sender, WebViewEventArgs e)
        {
            // We demonstrate the creation of a child global object.
            // Acquire the parent first.
            JSObject external = webControl.CreateGlobalJavascriptObject("external");

            using (external)
            {
                // Create a child using fully qualified name. This only succeeds if
                // the parent is created first.
                JSObject app = webControl.CreateGlobalJavascriptObject("external.app");

                using (app)
                {
                    app.Bind("updateNowPlaying", false, UpdateNowPlaying);
                    app.Bind("injectJs", false, InjectJs);
                    app.Bind("detectScrobble", false, DetectScrobble);
                }
            }
        }

        private void DetectScrobble(object sender, JavascriptMethodEventArgs e)
        {
            if (e.Arguments.Length < 1) return;

            var percent = double.Parse(e.Arguments[0]);

            if (percent <= 0.55 || scrobbled) return;

            //So if you pause a song for a day and scrobble it later the timestamp isn't stale
            currentTrack.WhenStartedPlaying = DateTime.Now - TimeSpan.FromSeconds(currentTrack.Duration.TotalSeconds / 2);

            scrobbler.Scrobble(currentTrack);
            scrobbled = true;
            ProcessScrobbleQueue();
        }

        private void InjectJs(object sender, JavascriptMethodEventArgs e)
        {
            InjectJavaScript();
        }

        private void UpdateNowPlaying(object sender, JavascriptMethodEventArgs e)
        {
            if (e.Arguments.Length < 3 || e.Arguments[2].ToString() == currentTrack.TrackName || e.Arguments[3] == 0)
                return;

            currentTrack = new Track
            {
                ArtistName = e.Arguments[0],
                AlbumName = e.Arguments[1],
                TrackName = e.Arguments[2],
                Duration = TimeSpan.FromSeconds(int.Parse(e.Arguments[3]))
            };

            if (((int)currentTrack.Duration.TotalMilliseconds) == 0) return;

            scrobbler.NowPlaying(currentTrack);
            scrobbled = false;
            currentlyPlaying = true;

            ProcessScrobbleQueue();
        }

        private void ProcessScrobbleQueue()
        {
            var doProcessScrobbles = new NoArgDelegate(ProcessScrobbles);
            doProcessScrobbles.BeginInvoke(null, null);
        }

        private void OnPlayPause()
        {
            currentlyPlaying = !currentlyPlaying;

            if (!currentlyPlaying) return;

            scrobbler.NowPlaying(currentTrack);
            ProcessScrobbleQueue();
        }

        private void Google_Music_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            webControl.RenderSize = e.NewSize;
            webControl.Height = e.NewSize.Height - 40;
            webControl.Width = e.NewSize.Width - 18;
        }
    }
}