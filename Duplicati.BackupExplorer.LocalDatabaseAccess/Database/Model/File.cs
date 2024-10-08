﻿namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model
{
    public class File
    {
        public long Id { get; set; }

        public string Prefix { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public long BlocksetId { get; set; }

        public long MetadataId { get; set; }

        public override string ToString()
        {
            return Prefix + Path;
        }
    }
}
