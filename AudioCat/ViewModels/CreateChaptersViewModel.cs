using AudioCat.Commands;
using AudioCat.Models;
using AudioCat.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace AudioCat.ViewModels;

public enum ChapterSourceType { Unknown, FileNames, MetadataTags, Template, Existing, SilenceScan, CueFile }
public sealed class ChapterSourceItem
{
    public ChapterSourceType SourceType { get; init; } = ChapterSourceType.Unknown;
    public string Description { get; init; } = "";
}

public sealed class CreateChaptersViewModel : ISilenceScanArgs, INotifyPropertyChanged
{
    #region Backing Fields
    private bool _trimStartingNonChars;
    private string _selectedTagName = "";                                // TODO Move to settings
    private string _template = "Chapter {}";                             // TODO Move to settings
    private int _templateStartNumber = 1;
    private string _templateFormat = "0";
    private int _silenceThreshold = Constants.DEFAULT_SILENCE_THRESHOLD; // TODO Move to settings
    private int _silenceDuration = Constants.DEFAULT_SILENCE_DURATION;   // TODO Move to settings
    private Visibility _silenceScanProgressVisibility = Visibility.Hidden;
    private Visibility _silenceScanButtonVisibility = Visibility.Visible;
    private Visibility _cancelSilenceScanButtonVisibility = Visibility.Hidden;
    private bool _isUserInputEnabled = true;
    private ChapterSourceItem _selectedChapterSource = new() {  SourceType = ChapterSourceType.Unknown, Description = "" };

    #endregion

    public IReadOnlyList<IMediaFileViewModel> Files { get; }

