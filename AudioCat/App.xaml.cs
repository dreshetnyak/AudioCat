using AudioCat.Commands;
using AudioCat.FFmpeg;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
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
            .AddSingleton<FixItemEncodingCommand>()
            .AddSingleton<FixItemsEncodingCommand>()
            .AddSingleton<AddFilesCommand>()
            .AddSingleton<AddPathCommand>()
            .AddSingleton<MoveFileCommand>()
            .AddSingleton<ConcatenateCommand>()
            .AddSingleton<CreateChaptersCommand>()
            .AddSingleton<MainWindow>()
            .AddSingleton<MainViewModel>()
            .BuildServiceProvider();

    private void OnStartup(object sender, StartupEventArgs e)
    {
        try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); }
        catch {/* ignore */ }
        ServiceProvider.GetService<MainWindow>()?.Show();
    }
}