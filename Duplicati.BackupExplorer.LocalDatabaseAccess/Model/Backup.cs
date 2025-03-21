using Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Model
{
    public class Backup
    {
        public Fileset Fileset { get; set; } = new Fileset();

        public FileTree? FileTree { get; set; }

        public long Size { get; set; }

        public override string ToString()
        {
            return Fileset.ToString();
        }
    }
}
