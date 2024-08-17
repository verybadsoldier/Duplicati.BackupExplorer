using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Database.Model
{
    public class Block
    {
        public long Id { get; set; }

        public long Size { get; set; }

        public long VolumeId { get; set; }


        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is Block block)
            {
                return Id == block.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
