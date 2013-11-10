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
        private static System.Timers.Timer scrobblerTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitLastFM();
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
                                        () => {
                                            webControl.ExecuteJavascript("SJBpost('playPause');");
                                            OnPlayPause();
                                        });
            GlobalHotkeys.RegisterHotKey(Keys.MediaNextTrack,
                                        this,
                                        () => webControl.ExecuteJavascript("SJBpost('nextSong');"));
            GlobalHotkeys.RegisterHotKey(Keys.MediaPreviousTrack, 
                                        this,
                                        () =>
                                        {
                                            webControl.ExecuteJavascript("$('.thumbs:first > li').first().click();");
                                            if(currentTrack != null)
                                                scrobbler.Love(currentTrack);
                                        });
        }

        private delegate void ProcessScrobblesDelegate();

        private void ProcessScrobbles()
        {
            // Processes the scrobbles and discards any responses. This could be improved with thread-safe
            //  logging and/or error handling
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
                    app.Bind("nowPlaying", false, UpdateNowPlaying);
                    app.Bind("injectJs", false, ReloadJs);
                }
            }
        }

        private void ReloadJs(object sender, JavascriptMethodEventArgs e)
        {
            InjectJavaScript();
        }

        private void UpdateNowPlaying(object sender, JavascriptMethodEventArgs e)
        {
            if (e.Arguments.Length < 3 || e.Arguments[2].ToString() == currentTrack.TrackName)
                return;

            currentTrack = new Track
            {
                ArtistName = e.Arguments[0],
                AlbumName = e.Arguments[1],
                TrackName = e.Arguments[2],
                Duration = TimeSpan.FromSeconds(int.Parse(e.Arguments[3])),
                WhenStartedPlaying = DateTime.Now
            };

            if (((int)currentTrack.Duration.TotalMilliseconds) == 0) return;

            scrobbler.NowPlaying(currentTrack);
            scrobbled = false;
            currentlyPlaying = true;

            ProcessScrobbleQueue();

            scrobblerTimer = new System.Timers.Timer(currentTrack.Duration.TotalMilliseconds/2);
            scrobblerTimer.Elapsed += ScrobblerTimerOnElapsed;
            scrobblerTimer.Start();
            scrobblerTimer.AutoReset = false;
        }

        private void ScrobblerTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            scrobblerTimer.Stop();
            if (scrobbled) return;

            scrobbler.Scrobble(currentTrack);
            ProcessScrobbleQueue();
            scrobbled = true;
        }

        private void ProcessScrobbleQueue()
        {
            var doProcessScrobbles = new ProcessScrobblesDelegate(ProcessScrobbles);
            doProcessScrobbles.BeginInvoke(null, null);
        }

        private void OnPlayPause()
        {
            currentlyPlaying = !currentlyPlaying;
            if (!currentlyPlaying)
            {
                scrobblerTimer.Stop();
                return;
            }
            scrobblerTimer.Start();
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