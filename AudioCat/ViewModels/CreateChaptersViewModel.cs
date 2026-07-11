using AudioCat.Commands;
using AudioCat.Controls;
using AudioCat.Models;
using AudioCat.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AudioCat.ViewModels;

public enum ChapterSourceType { Unknown, FileNames, MetadataTags, CueFiles, Template, Existing, SilenceScan }
public sealed class ChapterSourceItem
{
    public ChapterSourceType SourceType { get; init; } = ChapterSourceType.Unknown;
    public string Description { get; init; } = "";
}

public sealed class CreateChaptersViewModel : ISilenceScanArgs, INotifyPropertyChanged, IDisposable
{
    private const int DEFAULT_SEQUENCE_START = 1;

    #region Backing Fields

    private string _selectedTagName;
    private string _template;
    private int _silenceThreshold;
    private int _silenceDuration;

    #endregion

    public ReadOnlyCollection<IMediaFileViewModel> Files { get; }

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

    #region Playback
    public int PlaybackCapacity
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
    public ObservableCollection<float> WaveformSamples { get; } = [];
    public ObservableCollection<IStripBookmark> PlaybackBookmarks { get; } = [];
    public int CurrentPosition
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
    public ZoomRange ZoomRange
    {
        get;
        set
        {
            if (value == field) 
                return;
            field = value;
            OnPropertyChanged();
        }
    } = ZoomRange.Sentinel;

    public bool IsPlayerEnabled
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

    // True when at least one playable (non-image, duration-bearing) file exists and none of them use a codec
    // from Settings.PlaybackUnsupportedCodecs. Computed once at construction from the already-stored probe
    // data on Streams — no ffmpeg/ffprobe/NAudio calls. Transport entry points no-op when false, as defense
    // in depth behind the disabled player UI.
    private bool IsPlaybackSupported { get; }

    // Collapses the waveform strip for unsupported formats, giving the space back to the chapter list.
    // IsPlaybackSupported never changes after construction, so no change notification is needed.
    public Visibility WaveformVisibility => IsPlaybackSupported ? Visibility.Visible : Visibility.Collapsed;

    public bool IsPlaying
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

