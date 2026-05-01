using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using AudioCat.Commands;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.Windows;

namespace AudioCat.ViewModels;

public sealed class MainViewModel : IConcatParams, INotifyPropertyChanged
{
    private const string CHAPTERS_WARNING = 
        "[b]WARNING![/b] The files or their order has changed after the chapters has been " +
        "generated, [b]the output file will likely contain [u]invalid[/u] chapters[/b].";

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
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = "";

    public ObservableCollection<IMediaTagViewModel> OutputTags { get; }
    public ObservableCollection<IMediaChapterViewModel> OutputChapters { get; }

    private enum ChaptersSourceType { None, ExistingChapters, CustomChapters }
    private ChaptersSourceType OutputChaptersSource { get; set; }

    public int SelectedDataTabIndex
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
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
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalSizeText));
        }
    }

    public TimeSpan TotalDuration
    {
        get;
        set
        {
            if (value.Equals(field))
                return;
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DurationText));
        }
    }

    public string TotalSizeText => TotalSize.GetBytesCountToText();
    public string DurationText => $"{Math.Truncate(TotalDuration.TotalHours):00}:{TotalDuration.Minutes:00}:{TotalDuration.Seconds:00}";

    public bool IsUserEntryEnabled
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
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
            OnPropertyChanged(nameof(IsCreateChapters));
        }
    }

    public bool IsConcatenateEnabled => IsUserEntryEnabled && Files.Count > 0 && TotalDuration != TimeSpan.Zero;
    public bool IsCancelEnabled => !IsUserEntryEnabled;
    public bool IsAddPathEnabled => IsUserEntryEnabled;
    public bool IsAddFilesEnabled => IsUserEntryEnabled;
    public bool IsClearPathsEnabled => IsUserEntryEnabled && Files.Count > 0;
    public bool IsMoveUpEnabled => IsUserEntryEnabled && Files.Count > 0 && SelectedFile != null && SelectedFile.FileName != "" && SelectedFile != Files[0];
    public bool IsMoveDownEnabled => IsUserEntryEnabled && Files.Count > 0 && SelectedFile != null && SelectedFile.FileName != "" && SelectedFile != Files[^1];
    public bool IsRemoveEnabled => IsUserEntryEnabled && Files.Count > 0 && SelectedFile != null && SelectedFile.FileName != "";

    public bool IsTagsExpanded
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsStreamsExpanded
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsChaptersExpanded
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsOutputTagsExpanded
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsOutputChaptersExpanded
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    }

    public string TagsCount
    {
        get
        {
            if (SelectedFile == null) 
                return "";
            if (SelectedFile.Tags.Count > 0)
                return SelectedFile.Tags.Count > 1 
                    ? $"{SelectedFile.Tags.Count:N0} tags" 
                    : "1 tag";
            return "No tags";
        }
    }

    public string StreamsCount
    {
        get
        {
            if (SelectedFile == null) 
                return "";
            if (SelectedFile.Streams.Count > 0)
                return SelectedFile.Streams.Count > 1 
                    ? $"{SelectedFile.Streams.Count:N0} streams" 
                    : "1 stream";
            return "No streams";
        }
    }

    public string ChaptersCount
    {
        get
        {
            if (SelectedFile == null) 
                return "";
            if (SelectedFile.Chapters.Count > 0)
                return SelectedFile.Chapters.Count > 1 
                    ? $"{SelectedFile.Chapters.Count:N0} chapters" 
                    : "1 chapter";
            return "No chapters";
        }
    }

    public string OutputTagsCount
    {
        get
        {
            if (OutputTags.Count > 0) 
                return OutputTags.Count > 1 
                    ? $"{OutputTags.Count:N0} tags" 
                    : "1 tag";
            return "No tags";
        }
    }

    public string OutputChaptersCount
    {
        get
        {
            if (OutputChapters.Count > 0)
                return OutputChapters.Count > 1 
                    ? $"{OutputChapters.Count:N0} chapters" 
                    : "1 chapter";
            return "No chapters";
        }
    }

    public bool TagsEnabled
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
            IsTagsExpanded = value && SelectedFile is { Tags.Count: > 0 };
            IsOutputTagsExpanded = value && OutputTags.Count > 0;
            OnPropertyChanged(nameof(TagsVisibility));
            OnPropertyChanged(nameof(OutputTagsVisibility));
            OnPropertyChanged(nameof(IsChaptersFromTagsEnabled));
        }
    } = true;

    public Visibility TagsVisibility => TagsEnabled && SelectedFile is { IsImage: false } ? Visibility.Visible : Visibility.Collapsed;
    public Visibility OutputTagsVisibility => TagsEnabled ? Visibility.Visible : Visibility.Collapsed;

    public Visibility StreamsVisibility => SelectedFile != null ? Visibility.Visible : Visibility.Collapsed;

    public bool ChaptersEnabled
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
            IsChaptersExpanded = value && SelectedFile is { Chapters.Count: > 0 };
            IsOutputChaptersExpanded = OutputChapters.Count > 0;
            OnPropertyChanged(nameof(ChaptersVisibility));
            OnPropertyChanged(nameof(OutputChaptersVisibility));
            OnPropertyChanged(nameof(IsChaptersFromFilesEnabled));
            OnPropertyChanged(nameof(IsChaptersFromTagsEnabled));
            OnPropertyChanged(nameof(IsCreateChapters));
            RefreshChaptersWarning();
        }
    } = true;

    public Visibility ChaptersVisibility => ChaptersEnabled && SelectedFile is { IsImage: false } ? Visibility.Visible : Visibility.Collapsed;
    public Visibility OutputChaptersVisibility => ChaptersEnabled ? Visibility.Visible : Visibility.Collapsed;

    public bool IsChaptersFromTagsEnabled => IsUserEntryEnabled && Files.Count > 0 && ChaptersEnabled && TagsEnabled;
    public bool IsChaptersFromFilesEnabled => IsUserEntryEnabled && Files.Count > 0 && ChaptersEnabled;
    public bool IsCreateChapters => IsUserEntryEnabled && Files.Count > 0 && ChaptersEnabled;

    public string OutputWarning
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
            OutputWarningVisibility = string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible;
        }
    } = "";

    public Visibility OutputWarningVisibility
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = Visibility.Collapsed;

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
    public ICommand ClearChapters { get; }
    public ICommand CreateChapters { get; }

    public double TaskBarProgress
    {
        get => ProgressPercentage / 10000d;
        set => throw new NotSupportedException();
    }

    public int ProgressPercentage
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TaskBarProgress));
        }
    }

    public string ProgressText
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = "";

    #pragma warning disable S107
    public MainViewModel(
        IMediaFileToolkitService mediaFileToolkitService,
        IMediaFilesContainer mediaFilesContainer,
        IMediaFilesService mediaFilesService,
        AddFilesCommand addFilesCommand,
        AddPathCommand addPathCommand,
        MoveFileCommand moveFileCommand,
        ConcatenateCommand concatenate,
        CreateChaptersCommand createChapters,
        FixItemEncodingCommand fixItemEncodingCommand,
        FixItemsEncodingCommand fixItemsEncodingCommand)
    #pragma warning restore S107
    {
        MediaFileToolkitService  = mediaFileToolkitService;
        
        MediaFilesContainer = mediaFilesContainer;
        if (mediaFilesContainer is INotifyPropertyChanged container)
            container.PropertyChanged += OnMediaFilesContainerChanged;

        OutputTags = [];
        OutputTags.CollectionChanged += OnOutputTagsChanged;
        OutputChapters = [];
        OutputChapters.CollectionChanged += OnOutputChaptersChanged;

        MediaFilesService = mediaFilesService;
        mediaFileToolkitService.Status += OnStatusUpdate;
        mediaFileToolkitService.Progress += OnProgressUpdate;

        Files = mediaFilesContainer.Files;
        AddFiles = addFilesCommand;
        AddPath = addPathCommand;
        MoveSelected = moveFileCommand;

        concatenate.Starting += OnConcatStarting;
        concatenate.Finished += OnConcatFinished;
        Concatenate = concatenate;

        createChapters.Finished += OnCreateChaptersFinished;
        CreateChapters = createChapters;

        ClearChapters = new RelayCommand(OnClearChapters);

        ClearPaths = new RelayCommand(Files.Clear);
        Cancel = new RelayCommand(concatenate.Cancel);
        SelectTags = new RelayParameterCommand(OnSelectTags);
        SelectCover = new RelayParameterCommand(OnSelectCover);

        FixAllIso8859ToWin1251 = fixItemsEncodingCommand;
        FixSelectedIso8859ToWin1251 = fixItemEncodingCommand;

        ToggleTagsEnabled = new RelayCommand(OnToggleTagsEnabled);
        ToggleChaptersEnabled = new RelayCommand(OnToggleChaptersEnabled);

        Files.CollectionChanged += OnFilesCollectionChanged;

        _ = VerifyMediaFileServiceIsAccessible()
            .ContinueWith(AddCliFilesOnStartup)
            .ContinueWith(AddOutputChaptersOnStartup)
            .ContinueWith(EnableUserEntryOnStartup);
    }

    private void OnOutputTagsChanged(object? sender, NotifyCollectionChangedEventArgs e) => 
        OnPropertyChanged(nameof(OutputTagsCount));

    private bool DoNotInvokeOutputChaptersCountChangedEvent { get; set; } = false;
    private void OnOutputChaptersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!DoNotInvokeOutputChaptersCountChangedEvent) 
            OnPropertyChanged(nameof(OutputChaptersCount));
    }

    private ObservableCollection<IMediaTagViewModel>? SelectedFileTags { get; set; }

    private void OnMediaFilesContainerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaFilesContainer.SelectedFile))
            OnSelectedAudioFileChanged();
    }

    private void OnSelectedAudioFileChanged()
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
            MessageBox.Show($"{result.Message}{Environment.NewLine}The tools '{Settings.FFmpegName}' and '{Settings.FFprobeName}' are required for the application to work properly. Download the tools and place them in the system path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async Task AddCliFilesOnStartup(Task _)
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length < 2)
                return;

            var namesFromArgs = new string[args.Length - 1];
            for (var i = 0; i < namesFromArgs.Length; i++)
                namesFromArgs[i] = args[i + 1];

            var fileNames = namesFromArgs.IsAllDirectories()
                ? await Services.Files.GetFilesFromDirectories(namesFromArgs)
                : namesFromArgs;

            var response = await MediaFilesService.AddMediaFiles(fileNames, false);
            if (response.SkippedFiles.Count > 0)
                await Application.Current.Dispatcher.InvokeAsync(() => new SkippedFilesWindow(response.SkippedFiles).ShowDialog());
        }
        catch
        { /* ignore */ }
    }

    private async Task AddOutputChaptersOnStartup(Task _)
    {
        if (!Files.ChaptersExist())
            return;

        try
        {
            try
            {
                DoNotInvokeOutputChaptersCountChangedEvent = true;
                var newChapters = ChaptersFactory.CreateFromExisting(Files.AsReadOnly(), false); // TODO: Settings.TrimStartingNonChars
                for (var index = 0; index < newChapters.Count; index++)
                {
                    if (index == newChapters.Count - 1)
                        DoNotInvokeOutputChaptersCountChangedEvent = true;
                    var newChapter = newChapters[index];
                    await Application.Current.Dispatcher.InvokeAsync(() => OutputChapters.Add(newChapter));
                }
            }
            finally
            {
                DoNotInvokeOutputChaptersCountChangedEvent = false;
            }
            OutputChaptersSource = ChaptersSourceType.ExistingChapters;
            RememberChaptersFilesOrder();
            IsOutputChaptersExpanded = ChaptersEnabled && OutputChapters.Count > 0;
            OnPropertyChanged(nameof(OutputChaptersCount));
            // SelectedDataTabIndex = 1; // TODO: Maybe needed, evaluate
        }
        catch
        {
            if (OutputChapters.Count > 0)
                OutputChapters.Clear();
        }
    }

    private async Task EnableUserEntryOnStartup(Task _)
    {
        await Application.Current.Dispatcher.InvokeAsync(() => IsUserEntryEnabled = true);
    }

    // Called when tags source is selected in the DataGrid. Not called for initial selection.
    private void OnSelectTags(object? obj)
    {
        if (obj is not MediaFileViewModel { HasTags: true } selectedFile)
            return;

        selectedFile.Tags.SetTo(OutputTags);
        IsOutputTagsExpanded = OutputTags.Count > 0;
        SelectedDataTabIndex = 1;
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

    private void OnToggleTagsEnabled() =>
        TagsEnabled = !TagsEnabled;

    private void OnToggleChaptersEnabled()
    {
        if (Settings.CodecsThatDoesNotSupportChapters.Has(SelectedCodec))
            ChaptersEnabled = false;
        else
            ChaptersEnabled = !ChaptersEnabled;
    }

    private void OnConcatStarting(object? sender, EventArgs e)
    {
        ProgressPercentage = Constants.PROGRESS_BAR_MAX_VALUE;
        IsUserEntryEnabled = false;
    }

    private void OnConcatFinished(object sender, ResponseEventArgs eventArgs)
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
    
    private List<string> ChaptersFilesOrder { get; } = [];

    private void OnClearChapters()
    {
        ChaptersFilesOrder.Clear();
        OutputChapters.Clear();
        OutputWarning = "";
        OutputChaptersSource = ChaptersSourceType.None;
    }

    private void OnCreateChaptersFinished(object sender, ResponseEventArgs eventArgs)
    {
        var response = eventArgs.Response;
        if (response is { IsSuccess: true, Data: null } || response.IsFailure)
            return;

        var outputChapters = (ObservableCollection<IMediaChapterViewModel>)response.Data!;
        OutputChapters.Clear();
        foreach (var chapter in outputChapters)
            OutputChapters.Add(chapter);
        
        OutputChaptersSource = ChaptersSourceType.CustomChapters; //TODO: distinguish Custom vs Existing
        RememberChaptersFilesOrder();
        IsOutputChaptersExpanded = ChaptersEnabled && OutputChapters.Count > 0;
        OnPropertyChanged(nameof(OutputChaptersCount));
        SelectedDataTabIndex = 1;
    }

    private void RememberChaptersFilesOrder()
    {
        ChaptersFilesOrder.Clear();
        foreach (var file in Files)
            ChaptersFilesOrder.Add(file.FilePath);
        OutputWarning = "";
    }

    private bool IsChaptersFilesOrderChanged()
    {
        if (ChaptersFilesOrder.Count == 0)
            return false;

        if (ChaptersFilesOrder.Count != Files.Count)
            return true;

        for (var i = 0; i < Files.Count; i++)
        {
            if (ChaptersFilesOrder[i] != Files[i].FilePath)
                return true;
        }

        return false;
    }

    private void RefreshChaptersWarning() => 
        OutputWarning = ChaptersEnabled && OutputChaptersSource != ChaptersSourceType.ExistingChapters && IsChaptersFilesOrderChanged() ? CHAPTERS_WARNING : "";
    
    private bool ChaptersWasDisabledByCodec { get; set; } 
    private void OnFilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (MediaFilesContainer.DoNotInvokeFilesCollectionChangedEvent)
            return;

        if (Files.Count == 0)
        {
            SelectedDataTabIndex = 0;
            ClearOutput();
        }

        TotalSize = Files.GetFilesTotalSize();
        TotalDuration = Files.GetTotalDuration();
        SelectedCodec = Services.MediaFilesService.GetAudioCodec(Files);
        if (Settings.CodecsThatDoesNotSupportChapters.Has(SelectedCodec)) // Current codec does not support chapters
        {
            ChaptersEnabled = false; // This will call RefreshChaptersWarning
            ChaptersWasDisabledByCodec = true;
        }
        else if (ChaptersWasDisabledByCodec) // Chapters was disabled by the codec, but the current codec supports chapters. Re-enable chapters.
        {
            ChaptersEnabled = true; // This will call RefreshChaptersWarning
            ChaptersWasDisabledByCodec = false;
        }
        else 
            RefreshChaptersWarning();

        // Must go after the TotalDuration and other code above due to dependencies
        OnPropertyChanged(nameof(IsConcatenateEnabled));
        OnPropertyChanged(nameof(IsClearPathsEnabled));
        OnPropertyChanged(nameof(IsMoveUpEnabled));
        OnPropertyChanged(nameof(IsMoveDownEnabled));
        OnPropertyChanged(nameof(IsRemoveEnabled));
        OnPropertyChanged(nameof(IsChaptersFromTagsEnabled));
        OnPropertyChanged(nameof(IsChaptersFromFilesEnabled));
        OnPropertyChanged(nameof(IsCreateChapters));

        if (Files.Count == 0 || !Files.ChaptersExist()) // No files or no chapters in files
        {
            RememberChaptersFilesOrder();
            OutputChaptersSource = ChaptersSourceType.None;
            IsOutputChaptersExpanded = false;
            OnPropertyChanged(nameof(OutputChaptersCount));
            OutputTags.Clear();
            return;
        }
        if (OutputChaptersSource == ChaptersSourceType.None || OutputChaptersSource == ChaptersSourceType.ExistingChapters && IsChaptersFilesOrderChanged()) // Re-generate chapters from existing files
        {
            try
            {
                DoNotInvokeOutputChaptersCountChangedEvent = true;
                OutputChapters.Clear();
                var newChapters = ChaptersFactory.CreateFromExisting(Files.AsReadOnly(), false);
                for (var index = 0; index < newChapters.Count; index++)
                {
                    if (index == newChapters.Count - 1)
                        DoNotInvokeOutputChaptersCountChangedEvent = true;
                    var newChapter = newChapters[index];
                    OutputChapters.Add(newChapter);
                }
            }
            finally
            {
                DoNotInvokeOutputChaptersCountChangedEvent = false;
            }
            RememberChaptersFilesOrder();
            OutputChaptersSource = ChaptersSourceType.ExistingChapters;
            IsOutputChaptersExpanded = ChaptersEnabled && OutputChapters.Count > 0;
            OnPropertyChanged(nameof(OutputChaptersCount));
        }

        if (OutputTags.Count == 0)
            SelectOutputTagsOnFilesLoad();        
    }

    private void SelectOutputTagsOnFilesLoad()
    {
        foreach (var file in Files)
        {
            if (!file.HasTags || file.IsImage)
                continue; //Skip the files that doesn't have tags, take the tags from the first file
            file.Tags.SetTo(OutputTags);
            IsOutputTagsExpanded = OutputTags.Count > 0;
            break;
        }
    }

    private void ClearOutput()
    {
        IsOutputChaptersExpanded = false;
        IsOutputTagsExpanded = false;
        OutputChapters.Clear();
        ChaptersFilesOrder.Clear();
        OutputTags.Clear();
        OutputWarning = "";
    }

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    #endregion
}