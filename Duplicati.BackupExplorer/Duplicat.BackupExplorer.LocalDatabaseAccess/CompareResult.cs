using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess
{
    public class CompareResult
    {
        public long numBlocksRoot { get; set; }
        public long sizeRoot { get; set; }
        public long numBlocksCompare { get; set; }
        public long sizeCompare { get; set; }

        public long sizeShare {  get; set; }
        public float sharePercentage { get; set; }
        public float shareSizePercentage { get; set; }
    }
}
