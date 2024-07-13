using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Selection;
using Avalonia.Platform.Storage;
using Duplicati.BackupExplorer.LocalDatabaseAccess;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Database;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Model;
using Duplicati.BackupExplorer.UI.Views;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.UI.ViewModels;

public class FileSystemItem
{
    public ObservableCollection<FileSystemItem>? SubNodes { get; }
    public string Title { get; }

    public FileSystemItem(string title)
    {
        Title = title;
    }

    public FileSystemItem(string title, ObservableCollection<FileSystemItem> subNodes)
    {
        Title = title;
        SubNodes = subNodes;
    }
}

public partial class MainViewModel : ViewModelBase
{
    private DuplicatiDatabase _database;

    private Comparer _comparer;

    private IStorageProvider _provider;

    public MainViewModel(DuplicatiDatabase database, Comparer comparer, IStorageProvider provider)
    {
        _database = database;

        _comparer = comparer;

        _provider = provider;

        ProjectFilename = "D:\\duplicati.sqlite";

        items = new ObservableCollection<string> { "asd", "fsg" };


        SelectedBackups.CollectionChanged += SelectedBackups_CollectionChanged;
    }

    public MainViewModel()
    {
        if (Design.IsDesignMode)
        {
            ProjectFilename = @"D:\Duplicati\database.sqlite";
            Backups = new ObservableCollection<Backup> {
                new Backup {Fileset = new Fileset { Id = 1, Timestamp = new System.DateTimeOffset(2021, 12, 1, 12, 14, 55, System.TimeSpan.Zero), VolumeId=1 } },
                new Backup {Fileset = new Fileset { Id = 2, Timestamp = new System.DateTimeOffset(2022, 12, 1, 12, 14, 55, System.TimeSpan.Zero), VolumeId=2 } },
            };
            items = new ObservableCollection<string> { "asd", "fsg" };

            //FilesetFiles = new List<FileSystemItem>() { new FileSystemItem("C:\\Data", new ObservableCollection<FileSystemItem>() { new FileSystemItem("sdf") }) };
            var filePaths = new string[] { @"C:\Windows", @"C:\Temp\MyFile.cs", @"C:\", @"C:\Temp" };
            foreach (var path in filePaths) {
                FileTree.AddPath(path, 0);
            }
        }
    }

    private void ShowProgressBar(bool show)
    {
        Progress = 0;
        ProgressVisible = show;
    }

    private async void SelectedBackups_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (SelectedBackups.Count > 0)
        {
            var backup = SelectedBackups[SelectedBackups.Count - 1];
            if (backup.FileTree == null)
            {               
                var ft = new FileTree();
                ShowProgressBar(true);

                await Task.Run(() =>
                {
                    ProgressTextFormat = $"Loading fileset {backup} ({{1:0}} %)";
                    Progress = 20;

                    var fsentries = _database.GetFilesetEntriesById(backup.Fileset.Id);
                    var files = _database.GetFilesByIds2(fsentries.Select(x => x.FileId));

                    double progressStep = 100.0 / files.Count();

                    foreach (var file in files)
                    {
                        ProgressTextFormat = $"Loading file {file} ({{1:0}} %)";

                        ft.AddPath(Path.Join(file.Prefix, file.Path), file.BlocksetId);
                        Progress += progressStep;
                    }
                    ft.Sort();
                });
                backup.FileTree = ft;
                ShowProgressBar(false);
            }

            FileTree = backup.FileTree;
        }
    }

    public void SetProvider(IStorageProvider provider)
    {
        _provider = provider;
    }

    private FileTree _fileTree = new FileTree();

    public FileTree FileTree { get { return _fileTree; } set { _fileTree = value; OnPropertyChanged("FileTree"); } }

    public ObservableCollection<string> items { get; set; }

    private string _loadButtonLabel = "Load";
    private bool _progressVisible = false;
    private double _progress = 0;
    private string _progressTextFormat = "";

    private string _projectFilename;
    public string ProjectFilename { get { return _projectFilename; } set { _projectFilename = value; OnPropertyChanged("ProjectFilename"); } }

    public ObservableCollection<Backup> Backups { get; set; } = new ObservableCollection<Backup>();

    public ObservableCollection<Backup> SelectedBackups { get; set; } = new ObservableCollection<Backup>();

    public string LoadButtonLabel { get { return _loadButtonLabel; } set { _loadButtonLabel = value; OnPropertyChanged("LoadButtonLabel"); } }

