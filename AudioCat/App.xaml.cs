using AudioCat.Commands;
using AudioCat.FFmpeg;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace AudioCat;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider ServiceProvider { get; } =
        new ServiceCollection()
            .AddSingleton<IMediaFilesContainer, MediaFilesContainer>()
            .AddSingleton<IMediaFileToolkitService, FFmpegService>()
            .AddSingleton<IMediaFilesService, MediaFilesService>()
            .AddSingleton<AddFilesCommand>()
            .AddSingleton<AddPathCommand>()
            .AddSingleton<MoveFileCommand>()
            .AddSingleton<ConcatenateCommand>()
            .AddSingleton<MainWindow>()
            .AddSingleton<MainViewModel>()
            .BuildServiceProvider();

    private void OnStartup(object sender, StartupEventArgs e)
    {
        ServiceProvider.GetService<MainWindow>()?.Show();
    }

    private sealed class MediaFilesContainer : IMediaFilesContainer, INotifyPropertyChanged
    {
        private IMediaFileViewModel? _selectedFile;
        public ObservableCollection<IMediaFileViewModel> Files { get; } = [];
        public IMediaFileViewModel? SelectedFile
        {
            get => _selectedFile;
            set
            {
                _selectedFile = value;
                OnPropertyChanged();
            }
        }

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion
    }
}