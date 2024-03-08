using AudioCat.Commands;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace AudioCat;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider ServiceProvider { get; } =
        new ServiceCollection()
            .AddSingleton<IAudioFilesContainer, AudioFilesContainer>()
            .AddSingleton<IAudioFileService, FFmpegService>()
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

    private class AudioFilesContainer : IAudioFilesContainer
    {
        public ObservableCollection<IAudioFile> Files { get; } = [];
        public IAudioFile? SelectedFile { get; set; }
    }
}