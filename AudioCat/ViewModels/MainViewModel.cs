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
using AudioCat.Services;
using AudioCat.Windows;

namespace AudioCat.ViewModels;

public sealed class MainViewModel : IConcatParams, INotifyPropertyChanged
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
    private bool _tagsEnabled = true;
    private bool _chaptersEnabled = true;
    private string _selectedCodec = "";

    #endregion

    private IMediaFileToolkitService MediaFileToolkitService { get; }
    private IMediaFilesContainer MediaFilesContainer { get; }
    private IMediaFilesService MediaFilesService { get; }

    public ObservableCollection<IMediaFileViewModel> Files { get; }
    public IMediaFileViewModel? SelectedFile
    {
        get => MediaFilesContainer.SelectedFile;
        set => MediaFilesContainer.SelectedFile = value;
    }
    public string SelectedCodec
    {
        get => _selectedCodec;
        set
        {
            if (value == _selectedCodec) return;
            _selectedCodec = value;
            OnPropertyChanged();
        }
    }

    public Action? FocusFileDataGrid { get; set; }

    private void UpdateExpanders()
    {
        if (SelectedFile != null)
        {
            IsTagsExpanded = SelectedFile.Tags.Count > 0;
            IsStreamsExpanded = SelectedFile.Streams.Count > 0;
            IsChaptersExpanded = ChaptersEnabled && SelectedFile.Chapters.Count > 0;
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
            OnPropertyChanged(nameof(IsChaptersFromTagsEnabled));
            OnPropertyChanged(nameof(IsChaptersFromFilesEnabled));
        }
    }
    public bool IsConcatenateEnabled => IsUserEntryEnabled && Files.Count > 0 && TotalDuration != TimeSpan.Zero;
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

    public string TagsCount =>
        SelectedFile != null 
            ? SelectedFile.Tags.Count > 0 
                ? SelectedFile.Tags.Count > 1 ? $"{SelectedFile.Tags.Count:N0} tags" : "1 tag"
                : "No tags"
            : "";
    public string StreamsCount =>
        SelectedFile != null
            ? SelectedFile.Streams.Count > 0 
                ? SelectedFile.Streams.Count > 1 ? $"{SelectedFile.Streams.Count:N0} streams" : "1 stream"
                : "No streams"
            : "";
    public string ChaptersCount =>
        SelectedFile != null
            ? SelectedFile.Chapters.Count > 0 
                ? SelectedFile.Chapters.Count > 1 ? $"{SelectedFile.Chapters.Count:N0} chapters" : "1 chapter"
                : "No chapters"
            : "";

    public bool TagsEnabled
    {
        get => _tagsEnabled;
        set
        {
            if (value == _tagsEnabled) 
                return;
            _tagsEnabled = value;
            OnPropertyChanged();
            IsTagsExpanded = value && SelectedFile is { Tags.Count: > 0 };
            OnPropertyChanged(nameof(TagsVisibility));
            OnPropertyChanged(nameof(IsChaptersFromTagsEnabled));
        }
    }
    public Visibility TagsVisibility => TagsEnabled && SelectedFile is { IsImage: false } ? Visibility.Visible : Visibility.Collapsed;

    public Visibility StreamsVisibility => SelectedFile != null ? Visibility.Visible : Visibility.Collapsed;

    public bool ChaptersEnabled
    {
        get => _chaptersEnabled;
        set
        {
            if (value == _chaptersEnabled) 
                return;
            _chaptersEnabled = value;
            OnPropertyChanged();
            IsChaptersExpanded = value && SelectedFile is { Chapters.Count: > 0 };
            OnPropertyChanged(nameof(ChaptersVisibility));
            OnPropertyChanged(nameof(IsChaptersFromFilesEnabled));
            OnPropertyChanged(nameof(IsChaptersFromTagsEnabled));
        }
    }
    public Visibility ChaptersVisibility => ChaptersEnabled && SelectedFile is { IsImage: false } ? Visibility.Visible : Visibility.Collapsed;

    public bool IsChaptersFromTagsEnabled => IsUserEntryEnabled && Files.Count > 0 && ChaptersEnabled && TagsEnabled;
    public bool IsChaptersFromFilesEnabled => IsUserEntryEnabled && Files.Count > 0 && ChaptersEnabled;

    public ICommand Concatenate { get; }
    public ICommand Cancel { get; }
    public ICommand AddPath { get; }
    public ICommand AddFiles { get; }
    public ICommand ClearPaths { get; }
    public ICommand MoveSelected { get; }
    public ICommand SelectTags { get; }
    public ICommand SelectCover { get; }
    public ICommand FixAllIso8859ToWin1251 { get; }
    public ICommand FixSelectedIso8859ToWin1251 { get; }
    public ICommand ToggleTagsEnabled { get; }
    public ICommand ToggleChaptersEnabled { get; }
    public ICommand CreateChaptersFromFiles { get; }
    public ICommand CreateChaptersFromTags { get; }

    public double TaskBarProgress
    {
        get => ProgressPercentage / 10000d;
        set => throw new NotSupportedException();
    }
    public int ProgressPercentage
    {
        get => _progressPercentage;
        set
        {
            if (value == _progressPercentage)
                return;
            _progressPercentage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TaskBarProgress));
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
        IMediaFileToolkitService mediaFileToolkitService,
        IMediaFilesContainer mediaFilesContainer,
        IMediaFilesService mediaFilesService,
        AddFilesCommand addFilesCommand,
        AddPathCommand addPathCommand,
        MoveFileCommand moveFileCommand,
        ConcatenateCommand concatenate,
        CreateChaptersFromFilesCommand createChaptersFromFiles,
        CreateChaptersFromTagsCommand createChaptersFromTags)
    {
        MediaFileToolkitService  = mediaFileToolkitService;
        
        MediaFilesContainer = mediaFilesContainer;
        if (mediaFilesContainer is INotifyPropertyChanged container)
            container.PropertyChanged += OnSelectedAudioFileChanged;

        MediaFilesService = mediaFilesService;
        mediaFileToolkitService.Status += OnStatusUpdate;
        mediaFileToolkitService.Progress += OnProgressUpdate;

        Files = mediaFilesContainer.Files;
        AddFiles = addFilesCommand;
        AddPath = addPathCommand;
        MoveSelected = moveFileCommand;

        concatenate.Starting += OnStarting;
        concatenate.Finished += OnFinished;
        Concatenate = concatenate;

        createChaptersFromFiles.Finished += OnCreateChaptersFinished;
        CreateChaptersFromFiles = createChaptersFromFiles;

        createChaptersFromTags.Finished += OnCreateChaptersFinished;
        CreateChaptersFromTags = createChaptersFromTags;

        ClearPaths = new RelayCommand(Files.Clear);
        Cancel = new RelayCommand(concatenate.Cancel);
        SelectTags = new RelayParameterCommand(OnSelectTags);
        SelectCover = new RelayParameterCommand(OnSelectCover);

        FixAllIso8859ToWin1251 = new RelayParameterCommand(OnFixAllTagsEncoding);
        FixSelectedIso8859ToWin1251 = new RelayParameterCommand(OnFixSelectedTagEncoding);

        ToggleTagsEnabled = new RelayCommand(OnToggleTagsEnabled);
        ToggleChaptersEnabled = new RelayCommand(OnToggleChaptersEnabled);

        Files.CollectionChanged += OnFilesCollectionChanged;

        _ = VerifyMediaFileServiceIsAccessible()
            .ContinueWith(AddCliFilesOnStartup);
    }

    private ObservableCollection<IMediaTagViewModel>? SelectedFileTags { get; set; }
    private void OnSelectedAudioFileChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(SelectedFile));
        OnPropertyChanged(nameof(IsMoveUpEnabled));
        OnPropertyChanged(nameof(IsMoveDownEnabled));
        OnPropertyChanged(nameof(IsRemoveEnabled));
        OnPropertyChanged(nameof(TagsVisibility));
        OnPropertyChanged(nameof(StreamsVisibility));
        OnPropertyChanged(nameof(ChaptersVisibility));
        UpdateExpanders();
        OnPropertyChanged(nameof(TagsCount));
        OnPropertyChanged(nameof(StreamsCount));
        OnPropertyChanged(nameof(ChaptersCount));
        
        if (SelectedFileTags != null)
        {
            SelectedFileTags.CollectionChanged -= OnSelectedFileTagsChanged;
            SelectedFileTags = null;
        }
        if (SelectedFile != null)
        {
            SelectedFileTags = SelectedFile.Tags;
            SelectedFileTags.CollectionChanged += OnSelectedFileTagsChanged;
        }
        
        FocusFileDataGrid?.Invoke();
    }

    private void OnSelectedFileTagsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TagsCount));
    }

    private async Task VerifyMediaFileServiceIsAccessible()
    {
        var result = await MediaFileToolkitService.IsAccessible();
        if (result.IsSuccess)
            IsUserEntryEnabled = true;
        else
            MessageBox.Show(result.Message + Environment.NewLine + "The tools 'ffmpeg.exe' and 'ffprobe.exe' are required for the application to work properly. Download the tools and place them in the system path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async Task AddCliFilesOnStartup(Task _)
    {
        if (!IsUserEntryEnabled) // Media Files Service is not accessible
            return;

        try
        {
            IsUserEntryEnabled = false;
            var args = Environment.GetCommandLineArgs();
            if (args.Length < 2)
                return;

            var fileNames = new string[args.Length - 1];
            for (var i = 0; i < fileNames.Length; i++)
                fileNames[i] = args[i + 1];
            
            var response = await MediaFilesService.AddMediaFiles(fileNames, false); // Long operation, we fire the task and forget

            if (response.SkipFiles.Count > 0)
                await Application.Current.Dispatcher.InvokeAsync(() => new SkippedFilesWindow(response.SkipFiles).ShowDialog());
        }
        catch
        { /* ignore */ }
        finally
        {
            IsUserEntryEnabled = true;
        }
    }

    private void OnSelectTags(object? obj)
    {
        if (obj is not MediaFileViewModel selectedFile)
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

    private static void OnSelectCover(object? obj)
    {
        if (obj is not MediaFileViewModel selectedFile)
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

    private void OnFixAllTagsEncoding(object? _)
    {
        var selectedFile = MediaFilesContainer.SelectedFile;
        if (selectedFile == null || selectedFile.Tags.Count == 0)
            return;

        var iso8859 = Encoding.GetEncoding("ISO-8859-1");
        var win1251 = Encoding.GetEncoding("Windows-1251");
        foreach (var tag in selectedFile.Tags)
        {
            tag.Name = win1251.GetString(iso8859.GetBytes(tag.Name));
            tag.Value = win1251.GetString(iso8859.GetBytes(tag.Value));
        }
    }

    private void OnFixSelectedTagEncoding(object? dataGridObject)
    {
        var selectedFile = MediaFilesContainer.SelectedFile;
        if (selectedFile == null || selectedFile.Tags.Count == 0 || dataGridObject is not DataGrid dataGrid)
            return;

        var selectedTagIndex = dataGrid.SelectedIndex;
        if (selectedTagIndex < 0 || selectedTagIndex >= selectedFile.Tags.Count)
            return;

        var iso8859 = Encoding.GetEncoding("ISO-8859-1");
        var win1251 = Encoding.GetEncoding("Windows-1251");

        var tag = selectedFile.Tags[selectedTagIndex];
        tag.Name = win1251.GetString(iso8859.GetBytes(tag.Name));
        tag.Value = win1251.GetString(iso8859.GetBytes(tag.Value));
    }

    private void OnToggleTagsEnabled() =>
        TagsEnabled = !TagsEnabled;

    private void OnToggleChaptersEnabled()
    {
        if (Settings.CodecsThatDoesNotSupportChapters.Has(SelectedCodec))
            ChaptersEnabled = false;
        else
            ChaptersEnabled = !ChaptersEnabled;
    }

    private void OnStarting(object? sender, EventArgs e)
    {
        ProgressPercentage = Constants.PROGRESS_BAR_MAX_VALUE;
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

    private void OnStatusUpdate(object sender, MessageEventArgs eventArgs) => 
        ProgressText = eventArgs.Message;

    private void OnProgressUpdate(object sender, ProgressEventArgs eventArgs) => 
        ProgressPercentage = eventArgs.Progress.CalculatePercentage();

    private void OnCreateChaptersFinished(object sender, ResponseEventArgs eventArgs)
    {
        IsChaptersExpanded = ChaptersEnabled && SelectedFile is { Chapters.Count: > 0 };
        OnPropertyChanged(nameof(ChaptersCount));
    }

    private bool ChaptersWasDisabledByCodec { get; set; } 
    private void OnFilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsConcatenateEnabled));
        OnPropertyChanged(nameof(IsClearPathsEnabled));
        OnPropertyChanged(nameof(IsMoveUpEnabled));
        OnPropertyChanged(nameof(IsMoveDownEnabled));
        OnPropertyChanged(nameof(IsRemoveEnabled));
        OnPropertyChanged(nameof(IsChaptersFromTagsEnabled));
        OnPropertyChanged(nameof(IsChaptersFromFilesEnabled));
        TotalSize = Files.GetFilesTotalSize();
        TotalDuration = Files.GetTotalDuration();
        SelectedCodec = Services.MediaFilesService.GetAudioCodec(Files);
        if (Settings.CodecsThatDoesNotSupportChapters.Has(SelectedCodec))
        {
            ChaptersEnabled = false;
            ChaptersWasDisabledByCodec = true;
        }
        else if (ChaptersWasDisabledByCodec)
        {
            ChaptersEnabled = true;
            ChaptersWasDisabledByCodec = false;
        }
    }

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    #endregion
}