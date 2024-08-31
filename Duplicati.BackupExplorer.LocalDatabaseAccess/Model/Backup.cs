using Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Model
{
    public class Backup
    {
        public Fileset Fileset { get; set; } = new Fileset();

        public FileTree? FileTree { get; set; }

        public long Size
        {
            get
            {
                if (FileTree is null)
                {
                    throw new InvalidOperationException("FileTree is null");
                }
                return (FileTree.Nodes[0]).NodeSize;
            }
        }

        public override string ToString()
        {
            return Fileset.ToString();
        }

    }
}
