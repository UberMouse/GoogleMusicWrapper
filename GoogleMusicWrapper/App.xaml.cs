using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Awesomium.Core;

namespace GoogleMusicWrapper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var config = new WebConfig
            {
                RemoteDebuggingPort = 9001
            };
            WebCore.Initialize(config);
        }
    }
}
