namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model
{
    public class FilesetEntry
    {
        public long FilesetId { get; set; }

        public long FileId { get; set; }

        public long LastModified { get; set; }
    }
}
