using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Protocol;

using NuGetPackageExplorer.Types;

using NuGetPe;

using PackageExplorer.MefServices;

using PackageExplorerViewModel;

using static System.Net.WebRequestMethods;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IPackageChooser PackageChooser;
        public INuGetPackageDownloader PackageDownloader { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            InitCredentialService();

            PackageDownloader = new PackageDownloader()
            {
                MainWindow = new Lazy<MainWindow>(() => this)
            };

            var packageSourceSettings = new Sources();
            var packageSourceManager = new MruPackageSourceManager(packageSourceSettings);
            PackageChooser = new PackageChooserService()
            {
                Window = new Lazy<MainWindow>(() => this),
                ViewModelFactory = new PackageViewModelFactory(),
                PackageDownloader = PackageDownloader
            };
        }

        private void InitCredentialService()
        {
            Task<IEnumerable<ICredentialProvider>> getProviders()
            {
                var credManager = new CredentialManager();
                
                return Task.FromResult<IEnumerable<ICredentialProvider>>(new ICredentialProvider[]
                {
                    new CredentialManagerProvider(credManager),
                    new CredentialPublishProvider()
                    //new CredentialDialogProvider()
            });
            };

            HttpHandlerResourceV3.CredentialService =
                new Lazy<ICredentialService>(() => new CredentialService(
                    new AsyncLazy<IEnumerable<ICredentialProvider>>(() => getProviders()),
                    nonInteractive: false,
                    handlesDefaultCredentials: false));

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //SettingsManager = new SettingsManager();
                //var viewModel = ViewModelFactory.CreatePackageChooserViewModel(NuGetConstants.V3FeedUrl);
                //viewModel.PackageDownloadRequested += OnPackageDownloadRequested;
                var packageSourceSettings = new Sources();
                var packageSourceManager = new MruPackageSourceManager(packageSourceSettings);
                var model = new PackageChooserViewModel(packageSourceManager,
                    true,
                    //"https://api.nuget.org/v3/index.json"
                    "http://proget.dev.icm.aero:8624/nuget/Default/v3/index.json"
                    );
                //var dialog = new PackageChooserDialog(model)
                //{
                //    Owner = this
                //};
                //dialog.ShowDialog("nlog");

                //var package = PackageChooser.SelectPackage("nlog");

                await OpenPackageFromRepository("nlog");



            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
           
        }

        private void OnPackageDownloadRequested(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void OpenFeedItem_Click(object sender, ExecutedRoutedEventArgs e)
        {
            var parameter = (string)e.Parameter;
            if (!string.IsNullOrEmpty(parameter))
            {
                parameter = "id:" + parameter;
            }
            await OpenPackageFromRepository(parameter);
        }

        private async Task OpenPackageFromRepository(string searchTerm)
        {
            //var canceled = AskToSaveCurrentFile();
            //if (canceled)
            //{
            //    return;
            //}

            var selectedPackageInfo = PackageChooser.SelectPackage(searchTerm);
            if (selectedPackageInfo == null)
            {
                return;
            }

            var repository = PackageChooser.Repository;

            var cachePackage = MachineCache.Default.FindPackage(selectedPackageInfo.Id, selectedPackageInfo.SemanticVersion);

            DispatcherOperation processPackageAction(ISignaturePackage package)
            {
                LoadPackage(package,
                    repository.PackageSource.Source,
                    PackageType.RemotePackage);

                // adding package to the cache, but with low priority
                return Dispatcher.BeginInvoke(
                    (Action<IPackage>)MachineCache.Default.AddPackage,
                    DispatcherPriority.ApplicationIdle,
                    package);
            }

            if (cachePackage == null)
            {
                var downloadedPackage = await PackageDownloader.Download(
                    repository,
                    selectedPackageInfo.Identity);

                if (downloadedPackage != null)
                {
                    await processPackageAction(downloadedPackage);
                }
            }
            else
            {
                await processPackageAction(cachePackage);
            }
        }

        private async void LoadPackage(IPackage package, string packagePath, PackageType packageType)
        {
            //DisposeViewModel();

            //if (package != null)
            //{
            //    if (!HasLoadedContent<PackageViewer>())
            //    {
            //        var packageViewer = new PackageViewer(SettingsManager, UIServices, PackageChooser);
            //        var binding = new Binding
            //        {
            //            Converter = new NullToVisibilityConverter(),
            //            FallbackValue = Visibility.Collapsed
            //        };
            //        packageViewer.SetBinding(VisibilityProperty, binding);

            //        MainContentContainer.Children.Add(packageViewer);

            //        // HACK HACK: set the Export of IPackageMetadataEditor here
            //        EditorService = packageViewer.PackageMetadataEditor;
            //    }

            //    try
            //    {
            //        var packageViewModel = await PackageViewModelFactory.CreateViewModel(package, packagePath);
            //        packageViewModel.PropertyChanged += OnPackageViewModelPropertyChanged;

            //        DataContext = packageViewModel;
            //        if (!string.IsNullOrEmpty(packagePath))
            //        {
            //            _mruManager.NotifyFileAdded(package, packagePath, packageType);
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        if (!(e is ArgumentException))
            //        {
            //            DiagnosticsClient.Notify(e);
            //        }
            //        UIServices.Show($"Error loading package\n{e.Message}", MessageLevel.Error);
            //    }
            //}
        }
    }

    public class Sources : ISourceSettings
    {
        public string DefaultSource { get; } = "https://api.nuget.org/v3/index.json";
        public string ActiveSource { get; set; }
        public IList<string> GetSources()
        {
            return new[] { DefaultSource, "http://proget.dev.icm.aero:8624/nuget/Default/v3/index.json" };
        }

        public void SetSources(IEnumerable<string> sources)
        {
        }
    }
}
