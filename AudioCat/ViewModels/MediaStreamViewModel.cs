using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AudioCat.Models;

namespace AudioCat.ViewModels;

[DebuggerDisplay("{Index,nq}: Duration: {Duration,nq}; Codec: {CodecName,nq}; SampleRate: {SampleRate,nq};")]
public sealed class MediaStreamViewModel(IMediaStream mediaStream) : IMediaStream, INotifyPropertyChanged
{
    private bool _include = true;
    public IMediaStream MediaStream { get; } = mediaStream;

    public int Index => MediaStream.Index;
    public string? CodecName => MediaStream.CodecName;
    public string? CodecDescription => MediaStream.CodecDescription;
    public string? CodecType => MediaStream.CodecType;
    public string? CodecTag => MediaStream.CodecTag;
    public decimal? SampleRate => MediaStream.SampleRate;
    public decimal? Channels => MediaStream.Channels;
    public string? ChannelLayout => MediaStream.ChannelLayout;
    public decimal? StartTime => MediaStream.StartTime;
    public TimeSpan? Duration => MediaStream.Duration;
    public int? Width => MediaStream.Width;
    public int? Height => MediaStream.Height;
    public IReadOnlyList<IMediaTag> Tags => MediaStream.Tags;

    public bool Include
    {
        get => _include;
        set
        {
            if (value == _include) 
                return;
            _include = value;
            OnPropertyChanged();
        }
    }

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}