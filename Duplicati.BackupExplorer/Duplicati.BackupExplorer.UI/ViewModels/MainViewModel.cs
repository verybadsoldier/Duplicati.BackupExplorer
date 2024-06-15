using Avalonia.Controls;
using Duplicati.BackupExplorer.LocalDatabaseAccess;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Duplicati.BackupExplorer.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly DuplicatiDatabase _database;

    private readonly Comparer _comparer;

    public MainViewModel(DuplicatiDatabase database, Comparer comparer)
    {
        _database = database;

        _comparer = comparer;

        ProjectFilename = "D:\\duplicati.sqlite";

        if (Design.IsDesignMode)
        {
            ProjectFilename = @"D:\Duplicati\database.sqlite";
            Backups = new ObservableCollection<Fileset> { 
                new Fileset { Id = 1, Timestamp = new System.DateTimeOffset(2021, 12, 1, 12, 14, 55, System.TimeSpan.Zero), VolumeId=1 },
                new Fileset { Id = 2, Timestamp = new System.DateTimeOffset(2022, 12, 1, 12, 14, 55, System.TimeSpan.Zero), VolumeId=2 },
            };
        }
    }

    public string ProjectFilename { get; set; } = "";

    public ObservableCollection<Fileset> Backups { get; set; } = new ObservableCollection<Fileset>();

    public ObservableCollection<Fileset> SelectedBackups { get; set; } = new ObservableCollection<Fileset>();

    public void Compare()
    {
        if (SelectedBackups.Count != 2)
            return;

        var bak1 = SelectedBackups[0];
        var bak2 = SelectedBackups[1];

        var result = _comparer.CompareFilesets(bak1, bak2);
    }

    public void LoadProject()
    {
        _database.Open(ProjectFilename);

        var Filesets = _database.GetFilesets();

        Backups.Clear();
        foreach (var item in Filesets)
        {
            Backups.Add(item);
        }
    }
}
