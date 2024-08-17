// See https://aka.ms/new-console-template for more information

using Duplicati.BackupExplorer.LocalDatabaseAccess;
using Duplicati.BackupExplorer.LocalDatabaseAccess.Database;

if (args.Length != 3) throw new ArgumentException(nameof(args));

string databasePath = args[0];
string searchDir= args[1];
string searchFilename = args[2];


var d = new DuplicatiDatabase();
d.Open(databasePath);

var backups = d.GetBackups();

foreach(var v in backups)
{
    Console.WriteLine(v);
}

void FindFileVersions()
{
    var files = d.GetFilesByPath(searchDir, searchFilename);
    foreach(var file in files)
    {
        var filesets = d.GetFilesetsByFileId(file.Item1);
        foreach(var fileset in filesets)
        {
            var operations = d.GetOperationsByFilesetID(fileset);
            foreach(var operation in operations)
            {
                var op = d.GetOperationById(operation.Item1);
                var datetime = DateTimeOffset.FromUnixTimeSeconds(op.Item2);
                Console.WriteLine(op + datetime.ToString());
            }
        }
    }
    
    Console.WriteLine(files);
}

FindFileVersions();


