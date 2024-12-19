using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Duplicati.BackupExplorer.LocalDatabaseAccess;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Database;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Model;
using Duplicati.BackupExplorer.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.ViewModels;

public class FileSystemItem
{
    public FileSystemItem(string title)
    {
        Title = title;
    }

    public FileSystemItem(string title, ObservableCollection<FileSystemItem> subNodes)
    {
        Title = title;
        SubNodes = subNodes;
    }
    public ObservableCollection<FileSystemItem>? SubNodes { get; }
    public string Title { get; }
}

public partial class MainViewModel : ViewModelBase
{
    private readonly DuplicatiDatabase _database;

    private readonly Comparer _comparer;
    private const int MAX_LOADED_FILESETS = 5;
    private IStorageProvider _provider;

    private bool _isProcessing = false;

    private FileTree? _leftSide = null;
    private FileTree? _rightSide = null;

    private FileTree _fileTree = new("<None>");

    private bool _isCompareElementsSelected = false;

    private bool _isProjectLoaded = false;

    private IBrush _buttonSelectDatabaseColor = Brushes.Green;

    private string _loadButtonLabel = "Select Database";
    private bool _progressVisible = false;
    private double _progress = 0;
    private string _progressTextFormat = "";

    private string _projectFilename = "";
    private long? _allBackupsSize;
    private long? _allBackupsWasted;

    private CancellationTokenSource? _loadProjectCancellation;
    private readonly DBTaskScheduler _dbTaskScheduler;

    private bool _isLoadingDatabase = false;
    private readonly LinkedList<long> _loadedTrees = [];
    private readonly EventWaitHandle _loadingWait = new(false, EventResetMode.ManualReset);
    private Backup? _loadingFileTree = null;

    public MainViewModel(DuplicatiDatabase database, DBTaskScheduler dbTaskScheduler, Comparer comparer, IStorageProvider provider)
    {
        _database = database;

        _comparer = comparer;

        _provider = provider;

        ProjectFilename = "";

        Items = [];

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown Version";
        WindowTitle = $"Duplicati BackupExplorer - v{version}";

        SelectedBackups.CollectionChanged += SelectedBackups_CollectionChanged;

        // All database accesses should occur on this scheduler to prevent race conditions
        _dbTaskScheduler = dbTaskScheduler;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public MainViewModel()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        if (!Design.IsDesignMode)
        {
            throw new InvalidOperationException("Constructor only for designer");
        }

        ProjectFilename = @"D:\Duplicati\database.sqlite";
        Backups = [
            new() {Fileset = new Fileset { Id = 1, Timestamp = new System.DateTimeOffset(2021, 12, 1, 12, 14, 55, System.TimeSpan.Zero), VolumeId=1 }},
            new() {Fileset = new Fileset { Id = 2, Timestamp = new System.DateTimeOffset(2022, 12, 1, 12, 14, 55, System.TimeSpan.Zero), VolumeId=2 }},
        ];
        Items = ["asd", "fsg"];

        var ft = new FileTree();

#pragma warning disable S1075 // URIs should not be hardcoded
        ft.AddPath(@"C:\Temp\MyFile.cs", 1542351123);
        ft.AddPath(@"C:\Temp\MyFile2.cs", 3399293492);
        ft.AddPath(@"C:\Windows", 1);
        ft.AddPath(@"C:\Windows\System\System.dll", 1);
        ft.AddPath(@"E:\DataDir\content.dat", 1);
        ft.AddPath(@"D:\MyDir\MyFile3.cs", 5399293492);
#pragma warning restore S1075 // URIs should not be hardcoded

        FileTree = ft;

        IsProjectLoaded = true;
        AllBackupsWasted = 154352345345;
    }

    public string ProjectFilename { get { return _projectFilename; } set { _projectFilename = value; OnPropertyChanged(nameof(ProjectFilename)); } }

    public long? AllBackupsSize { get { return _allBackupsSize; } set { _allBackupsSize = value; OnPropertyChanged(nameof(AllBackupsSize)); } }
    public long? AllBackupsWasted { get { return _allBackupsWasted; } set { _allBackupsWasted = value; OnPropertyChanged(nameof(AllBackupsWasted)); } }

    public IBrush ButtonSelectDatabaseColor { get { return _buttonSelectDatabaseColor; } set { _buttonSelectDatabaseColor = value; OnPropertyChanged(nameof(ButtonSelectDatabaseColor)); } }

