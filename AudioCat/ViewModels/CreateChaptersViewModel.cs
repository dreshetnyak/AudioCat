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

public enum ChapterSourceType { Unknown, FileNames, MetadataTags, Template, Existing, SilenceScan }
public sealed class ChapterSourceItem
{
    public ChapterSourceType SourceType { get; init; } = ChapterSourceType.Unknown;
    public string Description { get; init; } = "";
}

public sealed class CreateChaptersViewModel : ISilenceScanArgs, INotifyPropertyChanged
{
    #region Backing Fields

    private const int DEFAULT_SEQUENCE_START = 1;

    private bool _trimStartingNonChars;
    private string _selectedTagName = "";                                // TODO Move to settings
    private string _template = "Chapter {}";                             // TODO Move to settings
    private string _templateStartNumber = DEFAULT_SEQUENCE_START.ToString();
    private int _silenceThreshold = Constants.DEFAULT_SILENCE_THRESHOLD; // TODO Move to settings
    private int _silenceDuration = Constants.DEFAULT_SILENCE_DURATION;   // TODO Move to settings
    private Visibility _silenceScanProgressVisibility = Visibility.Hidden;
    private Visibility _silenceScanButtonVisibility = Visibility.Visible;
    private Visibility _cancelSilenceScanButtonVisibility = Visibility.Hidden;
    private bool _isUserInputEnabled = true;
    private ChapterSourceItem _selectedChapterSource = new() {  SourceType = ChapterSourceType.Unknown, Description = "" };
    private string _textToTrim = "";
    private bool _isTrimExactText = true;
    private bool _isTrimCharsFromText;
    private bool _isTrimCaseSensitive;
    private bool _isTrimEnabled;
    private string _replaceWhatText = "";
    private string _replaceWithText = "";
    private bool _isReplaceCaseSensitive;
    private bool _isReplaceEnabled;
    private string _textToAdd = "";
    private string _textToAddSequenceStart = "";
    private bool _isAddEnabled;
    private bool _isTextToAddSequenceStartValid = true;
    private int _textToAddSequenceStartValue = DEFAULT_SEQUENCE_START;
    private bool _isTemplateStartNumberValid = true;

    #endregion

    public IReadOnlyList<IMediaFileViewModel> Files { get; }

