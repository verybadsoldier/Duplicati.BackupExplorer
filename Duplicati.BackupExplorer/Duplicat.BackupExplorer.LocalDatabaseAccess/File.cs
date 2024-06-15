using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess
{
    public class File
    {
        public long Id { get; set; }

        public string Prefix { get; set; }

        public string Path { get; set; }

        public long BlocksetId { get; set; }

        public long MetadataId { get; set; }
    }
}
