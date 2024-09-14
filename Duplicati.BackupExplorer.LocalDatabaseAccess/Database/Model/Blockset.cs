namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model
{
    public class Blockset
    {
        public long Id { get; set; }

        public long Length { get; set; }

        public string FullHash { get; set; } = string.Empty;


        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is Blockset blockset)
            {
                return Id == blockset.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return FullHash.GetHashCode();
        }
    }
}
