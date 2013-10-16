using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using Awesomium.Core;
using GoogleMusicWrapper.Properties;
using Lpfm.LastFmScrobbler;
using MessageBox = System.Windows.Forms.MessageBox;

namespace GoogleMusicWrapper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Scrobbler scrobbler;
        private static Track currentTrack;
        private static bool currentlyPlaying = false;
        private static bool scrobbled = false;
        private const string LASTFM_API_KEY = "49bcc70745b596db85c929ba15311acb";
        private const string LASTFM_SECRET = "55b12642d4608cfb9d3e0857d2cdefe2";

        public MainWindow()
        {
            WebCore.Started += WebCoreOnStarted;
            InitializeComponent();
            InitLastFM();
        }

        private void InitLastFM()
        {
            var settings = Settings.Default;
            scrobbler = new Scrobbler(LASTFM_API_KEY, LASTFM_SECRET, (settings.LastFmSession != "") ? settings.LastFmSession : null);

            if (settings.LastFmSession != "") return;

            Process.Start(scrobbler.GetAuthorisationUri());

            MessageBox.Show("Click OK when Application authenticated");

            settings.LastFmSession = scrobbler.GetSession();
            settings.Save();
        }

        private void WebCoreOnStarted(object sender, CoreStartEventArgs coreStartEventArgs)
        {
            var interceptor = new JSIntercepter();

            WebCore.ResourceInterceptor = interceptor;
        }

        private void webControl_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            if (!e.Message.Contains("Unsafe JavaScript"))
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.LineNumber);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            GlobalHotkey.RegisterHotKey(Keys.MediaPlayPause, this, () => webControl.ExecuteJavascript("SJBpost('playPause');"));
        }

        class JSIntercepter : IResourceInterceptor
        {
            private static readonly Regex RE_LISTEN_JS = new Regex(@"^https?:\/\/ssl\.gstatic\.com\/play\/music\/\w+\/\w+\/listen_extended_\w+\.js", 
                                                          RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private static readonly Regex RE_LEX_ANCHOR = new Regex(@"var\s(\w)=\{eventName:.*?,eventSrc:.*?,payload:.*?\},\w=.*?;",
                                                           RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

            public ResourceResponse OnRequest(ResourceRequest request)
            {
                if (!RE_LISTEN_JS.IsMatch(request.Url.AbsoluteUri)) return null;

                var req = (HttpWebRequest)WebRequest.Create(request.Url);
                var resp = (HttpWebResponse)req.GetResponse();

                using(var sr = new StreamReader(resp.GetResponseStream())) {
                    var code = sr.ReadToEnd();

                    var match = RE_LEX_ANCHOR.Match(code);
                    var slice_start = match.Index + match.Groups[0].Length;

                    var head = code.Substring(0, slice_start);
                    var tail = code.Substring(slice_start, code.Length-slice_start);
                    var modifiedData = head + "if(window.gms_event !== undefined){window.gms_event(" + match.Groups[1] +
                                       ");}" + tail;

                    var jqueryReq = (HttpWebRequest)WebRequest.Create("http://ajax.googleapis.com/ajax/libs/jquery/1.10.2/jquery.min.js");
                    var response = (HttpWebResponse)jqueryReq.GetResponse();
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var jquery = reader.ReadToEnd();
                        modifiedData += ";" + jquery;
                    }

                    var buffer = new byte[modifiedData.Length];
                    var encoding = new UTF8Encoding();

                    encoding.GetBytes(modifiedData, 0, modifiedData.Length, buffer, 0);

                    // Initialize unmanaged memory to hold the array.
                    var size = Marshal.SizeOf( buffer[ 0 ] ) * modifiedData.Length;
                    var pnt = Marshal.AllocHGlobal( size );

                    try
                    {
                        // Copy the array to unmanaged memory.
                        Marshal.Copy(buffer, 0, pnt, buffer.Length);
                        return ResourceResponse.Create((uint)buffer.Length, pnt, "text/javascript");
                    }
                    finally
                    {
                        // Data is not owned by the ResourceResponse. A copy is made 
                        // of the supplied buffer. We can safely free the unmanaged memory.
                        Marshal.FreeHGlobal(pnt);
                    }
                }
                return null;
            }

            public bool OnFilterNavigation(NavigationRequest request)
            {
                return false;
            }
        }

        private void WebControl_OnLoadingFrameComplete(object sender, FrameEventArgs e)
        {
            if (webControl == null || !webControl.IsLive || !e.IsMainFrame || !webControl.IsDocumentReady) return;

            webControl.ExecuteJavascript(File.ReadAllText("attrmonitor.js"));
            webControl.ExecuteJavascript(File.ReadAllText("Scrobbler.js"));
        }

        private void WebControl_OnNativeViewInitialized(object sender, WebViewEventArgs e)
        {
            // We demonstrate the creation of a child global object.
            // Acquire the parent first.
            JSObject external = webControl.CreateGlobalJavascriptObject("external");

            if (external == null)
                return;

            using (external)
            {
                // Create a child using fully qualified name. This only succeeds if
                // the parent is created first.
                JSObject app = webControl.CreateGlobalJavascriptObject("external.app");

                if (app == null)
                    return;

                using (app)
                {
                    app.Bind("nowPlaying", false, UpdateNowPlaying);
                    app.Bind("onPlayPause", false, OnPlayPause);
                    app.Bind("trackPercent", false, DetectScrobble);
                }
            }
        }

        private void UpdateNowPlaying(object sender, JavascriptMethodEventArgs e)
        {
            if (e.Arguments.Length < 3)
                return;
            currentTrack = new Track
            {
                ArtistName = e.Arguments[0],
                AlbumName = e.Arguments[1],
                TrackName = e.Arguments[2],
                Duration = TimeSpan.FromMilliseconds(int.Parse(e.Arguments[3])),
                WhenStartedPlaying = DateTime.Now
            };
            scrobbler.NowPlaying(currentTrack);
            scrobbled = false;
            currentlyPlaying = true;
        }

        private void DetectScrobble(object sender, JavascriptMethodEventArgs e)
        {
            if (e.Arguments.Length < 1)
                return;

            var percent = double.Parse(e.Arguments[0]);

            if (percent <= 0.55 || scrobbled) return;

            scrobbler.Scrobble(currentTrack);
            scrobbled = true;
        }

        private void OnPlayPause(object sender, JavascriptMethodEventArgs e)
        {
            currentlyPlaying = !currentlyPlaying;
            if(currentlyPlaying)
                scrobbler.NowPlaying(currentTrack);
        }

        private void Google_Music_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            webControl.RenderSize = e.NewSize;
            webControl.Height = e.NewSize.Height-40;
            webControl.Width = e.NewSize.Width-18;
        }
    }
}
