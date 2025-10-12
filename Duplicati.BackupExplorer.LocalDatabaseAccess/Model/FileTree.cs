using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Xml.Linq;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Model
{
    public enum SortMode
    {
        Lexical,
        Size
    }

    public class FileNode(string name, long? fileSize)
    {
        private readonly long? _fileSize = fileSize;

        public string Name { get; set; } = name;
        public FileNode? Parent { get; set; }
        public bool IsFile { get; set; } = false;
        public long? BlocksetId { get; set; }
        public CompareResult? CompareResult { get; set; }
        public OrderedDictionary Children { get; set; } = [];

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => _isExpanded = value; // Assumes SetProperty from a ViewModelBase
        }

        public void ExpandLevels(int levels, int currentLevel=1)
        {
            if (IsFile)
                return;

            IsExpanded = true;

            if (levels == currentLevel) return;

            foreach (var node in Children.Values)
            {
                ((FileNode)node).ExpandLevels(levels, currentLevel + 1);
            }
        }

        public long NodeSize
        {
            get
            {
                if (IsFile)
                {
                    if (_fileSize == null)
                        throw new InvalidOperationException("_fileSize is null");

                    return _fileSize.Value;
                }
                else
                {
                    return Children.Values.OfType<FileNode>().Sum(x => x.NodeSize);
                }
            }
        }

        public void AddChild(FileNode child)
        {
            Children[child.Name] = child;
        }

        public FileNode? GetChild(string name)
        {
            if (!Children.Contains(name))
                return null;

            return Children[name] as FileNode;
        }

        public void MergeCompareResult(CompareResult compareResult)
        {
            foreach (FileNode child in Children.Values)
            {
                if (child.IsFile)
                {
                    if (child.CompareResult == null)
                    {
                        throw new InvalidOperationException("File has no CompareResult");
                    }
                    compareResult.LeftNumBlocks += child.CompareResult.LeftNumBlocks;
                    compareResult.LeftSize += child.CompareResult.LeftSize;
                    compareResult.SharedSize += child.CompareResult.SharedSize;
                    compareResult.SharedNumBlocks += child.CompareResult.SharedNumBlocks;
                }
                else
                {
                    child.MergeCompareResult(compareResult);
                }
            }
        }
        public void UpdateDirectoryCompareResult()
        {
            if (IsFile)
            {
                return;
            }

            CompareResult = new CompareResult();

            MergeCompareResult(CompareResult);
        }

        public IEnumerable<FileNode> GetChildrensRecursive(bool filesOnly = true)
        {
            if (!filesOnly || IsFile)
            {
                yield return this;
            }

            foreach (var child in Children.Values)
            {
                var filenode = (FileNode)child;
                foreach (var item in filenode.GetChildrensRecursive(filesOnly))
                    yield return item;
            }
        }

        public void PrintTree(string indent = "")
        {
            Console.WriteLine($"{indent}{(IsFile ? "File: " : "Dir: ")}{Name}");
            foreach (var child in Children.Values)
            {
                var filenode = (FileNode)child;
                filenode.PrintTree(indent + "  ");
            }
        }

        public string FullPath
        {
            get
            {
                string text = "";
                if (Parent != null && Parent.Parent != null)
                {
                    text = Parent.ToString() + Path.DirectorySeparatorChar;
                }
                return text + Name;
            }
        }

        public void SortChildren(SortMode sortMode)
        {
            if (Children == null || Children.Count == 0)
            {
                return;
            }

            // Extract children to a list for sorting
            var childrenToSort = Children.Values.OfType<FileNode>().ToList();

            // Sort the list based on the chosen mode
            if (sortMode == SortMode.Size)
            {
                childrenToSort.Sort((a, b) => b.NodeSize.CompareTo(a.NodeSize));
            }
            else // Lexical
            {
                childrenToSort.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            }

            // Clear the dictionary and repopulate it from the sorted list
            Children.Clear();
            foreach (var sortedChild in childrenToSort)
            {
                Children.Add(sortedChild.Name, sortedChild);
            }
        }

        public void SortChildrenRecursive(SortMode sortMode)
        {
            // First, sort the direct children of this node
            this.SortChildren(sortMode);

            // Then, recursively call this method on each child that is a directory
            foreach (FileNode child in this.Children.Values)
            {
                if (!child.IsFile)
                {
                    child.SortChildrenRecursive(sortMode);
                }
            }
        }

        public override string ToString()
        {
            return FullPath;
        }
    }

    public class FileTree
    {
        private static readonly char[] separator = ['\\', '/'];

        public FileTree(string rootName="/")
        {
            Nodes.Add(new FileNode(rootName, null));
        }

        public ObservableCollection<FileNode> Nodes { get; set; } = [];

        public string? Name { get; set; }

        public void ExpandLevels(int levels)
        {
            foreach (var node in Nodes)
            {
                node.IsExpanded = true;
                node.ExpandLevels(levels);
            }
        }

        public void Sort(SortMode mode)
        {
            foreach (var node in Nodes)
            {
                node.SortChildrenRecursive(mode);
            }
            
        }

        override public string ToString()
        {
            if (Name != null)
                return Name;
            else
                return "<NoName>";
        }

        public IEnumerable<FileNode> GetFileNodes(bool filesOnly = true)
        {
            foreach (var item in Nodes[0].GetChildrensRecursive(filesOnly))
            {
                yield return item;
            }
        }

        public FileNode AddPath(string filePath, long blocksetId, long? size = null)
        {
            var parts = filePath.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            var current = Nodes[0];

            for (int i = 0; i < parts.Length; i++)
            {
                var lastPart = i == parts.Length - 1;
                var part = parts[i];
                var child = current.GetChild(part);

                if (child == null)
                {
                    child = new FileNode(part, size.GetValueOrDefault())
                    {
                        Parent = current
                    };

                    current.AddChild(child);
                }


                current = child;

                // attach blocksetId to final element
                if (lastPart)
                {
                    if (!filePath.EndsWith('/') && !filePath.EndsWith('\\'))
                    {
                        child.IsFile = true;
                    }

                    child.BlocksetId = blocksetId;
                }
            }
            return current;
        }


        public void UpdateDirectoryCompareResults()
        {
            foreach (var item in Nodes[0].GetChildrensRecursive(false))
            {
                item.UpdateDirectoryCompareResult();
            }
        }
    }

}
