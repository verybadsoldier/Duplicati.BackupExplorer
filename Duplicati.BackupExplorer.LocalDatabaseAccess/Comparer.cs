using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Database;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Model;
using static System.Reflection.Metadata.BlobBuilder;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess
{
    public class Comparer(DuplicatiDatabase database)
    {
        private readonly DuplicatiDatabase _database = database;

        public delegate void BlocksCompareFinished ();

        public event BlocksCompareFinished? OnBlocksCompareFinished;

        async public Task<HashSet<Block>> GetBlockIdsForFileset(Fileset fs)
        {
            List<FilesetEntry> fsEntries = _database.GetFilesetEntriesById(fs.Id);

            var files = fsEntries.Select(x => _database.GetFileById(x.FileId)).ToList();
            var blockIds = files.SelectMany(x => _database.GetBlockIdsByBlocksetId(x.BlocksetId));
            var blocks = new HashSet<Block>();
            foreach (var blockId in blockIds)
            {
                var a = await _database.GetBlock(blockId);
                blocks.Add(a);
            }
            return blocks;
        }

        async public Task<CompareResult> CompareFilesets(Fileset fs1, Fileset fs2)
        {
            var blocks1 = await GetBlockIdsForFileset(fs1);
            var blocks2  = await GetBlockIdsForFileset(fs2);

            return CalculateResults(blocks1, blocks2, blocks2.Sum(x => x.Size));
        }

        private static CompareResult CalculateResults(HashSet<Block> leftBlocks, HashSet<Block> rightBlocks, long rightSize)
        {
            var leftSize = leftBlocks.Sum(x => x.Size);

            // Create a copy of set1 to preserve the original
            var shared = new HashSet<Block>(leftBlocks);

            // Modify the copy to contain only elements also in set2
            shared.IntersectWith(rightBlocks);

            // The count of the intersection set is the number of common elements
            long sizeIntersect = shared.Sum(x => x.Size);

            var result = new CompareResult { 
                LeftNumBlocks = leftBlocks.Count,
                RightNumBlocks = rightBlocks.Count,
                LeftSize = leftSize,
                RightSize = rightSize,

                SharedSize = sizeIntersect,
                SharedNumBlocks = shared.Count,
            };

            return result;
        }

        async public Task CompareFiletree(FileTree left, FileTree rightF)
        {
            await CompareFiletrees(left, [rightF]);
        }

        async public Task CompareFiletrees(FileTree left, IEnumerable<FileTree> rightFss)
        {
            var rightBlocks = new List<Block>();
            var blockIds = new HashSet<long>();
            foreach(var rightF in rightFss)
            {
                foreach (var rightFs in rightF.GetFileNodes())
                {
                    if (!rightFs.BlocksetId.HasValue)
                        throw new InvalidOperationException($"File {rightFs.FullPath} has no blockset ID");

                    var f = _database.GetBlockIdsByBlocksetId(rightFs.BlocksetId.Value);
                    blockIds.UnionWith(f);
                }
            }

            rightBlocks.AddRange(await _database.GetBlocks(blockIds));

            var rightSet = new HashSet<Block>(rightBlocks);
            await Task.Run(() => CompareFiletreeWithBlocks(left, rightSet));
        }

        public void CompareFiletreeWithBlocks(FileTree left, HashSet<Block> rightBlocks)
        {
            var rightSizeSum = rightBlocks.Sum(x => x.Size);

            foreach (var node in left.GetFileNodes())
            {
                if (!node.BlocksetId.HasValue)
                    throw new InvalidOperationException($"File {node.FullPath} has no blockset ID");

                var blocks = _database.GetBlocksByBlocksetId(node.BlocksetId.Value);

                var result = CalculateResults(blocks, rightBlocks, rightSizeSum);
                node.CompareResult = result;

                OnBlocksCompareFinished?.Invoke();
            }
        }
    }
}
