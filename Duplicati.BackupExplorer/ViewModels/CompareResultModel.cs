using Avalonia.Controls;

using Duplicati.BackupExplorer.LocalDatabaseAccess;

using Duplicati.BackupExplorer.LocalDatabaseAccess.Model;
using System;

namespace Duplicati.BackupExplorer.ViewModels;


public partial class CompareResultModel : ViewModelBase
{

    private string? _rightSideName = null;

    private bool _showDisjunct = false;

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

    public FileTree FileTree { get; set; }

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

    static public void Close(object parent)
    {
        var wnd = (Window)parent;
        wnd.Close();
    }
}