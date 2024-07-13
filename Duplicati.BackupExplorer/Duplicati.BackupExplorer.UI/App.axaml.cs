using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Duplicati.BackupExplorer.LocalDatabaseAccess;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Database;
using Duplicati.BackupExplorer.UI.ViewModels;
using Duplicati.BackupExplorer.UI.Views;
using System;

namespace Duplicati.BackupExplorer.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        var db = new DuplicatiDatabase();
        var comparer = new Comparer(db);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();

            var storageProvider = ((TopLevel)desktop.MainWindow).StorageProvider;

            if (desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            //((MainViewModel)desktop.MainWindow.DataContext).SetProvider(provider);
            desktop.MainWindow.DataContext = new MainViewModel(db, comparer, provider);

        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var wnd = new MainView();
            singleViewPlatform.MainView = wnd;

            wnd.DataContext = new MainViewModel(db, comparer, TopLevel.GetTopLevel(wnd).StorageProvider);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