    public bool IsMuted
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
            ApplyVolumeToEngine();
        }
    }

    public float Volume
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
            ApplyVolumeToEngine();
        }
    } = 0.75f;

    private void ApplyVolumeToEngine() => ChaptersPlayer.Volume = IsMuted ? 0f : Volume;

    public bool CanRewind
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = false;

    public bool CanForward
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = false;

    public bool CanGoPrevious
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = false;

    public bool CanGoNext
    {
        get;
        set
        {
            if (value == field)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = false;

    public TimeSpan PlayerPosition
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

    public TimeSpan PlayerDuration
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

    private ChaptersPlayer ChaptersPlayer { get; }

    /// <summary>Waveform resolution of the strip: one peak per 1/10 of a second. Must match WaveformPeaksGenerator.</summary>
    private const int WAVEFORM_PEAKS_PER_SECOND = 10;

    // Cancels the fire-and-forget waveform generation when the wizard closes. Cancelled and disposed in Dispose,
    // before the engine — no cache is kept, so reopening the wizard regenerates the waveform from scratch.
    private CancellationTokenSource WaveformCancellation { get; } = new();

    private ScanForSilenceCommand ScanForSilenceCommand { get; }

    // Engine events arrive on the player's poller thread; every handler marshals through this dispatcher
    // before touching bound properties.
    private Dispatcher Dispatcher { get; }

    // A pending play origin armed from the waveform strip (Phase 2). Always null in Phase 1; consumed
    // (and cleared) by PlayPause when starting from the Stopped state.
    private TimeSpan? ArmedStripPosition { get; set; }

    // Latest global timeline position reported by the engine; the engine exposes no position getter,
    // so seek-relative commands are computed from this. Only touched on the UI thread.
    private TimeSpan LastKnownGlobalPosition { get; set; }

    // The chapter derived from the latest engine position; null while stopped or in a gap between chapters.
    private IMediaChapterViewModel? ActivePlaybackChapter { get; set; }

    // The chapter currently flagged with IsPlaying for the grid's playing-row indicator. Tracked here so
    // flag updates never scan the chapters collection. Only touched on the UI thread (marshaled handlers).
    private IMediaChapterViewModel? FlaggedPlaybackChapter { get; set; }

    public ICommand PlayPause { get; }
    public ICommand StopPlayback { get; }
    public ICommand PreviousChapter { get; }
    public ICommand RewindPlayback { get; }
    public ICommand ForwardPlayback { get; }
    public ICommand NextChapter { get; }

    #endregion

    public ObservableCollection<IMediaChapterViewModel> CreatedChapters { get; }

    public IMediaChapterViewModel? SelectedCreatedChapter
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
        ReadOnlyCollection<IMediaFileViewModel> files, 
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

        CreatedChapters = [];
        CreatedChapters.CollectionChanged += OnCreatedChaptersChanged;

        Dispatcher = System.Windows.Application.Current.Dispatcher;

        ChaptersPlayer = new ChaptersPlayer(files, CreatedChapters);
        ChaptersPlayer.StateChanged += OnPlayerStateChanged;
        ChaptersPlayer.PositionChanged += OnPlayerPositionChanged;
        ChaptersPlayer.ChapterChanged += OnPlayerChapterChanged;
        ChaptersPlayer.PlaybackError += OnPlayerPlaybackError;
        PlayerDuration = ChaptersPlayer.TotalDuration;
        ApplyVolumeToEngine();

        IsPlaybackSupported = GetIsPlaybackSupported(files);
        IsPlayerEnabled = IsPlaybackSupported;
        PlaybackCapacity = GetPlaybackCapacity(files);

        PlayPause = new RelayCommand(OnPlayPause);
        StopPlayback = new RelayCommand(OnStopPlayback);
        PreviousChapter = new RelayCommand(OnPreviousChapter);
        RewindPlayback = new RelayCommand(OnRewindPlayback);
        ForwardPlayback = new RelayCommand(OnForwardPlayback);
        NextChapter = new RelayCommand(OnNextChapter);

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
        ScanForSilenceCommand = scanForSilence;

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

        // Must run after every property above is initialized, and on the UI thread since CreatedChapters is bound to the DataGrid
        OnGenerateChapters();
        RebuildPlaybackBookmarks();

        // Waveform generation is fire-and-forget: never awaited on the UI thread, never gates any playback
        // feature, and failures stay silent (the generator goes quiet and the strip simply stays partial).
        if (IsPlaybackSupported && PlaybackCapacity > 0)
            _ = WaveformPeaksGenerator.Generate(Files, DeliverWaveformBatch, WaveformCancellation.Token);
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

    private void SetInitialSelectedChapterSource(IReadOnlyList<IMediaFileViewModel> files)
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

    private void OnCreatedChaptersChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsUseCreatedEnabled));

        RebuildPlaybackBookmarks();

        // Chapter regeneration stops playback inside the engine (it also listens to CollectionChanged);
        // here we only clear the playback UI leftovers the engine does not report on its stop path.
        ArmedStripPosition = null;
        if (IsPlaying)
            IsPlaying = false;
        if (PlayerPosition != TimeSpan.Zero)
            PlayerPosition = TimeSpan.Zero;
        if (CurrentPosition != 0)
            CurrentPosition = 0;
    }

    #region Playback Wiring
    private void OnPlayerStateChanged(object? sender, AudioPlayerState state) =>
        Dispatcher.BeginInvoke(() =>
        {
            IsPlaying = state == AudioPlayerState.Playing;
            var isActive = state != AudioPlayerState.Stopped;
            CanRewind = isActive;
            CanForward = isActive;

            // The engine does not emit a final PositionChanged/ChapterChanged on its stop paths
            // (explicit stop, end of timeline, error, chapter regeneration) — clear the UI state here
            if (state == AudioPlayerState.Stopped)
            {
                PlayerPosition = TimeSpan.Zero;
                LastKnownGlobalPosition = TimeSpan.Zero;
                CurrentPosition = 0;
                ActivePlaybackChapter = null;
                SetPlayingFlag(null);
            }
            UpdateChapterNavigation(state);
        });

    private void OnPlayerPositionChanged(object? sender, ChaptersPlayerPositionEventArgs e) =>
        Dispatcher.BeginInvoke(() =>
        {
            // The strip indicator follows every engine position report, including seeks made while
            // stopped (the engine fires PositionChanged for those), so it sits above the Stopped guard.
            CurrentPosition = (int)(e.GlobalPosition.TotalSeconds * WAVEFORM_PEAKS_PER_SECOND);
            if (ChaptersPlayer.State == AudioPlayerState.Stopped)
                return; // A stale tick queued around a stop must not resurrect the position readout
            LastKnownGlobalPosition = e.GlobalPosition;
            ActivePlaybackChapter = e.ActiveChapter;
            PlayerPosition = e.GlobalPosition;
            UpdateChapterNavigation(ChaptersPlayer.State);
        });

    private void OnPlayerChapterChanged(object? sender, IMediaChapterViewModel? chapter) =>
        Dispatcher.BeginInvoke(() =>
        {
            if (ChaptersPlayer.State == AudioPlayerState.Stopped)
                return;
            ActivePlaybackChapter = chapter;
            SetPlayingFlag(chapter); // Null chapter means a gap between chapters — no row is flagged
            UpdateChapterNavigation(ChaptersPlayer.State);
        });

    // Moves the IsPlaying flag between chapter rows. Must be called on the UI thread only.
    private void SetPlayingFlag(IMediaChapterViewModel? chapter)
    {
        if (ReferenceEquals(FlaggedPlaybackChapter, chapter))
            return;
        if (FlaggedPlaybackChapter != null)
            FlaggedPlaybackChapter.IsPlaying = false;
        FlaggedPlaybackChapter = chapter;
        if (chapter != null)
            chapter.IsPlaying = true;
    }

    // Previous/Next availability follows the engine position: Previous needs a chapter start strictly
    // before the active chapter's start (in a gap between chapters — before the current position),
    // Next needs one strictly after the current position. Mirrors the seek-target selection in
    // OnPreviousChapter/OnNextChapter. Must be called on the UI thread only.
    private void UpdateChapterNavigation(AudioPlayerState state)
    {
        if (state == AudioPlayerState.Stopped)
        {
            CanGoPrevious = false;
            CanGoNext = false;
            return;
        }

        var position = LastKnownGlobalPosition;
        var previousThreshold = ActivePlaybackChapter?.StartTime ?? position;
        var hasPrevious = false;
        var hasNext = false;
        foreach (var chapter in CreatedChapters)
        {
            if (chapter.StartTime is not { } start)
                continue;
            if (start < previousThreshold)
                hasPrevious = true;
            if (start > position)
                hasNext = true;
        }
        CanGoPrevious = hasPrevious;
        CanGoNext = hasNext;
    }

    private void OnPlayerPlaybackError(object? sender, string message) =>
        Dispatcher.BeginInvoke(() =>
        {
            // The engine already stops itself before raising this; on most error paths its
            // StateChanged(Stopped) handler has cleared the transport UI. The exception is a
            // play-from-Stopped failure, where no state transition occurs — reset here to
            // match the Stop command before surfacing the message.
            IsPlaying = false;
            PlayerPosition = TimeSpan.Zero;
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        });

    private void OnPlayPause()
    {
        if (!IsPlaybackSupported)
            return;
        switch (ChaptersPlayer.State)
        {
            case AudioPlayerState.Stopped:
                ChaptersPlayer.Play(GetPlayOrigin());
                break;
            case AudioPlayerState.Playing:
                ChaptersPlayer.Pause();
                break;
            case AudioPlayerState.Paused:
                ChaptersPlayer.Resume();
                break;
        }
    }

    // Play-origin precedence when starting from the Stopped state:
    // armed strip position (cleared on use) → selected chapter → first chapter with a start time → timeline start
    private TimeSpan GetPlayOrigin()
    {
        if (ArmedStripPosition is { } armedPosition)
        {
            ArmedStripPosition = null;
            return armedPosition;
        }

        if (SelectedCreatedChapter?.StartTime is { } selectedStart)
            return selectedStart;

        foreach (var chapter in CreatedChapters)
        {
            if (chapter.StartTime is { } start)
                return start;
        }

        return TimeSpan.Zero;
    }

    private void OnStopPlayback()
    {
        ChaptersPlayer.Stop();
        PlayerPosition = TimeSpan.Zero;
    }

    private void OnPreviousChapter()
    {
        if (!IsPlaybackSupported || ChaptersPlayer.State == AudioPlayerState.Stopped)
            return;

        // More than 3 seconds into the active chapter — restart it
        var activeChapter = ActivePlaybackChapter;
        var position = LastKnownGlobalPosition;
        if (activeChapter?.StartTime is { } activeStart && position - activeStart > TimeSpan.FromSeconds(3))
        {
            ChaptersPlayer.Seek(activeStart);
            return;
        }

        // Otherwise the closest chapter start strictly before the active chapter's start
        // (or, in a gap with no active chapter, strictly before the current position)
        var threshold = activeChapter?.StartTime ?? position;
        TimeSpan? previousStart = null;
        foreach (var chapter in CreatedChapters)
        {
            if (chapter.StartTime is { } start && start < threshold && (previousStart is null || start > previousStart))
                previousStart = start;
        }

        ChaptersPlayer.Seek(previousStart ?? TimeSpan.Zero);
    }

    private void OnNextChapter()
    {
        if (!IsPlaybackSupported || ChaptersPlayer.State == AudioPlayerState.Stopped)
            return;

        // The closest chapter start strictly after the current position; no-op when none exists
        var position = LastKnownGlobalPosition;
        TimeSpan? nextStart = null;
        foreach (var chapter in CreatedChapters)
        {
            if (chapter.StartTime is { } start && start > position && (nextStart is null || start < nextStart))
                nextStart = start;
        }

        if (nextStart is { } target)
            ChaptersPlayer.Seek(target);
    }

    private void OnRewindPlayback() => SeekRelative(TimeSpan.FromSeconds(-10));

    private void OnForwardPlayback() => SeekRelative(TimeSpan.FromSeconds(10));

    private void SeekRelative(TimeSpan offset)
    {
        if (!IsPlaybackSupported || ChaptersPlayer.State == AudioPlayerState.Stopped)
            return;
        // The engine clamps the target to [Zero, TotalDuration) and crosses file boundaries transparently
        ChaptersPlayer.Seek(LastKnownGlobalPosition + offset);
    }

    /// <summary>
    /// Starts playback from the given chapter's start regardless of the current state
    /// (an active playback is stopped and restarted by the engine). Chapters without
    /// a start time are ignored. Invoked from the view on chapter row double-click.
    /// </summary>
    public void PlayFromChapter(IMediaChapterViewModel chapter)
    {
        if (!IsPlaybackSupported)
            return;
        if (chapter.StartTime is not { } start)
            return;
        ArmedStripPosition = null;
        ChaptersPlayer.Play(start);
    }

    /// <summary>
    /// Handles a click on the waveform strip. Invoked from the view. While playing the engine
    /// seeks and playback continues; while paused the engine seeks and stays paused (position
    /// and indicator move, no audio); while stopped no device is touched — the position is
    /// only armed as the next play origin and the strip indicator moves to the armed spot.
    /// </summary>
    public void HandleStripPositionRequest(int index)
    {
        if (!IsPlaybackSupported)
            return;

        var time = TimeSpan.FromSeconds(index / (double)WAVEFORM_PEAKS_PER_SECOND);
        switch (ChaptersPlayer.State)
        {
            case AudioPlayerState.Playing:
            case AudioPlayerState.Paused:
                ChaptersPlayer.Seek(time);
                break;
            case AudioPlayerState.Stopped:
                // Engine position/chapter events are ignored while stopped, so the indicator
                // is moved directly here instead of relying on an engine round-trip
                ArmedStripPosition = time;
                CurrentPosition = index;
                break;
        }
    }

    // The strip's logical width in peaks. Summed per file with the exact skip rules and per-file rounding
    // WaveformPeaksGenerator uses (round of the summed total duration could drift off the sum of per-file
    // rounds by a peak per file boundary), so capacity always equals the generator's total output count
    // and the global index mapping "index = seconds × 10" stays aligned.
    private static int GetPlaybackCapacity(IEnumerable<IMediaFileViewModel> files)
    {
        var capacity = 0;
        foreach (var file in files)
        {
            if (file.IsImage || file.Duration is not { } duration)
                continue;
            var peaksCount = (int)Math.Round(duration.TotalSeconds * WAVEFORM_PEAKS_PER_SECOND);
            if (peaksCount > 0)
                capacity += peaksCount;
        }

        return capacity;
    }

    // A chapter-start marker on the waveform strip. Index math matches the strip resolution: seconds × 10.
    private sealed class StripBookmark : IStripBookmark
    {
        public int Index { get; init; }
        public string Description { get; init; } = "";
    }

    // Rebuilds the strip's chapter-start markers from every chapter that has a start time.
    // CreatedChapters mutates only on the UI thread, so no marshaling is needed here.
    private void RebuildPlaybackBookmarks()
    {
        PlaybackBookmarks.Clear();
        foreach (var chapter in CreatedChapters)
        {
            if (chapter.StartTime is { } start)
                PlaybackBookmarks.Add(new StripBookmark { Index = (int)(start.TotalSeconds * WAVEFORM_PEAKS_PER_SECOND), Description = chapter.Title });
        }
    }

    // Invoked by the waveform generator on its worker thread; a single dispatcher operation appends
    // the whole batch to the bound collection, in order — the strip renders left-to-right as data arrives.
    private void DeliverWaveformBatch(IReadOnlyList<float> batch) =>
        Dispatcher.BeginInvoke(() =>
        {
            foreach (var peak in batch)
                WaveformSamples.Add(peak);
        });

    // Playback is supported iff there is at least one playable (non-image, duration-bearing) file
    // and no playable file's audio codec is in Settings.PlaybackUnsupportedCodecs.
    private static bool GetIsPlaybackSupported(IEnumerable<IMediaFileViewModel> files)
    {
        var hasPlayableFile = false;
        foreach (var file in files)
        {
            if (file.IsImage || file.Duration == null)
                continue;
            if (Settings.PlaybackUnsupportedCodecs.Contains(GetAudioCodecName(file.Streams)))
                return false;
            hasPlayableFile = true;
        }

        return hasPlayableFile;
    }

    // Mirrors MediaFilesService.GetCodecName: the first stream whose codec is a supported audio codec
    private static string GetAudioCodecName(IEnumerable<IMediaStream> mediaFileStreams)
    {
        foreach (var stream in mediaFileStreams)
        {
            if (Settings.SupportedAudioCodecs.Contains(stream.CodecName))
                return stream.CodecName ?? "";
        }

        return "";
    }
    #endregion

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
                UpdateChapters(ChaptersFactory.CreateFromCueFiles(Files, CueFiles.AsReadOnly()));
                break;
            case ChapterSourceType.Template:
                UpdateChapters(ChaptersFactory.CreateFromTemplate(Files, Template, TemplateStartNumberValue, TemplateStartNumber, IsTemplateStartNumberValid));
                break;

            // TODO: We need to load from the OutputChapters
            // Existing here just generates chapters from files, that is not entirely correct thing to do on startup

            case ChapterSourceType.Existing: 
                UpdateChapters(ChaptersFactory.CreateFromExisting(Files, TrimStartingNonChars));
                break;
            case ChapterSourceType.SilenceScan:
            case ChapterSourceType.Unknown:
            default: 
                break;
        }
    }

    private void UpdateChapters(ReadOnlyCollection<IMediaChapterViewModel> newChapters)
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
        // Replace the list only when new data actually arrived; dialog cancel, abort and
        // failure outcomes must leave the previously loaded cue files untouched
        if (eventArgs.Response.IsFailure || eventArgs.Response.Data is not ReadOnlyCollection<Cue.ICue> cueFiles || cueFiles.Count == 0)
            return;
        CueFiles.Clear();
        foreach (var cueFile in cueFiles)
            CueFiles.Add(cueFile);
    }
    #endregion

    #region Create from Silence Scan
    private void OnScanForSilenceStarting(object? sender, EventArgs eventArgs)
    {
        // Starting a silence scan stops any audio; the player UI itself is disabled by the
        // IsUserInputEnabled binding on the grid that contains the PlayerControl
        ChaptersPlayer.Stop();
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

            if (response.Data is IReadOnlyList<IInterval> intervals)
                UpdateChapters(ChaptersFactory.CreateFromIntervals(intervals));
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

    // ScanForSilenceCommand is an application-wide singleton; without the unsubscribe every closed wizard VM stays rooted by its event invocation lists
    public void Dispose()
    {
        ScanForSilenceCommand.Starting -= OnScanForSilenceStarting;
        ScanForSilenceCommand.Finished -= OnScanForSilenceFinished;
        WaveformCancellation.Cancel(); // Stop waveform generation before the engine goes away; cancel completes the generator task normally
        WaveformCancellation.Dispose();
        ChaptersPlayer.StateChanged -= OnPlayerStateChanged;
        ChaptersPlayer.PositionChanged -= OnPlayerPositionChanged;
        ChaptersPlayer.ChapterChanged -= OnPlayerChapterChanged;
        ChaptersPlayer.PlaybackError -= OnPlayerPlaybackError;
        ChaptersPlayer.Dispose();
    }

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}
