using AudioCat.Models;

namespace AudioCat.Commands;

public class MoveFileCommand(IAudioFilesContainer audioFilesContainer) : CommandBase
{
    private IAudioFilesContainer AudioFilesContainer { get; } = audioFilesContainer;

    protected override Task<IResult> Command(object? parameter)
    {
        var selectedFile = AudioFilesContainer.SelectedFile;
        if (string.IsNullOrEmpty(selectedFile?.File.FullName))
            return Task.FromResult(Result.Success());
        if (parameter is not string action)
            return Task.FromResult(Result.Success());
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

        return Task.FromResult(Result.Success());
    }

    private void MoveUp(IAudioFile file)
    {
        var fileIndex = IndexOf(file);
        if (fileIndex <= 0) 
            return;
        var newFileIndex = fileIndex - 1;
        var files = AudioFilesContainer.Files;
        files.Move(fileIndex, newFileIndex);
        AudioFilesContainer.SelectedFile = files[newFileIndex];
    }

    private void MoveDown(IAudioFile file)
    {
        var fileIndex = IndexOf(file);
        var files = AudioFilesContainer.Files;
        if (fileIndex < 0 || fileIndex >= files.Count - 1) 
            return;
        var newFileIndex = fileIndex + 1;
        files.Move(fileIndex, newFileIndex);
        AudioFilesContainer.SelectedFile = files[newFileIndex];
    }

    private void Remove(IAudioFile file)
    {
        var fileIndex = IndexOf(file);
        if (fileIndex == -1)
            return;
        var files = AudioFilesContainer.Files;
        files.RemoveAt(fileIndex);
        if (files.Count != 0)
            AudioFilesContainer.SelectedFile = files[fileIndex < files.Count ? fileIndex : files.Count - 1];
    }

    private int IndexOf(IAudioFile file)
    {
        var audioFiles = AudioFilesContainer.Files;
        for (var i = 0; i < audioFiles.Count; i++)
        {
            var audioFile = audioFiles[i];
            if (audioFile == file)
                return i;
        }

        return -1;
    }
}