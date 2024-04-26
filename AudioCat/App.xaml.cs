﻿using AudioCat.Commands;
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
            .AddSingleton<IMediaFileService, FFmpegService>()
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
        private MediaFileViewModel? _selectedFile;
        public ObservableCollection<MediaFileViewModel> Files { get; } = [];
        public MediaFileViewModel? SelectedFile
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