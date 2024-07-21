using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Model
{
    public class FileNode
    {
        public string Name { get; set; }
        public FileNode? Parent { get; set; }
        public bool IsFile { get; set; }
        public long? BlocksetId { get; set; }
        public CompareResult? CompareResult { get; set; }
        public ObservableCollection<FileNode> Children { get; set; }
        public Dictionary<string, FileNode> ChildrenHashed { get; set; }

        public long Size { get; set; }

        public FileNode(string name)
        {
            Name = name;
            Children = new ObservableCollection<FileNode>();
            ChildrenHashed = new Dictionary<string, FileNode>();
        }

        public void AddChild(FileNode child)
        {
            Children.Add(child);
            ChildrenHashed[child.Name] = child;
        }

        public FileNode? GetChild(string name)
        {
            if (!ChildrenHashed.ContainsKey(name))
                return null;

            return ChildrenHashed[name];
        }

        public void MergeCompareResult(CompareResult compareResult)
        {
            foreach (FileNode child in Children) {
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

            foreach (var child in Children)
            {

                foreach (var item in child.GetChildrensRecursive(filesOnly))
                    yield return item;

            }
        }

        public void PrintTree(string indent = "")
        {
            Console.WriteLine($"{indent}{(IsFile ? "File: " : "Dir: ")}{Name}");
            foreach (var child in Children)
            {
                child.PrintTree(indent + "  ");
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
        public FileNode Root { get; set; }

        public FileTree()
        {
            Root = new FileNode("Root");
        }

        public IEnumerable<FileNode> GetFileNodes(bool filesOnly=true)
        {
            foreach (var item in Root.GetChildrensRecursive(filesOnly))
            {
                yield return item;
            }
        }

        public FileNode AddPath(string filePath, long blocksetId)
        {
            var parts = filePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var current = Root;

            for (int i = 0; i < parts.Length; i++)
            {
                var lastPart = i == parts.Length - 1;
                var part = parts[i];
                var child = current.GetChild(part);

                if (child == null)
                {
                    child = new FileNode(part);
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
            foreach(var item in Root.GetChildrensRecursive(false))
            {
                item.UpdateDirectoryCompareResult();
            }
        }
    }

}
