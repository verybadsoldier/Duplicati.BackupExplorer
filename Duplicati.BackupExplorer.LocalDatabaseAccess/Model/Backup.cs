using Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Model
{
    public class Backup
    {
        public Fileset Fileset { get; set; } = new Fileset();

        public FileTree? FileTree { get; set; }
        private long size = -1;

        public long Size
        {
            get
            {
                if (size >= 0)
                {
                    return size;
                }
                if (FileTree is null)
                {

                    throw new InvalidOperationException("FileTree is null");
                }
                else
                {
                    // Cache because this is a recursive calculation
                    size = (FileTree.Nodes[0]).NodeSize;
                    return size;
                }
            }
            set
            {
                size = value;
            }
        }

        public override string ToString()
        {
            return Fileset.ToString();
        }

    }
}
