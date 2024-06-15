using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess
{
    public class Fileset
    {
        public long Id { get; set; }

        public long OperationId { get; set; }

        public long VolumeId { get; set; }

        public bool IsFullBackup { get; set; }

        public DateTimeOffset Timestamp {  get; set; }

    }
}
