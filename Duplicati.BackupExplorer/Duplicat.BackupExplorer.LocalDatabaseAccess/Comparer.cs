using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess
{
    public class Comparer
    {
        private readonly DuplicatiDatabase _database;

        public Comparer(DuplicatiDatabase database) { _database = database; }

        public IEnumerable<Block> GetBlockIdsForFileset(Fileset fs)
        {
            List<FilesetEntry> fsEntries = _database.GetFilesetEntriesById(fs.Id);

            HashSet<long> allBlockIds = new HashSet<long>();

            var files = fsEntries.Select(x => _database.GetFileById(x.FileId)).ToList();
            var blockIds = files.SelectMany(x => _database.GetBlockIdsByBlocksetId(x.BlocksetId));
            //var files = _database.GetFilesByIds(fsEntries.Select(x => x.FileId));
            //var blocksetIds = fsEntries.Select(x => _database.GetFileById(x.FileId).BlocksetId);
            return blockIds.Select(x => _database.GetBlock(x));
        }

        public CompareResult CompareFilesets(Fileset fs1, Fileset fs2)
        {
            var blocks1 = GetBlockIdsForFileset(fs1).ToList();
            var blocks2  = GetBlockIdsForFileset(fs2).ToList();

            var h1 = new HashSet<Block>(blocks1);
            var h2 = new HashSet<Block>(blocks2);

            var sizeRoot = h1.Sum(x => x.Size);
            var sizeCompare = h2.Sum(x => x.Size);

            var result = new CompareResult { numBlocksRoot = h1.Count, numBlocksCompare = h2.Count, sizeRoot = sizeRoot, sizeCompare=sizeCompare};

            // Create a copy of set1 to preserve the original
            HashSet<Block> intersection = new HashSet<Block>(h1);

            // Modify the copy to contain only elements also in set2
            intersection.IntersectWith(h2);

            // The count of the intersection set is the number of common elements
            result.sizeShare = intersection.Sum(x => x.Size);
            result.shareSizePercentage = result.sizeShare / (float)sizeRoot;
            long commonElementCount = intersection.Count;
            result.sharePercentage = commonElementCount / (float)result.numBlocksRoot;
            return result;
        }
    }
}
