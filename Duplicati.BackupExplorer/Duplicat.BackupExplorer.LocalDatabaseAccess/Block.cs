using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess
{
    public class Block : IEquatable<Block>
    {
        public long Id { get; set; }

        public long Size { get; set; }

        public long VolumeId { get; set; }

        public bool Equals(Block p)
        {
            return p != null && Id == p.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
