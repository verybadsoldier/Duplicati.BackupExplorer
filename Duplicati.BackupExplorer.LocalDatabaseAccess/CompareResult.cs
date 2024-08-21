using Duplicati.BackupExplorer.LocalDatabaseAccess.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess
{
    public class CompareResult
    {
        #region Input State
        public long LeftNumBlocks { get; set; }
        public long LeftSize { get; set; }
        public float LeftSizeGb => BytesToGb(LeftSize);
        public long RightNumBlocks { get; set; }
        public long RightSize { get; set; }
        public float RightSizeGb => BytesToGb(RightSize);
        #endregion

        public long SharedSize {  get; set; }
        public float SharedSizeGb => BytesToGb(SharedSize);

        public long DisjunctSize => LeftSize - SharedSize;

        public float DisjunctSizeGb => BytesToGb(DisjunctSize);

        public long SharedNumBlocks { get; set; }
        public float SharedPercentageNumBlocks => SharedNumBlocks / (float)LeftNumBlocks;
        public float SharedPercentageSize => SharedSize / (float)LeftSize;

        public float DisjunctPercentageNumBlocks => LeftNumBlocks - SharedNumBlocks / (float)LeftNumBlocks;

        public float DisjunctPercentageSize => (LeftSize - SharedSize) / (float)LeftSize;

        static private float BytesToGb(long size)
        {
            return (float)size / 1024 / 1024 / 1024;
        }
    }
}
