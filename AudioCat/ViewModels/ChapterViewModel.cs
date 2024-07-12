using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AudioCat.Models;

namespace AudioCat.ViewModels;

public interface IMediaChapterViewModel : IMediaChapter
{
    new int Id { get; set; }
    new long? Start { get; set; }
    new long? End { get; set; }
    new decimal? TimeBaseDivident { get; set; }
    new decimal? TimeBaseDivisor { get; set; }
    new TimeSpan? StartTime { get; set; }
    new TimeSpan? EndTime { get; set; }
    string Title { get; set; }
}

[DebuggerDisplay("StartTime: {StartTime,nq}; EndTime: {EndTime,nq}; Title: {Title}")]
internal sealed class ChapterViewModel : IMediaChapterViewModel, INotifyPropertyChanged
{
    private const string TITLE_TAG_NAME = "title";

    #region Backing Fields
    private int _id;
    private long? _start;
    private long? _end;
    private decimal? _timeBaseDivident;
    private decimal? _timeBaseDivisor;
    private TimeSpan? _startTime;
    private TimeSpan? _endTime;
    private List<IMediaTag> _tags = [];

    #endregion

    public int Id
    {
        get => _id;
        set
        {
            if (value == _id) 
                return;
            _id = value;
            OnPropertyChanged();
        }
    }
    public long? Start
    {
        get => _start;
        set
        {
            if (value == _start) 
                return;
            _start = value;
            OnPropertyChanged();
        }
    }
    public long? End
    {
        get => _end;
        set
        {
            if (value == _end) 
                return;
            _end = value;
            OnPropertyChanged();
        }
    }
    public decimal? TimeBaseDivident
    {
        get => _timeBaseDivident;
        set
        {
            if (value == _timeBaseDivident) 
                return;
            _timeBaseDivident = value;
            OnPropertyChanged();
        }
    }
    public decimal? TimeBaseDivisor
    {
        get => _timeBaseDivisor;
        set
        {
            if (value == _timeBaseDivisor) 
                return;
            _timeBaseDivisor = value;
            OnPropertyChanged();
        }
    }
    public TimeSpan? StartTime
    {
        get => _startTime;
        set
        {
            if (Nullable.Equals(value, _startTime)) 
                return;
            _startTime = value;
            OnPropertyChanged();
        }
    }
    public TimeSpan? EndTime
    {
        get => _endTime;
        set
        {
            if (Nullable.Equals(value, _endTime)) return;
            _endTime = value;
            OnPropertyChanged();
        }
    }
    public IReadOnlyList<IMediaTag> Tags => _tags;

    public string Title
    {
        get => Tags.GetTagValue(TITLE_TAG_NAME);
        set
        {
            var titleTagIndex = Tags.GetTagIndex(TITLE_TAG_NAME);
            if (titleTagIndex == -1) 
                _tags.Add(new MediaTag(TITLE_TAG_NAME, value));
            else if (value != Tags[titleTagIndex].Value) 
                _tags[titleTagIndex] = new MediaTag(TITLE_TAG_NAME, value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(Tags));
        }
    }

    public static IMediaChapterViewModel CreateFrom(IMediaChapter mediaChapter) =>
        new ChapterViewModel
        {
            _id = mediaChapter.Id,
            _start = mediaChapter.Start,
            _end = mediaChapter.End,
            _timeBaseDivident = mediaChapter.TimeBaseDivident,
            _timeBaseDivisor = mediaChapter.TimeBaseDivisor,
            _startTime = mediaChapter.StartTime,
            _endTime = mediaChapter.EndTime,
            _tags = [..mediaChapter.Tags]
        };

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}