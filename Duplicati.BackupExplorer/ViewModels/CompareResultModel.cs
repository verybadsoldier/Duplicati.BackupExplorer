using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

using Avalonia.Platform.Storage;
using Duplicati.BackupExplorer.LocalDatabaseAccess;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Database;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Formats.Tar;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.ViewModels;


public partial class CompareResultModel : ViewModelBase
{
    public FileTree FileTree { get; set; }

    private string? _rightSideName = null;

    private bool _showDisjunct = false;

    public string RightSideName
    {
        get
        {
            if (_rightSideName == null)
            {
                throw new InvalidOperationException("No right side name set");
            }
            return _rightSideName;
        }
        set
        {
            _rightSideName = value;
            OnPropertyChanged(nameof(RightSideName));
        }
    }

    public bool ShowDisjunct
    {
        get
        {
            return _showDisjunct;
        }
        set
        {
            _showDisjunct = value;
            OnPropertyChanged(nameof(ShowDisjunct));
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "<Pending>")]
    public CompareResultModel()
    {
        var fileTree = new FileTree();
        var node = fileTree.AddPath("D:\\myPath\\myFile.dat", 224);
        node.CompareResult = new CompareResult() { RightSize = 1000000000, LeftSize = 12300000000, LeftNumBlocks = 200, RightNumBlocks = 900, SharedSize = 616515125, SharedNumBlocks = 180 };
        fileTree.UpdateDirectoryCompareResults();
        RightSideName = "Backup 20-12-2024";
        FileTree = fileTree;
    }

    public void Close(object parent)
    {
        var wnd = (Window)parent;
        wnd.Close();
    }
}