    public bool IsCompareElementsSelected { get { return _isCompareElementsSelected; } set { _isCompareElementsSelected = value; OnPropertyChanged(nameof(IsCompareElementsSelected)); } }

    public bool IsProjectLoaded { get { return _isProjectLoaded; } set { _isProjectLoaded = value; OnPropertyChanged(nameof(IsProjectLoaded)); } }

    public FileTree FileTree { get { return _fileTree; } set { _fileTree = value; OnPropertyChanged(nameof(FileTree)); } }

    public ObservableCollection<string> Items { get; set; }

    public ObservableCollection<Backup> Backups { get; set; } = [];

    public ObservableCollection<Backup> SelectedBackups { get; set; } = [];

    public string LoadButtonLabel { get { return _loadButtonLabel; } set { _loadButtonLabel = value; OnPropertyChanged(nameof(LoadButtonLabel)); } }

    public bool ProgressVisible { get { return _progressVisible; } set { _progressVisible = value; OnPropertyChanged(nameof(ProgressVisible)); } }
    public double Progress { get { return _progress; } set { _progress = value; OnPropertyChanged(nameof(Progress)); } }
    public string ProgressTextFormat { get { return _progressTextFormat; } set { _progressTextFormat = value; OnPropertyChanged(nameof(ProgressTextFormat)); } }

    public bool IsLoadingDatabase { get { return _isLoadingDatabase; } set { _isLoadingDatabase = value; OnPropertyChanged(nameof(IsLoadingDatabase)); } }

    public FileTree? LeftSide { get { return _leftSide; } set { _leftSide = value; OnPropertyChanged(nameof(LeftSide)); } }
    public FileTree? RightSide { get { return _rightSide; } set { _rightSide = value; OnPropertyChanged(nameof(RightSide)); } }


    public bool IsProcessing { get { return _isProcessing; } set { _isProcessing = value; OnPropertyChanged(nameof(IsProcessing)); } }

    public string WindowTitle { get; set; }


    private void ShowProgressBar(bool show)
    {
        Progress = 0;
        ProgressTextFormat = $"Progress... ({{1:0}} %)";
        ProgressVisible = show;
    }