    public bool ProgressVisible { get { return _progressVisible; } set { _progressVisible = value; OnPropertyChanged("ProgressVisible"); } }
    public double Progress { get { return _progress; } set { _progress = value; OnPropertyChanged("Progress"); } }
    public string ProgressTextFormat { get { return _progressTextFormat; } set { _progressTextFormat = value; OnPropertyChanged("ProgressTextFormat"); } }

    async public void Compare()
    {
        if (SelectedBackups.Count != 2)
            return;

        ShowProgressBar(true);
        var bak1 = SelectedBackups[0];
        var bak2 = SelectedBackups[1];

        var progressStep = 100.0 / bak2.FileTree.GetFileNodes().Count();
        _comparer.OnBlocksCompareFinished += () => { 
            Progress += progressStep;
        };

        await Task.Run(() => _comparer.CompareFiletree(bak1.FileTree, bak2.FileTree));
        bak1.FileTree.UpdateDirectoryCompareResults();

        var dialog = new CompareResultWindow();
        dialog.DataContext = new CompareResultModel() { FileTree=bak1.FileTree };

        dialog.Show();
    }

    public void CompareDetailed()
    {
        if (SelectedBackups.Count != 2)
            return;

        var bak1 = SelectedBackups[0];
        var bak2 = SelectedBackups[1];

        var result = _comparer.CompareFilesets(bak1.Fileset, bak2.Fileset);
    }

    async public void CompareUnique()
    {
        if (SelectedBackups.Count != 1)
            return;

        var bak = SelectedBackups[0];


        var result = await _comparer.CompareFilesetsUnique(bak.Fileset, Backups.Where(x => x != bak).Select(x => x.Fileset).ToList());
        int i = 0;
        i++;
    }
    class FsEntry
    {
        public string Name;

        public Dictionary<string, FsEntry> Folder { get; set; }
    }

    public async void Browse()
    {
        var storageFile = await DoOpenFilePickerAsync();
        ProjectFilename = storageFile.Path.AbsolutePath;
    }

    private async Task<IStorageFile?> DoOpenFilePickerAsync()
    {
        // For learning purposes, we opted to directly get the reference
        // for StorageProvider APIs here inside the ViewModel. 

        // For your real-world apps, you should follow the MVVM principles
        // by making service classes and locating them with DI/IoC.

        // See IoCFileOps project for an example of how to accomplish this.
        /*if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");
        */
        var files = await _provider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open Text File",
            AllowMultiple = false
        });

        return files?.Count >= 1 ? files[0] : null;
    }

    private async Task<string?> OpenFile(CancellationToken token)
    {
        //ErrorMessages?.Clear();
        try
        {
            var file = await DoOpenFilePickerAsync();
            if (file is null) return null;

            // Limit the text file to 1MB so that the demo won't lag.
            if ((await file.GetBasicPropertiesAsync()).Size <= 1024 * 1024 * 1)
            {
                await using var readStream = await file.OpenReadAsync();
                using var reader = new StreamReader(readStream);
                return await reader.ReadToEndAsync(token);
            }
            else
            {
                throw new Exception("File exceeded 1MB limit.");
            }
        }
        catch (Exception e)
        {
            throw;
            //ErrorMessages?.Add(e.Message);
        }
    }

    async Task<List<Backup>> LoadBackups()
    {
        _database.Open(ProjectFilename);

        var filesets = _database.GetFilesets();

        Progress = 0;
        var progStep = 100 / filesets.Count;

        var backups = new List<Backup>();
        foreach (var item in filesets)
        {
            _loadProjectCancellation.Token.ThrowIfCancellationRequested();



            backups.Add(new Backup { Fileset = item, FileTree = null });
            Progress += progStep;
        }

        return backups;
    }
    private CancellationTokenSource? _loadProjectCancellation;

    private bool _loadButtonEnabled = true;

    public bool LoadButtonEnabled { get { return _loadButtonEnabled; } set { _loadButtonEnabled = value; OnPropertyChanged("LoadButtonEnabled"); } }

    public async void LoadProject()
    {
        if (_loadProjectCancellation != null)
        {
            _loadProjectCancellation.Cancel();
            LoadButtonEnabled = false;
        }
        else
        {
            _loadProjectCancellation = new CancellationTokenSource();

            LoadButtonLabel = "Cancel";


            Backups.Clear();

            try
            {
                var backups = await Task.Run(LoadBackups);

                backups.ForEach(x => Backups.Add(x));
            }
            catch (OperationCanceledException ex)
            {
                //
            }
            finally
            {
                LoadButtonEnabled = true;
                ShowProgressBar(false);
                LoadButtonLabel = "Load";
                _loadProjectCancellation.Dispose();
                _loadProjectCancellation = null;

            }
        }
    }
}
