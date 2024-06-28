using AudioCat.Services;

namespace AudioCat.Models;

public sealed class MessageEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}
public delegate void MessageEventHandler(object sender, MessageEventArgs eventArgs);

public sealed class ProgressEventArgs(Progress progress) : EventArgs
{
    public Progress Progress { get; } = progress;
}
public delegate void ProgressEventHandler(object sender, ProgressEventArgs eventArgs);
