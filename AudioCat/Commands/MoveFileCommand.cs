using AudioCat.Models;
using AudioCat.ViewModels;

namespace AudioCat.Commands;

public class MoveFileCommand(IMediaFilesContainer mediaFilesContainer) : CommandBase
{
    private IMediaFilesContainer MediaFilesContainer { get; } = mediaFilesContainer;

    protected override Task<IResponse<object>> Command(object? parameter)
    {
        var selectedFile = MediaFilesContainer.SelectedFile;
        if (string.IsNullOrEmpty(selectedFile?.File.FullName))
            return Task.FromResult(Response<object>.Success());
        if (parameter is not string action)
            return Task.FromResult(Response<object>.Success());
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

        return Task.FromResult(Response<object>.Success());
    }

    private void MoveUp(IMediaFileViewModel file)
    {
        var fileIndex = IndexOf(file);
        if (fileIndex <= 0) 
            return;
        var newFileIndex = fileIndex - 1;
        var files = MediaFilesContainer.Files;
        files.Move(fileIndex, newFileIndex);
        MediaFilesContainer.SelectedFile = files[newFileIndex];
    }

    private void MoveDown(IMediaFileViewModel file)
    {
        var fileIndex = IndexOf(file);
        var files = MediaFilesContainer.Files;
        if (fileIndex < 0 || fileIndex >= files.Count - 1) 
            return;
        var newFileIndex = fileIndex + 1;
        files.Move(fileIndex, newFileIndex);
        MediaFilesContainer.SelectedFile = files[newFileIndex];
    }

    private void Remove(IMediaFileViewModel file)
    {
        var fileIndex = IndexOf(file);
        if (fileIndex == -1)
            return;
        var files = MediaFilesContainer.Files;
        files.RemoveAt(fileIndex);
        if (files.Count != 0)
            MediaFilesContainer.SelectedFile = files[fileIndex < files.Count ? fileIndex : files.Count - 1];
    }

    private int IndexOf(IMediaFileViewModel file)
    {
        var mediaFiles = MediaFilesContainer.Files;
        for (var i = 0; i < mediaFiles.Count; i++)
        {
            var audioFile = mediaFiles[i];
            if (audioFile == file)
                return i;
        }

        return -1;
    }
}