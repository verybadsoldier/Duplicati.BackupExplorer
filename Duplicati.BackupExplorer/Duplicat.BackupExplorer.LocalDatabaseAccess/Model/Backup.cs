using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Model
{
    public class Backup
    {
        public Fileset Fileset { get; set; }

        public FileTree? FileTree { get; set; }

        public long Size {  get { return ((FileNode)FileTree.Nodes[0]).NodeSize; } }

        public override string ToString()
        {
            return Fileset.ToString();
        }

    }
}
