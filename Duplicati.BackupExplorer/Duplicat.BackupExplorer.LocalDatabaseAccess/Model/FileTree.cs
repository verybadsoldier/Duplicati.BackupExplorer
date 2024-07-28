using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Model
{
    public class FileNode
    {
        public string Name { get; set; }
        public FileNode? Parent { get; set; }
        public bool IsFile { get; set; } = false;
        public long? BlocksetId { get; set; }
        public CompareResult? CompareResult { get; set; }
        public OrderedDictionary Children { get; set; } = new OrderedDictionary();

        private long? _fileSize;

        public long NodeSize
        {
            get
            {
                if (IsFile)
                    return _fileSize.Value;
                else
                {
                    return Children.Values.OfType<FileNode>().Sum(x => x.NodeSize);
                }
            }
        }

        public FileNode(string name, long? fileSize)
        {
            Name = name;
            _fileSize = fileSize;
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
            foreach (FileNode child in Children.Values) {
                if (child.IsFile)
                {
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

        public IEnumerable<FileNode> GetChildrensRecursive(bool filesOnly=true)
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

        public string FullPath { get
            {
                string text = "";
                if (Parent != null && Parent.Parent != null)
                {
                    text = Parent.ToString() + Path.DirectorySeparatorChar;
                }
                return text + Name;
            }
        }

        public override string ToString()
        {
            return FullPath;
        }
    }

    public class FileTree
    {                                    
        public ObservableCollection<FileNode> Nodes { get; set; } = new ObservableCollection<FileNode>();
        
        public string? Name { get; set; }

        public FileTree()
        {
            Nodes.Add(new FileNode("Root", null));
        }

        override public string ToString()
        {
            if (Name != null)
                return Name;
            else
                return "<NoName>";
        }

        public IEnumerable<FileNode> GetFileNodes(bool filesOnly=true)
        {
            foreach (var item in Nodes[0].GetChildrensRecursive(filesOnly))
            {
                yield return item;
            }
        }

        public FileNode AddPath(string filePath, long blocksetId, long? size=null)
        {
            var parts = filePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var current = Nodes[0];

            for (int i = 0; i < parts.Length; i++)
            {
                var lastPart = i == parts.Length - 1;
                var part = parts[i];
                var child = current.GetChild(part);

                if (child == null)
                {
                    child = new FileNode(part, size.GetValueOrDefault());
                    child.Parent = current;

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
            foreach(var item in Nodes[0].GetChildrensRecursive(false))
            {
                item.UpdateDirectoryCompareResult();
            }
        }
    }

}
