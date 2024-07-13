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

            var filePaths = new string[] { @"C:\Windows", @"C:\Temp\MyFile.cs", @"C:\", @"C:\Temp" };

            foreach (var f in filePaths) {
                ft.AddPath(f, 1);
            }

            Assert.IsTrue(ft.Root.Name == @"C:\");
        }
    }
}