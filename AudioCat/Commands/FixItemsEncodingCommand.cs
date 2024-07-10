using System.Collections.ObjectModel;
using System.Windows.Controls;
using AudioCat.Models;
using AudioCat.ViewModels;

namespace AudioCat.Commands;

public sealed class FixItemsEncodingCommand : CommandBase
{
    protected override Task<IResponse<object>> Command(object? parameter)
    {
        if (parameter is not DataGrid dataGrid || dataGrid.Items.Count == 0)
            return Task.FromResult(Response<object>.Success());

        switch (dataGrid.ItemsSource)
        {
            case ObservableCollection<IMediaTagViewModel> outputTags:
                FixTags(outputTags);
                break;
            case ObservableCollection<IMediaChapterViewModel> outputChapters:
                FixChapters(outputChapters);
                break;
        }

        return Task.FromResult(Response<object>.Success());
    }

    private static void FixTags(ObservableCollection<IMediaTagViewModel> outputTags)
    {
        foreach (var tag in outputTags)
        {
            tag.Name = Encodings.Win1251.GetString(Encodings.Iso8859.GetBytes(tag.Name));
            tag.Value = Encodings.Win1251.GetString(Encodings.Iso8859.GetBytes(tag.Value));
        }
    }

    private static void FixChapters(ObservableCollection<IMediaChapterViewModel> outputChapters)
    {
        foreach (var chapter in outputChapters)
            chapter.Title = Encodings.Win1251.GetString(Encodings.Iso8859.GetBytes(chapter.Title));
    }
}