    public IReadOnlyList<ChapterSourceItem> ChapterSources { get; } =
    [
        new ChapterSourceItem { SourceType = ChapterSourceType.FileNames, Description = "File Names" },
        new ChapterSourceItem { SourceType = ChapterSourceType.MetadataTags, Description = "Metadata Tags" },
        new ChapterSourceItem { SourceType = ChapterSourceType.CueFile, Description = "CUE Sheet Files" },
        new ChapterSourceItem { SourceType = ChapterSourceType.Template, Description = "Template" },
        new ChapterSourceItem { SourceType = ChapterSourceType.SilenceScan, Description = "Silence Scan" },
        new ChapterSourceItem { SourceType = ChapterSourceType.Existing, Description = "Existing Chapters" },
    ];
    public ChapterSourceItem SelectedChapterSource
    {
        get => _selectedChapterSource;
        set
        {
            if (value == _selectedChapterSource) 
                return;
            _selectedChapterSource = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FileNamesOptionsVisibility));
            OnPropertyChanged(nameof(MetadataTagsOptionsVisibility));
            OnPropertyChanged(nameof(TemplateOptionsVisibility));
            OnPropertyChanged(nameof(ExistingChaptersOptionsVisibility));
            OnPropertyChanged(nameof(SilenceScanOptionsVisibility));
            OnPropertyChanged(nameof(CueFileOptionsVisibility));
            OnPropertyChanged(nameof(IsGenerateEnabled));
        }
    }

    #region Options Visibility
    public Visibility FileNamesOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.FileNames ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MetadataTagsOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.MetadataTags ? Visibility.Visible : Visibility.Collapsed;
    public Visibility TemplateOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.Template ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ExistingChaptersOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.Existing ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SilenceScanOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.SilenceScan ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CueFileOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.CueFile ? Visibility.Visible : Visibility.Collapsed;
    #endregion

    public bool TrimStartingNonChars
    {
        get => _trimStartingNonChars;
        set
        {
            if (value == _trimStartingNonChars)
                return;
            _trimStartingNonChars = value;
            OnPropertyChanged();
        }
    }

    #region Metedata Tags Options
    public ObservableCollection<string> TagNames { get; } = [];
    public string SelectedTagName
    {
        get => _selectedTagName;
        set
        {
            if (value == _selectedTagName)
                return;
            _selectedTagName = value;
            OnPropertyChanged();
        }
    }
    #endregion

    #region Template Options
    public string Template
    {
        get => _template;
        set
        {
            if (value == _template) 
                return;
            _template = value;
            OnPropertyChanged(); 
        }
    }
    public int TemplateStartNumber
    {
        get => _templateStartNumber;
        set
        {
            if (value == _templateStartNumber) return;
            _templateStartNumber = value;
            OnPropertyChanged();
        }
    }
    public string TemplateFormat
    {
        get => _templateFormat;
        set
        {
            if (value == _templateFormat) return;
            _templateFormat = value;
            OnPropertyChanged();
        }
    }
    #endregion

    #region Silence Scan Options
    public int SilenceThreshold
    {
        get => _silenceThreshold;
        set
        {
            if (value == _silenceThreshold) 
                return;
            _silenceThreshold = value;
            OnPropertyChanged();
        }
    }
    public int SilenceDuration
    {
        get => _silenceDuration;
        set
        {
            if (value == _silenceDuration) 
                return;
            _silenceDuration = value;
            OnPropertyChanged();
        }
    }
    public Visibility SilenceScanProgressVisibility
    {
        get => _silenceScanProgressVisibility;
        set
        {
            if (value == _silenceScanProgressVisibility) 
                return;
            _silenceScanProgressVisibility = value;
            OnPropertyChanged();
        }
    }
    public Visibility SilenceScanButtonVisibility
    {
        get => _silenceScanButtonVisibility;
        set
        {
            if (value == _silenceScanButtonVisibility) 
                return;
            _silenceScanButtonVisibility = value;
            OnPropertyChanged();
        }
    }
    public Visibility CancelSilenceScanButtonVisibility
    {
        get => _cancelSilenceScanButtonVisibility;
        set
        {
            if (value == _cancelSilenceScanButtonVisibility) 
                return;
            _cancelSilenceScanButtonVisibility = value;
            OnPropertyChanged();
        }
    }
    #endregion

    #region CUE Files Handling
    public ObservableCollection<FileInfo> CueFiles { get; } = [];
    public ICommand SelectCueFile { get; }
    #endregion

    public ObservableCollection<IMediaChapterViewModel> CreatedChapters { get; }

    public bool IsExistingChaptersEnabled { get; }
    public bool IsUseCreatedEnabled => CreatedChapters.Count > 0;
    public bool IsUserInputEnabled
    {
        get => _isUserInputEnabled;
        set
        {
            if (value == _isUserInputEnabled) 
                return;
            _isUserInputEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsGenerateEnabled => SelectedChapterSource.SourceType switch
    {
        ChapterSourceType.FileNames or ChapterSourceType.MetadataTags or ChapterSourceType.Template or ChapterSourceType.Existing or ChapterSourceType.SilenceScan => true,
        ChapterSourceType.CueFile => CueFiles.Count > 0,
        ChapterSourceType.Unknown => false,
        _ => false
    };

    public ICommand GenerateChapters { get; }
    public ICommand CloseDialog { get; }
    public ICommand UseCreatedChapters { get; }
    public ICommand FixAllIso8859ToWin1251 { get; }
    public ICommand FixSelectedIso8859ToWin1251 { get; }
    public ICommand ScanForSilence { get; }
    public ICommand CancelScanForSilence { get; }

    public event EventHandler? Close;
    public event EventHandler? UseCreated;

    public CreateChaptersViewModel(
        ObservableCollection<IMediaFileViewModel> files, 
        FixItemEncodingCommand fixItemEncodingCommand,
        FixItemsEncodingCommand fixItemsEncodingCommand,
        ScanForSilenceCommand scanForSilence)
    {
        CreatedChapters = [];
        CreatedChapters.CollectionChanged += OnCreatedChaptersChanged;

        GenerateChapters = new RelayCommand(OnGenerateChapters);
        CloseDialog = new RelayCommand(OnClose);
        Files = files;
        FixAllIso8859ToWin1251 = fixItemsEncodingCommand;
        FixSelectedIso8859ToWin1251 = fixItemEncodingCommand;
        UseCreatedChapters = new RelayCommand(() => { OnUseCreated(); OnClose(); });
        PopulateTagNames();
        IsExistingChaptersEnabled = FilesHasChapters(Files);

        scanForSilence.Starting += OnScanForSilenceStarting;
        scanForSilence.Finished += OnScanForSilenceFinished;
        CancelScanForSilence = new RelayCommand(scanForSilence.Cancel);
        ScanForSilence = scanForSilence;

        SelectedChapterSource = ChapterSources[0];
        SelectCueFile = new RelayCommand(OnSelectCueFile);

        _ = Task.Run(CreateChaptersFromFileNames);
    }

    private void OnSelectCueFile()
    {
        var fileNames = SelectionDialog.ChooseFilesToOpen("CUE Sheet|*.cue", true);
        if (fileNames.Length == 0)
            return;

        CueFiles.Clear();
        foreach (var fileName in fileNames)
        {
            var fileInfo = new FileInfo(fileName);
            if (fileInfo.Exists)
                CueFiles.Add(fileInfo);
        }
    }

    private void OnCreatedChaptersChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => 
        OnPropertyChanged(nameof(IsUseCreatedEnabled));

    private void PopulateTagNames()
    {
        TagNames.Clear();
        foreach (var file in Files)
        {
            foreach (var tag in file.Tags)
            {
                if (!TagNames.Contains(tag.Name))
                    TagNames.Add(tag.Name);
            }
        }
    }

    private static bool FilesHasChapters(IEnumerable<IMediaFileViewModel> files)
    {
        foreach (var file in files)
        {
            if (file is { IsImage: false, Chapters.Count: > 0 })
                return true;
        }

        return false;
    }



    private void OnGenerateChapters()
    {
        switch (SelectedChapterSource.SourceType)
        {
            case ChapterSourceType.FileNames: CreateChaptersFromFileNames(); break;
            case ChapterSourceType.MetadataTags: CreateChaptersFromMetadataTags(); break;
            case ChapterSourceType.Template: CreateChaptersFromTemplate(); break;
            case ChapterSourceType.Existing: CreateChaptersFromExisting(); break;
            case ChapterSourceType.Unknown:
            case ChapterSourceType.SilenceScan:
            case ChapterSourceType.CueFile:
            default: break;
        }
    }




    private void OnScanForSilenceStarting(object? sender, EventArgs eventArgs)
    {
        IsUserInputEnabled = false;
        SilenceScanProgressVisibility = Visibility.Visible;
        SilenceScanButtonVisibility = Visibility.Hidden;
        CancelSilenceScanButtonVisibility = Visibility.Visible;
    }

    private void OnScanForSilenceFinished(object sender, ResponseEventArgs eventArgs)
    {
        CancelSilenceScanButtonVisibility = Visibility.Hidden;

        try
        {
            var response = eventArgs.Response;
            if (response.IsFailure)
            {
                if (response.Message is nameof(OperationCanceledException) or nameof(TaskCanceledException))
                    return;
                MessageBox.Show(response.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (response.Data != null)
                CreateChaptersFromIntervals((IReadOnlyList<IInterval>)response.Data);
        }
        finally
        {
            SilenceScanProgressVisibility = Visibility.Hidden;
            SilenceScanButtonVisibility = Visibility.Visible;
            IsUserInputEnabled = true;
        }
    }

    private void CreateChaptersFromIntervals(IReadOnlyList<IInterval> intervals)
    {
        var startTime = TimeSpan.Zero;
        CreatedChapters.Clear();
        foreach (var interval in intervals)
        {
            var chapter = CreateChapter(startTime, interval.Start - startTime, CreatedChapters.Count.ToString(), CreatedChapters.Count);
            CreatedChapters.Add(chapter);
            startTime += interval.End - startTime;
        }
    }




    private void CreateChaptersFromFileNames()
    {
        var chapters = CreateChapters(GetTitleFromFileName);
        CreatedChapters.Clear();
        foreach (var chapter in chapters)
            CreatedChapters.Add(chapter);
    }

    private void CreateChaptersFromMetadataTags()
    {
        if (string.IsNullOrEmpty(SelectedTagName))
        {
            if (!TagNames.Has("title"))
            {
                CreatedChapters.Clear();
                return;
            }

            SelectedTagName = "title";
        }

        var chapters = CreateChapters(GetTitleFromTags);
        CreatedChapters.Clear();
        foreach (var chapter in chapters)
            CreatedChapters.Add(chapter);
    }

    private void CreateChaptersFromTemplate()
    {
        var chapters = CreateChapters(GetTitleFromTemplate);
        CreatedChapters.Clear();
        foreach (var chapter in chapters)
            CreatedChapters.Add(chapter);
    }

    private string GetTitleFromFileName(IMediaFileViewModel file, int _)
    {
        var title = Path.GetFileNameWithoutExtension(file.File.Name);
        return TrimStartingNonChars ? title.TrimStartNonChars() : title;
    }

    private string GetTitleFromTags(IMediaFileViewModel file, int _)
    {
        var title = file.Tags.GetTagValue(SelectedTagName);
        return TrimStartingNonChars ? title.TrimStartNonChars() : title;
    }

    private string GetTitleFromTemplate(IMediaFileViewModel _, int index)
    {
        var title = Template;
        var format = TemplateFormat;
        string titleNumber;
        try { titleNumber = string.Format($"{{0:{format}}}", TemplateStartNumber + index); }
        catch { return title; }
        return title.Replace("{}", titleNumber);
    }

    private IReadOnlyList<IMediaChapterViewModel> CreateChapters(Func<IMediaFileViewModel, int, string> getTitle)
    {
        var startTime = TimeSpan.Zero;
        var chapters = new List<IMediaChapterViewModel>(Files.Count);
        for (var index = 0; index < Files.Count; index++)
        {
            var file = Files[index];
            if (file.IsImage || file.Duration == null)
                continue;
            var title = getTitle(file, index);
            var chapter = CreateChapter(startTime, file.Duration.Value, title, index);
            chapters.Add(chapter);
            startTime = chapter.EndTime!.Value;
        }

        return chapters;
    }

    private static IMediaChapterViewModel CreateChapter(TimeSpan startTime, TimeSpan duration, string title, int index)
    {
        const decimal divident = 1m;
        const decimal divisor = 1000m;

        var endTime = startTime.Add(duration);
        var calculatedStart = (long)((decimal)startTime.TotalSeconds * divisor);
        var calculatedEnd = (long)((decimal)endTime.TotalSeconds * divisor);

        return new ChapterViewModel
        {
            Id = index,
            Start = calculatedStart,
            End = calculatedEnd,
            TimeBaseDivident = divident,
            TimeBaseDivisor = divisor,
            StartTime = startTime,
            EndTime = endTime,
            Title = title
        };
    }

    private void CreateChaptersFromExisting()
    {
        var startTime = TimeSpan.Zero;
        CreatedChapters.Clear();
        foreach (var file in Files)
        {
            if (file.IsImage || !file.Duration.HasValue)
                continue;

            if (file.Chapters.Count == 0)
            {
                var chapter = CreateChapter(startTime, file.Duration.Value, "", CreatedChapters.Count);
                CreatedChapters.Add(chapter);
                startTime += file.Duration.Value;
                continue;
            }

            foreach (var sourceChapter in file.Chapters)
            {
                var duration = sourceChapter.EndTime!.Value - sourceChapter.StartTime!.Value;
                var title = TrimStartingNonChars ? sourceChapter.Title.TrimStartNonChars() : sourceChapter.Title;
                var chapter = CreateChapter(startTime, duration, title, CreatedChapters.Count);
                CreatedChapters.Add(chapter);
                startTime += duration;
            }
        }
    }

    private void OnClose() => Close?.Invoke(this, EventArgs.Empty);

    private void OnUseCreated() => UseCreated?.Invoke(this, EventArgs.Empty);

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}