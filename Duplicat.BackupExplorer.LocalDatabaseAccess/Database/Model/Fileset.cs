using Duplicati.BackupExplorer.LocalDatabaseAccess.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model
{
    public class Fileset
    {
        public long Id { get; set; }

        public long OperationId { get; set; }

        public long VolumeId { get; set; }

        public bool IsFullBackup { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public FileTree FileTree { get; set; } = new FileTree();

        override public String ToString()
        {
            return Timestamp.ToString("yyyy-MM-dd HH:mm");
        }
    }
}
