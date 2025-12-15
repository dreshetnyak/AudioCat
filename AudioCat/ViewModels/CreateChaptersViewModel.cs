using AudioCat.Commands;
using AudioCat.Models;
using AudioCat.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace AudioCat.ViewModels;

public enum ChapterSourceType { Unknown, FileNames, MetadataTags, CueFiles, Template, Existing, SilenceScan }
public sealed class ChapterSourceItem
{
    public ChapterSourceType SourceType { get; init; } = ChapterSourceType.Unknown;
    public string Description { get; init; } = "";
}

public sealed class CreateChaptersViewModel : ISilenceScanArgs, INotifyPropertyChanged
{
    private const int DEFAULT_SEQUENCE_START = 1;

    #region Backing Fields

    private string _selectedTagName;
    private string _template;
    private int _silenceThreshold;
    private int _silenceDuration;

    #endregion

    public IReadOnlyList<IMediaFileViewModel> Files { get; }

    public ObservableCollection<ChapterSourceItem> ChapterSources { get; }

    public ChapterSourceItem SelectedChapterSource
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FileNamesOptionsVisibility));
            OnPropertyChanged(nameof(MetadataTagsOptionsVisibility));
            OnPropertyChanged(nameof(CueFilesVisibility));
            OnPropertyChanged(nameof(TemplateOptionsVisibility));
            OnPropertyChanged(nameof(ExistingChaptersOptionsVisibility));
            OnPropertyChanged(nameof(SilenceScanOptionsVisibility));
            OnPropertyChanged(nameof(IsGenerateEnabled));
            OnPropertyChanged(nameof(OptionsVisibility));
        }
    } = new() { SourceType = ChapterSourceType.Unknown, Description = "" };

    #region Options Visibility
    public Visibility FileNamesOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.FileNames ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MetadataTagsOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.MetadataTags ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CueFilesVisibility => SelectedChapterSource.SourceType == ChapterSourceType.CueFiles ? Visibility.Visible : Visibility.Collapsed;
    public Visibility TemplateOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.Template ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ExistingChaptersOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.Existing ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SilenceScanOptionsVisibility => SelectedChapterSource.SourceType == ChapterSourceType.SilenceScan ? Visibility.Visible : Visibility.Collapsed;
    public Visibility OptionsVisibility => SelectedChapterSource.SourceType != ChapterSourceType.FileNames && SelectedChapterSource.SourceType != ChapterSourceType.Existing ? Visibility.Visible : Visibility.Collapsed;
    #endregion

    public bool TrimStartingNonChars
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

    #region Cue Files Options
    public ObservableCollection<Cue.ICue> CueFiles { get; } = [];

    public int SelectedCueFileIndex
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanMoveCueUp));
            OnPropertyChanged(nameof(CanMoveCueDown));
            OnPropertyChanged(nameof(CanRemoveCue));
        }
    }

    public bool CanMoveCueUp => SelectedCueFileIndex > 0;
    public bool CanMoveCueDown => CueFiles.Count > 0 && SelectedCueFileIndex < CueFiles.Count - 1;
    public bool CanRemoveCue => CueFiles.Count > 0 && SelectedCueFileIndex < CueFiles.Count;

    public ICommand GetCueFiles { get; }
    public ICommand MoveCueFile { get; }
    public ICommand ClearCueFiles { get; }

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
        get;
        set
        {
            if (value == field)
                return;
            field = value;
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
    } = DEFAULT_SEQUENCE_START.ToString();

    public int TemplateStartNumberValue { get; set; } = DEFAULT_SEQUENCE_START;

    public bool IsTemplateStartNumberValid
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = true;

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
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = Visibility.Hidden;

    public Visibility SilenceScanButtonVisibility
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = Visibility.Visible;

    public Visibility CancelSilenceScanButtonVisibility
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = Visibility.Hidden;

    #endregion

    #region Modifying the Chapters

    public string TextToTrim
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
            IsTrimEnabled = !string.IsNullOrEmpty(value);
        }
    } = "";

    public bool IsTrimExactText
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = true;

    public bool IsTrimCharsFromText
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

    public bool IsTrimCaseSensitive
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

    public bool IsTrimEnabled
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

    public ICommand TrimStart { get; }
    public ICommand TrimEnd { get; }

    public string ReplaceWhatText
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
            IsReplaceEnabled = !string.IsNullOrEmpty(value);
        }
    } = "";

    public string ReplaceWithText
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

    public bool IsReplaceCaseSensitive
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

    public bool IsReplaceEnabled
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

    public ICommand ReplaceInTitles { get; }

    public string TextToAdd
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
            IsAddEnabled = !string.IsNullOrEmpty(value);
        }
    } = "";

    public string TextToAddSequenceStart
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
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
    } = "";

    public bool IsTextToAddSequenceStartValid
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = true;

    public int TextToAddSequenceStartValue
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = DEFAULT_SEQUENCE_START;

    public bool IsAddEnabled
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

    public ICommand AddToStart { get; }
    public ICommand AddToEnd { get; }
    #endregion
    
    public ObservableCollection<IMediaChapterViewModel> CreatedChapters { get; }

    public bool IsExistingChaptersEnabled { get; }
    public bool IsUseCreatedEnabled => CreatedChapters.Count > 0;

    public bool IsUserInputEnabled
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = true;

    public bool IsGenerateEnabled => SelectedChapterSource.SourceType switch
    {
        ChapterSourceType.FileNames or ChapterSourceType.MetadataTags or ChapterSourceType.CueFiles or ChapterSourceType.Template or ChapterSourceType.Existing or ChapterSourceType.SilenceScan => true,
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
        _selectedTagName = Settings.ChapterWizard.DefaultSelectedTag;
        _template = Settings.ChapterWizard.DefaultTemplate;
        _silenceThreshold = Settings.ChapterWizard.DefaultSilenceThreshold;
        _silenceDuration = Settings.ChapterWizard.DefaultAudioThreshold;

        ChapterSources = new ObservableCollection<ChapterSourceItem>(GetChapterSources(files));
        SetInitialSelectedChapterSource(files);
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

        var getQueueFile = new GetCueFileCommand();
        getQueueFile.Finished += OnGetQueueFileFinished;
        GetCueFiles = getQueueFile;
        MoveCueFile = new RelayParameterCommand(OnMoveQueueFile);
        ClearCueFiles = new RelayCommand(CueFiles.Clear);

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
        if (files.TagsExist())
            yield return new ChapterSourceItem { SourceType = ChapterSourceType.MetadataTags, Description = "Metadata Tags" };
        yield return new ChapterSourceItem { SourceType = ChapterSourceType.CueFiles, Description = "Cue Files" };
        yield return new ChapterSourceItem { SourceType = ChapterSourceType.Template, Description = "Template" };
        yield return new ChapterSourceItem { SourceType = ChapterSourceType.SilenceScan, Description = "Silence Scan" };
        if (files.ChaptersExist())
            yield return new ChapterSourceItem { SourceType = ChapterSourceType.Existing, Description = "Existing Chapters" };
    }

    private void SetInitialSelectedChapterSource(ObservableCollection<IMediaFileViewModel> files)
    {
        foreach (var file in files)
        {
            if (file.Chapters.Count <= 0) 
                continue;
            if (TryGetChaptersSource(ChapterSourceType.Existing, out var chaptersSource))
            {
                SelectedChapterSource = chaptersSource!;
                return;
            }
            break;
        }

        SelectedChapterSource = ChapterSources[0];
    }

    private bool TryGetChaptersSource(ChapterSourceType chapterSourceType, out ChapterSourceItem? chapterSource)
    {
        foreach (var source in ChapterSources)
        {
            if (source.SourceType != chapterSourceType) 
                continue;
            chapterSource = source;
            return true;
        }

        chapterSource = null;
        return false;
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

    #region Chapters Generation
    private void OnGenerateChapters()
    {
        switch (SelectedChapterSource.SourceType)
        {
            case ChapterSourceType.FileNames: 
                UpdateChapters(ChaptersFactory.CreateFromFileNames(Files, TrimStartingNonChars)); 
                break;
            case ChapterSourceType.MetadataTags:
                if (TryGetSelectedTagNameOrDefault(out var selectedTagName))
                    UpdateChapters(ChaptersFactory.CreateFromMetadataTags(Files, selectedTagName, TrimStartingNonChars));
                else
                    CreatedChapters.Clear();
                break;
            case ChapterSourceType.CueFiles:
                UpdateChapters(ChaptersFactory.CreateFromCueFiles(Files, CueFiles));
                break;
            case ChapterSourceType.Template:
                UpdateChapters(ChaptersFactory.CreateFromTemplate(Files, Template, TemplateStartNumberValue, TemplateStartNumber, IsTemplateStartNumberValid));
                break;
            case ChapterSourceType.Existing: 
                UpdateChapters(ChaptersFactory.CreateFromExisting(Files, TrimStartingNonChars));
                break;
            case ChapterSourceType.SilenceScan:
            case ChapterSourceType.Unknown:
            default: 
                break;
        }
    }

    private void UpdateChapters(IReadOnlyList<IMediaChapterViewModel> newChapters)
    {
        CreatedChapters.Clear();
        if (newChapters.Count == 0)
            return;
        foreach (var newChapter in newChapters)
            CreatedChapters.Add(newChapter);
    }
    #endregion

    #region Create from Cue Files
    private void OnMoveQueueFile(object? parameter)
    {
        if (parameter is not string direction)
            return;
        var currentIndex = SelectedCueFileIndex;
        switch (direction)
        {
            case "Up" when currentIndex <= 0:
                return;
            case "Up":
            {
                var newIndex = currentIndex - 1;
                CueFiles.Move(currentIndex, newIndex);
                SelectedCueFileIndex = newIndex;
                break;
            }
            case "Down" when currentIndex >= CueFiles.Count - 1:
                return;
            case "Down":
            {
                var newIndex = currentIndex + 1;
                CueFiles.Move(currentIndex, newIndex);
                SelectedCueFileIndex = newIndex;
                break;
            }
            case "Remove" when currentIndex < 0 || currentIndex >= CueFiles.Count:
                return;
            case "Remove":
            {
                CueFiles.RemoveAt(currentIndex);
                SelectedCueFileIndex = currentIndex < CueFiles.Count ? currentIndex : CueFiles.Count - 1;
                break;
            }
        }
    }

    private void OnGetQueueFileFinished(object sender, ResponseEventArgs eventArgs)
    {
        CueFiles.Clear();
        if (eventArgs.Response.IsFailure || eventArgs.Response.Data is not ReadOnlyCollection<Cue.ICue> cueFiles || cueFiles.Count == 0)
            return;
        foreach (var cueFile in cueFiles) 
            CueFiles.Add(cueFile);
    }
    #endregion

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
                UpdateChapters(ChaptersFactory.CreateFromIntervals((IReadOnlyList<IInterval>)response.Data));
        }
        finally
        {
            SilenceScanProgressVisibility = Visibility.Hidden;
            SilenceScanButtonVisibility = Visibility.Visible;
            IsUserInputEnabled = true;
        }
    }
    #endregion

    private bool TryGetSelectedTagNameOrDefault(out string selectedTagName)
    {
        if (!string.IsNullOrEmpty(SelectedTagName))
        {
            selectedTagName = SelectedTagName;
            return true;
        }

        if (!TagNames.Has(Settings.ChapterWizard.DefaultSelectedTag))
        {
            selectedTagName = "";
            return false;
        }

        SelectedTagName = Settings.ChapterWizard.DefaultSelectedTag;
        selectedTagName = SelectedTagName;
        return true;
    }

    private void OnClose() => Close?.Invoke(this, EventArgs.Empty);

    private void OnUseCreated() => UseCreated?.Invoke(this, EventArgs.Empty);

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}