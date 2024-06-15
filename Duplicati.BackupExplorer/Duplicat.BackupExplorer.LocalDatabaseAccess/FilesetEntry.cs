using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess
{
    public class FilesetEntry
    {
        public long FilesetId { get; set; }

        public long FileId { get; set; }

        public long LastModified { get; set; }
    }
}
