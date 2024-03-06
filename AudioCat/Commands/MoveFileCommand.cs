using System.Collections.ObjectModel;

namespace AudioCat.Commands;

internal interface IAudioFilesProvider
{
    ObservableCollection<AudioFile> Files { get; } 
    AudioFile SelectedFile { get; set; }
}

internal class MoveFileCommand(IAudioFilesProvider audioFilesProvider) : CommandBase
{
    private IAudioFilesProvider AudioFilesProvider { get; } = audioFilesProvider;

    protected override Task Command(object? parameter)
    {
        var selectedFile = AudioFilesProvider.SelectedFile;
        if (selectedFile.Path == "") 
            return Task.CompletedTask;
        if (parameter is not string action)
            return Task.CompletedTask;
        switch (action)
        {
            case "Up":
                MoveUp(selectedFile);
                break;
            case "Down":
                MoveDown(selectedFile);
                break;
            case "Remove":
                Remove(selectedFile);
                break;
        }

        return Task.CompletedTask;
    }

    private void MoveUp(AudioFile file)
    {
        var fileIndex = IndexOf(file);
        if (fileIndex <= 0) 
            return;
        var newFileIndex = fileIndex - 1;
        var files = AudioFilesProvider.Files;
        files.Move(fileIndex, newFileIndex);
        AudioFilesProvider.SelectedFile = files[newFileIndex];
    }

    private void MoveDown(AudioFile file)
    {
        var fileIndex = IndexOf(file);
        var files = AudioFilesProvider.Files;
        if (fileIndex < 0 || fileIndex >= files.Count - 1) 
            return;
        var newFileIndex = fileIndex + 1;
        files.Move(fileIndex, newFileIndex);
        AudioFilesProvider.SelectedFile = files[newFileIndex];
    }

    private void Remove(AudioFile file)
    {
        var fileIndex = IndexOf(file);
        if (fileIndex == -1)
            return;
        var files = AudioFilesProvider.Files;
        files.RemoveAt(fileIndex);
        if (files.Count != 0)
            AudioFilesProvider.SelectedFile = files[fileIndex < files.Count ? fileIndex : files.Count - 1];
    }

    private int IndexOf(AudioFile file)
    {
        var audioFiles = AudioFilesProvider.Files;
        for (var i = 0; i < audioFiles.Count; i++)
        {
            var audioFile = audioFiles[i];
            if (audioFile == file)
                return i;
        }

        return -1;
    }
}