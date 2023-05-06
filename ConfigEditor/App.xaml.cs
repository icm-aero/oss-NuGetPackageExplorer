using System.ComponentModel;
using System.Windows;
using NuGet.Protocol;
using System.Windows.Controls;
using NuGetPackageExplorer.Types;

using PackageExplorer;

namespace YourNamespace
{
    public partial class App : Application
    {
        // Application-level event handlers and other logic here

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            //Resources.Add("Settings", new SettingsManager());

        }

        private void PackageIconImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            var image = sender as Image;
            image.Source = Images.DefaultPackageIcon;
        }
    }


}
