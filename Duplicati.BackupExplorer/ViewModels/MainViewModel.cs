using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Duplicati.BackupExplorer.LocalDatabaseAccess;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Database;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Model;
using Duplicati.BackupExplorer.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
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

    private CancellationTokenSource? _loadProjectCancellation;

    private bool _isLoadingDatabase = false;


    public MainViewModel(DuplicatiDatabase database, Comparer comparer, IStorageProvider provider)
    {
        _database = database;

        _comparer = comparer;

        _provider = provider;

        ProjectFilename = "";

        Items = [];

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown Version";
        WindowTitle = $"Duplicati BackupExplorer - v{version}";

        SelectedBackups.CollectionChanged += SelectedBackups_CollectionChanged;
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
        ft.AddPath(@"D:\MyDir\MyFile3.cs", 5399293492);
#pragma warning restore S1075 // URIs should not be hardcoded

        FileTree = ft;

        LeftSide = new FileTree();

        RightSide = new FileTree();
    }

    public string ProjectFilename { get { return _projectFilename; } set { _projectFilename = value; OnPropertyChanged(nameof(ProjectFilename)); } }

    public long? AllBackupsSize { get { return _allBackupsSize; } set { _allBackupsSize = value; OnPropertyChanged(nameof(AllBackupsSize)); } }

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
                throw new InvalidOperationException("No FileTree in backup");
            }
            FileTree = backup.FileTree;
        }
    }

    public void SetProvider(IStorageProvider provider)
    {
        _provider = provider;
    }

    public void SelectLeftSide(object? sender)
    {
        SelectSide(sender, true);
    }

    public void SelectRightSide(object? sender)
    {
        SelectSide(sender, false);
    }

    private FileTree GetFileTreeFromSelection(object? selection)
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
            }
        }

        if (ft == null)
        {
            throw new InvalidOperationException("FileTree is null");
        }

        return ft;
    }

    public void SelectSide(object? sender, bool left)
    {
        var ft = GetFileTreeFromSelection(sender);

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

        var ftLeft = GetFileTreeFromSelection(sender);

        var progressStep = 100.0 / ftLeft.GetFileNodes().Count();
        _comparer.OnBlocksCompareFinished += () =>
        {
            Progress += progressStep;
        };

        Progress = 5;

        if (Backups.Any(x => x.FileTree is null))
            throw new InvalidOperationException("Found Backup with null FileTree");

        await Task.Run(() => _comparer.CompareFiletrees(ftLeft, Backups.Select(x => x.FileTree!).Where(x => x != ftLeft)));

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
                using (_loadProjectCancellation = new CancellationTokenSource())
                {
                    ProjectFilename = storageFile.Path.AbsolutePath;

                    LoadButtonLabel = "Cancel";
                    ButtonSelectDatabaseColor = Brushes.Red;
                    Backups.Clear();

                    IsLoadingDatabase = true;
                    ShowProgressBar(true);

                    try
                    {
                        await Task.Run(LoadBackups);

                        IsProjectLoaded = true;
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
                var box = MessageBoxManager.GetMessageBoxStandard("Error opening Database", $"An error occured when trying to load the Duplicati database file '{storageFile}': {e.Message}", ButtonEnum.Ok);
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

    void LoadBackups()
    {
        Progress = 3;
        ProgressTextFormat = $"Opening database... ({{1:0}} %)";

        _database.Open(ProjectFilename);

        Progress = 10;

        var filesets = _database.GetFilesets();

        var progStep = (100 - Progress) / filesets.Count;

        AllBackupsSize = 0;
        HashSet<Block> allBlocks = [];
        foreach (var item in filesets)
        {
            if (_loadProjectCancellation is null)
                throw new InvalidOperationException("Cancellation is null");

            _loadProjectCancellation.Token.ThrowIfCancellationRequested();


            var ft = new FileTree() { Name = $"Backup {item}" };
            var backup = new Backup { Fileset = item, FileTree = null };

            ProgressTextFormat = $"Loading fileset {backup} ({{1:0}} %)";

            var fsentries = _database.GetFilesetEntriesById(backup.Fileset.Id);
            var files = _database.GetFilesByIds4(fsentries.Select(x => x.FileId));

            foreach (var file in files)
            {
                long? fileSize = null;
                if (file.BlocksetId >= 0)
                {
                    var blocks = _database.GetBlocksByBlocksetId(file.BlocksetId);
                    allBlocks.UnionWith(blocks);

                    fileSize = blocks.Sum(x => x.Size);
                }


                ft.AddPath(Path.Join(file.Prefix, file.Path), file.BlocksetId, fileSize);

            }

            Progress += progStep;

            backup.FileTree = ft;

            AllBackupsSize = allBlocks.Sum(x => x.Size);
            Backups.Add(backup);
        }
    }
}
