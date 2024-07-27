﻿using System;
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
    public class Comparer
    {
        private readonly DuplicatiDatabase _database;

        public Comparer(DuplicatiDatabase database) { _database = database; }

        public delegate void BlocksCompareFinished ();

        // Declare the event.
        public event BlocksCompareFinished OnBlocksCompareFinished;

        async public Task<HashSet<Block>> GetBlockIdsForFileset(Fileset fs)
        {
            List<FilesetEntry> fsEntries = _database.GetFilesetEntriesById(fs.Id);

            HashSet<long> allBlockIds = new HashSet<long>();

            var files = fsEntries.Select(x => _database.GetFileById(x.FileId)).ToList();
            var blockIds = files.SelectMany(x => _database.GetBlockIdsByBlocksetId(x.BlocksetId));
            //var files = _database.GetFilesByIds(fsEntries.Select(x => x.FileId));
            //var blocksetIds = fsEntries.Select(x => _database.GetFileById(x.FileId).BlocksetId);
            var blocks = new HashSet<Block>();
            foreach (var blockId in blockIds)
            {
                var a = await _database.GetBlock(blockId);
                blocks.Add(a);
            }
            //var f = blockIds.Select(async x => await _database.GetBlock(x)).ToList();
            return blocks;
        }

        async public Task<CompareResult> CompareFilesets(Fileset fs1, Fileset fs2)
        {
            var blocks1 = await GetBlockIdsForFileset(fs1);
            var blocks2  = await GetBlockIdsForFileset(fs2);

            return CalculateResults(blocks1, blocks2, blocks2.Sum(x => x.Size));
        }

        private CompareResult CalculateResults(HashSet<Block> leftBlocks, HashSet<Block> rightBlocks, long rightSize)
        {
            //var leftBlocksSet = new HashSet<Block>(leftBlocks);
            //var rightBlocksSet = new HashSet<Block>(rightBlocks);

            var leftSize = leftBlocks.Sum(x => x.Size);
            //rightSize = rightBlocks.Sum(x => x.Size);


            // Create a copy of set1 to preserve the original
            HashSet<Block> shared = new HashSet<Block>(leftBlocks);

            // Modify the copy to contain only elements also in set2
            shared.IntersectWith(rightBlocks);

            // The count of the intersection set is the number of common elements
            long sizeIntersect = shared.Sum(x => x.Size);
            long commonElementCount = shared.Count;

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
            var rightBlocks = new List<Block>();
            var blockIds = new HashSet<long>();
            foreach (var rightFs in rightF.GetFileNodes())
            {
                var f = _database.GetBlockIdsByBlocksetId(rightFs.BlocksetId.Value);
                blockIds.UnionWith(f);
            }

            rightBlocks.AddRange(await _database.GetBlocks(blockIds));

            var rightSet = new HashSet<Block>(rightBlocks);
            await CompareFiletreeWithBlocks(left, rightSet);
        }

        async public Task CompareFiletreeWithBlocks(FileTree left, HashSet<Block> rightBlocks)
        {
            var rightSizeSum = rightBlocks.Sum(x => x.Size);

            foreach (var node in left.GetFileNodes())
            {
                //var blockIds = this._database.GetBlockIdsByBlocksetId(node.BlocksetId.Value);
                //var blocks = await this._database.GetBlocks(blockIds);
                var blocks = _database.GetBlocksByBlocksetId(node.BlocksetId.Value);

                var result = CalculateResults(blocks, rightBlocks, rightSizeSum);
                node.CompareResult = result;

                OnBlocksCompareFinished?.Invoke();
            }
        }
        /*
        async public Task<CompareResult> CompareFilesetsUnique(Fileset leftFs, List<Fileset> rightFss)
        {
            var blocks1 = await GetBlockIdsForFileset(leftFs);


            var rightBlocks = new List<Block>();
            foreach (var rightFs in rightFss)
            {
                rightBlocks.AddRange(await GetBlockIdsForFileset(rightFs));
            }

            return CalculateResults(blocks1, rightBlocks);
        }*/
    }
}
