﻿using System.ComponentModel;
using System.Windows;
using AudioCat.ViewModels;

namespace AudioCat.Windows;

/// <summary>
/// Interaction logic for CreateChaptersWindow.xaml
/// </summary>
public partial class CreateChaptersWindow : Window
{
    public CreateChaptersWindow(CreateChaptersViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Close += (_, _) => Close();
        viewModel.UseCreated += (_, _) => { DialogResult = true; Close(); };
        DataContext = viewModel;
        Owner = Application.Current.MainWindow;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is CreateChaptersViewModel { IsUserInputEnabled: false } viewModel) 
            viewModel.CancelScanForSilence.Execute(null);
    }
}