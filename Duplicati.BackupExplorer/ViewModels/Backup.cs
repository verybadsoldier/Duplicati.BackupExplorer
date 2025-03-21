using Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.ViewModels
{
    
    public class Backup : ViewModelBase
    {
        private Duplicati.BackupExplorer.LocalDatabaseAccess.Model.Backup _backup = new Duplicati.BackupExplorer.LocalDatabaseAccess.Model.Backup();

        public Fileset Fileset { get { return _backup.Fileset; } set { _backup.Fileset = value; OnPropertyChanged(nameof(Fileset)); } }

        public FileTree? FileTree { get { return _backup.FileTree; } set { _backup.FileTree = value; OnPropertyChanged(nameof(FileTree)); } }

        public long Size { get { return _backup.Size; } set { _backup.Size = value; OnPropertyChanged(nameof(Size)); } }

        public override string ToString()
        {
            return _backup.ToString();
        }
    }
}
