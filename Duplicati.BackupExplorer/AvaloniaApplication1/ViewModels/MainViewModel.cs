using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ReactiveUI;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reactive;

namespace AvaloniaApplication1.ViewModels;

public class MainViewModel : ViewModelBase, INotifyPropertyChanged
{

    public MainViewModel() {
        Backups = new List<string>();

        if (Design.IsDesignMode)
        {
            Backups = new List<string> { "Backup 02-05-2014", "Backup 06-07-2021" };
        }

        OpenCommand = ReactiveCommand.Create(RunOpenCommand);
    }

    public ReactiveCommand<Unit, Unit> OpenCommand { get; }

    public string Greeting => "Welcome to Avalonia!";

    public List<string> Backups { get; set; }

    public void RunOpenCommand()
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        //var topLevel = TopLevel.GetTopLevel(this);
        /*
        // Start async operation to open the dialog.
        var files = await wnd.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            AllowMultiple = false
        });

        if (files.Count >= 1)
        {
            // Open reading stream from the first file.
            await using var stream = await files[0].OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            // Reads all the content of file as a text.
            var fileContent = await streamReader.ReadToEndAsync();
        }*/
    }
}
