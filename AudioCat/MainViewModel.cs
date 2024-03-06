using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
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
            concatenate.Output += OnOutput;
            
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
            IsUserEntryEnabled = true;
        }

        private void OnOutput(object sender, MessageEventArgs eventArgs)
        {
            ProgressText = eventArgs.Message;
        }

        private void OnFilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsConcatenateEnabled));
            OnPropertyChanged(nameof(IsClearPathsEnabled));
            OnPropertyChanged(nameof(IsMoveUpEnabled));
            OnPropertyChanged(nameof(IsMoveDownEnabled));
            OnPropertyChanged(nameof(IsRemoveEnabled));
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
