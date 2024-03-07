using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using AudioCat.Commands;

namespace AudioCat
{
    internal class AudioFile
    {
        public string Path { get; }
        public long Size { get; }
        public DateTime Date { get; }

        public AudioFile(string path)
        {
            Path = path;
            if (!File.Exists(path))
                return;
            var fileInfo = new FileInfo(path);
            Size = fileInfo.Length;
            Date = fileInfo.LastWriteTime;
        }
    }

    internal sealed class MainViewModel : IAudioFilesProvider, INotifyPropertyChanged
    {
        #region Backing Fields
        private bool _isUserEntryEnabled = true;
        private AudioFile _selectedFile = new("");
        private long _totalSize;
        private TimeSpan _totalDuration;
        private int _progressPercentage;
        private string _progressText = "";

        #endregion

        public ObservableCollection<AudioFile> Files { get; } = [];
        public AudioFile SelectedFile
        {
            get => _selectedFile;
            set
            {
                // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
                _selectedFile = value ?? new(""); // Can be null, although we don't assign it to null anywhere
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMoveUpEnabled));
                OnPropertyChanged(nameof(IsMoveDownEnabled));
            }
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

        public string TotalSizeText => GetTotalSizeText(TotalSize); 
        public string DurationText => $"{TotalDuration.TotalHours:00}:{TotalDuration.Minutes:00}:{TotalDuration.Seconds:00}";

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
        public bool IsMoveUpEnabled => IsUserEntryEnabled && Files.Count > 0 && SelectedFile.Path != "" && SelectedFile != Files.First();
        public bool IsMoveDownEnabled => IsUserEntryEnabled && Files.Count > 0 && SelectedFile.Path != "" && SelectedFile != Files.Last();
        public bool IsRemoveEnabled => IsUserEntryEnabled && Files.Count > 0;
        
        public ICommand Concatenate { get; }
        public ICommand Cancel { get; }
        public ICommand AddPath { get; }
        public ICommand AddFiles { get; }
        public ICommand ClearPaths { get; }
        public ICommand MoveSelected { get; }

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

        public MainViewModel()
        {
            AddFiles = new AddFilesCommand(Files);
            AddPath = new AddPathCommand(Files);
            ClearPaths = new RelayCommand(Files.Clear);
            MoveSelected = new MoveFileCommand(this);
            var concatenate = new ConcatenateCommand(Files);
            concatenate.Starting += OnStarting;
            concatenate.Finished += OnFinished;
            concatenate.StatusUpdate += OnStatusUpdate;
            
            Concatenate = concatenate;
            Cancel = new RelayCommand(concatenate.Cancel);

            Files.CollectionChanged += OnFilesCollectionChanged;
        }

        private void OnStarting(object? sender, EventArgs e)
        {
            ProgressPercentage = 10000;
            IsUserEntryEnabled = false;
        }

        private void OnFinished(object? sender, EventArgs e)
        {
            ProgressPercentage = 0;
            ProgressText = "Done.";
            IsUserEntryEnabled = true;
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
                sb.Append($"Time: {stats.Time.TotalHours:00}:{stats.Time.Minutes:00}:{stats.Time.Seconds:00}");
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
        
        private static string GetTotalSizeText(decimal size)
        {
            return size switch
            {
                < 1024 => $"{size:N0} B",
                < 1_048_576 => $"{size / 1024m:N0} KB",
                < 1_073_741_824 => $"{size / 1_048_576m:N1} MB",
                _ => $"{size / 1_073_741_824m:N2} GB"
            };
        }

        private long GetFilesTotalSize()
        {
            var totalSize = 0L;
            foreach (var file in Files) 
                totalSize += file.Size;
            return totalSize;
        }
        
        private TimeSpan GetTotalDuration()
        {
            TimeSpan totalDuration = default;
            //TODO
            //foreach (var file in Files)
            //    totalDuration.Add(file.);
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

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
