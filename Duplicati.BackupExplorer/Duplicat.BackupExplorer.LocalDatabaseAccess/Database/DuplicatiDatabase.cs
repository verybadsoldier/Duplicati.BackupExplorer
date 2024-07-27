namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Database
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Microsoft.Data.Sqlite;
    using System.IO;

    using SizeBytes = long;
    using BlockID = long;
    using FileID = long;
    using VolumeID = long;
    using BlocksetID = long;
    using OperationID = long;
    using FilesetID = long;
    using PathPrefixID = long;
    using static System.Net.WebRequestMethods;
    using Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model;
    using File = Model.File;

    public class DuplicatiDatabase : IDisposable
    {
        private string _filepath;
        private SqliteConnection _conn;
        private Dictionary<BlocksetID, List<BlockID>> _blocklistIdCache = new Dictionary<BlocksetID, List<BlockID>>();
        private Dictionary<BlockID, SizeBytes> _blocksizesCache = new Dictionary<BlockID, SizeBytes>();
        private bool _disposed = false;
        private List<File>? _filesCache = null;
        private Dictionary<BlockID, Block>? _blocksCache = null;
        private Dictionary<BlocksetID, HashSet<Block>>? _blocksetCache = null;

        public DuplicatiDatabase()
        { }

        public void Open(string filepath)
        {
            _filepath = filepath;
            if (!System.IO.File.Exists(_filepath))
            {
                Console.WriteLine($"Database file {_filepath} does not exist.");
                Environment.Exit(1);
            }

            _conn = OpenInMemory(_filepath);

            //_conn = new SqliteConnection($"Data Source={_filepath}");
            //_conn.Open();
            InitFilesCache();
            InitBlocksCache();
            InitBlocksetCache();
        }

        private SqliteConnection OpenInMemory(string filePath)
        {
            string inMemoryConnectionString = "Data Source=:memory:;";

            // Open the file-based database connection
            using (var fileConnection = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly;"))
            {
                fileConnection.Open();

                // Create an in-memory database connection
                var memoryConnection = new SqliteConnection(inMemoryConnectionString);
                memoryConnection.Open();

                // Backup the file-based database to the in-memory database
                fileConnection.BackupDatabase(memoryConnection);//, "main", "main", -1, null, 0);

                return memoryConnection;
            }
        }

        public List<Tuple<string, string>> GetFileVersionsAi(string filename)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
            SELECT DISTINCT 
                FL.Path, B.Hash
            FROM 
                FileLookup FL
            JOIN 
                FilesetEntry FSE ON FL.ID = FSE.FileID
            JOIN 
                Fileset FS ON FSE.FilesetID = FS.ID
            JOIN 
                BlocksetEntry BSE ON FL.BlocksetID = BSE.BlocksetID
            JOIN 
                Block B ON BSE.BlockID = B.ID
            WHERE 
                FL.Path = @filename";
            cmd.Parameters.AddWithValue("@filename", filename);

            var result = new List<Tuple<string, string>>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(Tuple.Create(reader.GetString(0), reader.GetString(1)));
            }

            return result;
        }

        public List<Tuple<int, string, int>> GetFileVersions(string filename)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
            SELECT
                ID, Path, BlocksetID
            FROM 
                FileLookup FL
            WHERE 
                FL.Path = @filename";
            cmd.Parameters.AddWithValue("@filename", filename);

            var result = new List<Tuple<int, string, int>>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(Tuple.Create(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)));
            }

            return result;
        }

        public List<long> GetBlockIdsByBlocksetId(BlocksetID blocksetid)
        {
            if (!_blocklistIdCache.ContainsKey(blocksetid))
            {
                using var cmd = _conn.CreateCommand();
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
            }

            return _blocklistIdCache[blocksetid];
        }

        public List<Tuple<int, long>> GetBackups()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
            SELECT
                ID, Timestamp
            FROM 
                Operation
            WHERE 
                Description = @description";
            cmd.Parameters.AddWithValue("@description", "Backup");

            var result = new List<Tuple<int, long>>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(Tuple.Create(reader.GetInt32(0), reader.GetInt64(1)));
            }

            return result;
        }

        public Tuple<string, int> GetOperationById(OperationID operationId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
            SELECT
                Description, Timestamp
            FROM 
                Operation
            WHERE 
                ID = @operationId";
            cmd.Parameters.AddWithValue("@operationId", operationId);

            using var reader = cmd.ExecuteReader();
            reader.Read();

            return Tuple.Create(reader.GetString(0), reader.GetInt32(1));
        }


        public List<Tuple<OperationID, VolumeID, bool, int>> GetOperationsByFilesetID(FilesetID filesetID)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
            SELECT
                OperationID, Timestamp, VolumeID, IsFullBackup
            FROM 
                Fileset
            WHERE 
                ID = @filesetID";
            cmd.Parameters.AddWithValue("@filesetID", filesetID);

            var result = new List<Tuple<OperationID, VolumeID, bool, int>>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(Tuple.Create(reader.GetInt64(0), reader.GetInt64(1), reader.GetBoolean(2), reader.GetInt32(3)));
            }

            return result;
        }

        public Fileset GetFilesetByOperationId(OperationID operationId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
            SELECT
                ID, OperationID, VolumeID, IsFullBackup, Timestamp
            FROM 
                Fileset
            ";

            var result = new List<Fileset>();
            using var reader = cmd.ExecuteReader();
            reader.Read();
            return new Fileset
            {
                Id = reader.GetInt64(0),
                OperationId = reader.GetInt64(1),
                VolumeId = reader.GetInt64(2),
                IsFullBackup = reader.GetBoolean(3),
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(4)),
            };
        }

        public Fileset GetFilesetForOperation(OperationID operationId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
            SELECT
                ID, VolumeID, Timestamp
            FROM 
                Fileset
            WHERE 
                OperationID = @operationId";
            cmd.Parameters.AddWithValue("@operationId", operationId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Fileset { Id = reader.GetInt64(0), VolumeId = reader.GetInt64(1), Timestamp = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(2)) };
            }

            return null;
        }

        public List<FilesetID> GetFilesetsByFileId(FileID fileId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
            SELECT
                FilesetID
            FROM 
                FilesetEntry
            WHERE 
                FileID = @fileId";
            cmd.Parameters.AddWithValue("@fileId", fileId);

            var result = new List<FilesetID>();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                result.Add(reader.GetInt64(0));
            }

            return result;
        }

        public List<Fileset> GetFilesets()
        {
            return GetFilesetsRaw();
        }

        private List<Fileset> GetFilesetsRaw()
        {
            using var cmd = _conn.CreateCommand();
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
            using var cmd = _conn.CreateCommand();
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
            using var cmd = _conn.CreateCommand();
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

        public IEnumerable<File> GetFilesByIds(IEnumerable<long> fileIds)
        {
            foreach (var fileId in fileIds)
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = @"
                SELECT
                    F.ID, F.BlocksetID, Path, pp.Prefix, MetadataID
                FROM 
                    FileLookup F
                LEFT JOIN
	                PathPrefix pp ON pp.ID = F.PrefixID 
                WHERE 
                    F.ID = @fileId";
                cmd.Parameters.AddWithValue("@fileId", fileId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    yield return new File { Id = reader.GetInt64(0), BlocksetId = reader.GetInt32(1), Path = reader.GetString(2), Prefix = reader.GetString(3), MetadataId = reader.GetInt64(4) };
                }
            }
        }

        public IEnumerable<File> GetFilesByIds2(IEnumerable<long> fileIds)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    F.ID, F.BlocksetID, Path, pp.Prefix, MetadataID
                FROM 
                    FileLookup F
                LEFT JOIN
	                PathPrefix pp ON pp.ID = F.PrefixID 
                WHERE 
                    F.ID IN ({fileIds})";
            cmd.AddArrayParameters("fileIds", fileIds);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new File { Id = reader.GetInt64(0), BlocksetId = reader.GetInt32(1), Path = reader.GetString(2), Prefix = reader.GetString(3), MetadataId = reader.GetInt64(4) };
            }
        }

        public List<File> GetFilesByIds4(IEnumerable<long> fileIds)
        {
            var list = new List<File>();
            var ids = fileIds.ToHashSet();
            foreach (var file in _filesCache)
            {
                if (ids.Contains(file.Id))
                {
                    list.Add(file);
                }
            }
            return list;
        }


        public void InitFilesCache()
        {
            using var cmd = _conn.CreateCommand();
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

            _filesCache = unsortedFiles.OrderBy(x => x.Prefix + x.Path).ToList();
        }

        public void InitBlocksCache()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
            SELECT
                ID, Size, VolumeID
            FROM 
                Block";

            _blocksCache = new Dictionary<BlockID, Block>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var blockId = reader.GetInt64(0);
                _blocksCache.Add(blockId, new Block { Id = reader.GetInt64(0), Size = reader.GetInt64(1), VolumeId = reader.GetInt64(2) });
            }
        }

        public void InitBlocksetCache()
        {

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
            SELECT
                BlocksetID, BlockID
            FROM 
                BlocksetEntry
            ";


            _blocksetCache = new Dictionary<BlocksetID, HashSet<Block>>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var blocksetID = reader.GetInt64(0);
                var blockID = reader.GetInt64(1);
                if (!_blocksetCache.ContainsKey(blocksetID))
                {
                    _blocksetCache.Add(blocksetID, new HashSet<Block>());
                }
                _blocksetCache[blocksetID].Add(_blocksCache[blockID]);
            }
        }

        public List<File> GetFilesByIds3(IEnumerable<long> fileIds)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    F.ID, F.BlocksetID, Path, pp.Prefix, MetadataID
                FROM 
                    FileLookup F
                LEFT JOIN
	                PathPrefix pp ON pp.ID = F.PrefixID 
                WHERE 
                    F.ID IN ({fileIds})";
            cmd.AddArrayParameters("fileIds", fileIds);

            var files = new List<File>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                files.Add(new File { Id = reader.GetInt64(0), BlocksetId = reader.GetInt32(1), Path = reader.GetString(2), Prefix = reader.GetString(3), MetadataId = reader.GetInt64(4) });
            }
            return files;
        }

        public List<Tuple<int, int, string, string>> GetFilesByPath(string prefix, string filename)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    F.ID, F.BlocksetID, Path, pp.Prefix 
                FROM 
                    FileLookup F
                LEFT JOIN
	                PathPrefix pp ON pp.ID = F.PrefixID 
                WHERE 
                    pp.Prefix = @prefix AND F.Path = @path";
            cmd.Parameters.AddWithValue("@prefix", prefix);
            cmd.Parameters.AddWithValue("@path", filename);

            var result = new List<Tuple<int, int, string, string>>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(Tuple.Create(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3)));
            }
            return result;
        }

        async public Task<Block> GetBlock(BlockID blockId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
            SELECT
                ID, Size, VolumeID
            FROM 
                Block
            WHERE 
                ID = @blockid";
            cmd.Parameters.AddWithValue("@blockid", blockId);

            var result = new List<Tuple<SizeBytes, VolumeID>>();
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
            var result = new List<Block>();

            int pos = 0;
            int batchSize = 100;
            while (true)
            {
                using var cmd = _conn.CreateCommand();
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
                while (reader.Read())
                {
                    result.Add(new Block { Id = reader.GetInt64(0), Size = reader.GetInt64(1), VolumeId = reader.GetInt64(2) });
                }

                if (cmd.Parameters.Count != batchSize)
                    break;
            }

            return result;
        }

        public SizeBytes GetBlocksSize(IEnumerable<BlockID> blockIds)
        {
            var blockIdsParameter = string.Join(", ", blockIds);

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $@"
            SELECT
                SUM(Size)
            FROM 
                Block
            WHERE 
                ID IN ({blockIdsParameter})";

            using var reader = cmd.ExecuteReader();

            SizeBytes size = 0;
            while (reader.Read())
            {
                size = reader.GetInt64(0);
            }
            return size;
        }

        async public Task<List<Block>> GetBlocksForOperation(OperationID operationId)
        {
            var fileset = GetFilesetForOperation(operationId);
            var filesetEntries = GetFilesetEntriesById(fileset.Id);

            var allBlockIds = new List<BlockID>();
            foreach (var entry in filesetEntries)
            {
                var file = GetFileById(entry.FileId);
                var blockIds = GetBlockIdsByBlocksetId(file.BlocksetId);
                allBlockIds.AddRange(blockIds);
            }

            var blocks = await GetBlocks(allBlockIds);
            return blocks;
        }


        async public Task<SizeBytes> GetSizeOfBackup(OperationID operationID)
        {
            var blocks = await GetBlocksForOperation(operationID);
            return GetBlocksSize(blocks.Select(x => x.Id));
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
