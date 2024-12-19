namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Database
{
    using Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model;
    using Duplicati.BackupExplorer.LocalDatabaseAccess.Model;
    using Microsoft.Data.Sqlite;
    using Microsoft.VisualBasic;
    using SQLitePCL;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using BlockID = long;
    using BlocksetID = long;
    using File = Model.File;

    public class DuplicatiDatabase : IDisposable
    {
        private SqliteConnection? _conn;
        private readonly Dictionary<BlocksetID, List<BlockID>> _blocklistIdCache = [];
        private bool _disposed = false;
        public const int MAX_CACHE_SIZE = 1000;
        private Dictionary<BlocksetID, HashSet<Block>> _blocksetCache = [];
        private readonly HashSet<int> _supportedDatabaseVersions = [12, 13];
        private readonly HashSet<int> _unsupportedDatabaseVersions = [];

        public DuplicatiDatabase()
        {
        }

        /// <summary>
        /// Check if the database version is explicitly unsupported. Fail if it is.
        /// </summary>
        /// <param name="dbVersion">Database version to check</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void CheckUnsupportedDatabaseVersion()
        {
            var dbVersion = GetVersion();
            if (_unsupportedDatabaseVersions.Contains(dbVersion))
            {
                throw new InvalidOperationException($"Database version {dbVersion} is not supported. Supported database versions: {_supportedDatabaseVersions}. Please use a different version of Duplicati or open a GitHub issue for a request.");
            }
        }

        /// <summary>
        /// Check if the database version is explicitly supported. 
        /// </summary>
        /// <param name="dbVersion">Database version to check</param>
        /// <returns></returns>
        public bool CheckSupportedDatabaseVersion()
        {
            return _supportedDatabaseVersions.Contains(GetVersion());
        }


        public long WastedSpaceSum()
        {
            CheckConnectionNotNull();

            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"SELECT SUM(Size) AS InactiveSize FROM DeletedBlock";

            using var reader = cmd.ExecuteReader();
            reader.Read();
            if (reader.IsDBNull(0))
                return 0;

            return reader.GetInt64(0);
        }

        /*
        private struct VolumeUsage
        {
            public readonly string Name;
            public readonly long DataSize;
            public readonly long WastedSize;
            public readonly long CompressedSize;

            public VolumeUsage(string name, long datasize, long wastedsize, long compressedsize)
            {
                this.Name = name;
                this.DataSize = datasize;
                this.WastedSize = wastedsize;
                this.CompressedSize = compressedsize;
            }
        }

        private IEnumerable<VolumeUsage> GetWastedSpaceReport(System.Data.IDbTransaction transaction)
        {
            var a = Guid.NewGuid().ToString();
            var tmptablename = "UsageReport-" + a;

            var usedBlocks = @"SELECT SUM(Block.Size) AS ActiveSize, Block.VolumeID AS VolumeID FROM Block, Remotevolume
                                WHERE Block.VolumeID = Remotevolume.ID AND Block.ID NOT IN 
                                    (SELECT Block.ID FROM Block,DeletedBlock WHERE Block.Hash = DeletedBlock.Hash AND Block.Size = DeletedBlock.Size AND Block.VolumeID = DeletedBlock.VolumeID)
                                GROUP BY Block.VolumeID ";
            var lastmodifiedFile = @"SELECT Block.VolumeID AS VolumeID, Fileset.Timestamp AS Sorttime FROM Fileset, FilesetEntry, FileLookup, BlocksetEntry, Block WHERE FilesetEntry.FileID = FileLookup.ID AND FileLookup.BlocksetID = BlocksetEntry.BlocksetID AND BlocksetEntry.BlockID = Block.ID AND Fileset.ID = FilesetEntry.FilesetID ";
            var lastmodifiedMetadata = @"SELECT Block.VolumeID AS VolumeID, Fileset.Timestamp AS Sorttime FROM Fileset, FilesetEntry, FileLookup, BlocksetEntry, Block, Metadataset WHERE FilesetEntry.FileID = FileLookup.ID AND FileLookup.MetadataID = Metadataset.ID AND Metadataset.BlocksetID = BlocksetEntry.BlocksetID AND BlocksetEntry.BlockID = Block.ID AND Fileset.ID = FilesetEntry.FilesetID ";
            var scantime = @"SELECT VolumeID AS VolumeID, MIN(Sorttime) AS Sorttime FROM (" + lastmodifiedFile + @" UNION " + lastmodifiedMetadata + @") GROUP BY VolumeID ";
            var active = @"SELECT A.ActiveSize AS ActiveSize,  0 AS InactiveSize, A.VolumeID AS VolumeID, CASE WHEN B.Sorttime IS NULL THEN 0 ELSE B.Sorttime END AS Sorttime FROM (" + usedBlocks + @") A LEFT OUTER JOIN (" + scantime + @") B ON B.VolumeID = A.VolumeID ";

            var inactive = @"SELECT 0 AS ActiveSize, SUM(Size) AS InactiveSize, VolumeID AS VolumeID, 0 AS SortScantime FROM DeletedBlock GROUP BY VolumeID ";
            var empty = @"SELECT 0 AS ActiveSize, 0 AS InactiveSize, Remotevolume.ID AS VolumeID, 0 AS SortScantime FROM Remotevolume WHERE Remotevolume.Type = ? AND Remotevolume.State IN (?, ?) AND Remotevolume.ID NOT IN (SELECT VolumeID FROM Block) ";

            var combined = active + " UNION " + inactive + " UNION " + empty;
            var collected = @"SELECT VolumeID AS VolumeID, SUM(ActiveSize) AS ActiveSize, SUM(InactiveSize) AS InactiveSize, MAX(Sorttime) AS Sorttime FROM (" + combined + @") GROUP BY VolumeID ";
            var createtable = @"CREATE TEMPORARY TABLE " + tmptablename + @" AS " + collected;

            using (var cmd = _conn.CreateCommand())
            {
                try
                {
                    cmd.ExecuteNonQuery(createtable, RemoteVolumeType.Blocks.ToString(), RemoteVolumeState.Uploaded.ToString(), RemoteVolumeState.Verified.ToString());
                    using (var rd = cmd.ExecuteReader(string.Format(@"SELECT A.Name, B.ActiveSize, B.InactiveSize, A.Size FROM Remotevolume A, {0} B WHERE A.ID = B.VolumeID ORDER BY B.Sorttime ASC ", tmptablename)))
                        while (rd.Read())
                            yield return new VolumeUsage(rd.GetValue(0).ToString(),
                                                         rd.ConvertValueToInt64(1, 0) + rd.ConvertValueToInt64(2, 0),
                                                         rd.ConvertValueToInt64(2, 0),
                                                         rd.ConvertValueToInt64(3, 0),
                                                         );
                }
                finally
                {
                    try { cmd.ExecuteNonQuery(string.Format(@"DROP TABLE IF EXISTS {0} ", tmptablename)); }
                    catch { }
                }
            }
        }
        */

        public void Open(string filepath)
        {
            _conn = OpenInMemory(filepath);
        }

        public void InitCaches()
        {
            InitBlocksetCache();
        }

        private static SqliteConnection OpenInMemory(string filePath)
        {
            // Open the file-based database connection
            SqliteConnectionStringBuilder fileConnectionBuilder = new SqliteConnectionStringBuilder();
            fileConnectionBuilder["Data Source"] = filePath;
            fileConnectionBuilder["Mode"] = "ReadOnly";

            using var fileConnection = new SqliteConnection(fileConnectionBuilder.ConnectionString);
            fileConnection.Open();

            // Create an in-memory database connection
            SqliteConnectionStringBuilder memoryConnectionBuilder = new SqliteConnectionStringBuilder();
            memoryConnectionBuilder["Data Source"] = ":memory:";

            var memoryConnection = new SqliteConnection(memoryConnectionBuilder.ConnectionString);
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

        public List<File> GetFilesInFileset(long filesetId)
        {
            CheckConnectionNotNull();

            // Too big for cache, query from database
            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    F.ID, F.BlocksetID, Path, pp.Prefix, MetadataID
                FROM 
                    FileLookup F
                LEFT JOIN
	                PathPrefix pp ON pp.ID = F.PrefixID
                WHERE
	                F.ID IN (SELECT FileID FROM FilesetEntry WHERE FilesetID = @filesetId);";
            cmd.Parameters.AddWithValue("@filesetId", filesetId);

            var files = new List<File>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                files.Add(new File { Id = reader.GetInt64(0), BlocksetId = reader.GetInt32(1), Path = reader.GetString(2), Prefix = reader.GetString(3), MetadataId = reader.GetInt64(4) });
            }
            files.Sort((x, y) => (x.Prefix + x.Path).CompareTo(y.Prefix + y.Path));
            return files;
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

        public void InitBlocksetCache()
        {
            CheckConnectionNotNull();

            _blocksetCache = GetBlocksets().Take(MAX_CACHE_SIZE).ToDictionary(x => x.Id, y => new HashSet<Block>());

            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"
            SELECT
                BlocksetID, BlockID
            FROM 
                BlocksetEntry
            ";
            using var cmd2 = _conn!.CreateCommand();
            cmd2.CommandText = @"
                SELECT
                    ID, Size, VolumeID
                FROM 
                    Block
                WHERE ID=@blockId";

            using var reader = cmd.ExecuteReader();
            var blockIdParam = cmd2.Parameters.Add("@blockId", SqliteType.Integer);
            while (reader.Read())
            {
                var blocksetID = reader.GetInt64(0);
                var blockID = reader.GetInt64(1);

                if (!_blocksetCache.TryGetValue(blocksetID, out HashSet<Block>? hset))
                {
                    // Did not fit in cache
                    continue;
                }

                blockIdParam.Value = blockID;

                using var reader2 = cmd2.ExecuteReader();
                if (reader2.Read())
                {
                    hset.Add(new Block { Id = reader2.GetInt64(0), Size = reader2.GetInt64(1), VolumeId = reader2.GetInt64(2) });
                    if (hset.Count > MAX_CACHE_SIZE)
                    {
                        _blocksetCache.Remove(blocksetID);
                    }
                }
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

        public IEnumerable<Blockset> GetBlocksets()
        {
            CheckConnectionNotNull();

            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"
            SELECT
                ID, Length, FullHash
            FROM 
                Blockset
            ";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new Blockset { Id = reader.GetInt64(0), Length = reader.GetInt64(1), FullHash = reader.GetString(2) };
            }
        }

        public HashSet<Block> GetBlocksByBlocksetId(BlocksetID blocksetId)
        {
            HashSet<Block>? blocks;
            if (_blocksetCache.TryGetValue(blocksetId, out blocks))
            {
                return blocks;
            }
            else
            {
                CheckConnectionNotNull();
                blocks = new HashSet<Block>();

                using var cmd = _conn!.CreateCommand();
                cmd.CommandText = @"
                SELECT
                    B.ID, B.Size, B.VolumeID
                FROM 
                    Block B
                JOIN
                    BlocksetEntry S
                ON
                    S.BlockID = B.ID
                WHERE
                    S.BlocksetID=@blocksetid;
                ";
                cmd.Parameters.AddWithValue("@blocksetid", blocksetId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    blocks.Add(new Block { Id = reader.GetInt64(0), Size = reader.GetInt64(1), VolumeId = reader.GetInt64(2) });
                }
                return blocks;
            }
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

        public long GetFilesetSize(long filesetId)
        {
            CheckConnectionNotNull();
            using var cmd = _conn!.CreateCommand();
            // Get the sum of all recorded file sizes in the fileset
            cmd.CommandText = @"
            SELECT
                SUM(S.Length)
            FROM
                Blockset S
            JOIN
                FileLookup L
            ON
                L.BlocksetID = S.ID
            WHERE
                L.ID
            IN (SELECT FileID FROM FilesetEntry WHERE FilesetID = @filesetId)";
            cmd.Parameters.AddWithValue("@filesetId", filesetId);
            return (long)(cmd.ExecuteScalar() ?? 0);
        }

        public long GetTotalSize()
        {

            CheckConnectionNotNull();
            using var cmd = _conn!.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    SUM(Size)
                FROM 
                    Block";
            return (long)(cmd.ExecuteScalar() ?? 0);
        }
    }

}