    private void SelectedBackups_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (SelectedBackups.Count > 0)
        {
            var backup = SelectedBackups[0];
            if (backup.FileTree == null)
            {
                //throw new InvalidOperationException("No FileTree in backup");
                FileTree = new("Loading...");
                _loadingFileTree = backup;
                _loadingWait.Set();
            }
            else
            {
                FileTree = backup.FileTree;
            }
        }
    }

    public void SetProvider(IStorageProvider provider)
    {
        _provider = provider;
    }

    public Task SelectLeftSide(object? sender)
    {
        return SelectSide(sender, true);
    }

    public Task SelectRightSide(object? sender)
    {
        return SelectSide(sender, false);
    }

    private async Task<FileTree> GetFileTreeFromSelection(object? selection)
    {
        FileTree? ft = null;

        if (selection is TreeView tree)
        {
            if (tree.SelectedItem != null)
            {
                var file = (FileNode)tree.SelectedItem;

                ft = new FileTree()
                {
                    Name = $"{FileTree} - {file}"
                };
                foreach (var f in file.GetChildrensRecursive().Where(x => x.IsFile))
                {
                    ft.AddPath(f.FullPath, f.BlocksetId.GetValueOrDefault(), f.NodeSize);
                }
            }
        }
        else if (selection is ListBox box)
        {
            ft = new FileTree();

            ListBox listbox = box;

            if (listbox.SelectedItem != null)
            {
                var backup = (Backup)listbox.SelectedItem;
                ft = backup.FileTree;
                if (ft == null)
                {
                    // Try to load fileset
                    await _dbTaskScheduler.Run(() => LoadFileset(backup, 100.0));
                    ft = backup.FileTree;
                }
            }
        }

        if (ft == null)
        {
            throw new InvalidOperationException("FileTree is null");
        }

        return ft;
    }

    public async Task SelectSide(object? sender, bool left)
    {
        var ft = await GetFileTreeFromSelection(sender);

        if (left)
        {
            LeftSide = ft;
        }
        else
        {
            RightSide = ft;
        }

        if (LeftSide != null && RightSide != null)
        {
            IsCompareElementsSelected = true;
        }
    }

    async public Task CompareToAll(object? sender)
    {
        IsProcessing = true;
        ShowProgressBar(true);

        var ftLeft = await GetFileTreeFromSelection(sender);

        var progressStep = 100.0 / ftLeft.GetFileNodes().Count();
        _comparer.OnBlocksCompareFinished += () =>
        {
            Progress += progressStep;
        };

        Progress = 5;

        // Load each fileset individually as the collection is enumerated
        // This means not everything has to be in memory at the same time
        await Task.Run(() => _comparer.CompareFiletrees(
            ftLeft,
            Backups.Select(x =>
            {
                if (x.FileTree == null)
                {
                    return _dbTaskScheduler.Run(() =>
                    {
                        LoadFileset(x);
                        return x.FileTree!;
                    });
                }
                else
                {
                    return Task.FromResult(x.FileTree);
                }
            })
            // Blocks for each result
            .Select(t => t.Result).Where(x => x != ftLeft)));

        ftLeft.UpdateDirectoryCompareResults();

        var dialog = new CompareResultWindow
        {
            Title = $"Comparison Result - {ftLeft.Name} <-> All",
            DataContext = new CompareResultModel() { FileTree = ftLeft, RightSideName = "All Backups" }
        };

        dialog.Show();

        IsProcessing = false;
        ShowProgressBar(false);
    }

    async public Task Compare(object? _)
    {
        if (LeftSide == null)
            throw new InvalidOperationException("LeftSide not set");
        if (RightSide == null)
            throw new InvalidOperationException("RightSide not set");
        if (RightSide.Name == null)
            throw new InvalidOperationException("RightSide name not set");

        IsProcessing = true;
        ShowProgressBar(true);

        var progressStep = 100.0 / LeftSide.GetFileNodes().Count();
        _comparer.OnBlocksCompareFinished += () =>
        {
            Progress += progressStep;
        };

        Progress = 5;
        await Task.Run(() => _comparer.CompareFiletree(LeftSide, RightSide));
        LeftSide.UpdateDirectoryCompareResults();

        var dialog = new CompareResultWindow
        {
            Title = $"Comparison Result - {LeftSide.Name} <-> {RightSide.Name}",
            DataContext = new CompareResultModel() { FileTree = LeftSide, RightSideName = RightSide.Name }
        };

        dialog.Show();

        IsProcessing = false;
        ShowProgressBar(false);
    }


    public async Task SelectDatabase(object parent)
    {
        if (IsLoadingDatabase)
        {
            if (_loadProjectCancellation is null)
                throw new InvalidOperationException("Cancellation is null");

            await _loadProjectCancellation.CancelAsync();
            IsLoadingDatabase = false;
        }
        else
        {
            var storageFile = await DoOpenFilePickerAsync();
            if (storageFile == null) return;

            try
            {
                if (storageFile.Path.AbsolutePath.EndsWith("Duplicati-server.sqlite"))
                {
                    throw new InvalidOperationException("Cannot load the Duplicati internal database. Please select one of the randomly named sqlite database files each one representing one backup job.");
                }

                using (_loadProjectCancellation = new CancellationTokenSource())
                {
                    ProjectFilename = storageFile.Path.LocalPath;

                    IsProjectLoaded = false;
                    LoadButtonLabel = "Cancel";
                    ButtonSelectDatabaseColor = Brushes.Red;
                    Backups.Clear();

                    IsLoadingDatabase = true;
                    ShowProgressBar(true);

                    try
                    {
                        Progress = 3;
                        ProgressTextFormat = $"Opening database... ({{1:0}} %)";

                        await Task.Run(() => _database.Open(ProjectFilename));
                  
                        _database.CheckUnsupportedDatabaseVersion();

                        if (!_database.CheckSupportedDatabaseVersion())
                        {
                            var box = MessageBoxManager.GetMessageBoxStandard("Unknown Database Version", $"The database version ({_database.GetVersion()}) of the selected file ({storageFile.Path}) is unknown and therefore not tested. If you encounter problems, feel free to open an issue on Github.", ButtonEnum.Ok, Icon.Warning);
                            await box.ShowAsPopupAsync((Window)parent);
                        }

                        await _dbTaskScheduler.Run(LoadBackups);

                        IsProjectLoaded = true;
                        _ = Task.Run(() => LazyLoadFileTrees(CancellationToken.None));
                    }
                    catch (OperationCanceledException)
                    {
                        IsProjectLoaded = false;
                        AllBackupsSize = 0;
#pragma warning disable S4158 // Empty collections should not be accessed or iterated
                        Backups.Clear();
#pragma warning restore S4158 // Empty collections should not be accessed or iterated
                    }
                    finally
                    {
                        IsLoadingDatabase = false;
                        ShowProgressBar(false);
                        LoadButtonLabel = "Select Database";
                        ButtonSelectDatabaseColor = Brushes.Green;
                    }
                }
            }
            catch (Exception e)
            {
                AllBackupsSize = 0;
                var box = MessageBoxManager.GetMessageBoxStandard("Error opening Database", $"An error occured when trying to load the Duplicati database file '{storageFile.Path}': {e.Message}", ButtonEnum.Ok);
                await box.ShowAsPopupAsync((Window)parent);
            }
        }
    }

    private async Task<IStorageFile?> DoOpenFilePickerAsync()
    {
        var files = await _provider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open Text File",
            AllowMultiple = false
        });

        return files?.Count >= 1 ? files[0] : null;
    }

    void LoadFileset(Backup backup, double detailedProgress = 0.0)
    {
        if (backup.FileTree != null)
        {
            // Already loaded
            return;
        }

        var ft = new FileTree() { Name = $"Backup {backup.Fileset}" };
        ProgressTextFormat = $"Loading fileset {backup} ({{1:0}} %)";

        var files = _database.GetFilesInFileset(backup.Fileset.Id);

        // Divide detailed progress into steps per file
        detailedProgress /= files.Count;

        foreach (var file in files)
        {
            long? fileSize = null;
            if (file.BlocksetId >= 0)
            {
                var blocks = _database.GetBlocksByBlocksetId(file.BlocksetId);
                fileSize = blocks.Sum(x => x.Size);
            }

            ft.AddPath(Path.Join(file.Prefix, file.Path), file.BlocksetId, fileSize);
            Progress += detailedProgress;
        }

        var node = _loadedTrees.Find(backup.Fileset.Id);
        if (node != null)
        {
            // Move to front of queue
            _loadedTrees.Remove(node);
            _loadedTrees.AddFirst(node);
        }
        else
        {
            _loadedTrees.AddFirst(backup.Fileset.Id);
            if (_loadedTrees.Count > MAX_LOADED_FILESETS)
            {
                // Unload the latest fileset
                var id = _loadedTrees.Last!.Value;
                var lastBackup = Backups.FirstOrDefault(b => b.Fileset.Id == id);
                if (lastBackup != null)
                {
                    lastBackup.FileTree = null;
                    _loadedTrees.RemoveLast();
                }
            }
        }
        backup.FileTree = ft;

    }
    void LoadFilesetStats(Backup backup)
    {
        ProgressTextFormat = $"Loading fileset stats {backup} ({{1:0}} %)";
        // Load only the overview statistics for the backup
        if (backup.FileTree != null)
        {
            // Already loaded
            return;
        }
        backup.Size = _database.GetFilesetSize(backup.Fileset.Id);
    }

    // Runs in background thread and loads the filetree for _loadingFileTree
    private async Task LazyLoadFileTrees(CancellationToken token)
    {
        try
        {
            while (true)
            {
                if (!_loadingWait.WaitOne(100) || IsLoadingDatabase)
                {
                    token.ThrowIfCancellationRequested();
                    continue;
                }
                var backup = _loadingFileTree;
                _loadingFileTree = null;
                _loadingWait.Reset();
                if (backup == null)
                {
                    continue;
                }
                ShowProgressBar(true);
                ProgressTextFormat = $"Loading fileset {backup} ({{1:0}} %)";
                await _dbTaskScheduler.Run(() => LoadFileset(backup, 100.0));
                ShowProgressBar(false);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Check whether another file tree was requested while loading
                    if (_loadingFileTree == null)
                    {
                        FileTree = backup.FileTree!;
                    }
                });
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _loadingWait.Dispose();
        }
    }

    void LoadBackups()
    {
        _database.InitCaches();

        Progress = 10;

        var filesets = _database.GetFilesets();

        var progStep = (100 - Progress) / filesets.Count;

        AllBackupsSize = _database.GetTotalSize();
        int fullLoadCount = 0;
        foreach (var item in filesets)
        {
            if (_loadProjectCancellation is null)
                throw new InvalidOperationException("Cancellation is null");

            _loadProjectCancellation.Token.ThrowIfCancellationRequested();


            var backup = new Backup { Fileset = item, FileTree = null };

            if (fullLoadCount < MAX_LOADED_FILESETS)
            {
                LoadFileset(backup);
                fullLoadCount += 1;
            }
            else
            {
                LoadFilesetStats(backup);
            }

            Progress += progStep;

            Backups.Add(backup);
        }
        AllBackupsWasted = _database.WastedSpaceSum();
    }
}