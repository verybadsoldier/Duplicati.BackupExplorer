namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Database
{
    using Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model;
    using Microsoft.Data.Sqlite;
    using Microsoft.VisualBasic;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using BlockID = long;
    using BlocksetID = long;
    using File = Model.File;

    public class DuplicatiDatabase : IDisposable
    {
        private SqliteConnection? _conn;
        private readonly Dictionary<BlocksetID, List<BlockID>> _blocklistIdCache = [];
        private bool _disposed = false;
        private List<File> _filesCache = [];
        private Dictionary<BlockID, Block> _blocksCache = [];
        private Dictionary<BlocksetID, HashSet<Block>> _blocksetCache = [];
        private readonly Dictionary<string, int> _databaseVersion = new();

        public DuplicatiDatabase()
        {
            _databaseVersion  = new Dictionary<string, int>()
            {
                { "2.0.8", 12 },
            };
        }

        public void CheckDatabaseCompatibility(int dbVersion)
        {
            foreach (var kvp in _databaseVersion)
            {
                if (kvp.Value == dbVersion)
                    return;
            }
            var versions = string.Join(",", _databaseVersion.Keys);
            throw new InvalidOperationException($"Database version {dbVersion} not supported. Supported Duplicati versions: {versions}");
        }

        public void Open(string filepath)
        {
            if (!System.IO.File.Exists(filepath))
            {
                Console.WriteLine($"Database file {filepath} does not exist.");
                Environment.Exit(1);
            }

            _conn = OpenInMemory(filepath);

            InitFilesCache();
            InitBlocksCache();
            InitBlocksetCache();
        }

        private static SqliteConnection OpenInMemory(string filePath)
        {
            string inMemoryConnectionString = "Data Source=:memory:;";

            // Open the file-based database connection
            using var fileConnection = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly;");
            fileConnection.Open();

            // Create an in-memory database connection
            var memoryConnection = new SqliteConnection(inMemoryConnectionString);
            memoryConnection.Open();

            // Backup the file-based database to the in-memory database
            fileConnection.BackupDatabase(memoryConnection);

            return memoryConnection;
        }

        private void CheckConnectionNotNull()
        {
            if (_conn == null)
                throw new InvalidOperationException("No active SQL connection");
        }

        public List<long> GetBlockIdsByBlocksetId(BlocksetID blocksetid)
        {
            CheckConnectionNotNull();

            if (_blocklistIdCache.TryGetValue(blocksetid, out var result))
                return result;

            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"
            SELECT
                BlockID
            FROM 
                BlocksetEntry
            WHERE 
                BlocksetID = @blocksetid";
            cmd.Parameters.AddWithValue("@blocksetid", blocksetid);

            var blockIds = new List<long>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                blockIds.Add(reader.GetInt64(0));
            }

            _blocklistIdCache[blocksetid] = blockIds;
            return blockIds;
        }


        public List<Fileset> GetFilesets()
        {
            return GetFilesetsRaw();
        }

        private List<Fileset> GetFilesetsRaw()
        {
            CheckConnectionNotNull();

            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"
            SELECT
                ID, OperationID, VolumeID, IsFullBackup, Timestamp
            FROM 
                Fileset
            ";

            var result = new List<Fileset>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new Fileset
                {
                    Id = reader.GetInt64(0),
                    OperationId = reader.GetInt64(1),
                    VolumeId = reader.GetInt64(2),
                    IsFullBackup = reader.GetBoolean(3),
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(4)),
                });
            }

            return result;
        }

        public List<FilesetEntry> GetFilesetEntriesById(long filesetId)
        {
            CheckConnectionNotNull();

            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"
            SELECT
                FileID, Lastmodified
            FROM 
                FilesetEntry
            WHERE 
                FilesetID = @filesetId";
            cmd.Parameters.AddWithValue("@filesetId", filesetId);

            var result = new List<FilesetEntry>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new FilesetEntry { FilesetId = filesetId, FileId = reader.GetInt32(0), LastModified = reader.GetInt64(1) });
            }

            return result;
        }

        public File GetFileById(long fileId)
        {
            CheckConnectionNotNull();

            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    F.BlocksetID, Path, pp.Prefix, MetadataID
                FROM 
                    FileLookup F
                LEFT JOIN
	                PathPrefix pp ON pp.ID = F.PrefixID 
                WHERE 
                    F.ID = @fileId";
            cmd.Parameters.AddWithValue("@fileId", fileId);

            using var reader = cmd.ExecuteReader();
            reader.Read();
            return new File { Id = fileId, BlocksetId = reader.GetInt32(0), Path = reader.GetString(1), Prefix = reader.GetString(2), MetadataId = reader.GetInt64(3) };
        }

        public List<File> GetFilesByIds4(IEnumerable<long> fileIds)
        {
            var ids = fileIds.ToHashSet();
            return _filesCache.Where(x => ids.Contains(x.Id)).ToList();
        }

        public int GetVersion()
        {
            CheckConnectionNotNull();

            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    ID, Version
                FROM 
                    Version";

            using var reader = cmd.ExecuteReader();
            reader.Read();
            return reader.GetInt32(1);
        }


        public void InitFilesCache()
        {
            CheckConnectionNotNull();

            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    F.ID, F.BlocksetID, Path, pp.Prefix, MetadataID
                FROM 
                    FileLookup F
                LEFT JOIN
	                PathPrefix pp ON pp.ID = F.PrefixID";

            var unsortedFiles = new List<File>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                unsortedFiles.Add(new File { Id = reader.GetInt64(0), BlocksetId = reader.GetInt32(1), Path = reader.GetString(2), Prefix = reader.GetString(3), MetadataId = reader.GetInt64(4) });
            }

            _filesCache = [.. unsortedFiles.OrderBy(x => x.Prefix + x.Path)];
        }

        public void InitBlocksCache()
        {
            CheckConnectionNotNull();

            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"
            SELECT
                ID, Size, VolumeID
            FROM 
                Block";

            _blocksCache = [];

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var blockId = reader.GetInt64(0);
                _blocksCache.Add(blockId, new Block { Id = reader.GetInt64(0), Size = reader.GetInt64(1), VolumeId = reader.GetInt64(2) });
            }
        }

        public void InitBlocksetCache()
        {
            CheckConnectionNotNull();

            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"
            SELECT
                BlocksetID, BlockID
            FROM 
                BlocksetEntry
            ";

            _blocksetCache = [];

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var blocksetID = reader.GetInt64(0);
                var blockID = reader.GetInt64(1);

                if (!_blocksetCache.TryGetValue(blocksetID, out HashSet<Block>? hset))
                {
                    hset = [];
                    _blocksetCache[blocksetID] = hset;
                }
                hset.Add(_blocksCache[blockID]);
            }
        }

        async public Task<Block> GetBlock(BlockID blockId)
        {
            CheckConnectionNotNull();

            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"
            SELECT
                ID, Size, VolumeID
            FROM 
                Block
            WHERE 
                ID = @blockid";
            cmd.Parameters.AddWithValue("@blockid", blockId);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            return new Block { Id = reader.GetInt64(0), Size = reader.GetInt64(1), VolumeId = reader.GetInt64(2) };
        }

        public HashSet<Block> GetBlocksByBlocksetId(BlocksetID blocksetId)
        {
            return _blocksetCache[blocksetId];
        }

        async public Task<List<Block>> GetBlocks(IEnumerable<BlockID> blockIds)
        {
            CheckConnectionNotNull();

            var result = new List<Block>();

            int pos = 0;
            int batchSize = 100;
            while (true)
            {
                using var cmd = _conn!.CreateCommand();
                cmd.CommandText = @"
                SELECT
                    ID, Size, VolumeID
                FROM 
                    Block
                WHERE 
                    ID IN ({blockIds})";
                cmd.AddArrayParameters("blockIds", blockIds.Skip(pos).Take(batchSize));

                pos += batchSize;

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new Block { Id = reader.GetInt64(0), Size = reader.GetInt64(1), VolumeId = reader.GetInt64(2) });
                }

                if (cmd.Parameters.Count != batchSize)
                    break;
            }

            return result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _conn?.Dispose();
                }
                // Dispose unmanaged resources (if any)

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

}
