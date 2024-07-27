using Duplicati.BackupExplorer.LocalDatabaseAccess.Model;
using System.ComponentModel.DataAnnotations;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestFileTree()
        {
            var ft = new FileTree();

            ft.AddPath(@"C:\Windows", 1);
            ft.AddPath(@"C:\Temp\MyFile.cs", 1542351123);
            ft.AddPath(@"C:\Temp\MyFile2.cs", 3399293492);
            ft.AddPath(@"C:\", 1);
            ft.AddPath(@"C:\Temp", 1);
            ft.AddPath(@"C:\Windows", 1);

            var root = (FileNode)ft.Nodes[0];
            var cNode = (FileNode)root.Children[0];
            Assert.IsTrue(cNode.Name == @"C:");
        }
    }
}