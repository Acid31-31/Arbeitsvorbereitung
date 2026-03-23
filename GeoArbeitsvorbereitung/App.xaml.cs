using System.Windows;
using GeoArbeitsvorbereitung.Services;
using GeoArbeitsvorbereitung.ViewModels;

namespace GeoArbeitsvorbereitung;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new SettingsService();
        var geoFileService = new GeoFileService();
        var dialogService = new WpfDialogService();

        var vm = new MainViewModel(settingsService, geoFileService, dialogService);

        var window = new MainWindow { DataContext = vm };
        window.Show();
    }
}
