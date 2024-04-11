using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AudioCat.Commands;
using AudioCat.Models;
using AudioCat.Windows;

namespace AudioCat.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    #region Backing Fields
    private bool _isUserEntryEnabled;
    private long _totalSize;
    private TimeSpan _totalDuration;
    private int _progressPercentage;
    private string _progressText = "";
    private bool _isTagsExpanded;
    private bool _isStreamsExpanded;
    private bool _isChaptersExpanded;

    #endregion

    private IAudioFileService AudioFileService { get; }
    private IAudioFilesContainer AudioFilesContainer { get; }
    public ObservableCollection<AudioFileViewModel> Files { get; } // AudioFilesContainer.Files
    public AudioFileViewModel? SelectedFile
    {
        get => AudioFilesContainer.SelectedFile;
        set => AudioFilesContainer.SelectedFile = value;
    }
    public Action? FocusFileDataGrid { get; set; }

    private void UpdateExpanders()
    {
        if (SelectedFile != null)
        {
            IsTagsExpanded = SelectedFile.Tags.Count > 0;
            IsStreamsExpanded = SelectedFile.Streams.Count > 0;
            IsChaptersExpanded = SelectedFile.Chapters.Count > 0;
        }
        else
            IsTagsExpanded = IsStreamsExpanded = IsChaptersExpanded = false;
    }

    public long TotalSize
    {
        get => _totalSize;
        set
        {
            if (value == _totalSize)
                return;
            _totalSize = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalSizeText));
        }
    }

    public TimeSpan TotalDuration
    {
        get => _totalDuration;
        set
        {
            if (value.Equals(_totalDuration))
                return;
            _totalDuration = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DurationText));
        }
    }

    public string TotalSizeText => TotalSize.GetBytesCountToText();
    public string DurationText => $"{Math.Truncate(TotalDuration.TotalHours):00}:{TotalDuration.Minutes:00}:{TotalDuration.Seconds:00}";

    public bool IsUserEntryEnabled
    {
        get => _isUserEntryEnabled;
        set
        {
            if (value == _isUserEntryEnabled)
                return;
            _isUserEntryEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsConcatenateEnabled));
            OnPropertyChanged(nameof(IsCancelEnabled));
            OnPropertyChanged(nameof(IsAddPathEnabled));
            OnPropertyChanged(nameof(IsAddFilesEnabled));
            OnPropertyChanged(nameof(IsClearPathsEnabled));
            OnPropertyChanged(nameof(IsMoveUpEnabled));
            OnPropertyChanged(nameof(IsMoveDownEnabled));
            OnPropertyChanged(nameof(IsRemoveEnabled));
        }
    }
    public bool IsConcatenateEnabled => IsUserEntryEnabled && Files.Count > 0;
    public bool IsCancelEnabled => !IsUserEntryEnabled;
    public bool IsAddPathEnabled => IsUserEntryEnabled;
    public bool IsAddFilesEnabled => IsUserEntryEnabled;
    public bool IsClearPathsEnabled => IsUserEntryEnabled && Files.Count > 0;
    public bool IsMoveUpEnabled => IsUserEntryEnabled && Files.Count > 0 && SelectedFile != null && SelectedFile.FileName != "" && SelectedFile != Files.First();
    public bool IsMoveDownEnabled => IsUserEntryEnabled && Files.Count > 0 && SelectedFile != null && SelectedFile.FileName != "" && SelectedFile != Files.Last();
    public bool IsRemoveEnabled => IsUserEntryEnabled && Files.Count > 0 && SelectedFile != null && SelectedFile.FileName != "";

    public bool IsTagsExpanded
    {
        get => _isTagsExpanded;
        set
        {
            if (value == _isTagsExpanded) 
                return;
            _isTagsExpanded = value;
            OnPropertyChanged();
        }
    }
    public bool IsStreamsExpanded
    {
        get => _isStreamsExpanded;
        set
        {
            if (value == _isStreamsExpanded) 
                return;
            _isStreamsExpanded = value;
            OnPropertyChanged();
        }
    }
    public bool IsChaptersExpanded
    {
        get => _isChaptersExpanded;
        set
        {
            if (value == _isChaptersExpanded)
                return;
            _isChaptersExpanded = value;
            OnPropertyChanged();
        }
    }

    public ICommand Concatenate { get; }
    public ICommand Cancel { get; }
    public ICommand AddPath { get; }
    public ICommand AddFiles { get; }
    public ICommand ClearPaths { get; }
    public ICommand MoveSelected { get; }
    public ICommand SelectTags { get; }
    public ICommand SelectCover { get; }

    public int ProgressPercentage
    {
        get => _progressPercentage;
        set
        {
            if (value == _progressPercentage)
                return;
            _progressPercentage = value;
            OnPropertyChanged();
        }
    }
    public string ProgressText
    {
        get => _progressText;
        set
        {
            if (value == _progressText)
                return;
            _progressText = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel(
        IAudioFileService audioFileService,
        IAudioFilesContainer audioFilesContainer,
        AddFilesCommand addFilesCommand,
        AddPathCommand addPathCommand,
        MoveFileCommand moveFileCommand,
        ConcatenateCommand concatenate)
    {
        AudioFileService  = audioFileService;
        
        AudioFilesContainer = audioFilesContainer;
        if (audioFilesContainer is INotifyPropertyChanged container)
            container.PropertyChanged += OnSelectedAudioFileChanged;

        Files = audioFilesContainer.Files;
        AddFiles = addFilesCommand;
        AddPath = addPathCommand;
        MoveSelected = moveFileCommand;

        concatenate.Starting += OnStarting;
        concatenate.Finished += OnFinished;
        concatenate.StatusUpdate += OnStatusUpdate;
        Concatenate = concatenate;

        ClearPaths = new RelayCommand(Files.Clear);
        Cancel = new RelayCommand(concatenate.Cancel);
        SelectTags = new RelayParameterCommand(OnSelectTags);
        SelectCover = new RelayParameterCommand(OnSelectCover);

        Files.CollectionChanged += OnFilesCollectionChanged;

        _ = VerifyAudioFileServiceIsAccessible();
    }

    private void OnSelectedAudioFileChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(SelectedFile));
        OnPropertyChanged(nameof(IsMoveUpEnabled));
        OnPropertyChanged(nameof(IsMoveDownEnabled));
        OnPropertyChanged(nameof(IsRemoveEnabled));
        UpdateExpanders();
        FocusFileDataGrid?.Invoke();
    }

    private async Task VerifyAudioFileServiceIsAccessible()
    {
        var result = await AudioFileService.IsAccessible();
        if (result.IsSuccess)
            IsUserEntryEnabled = true;
        else
            MessageBox.Show(result.Message + Environment.NewLine + "The tools 'ffmpeg.exe' and 'ffprobe.exe' are required for the application to work properly. Download the tools and place them in the system path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void OnSelectTags(object? obj)
    {
        if (obj is not AudioFileViewModel selectedFile)
            return;

        if (selectedFile.IsTagsSource)
        {
            selectedFile.IsTagsSource = false;
            return;
        }

        if (!selectedFile.HasTags)
            return;

        foreach (var currentFile in Files) 
            currentFile.IsTagsSource = currentFile == selectedFile;
    }

    private void OnSelectCover(object? obj)
    {
        if (obj is not AudioFileViewModel selectedFile)
            return;

        if (selectedFile.IsCoverSource)
        {
            selectedFile.IsCoverSource = false;
            return;
        }

        if (!selectedFile.HasCover)
            return;

        selectedFile.IsCoverSource = !selectedFile.IsCoverSource;
    }

    private void OnStarting(object? sender, EventArgs e)
    {
        ProgressPercentage = 10000;
        IsUserEntryEnabled = false;
    }

    private void OnFinished(object sender, ResponseEventArgs eventArgs)
    {
        try
        {
            if (eventArgs.Response.IsFailure)
                new ConcatErrorWindow(eventArgs.Response.Message, eventArgs.Response.Data as string ?? "").ShowDialog();
        }
        finally
        {
            ProgressPercentage = 0;
            ProgressText = "Done.";
            IsUserEntryEnabled = true;
        }
    }

    private void OnStatusUpdate(object sender, StatusEventArgs eventArgs)
    {
        var stats = eventArgs.Stats;
        const string prefix = "Processing: ";
        var sb = new StringBuilder(prefix);

        if (stats.Size is > 0)
            sb.Append($"Size: {stats.Size.Value / 1024:N0}KiB");
        if (stats.Time != default)
        {
            if (sb.Length > prefix.Length)
                sb.Append("; ");
            sb.Append($"Time: {Math.Truncate(stats.Time.TotalHours):00}:{stats.Time.Minutes:00}:{stats.Time.Seconds:00}");
        }
        if (stats.Bitrate is > 0)
        {
            if (sb.Length > prefix.Length)
                sb.Append("; ");
            sb.Append($"Bitrate: {stats.Bitrate:0.0}Kb/s");
        }
        if (stats.Speed is > 0)
        {
            if (sb.Length > prefix.Length)
                sb.Append("; ");
            sb.Append($"Speed: {stats.Speed:N0}x");
        }

        if (sb.Length == prefix.Length)
            sb.Append("...");

        ProgressText = sb.ToString();
        if (stats.Size.HasValue)
            ProgressPercentage = GetProgressPercentage((long)stats.Size.Value);
    }

    private void OnFilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsConcatenateEnabled));
        OnPropertyChanged(nameof(IsClearPathsEnabled));
        OnPropertyChanged(nameof(IsMoveUpEnabled));
        OnPropertyChanged(nameof(IsMoveDownEnabled));
        OnPropertyChanged(nameof(IsRemoveEnabled));
        TotalSize = GetFilesTotalSize();
        TotalDuration = GetTotalDuration();
    }

    private long GetFilesTotalSize()
    {
        var totalSize = 0L;
        foreach (var file in Files)
            totalSize += file.File.Length;
        return totalSize;
    }

    private TimeSpan GetTotalDuration()
    {
        var totalDuration = TimeSpan.Zero;
        foreach (var file in Files)
        {
            if (file.Duration.HasValue)
                totalDuration = totalDuration.Add(file.Duration.Value);
        }
        return totalDuration;
    }

    private const int PROGRESS_BAR_MAX_VALUE = 10000;
    private int GetProgressPercentage(long processedSize)
    {
        return TotalSize != 0
            ? (int)((decimal)processedSize * PROGRESS_BAR_MAX_VALUE / TotalSize)
            : PROGRESS_BAR_MAX_VALUE;
    }


    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    #endregion
}