    public ObservableCollection<ChapterSourceItem> ChapterSources { get; }
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
            OnPropertyChanged(nameof(IsGenerateEnabled));
            OnPropertyChanged(nameof(OptionsVisibility));
        }
    }

    #region Options Visibility
    public Visibility FileNamesOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.FileNames ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MetadataTagsOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.MetadataTags ? Visibility.Visible : Visibility.Collapsed;
    public Visibility TemplateOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.Template ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ExistingChaptersOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.Existing ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SilenceScanOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.SilenceScan ? Visibility.Visible : Visibility.Collapsed;
    public Visibility OptionsVisibility => SelectedChapterSource.SourceType != ChapterSourceType.FileNames && SelectedChapterSource.SourceType != ChapterSourceType.Existing ? Visibility.Visible : Visibility.Collapsed;
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
    public string TemplateStartNumber
    {
        get => _templateStartNumber;
        set
        {
            if (value == _templateStartNumber) 
                return;
            _templateStartNumber = value;
            OnPropertyChanged();

            if (int.TryParse(TemplateStartNumber, out var numberValue))
            {
                IsTemplateStartNumberValid = true;
                TemplateStartNumberValue = numberValue;
            }
            else
            {
                IsTemplateStartNumberValid = false;
                TemplateStartNumberValue = DEFAULT_SEQUENCE_START;
            }
        }
    }
    public int TemplateStartNumberValue { get; set; } = DEFAULT_SEQUENCE_START;
    public bool IsTemplateStartNumberValid
    {
        get => _isTemplateStartNumberValid;
        set
        {
            if (value == _isTemplateStartNumberValid) 
                return;
            _isTemplateStartNumberValid = value;
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

    #region Modifying the Chapters
    public string TextToTrim
    {
        get => _textToTrim;
        set
        {
            if (value == _textToTrim) 
                return;
            _textToTrim = value;
            OnPropertyChanged();
            IsTrimEnabled = !string.IsNullOrEmpty(value);
        }
    }
    public bool IsTrimExactText
    {
        get => _isTrimExactText;
        set
        {
            if (value == _isTrimExactText) 
                return;
            _isTrimExactText = value;
            OnPropertyChanged();
        }
    }
    public bool IsTrimCharsFromText
    {
        get => _isTrimCharsFromText;
        set
        {
            if (value == _isTrimCharsFromText) 
                return;
            _isTrimCharsFromText = value;
            OnPropertyChanged();
        }
    }
    public bool IsTrimCaseSensitive
    {
        get => _isTrimCaseSensitive;
        set
        {
            if (value == _isTrimCaseSensitive) 
                return;
            _isTrimCaseSensitive = value;
            OnPropertyChanged();
        }
    }
    public bool IsTrimEnabled
    {
        get => _isTrimEnabled;
        set
        {
            if (value == _isTrimEnabled) 
                return;
            _isTrimEnabled = value;
            OnPropertyChanged();
        }
    }
    public ICommand TrimStart { get; }
    public ICommand TrimEnd { get; }

    public string ReplaceWhatText
    {
        get => _replaceWhatText;
        set
        {
            if (value == _replaceWhatText) 
                return;
            _replaceWhatText = value;
            OnPropertyChanged();
            IsReplaceEnabled = !string.IsNullOrEmpty(value);
        }
    }
    public string ReplaceWithText
    {
        get => _replaceWithText;
        set
        {
            if (value == _replaceWithText) 
                return;
            _replaceWithText = value;
            OnPropertyChanged();
        }
    }
    public bool IsReplaceCaseSensitive
    {
        get => _isReplaceCaseSensitive;
        set
        {
            if (value == _isReplaceCaseSensitive) 
                return;
            _isReplaceCaseSensitive = value;
            OnPropertyChanged();
        }
    }
    public bool IsReplaceEnabled
    {
        get => _isReplaceEnabled;
        set
        {
            if (value == _isReplaceEnabled) 
                return;
            _isReplaceEnabled = value;
            OnPropertyChanged();
        }
    }
    public ICommand ReplaceInTitles { get; }

    public string TextToAdd
    {
        get => _textToAdd;
        set
        {
            if (value == _textToAdd) 
                return;
            _textToAdd = value;
            OnPropertyChanged();
            IsAddEnabled = !string.IsNullOrEmpty(value);
        }
    }
    public string TextToAddSequenceStart
    {
        get => _textToAddSequenceStart;
        set
        {
            if (value == _textToAddSequenceStart) 
                return;
            _textToAddSequenceStart = value;
            OnPropertyChanged();
            if (!string.IsNullOrWhiteSpace(value))
            {
                IsTextToAddSequenceStartValid = int.TryParse(TextToAddSequenceStart, out var sequenceStart);
                TextToAddSequenceStartValue = sequenceStart;
            }
            else
            {
                IsTextToAddSequenceStartValid = true;
                TextToAddSequenceStartValue = DEFAULT_SEQUENCE_START;
            }

        }
    }
    public bool IsTextToAddSequenceStartValid
    {
        get => _isTextToAddSequenceStartValid;
        set
        {
            if (value == _isTextToAddSequenceStartValid) 
                return;
            _isTextToAddSequenceStartValid = value;
            OnPropertyChanged();
        }
    }
    public int TextToAddSequenceStartValue
    {
        get => _textToAddSequenceStartValue;
        set
        {
            if (value == _textToAddSequenceStartValue) 
                return;
            _textToAddSequenceStartValue = value;
            OnPropertyChanged();
        }
    }
    public bool IsAddEnabled
    {
        get => _isAddEnabled;
        set
        {
            if (value == _isAddEnabled) 
                return;
            _isAddEnabled = value;
            OnPropertyChanged();
        }
    }
    public ICommand AddToStart { get; }
    public ICommand AddToEnd { get; }
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
        ChapterSources = new ObservableCollection<ChapterSourceItem>(GetChapterSources(files));
        SetInitialSelectedChapterSource();
        _ = Task.Run(OnGenerateChapters);

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

        SetInitialSelectedChapterSource();

        TrimStart = new RelayCommand(() => CreatedChapters.TrimStart(TextToTrim, IsTrimExactText, IsTrimCharsFromText, IsTrimCaseSensitive));
        TrimEnd = new RelayCommand(() => CreatedChapters.TrimEnd(TextToTrim, IsTrimExactText, IsTrimCharsFromText, IsTrimCaseSensitive));
        ReplaceInTitles = new RelayCommand(() => CreatedChapters.Replace(ReplaceWhatText, ReplaceWithText, IsReplaceCaseSensitive));
        AddToStart = new RelayCommand(() => CreatedChapters.AddToStart(TextToAdd, TextToAddSequenceStartValue, TextToAddSequenceStart.Length));
        AddToEnd = new RelayCommand(() => CreatedChapters.AddToEnd(TextToAdd, TextToAddSequenceStartValue, TextToAddSequenceStart.Length));
    }

    #region Initialization of Chapter Sources and the Selected Chapter Source
    private static IEnumerable<ChapterSourceItem> GetChapterSources(IReadOnlyList<IMediaFileViewModel> files)
    {
        yield return new ChapterSourceItem { SourceType = ChapterSourceType.FileNames, Description = "File Names" };
        if (TagsExist(files))
            yield return new ChapterSourceItem { SourceType = ChapterSourceType.MetadataTags, Description = "Metadata Tags" };
        yield return new ChapterSourceItem { SourceType = ChapterSourceType.Template, Description = "Template" };
        yield return new ChapterSourceItem { SourceType = ChapterSourceType.SilenceScan, Description = "Silence Scan" };
        if (ChaptersExist(files))
            yield return new ChapterSourceItem { SourceType = ChapterSourceType.Existing, Description = "Existing Chapters" };
    }

    private static bool TagsExist(IReadOnlyList<IMediaFileViewModel> files)
    {
        foreach (var file in files)
        {
            if (file.HasTags)
                return true;
        }

        return false;
    }

    private static bool ChaptersExist(IReadOnlyList<IMediaFileViewModel> files)
    {
        foreach (var file in files)
        {
            if (file.Chapters.Count > 0)
                return true;
        }

        return false;
    }

    private void SetInitialSelectedChapterSource()
    {
        SelectedChapterSource = ChapterSources[0];
    }
    #endregion

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
            case ChapterSourceType.SilenceScan:
            case ChapterSourceType.Unknown:
            default: break;
        }
    }

    #region Create from Silence Scan
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
    #endregion

    #region Create from File Names
    private void CreateChaptersFromFileNames()
    {
        var chapters = CreateChapters(GetTitleFromFileName);
        CreatedChapters.Clear();
        foreach (var chapter in chapters)
            CreatedChapters.Add(chapter);
    }

    private string GetTitleFromFileName(IMediaFileViewModel file, int _)
    {
        var title = Path.GetFileNameWithoutExtension(file.File.Name);
        return TrimStartingNonChars ? title.TrimStartNonChars() : title;
    }
    #endregion

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

    private string GetTitleFromTags(IMediaFileViewModel file, int _)
    {
        var title = file.Tags.GetTagValue(SelectedTagName);
        return TrimStartingNonChars ? title.TrimStartNonChars() : title;
    }

    private void CreateChaptersFromTemplate()
    {
        var chapters = CreateChapters(GetTitleFromTemplate);
        CreatedChapters.Clear();
        foreach (var chapter in chapters)
            CreatedChapters.Add(chapter);
    }

    private string GetTitleFromTemplate(IMediaFileViewModel _, int index) => IsTemplateStartNumberValid
        ? Template.Replace("{}", (TemplateStartNumberValue + index).ToString(new string('0', TemplateStartNumber.Length)))
        : Template;

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