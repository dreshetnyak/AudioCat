using System.Windows.Controls;
using AudioCat.Models;
using AudioCat.ViewModels;

namespace AudioCat.Commands;

public class FixItemEncodingCommand : CommandBase
{
    protected override Task<IResponse<object>> Command(object? parameter)
    {
        if (parameter is not DataGrid dataGrid || dataGrid.Items.Count == 0)
            return Task.FromResult(Response<object>.Success());

        var selectedTagIndex = dataGrid.SelectedIndex;
        if (selectedTagIndex < 0 || selectedTagIndex >= dataGrid.Items.Count)
            return Task.FromResult(Response<object>.Success());

        switch (dataGrid.Items[selectedTagIndex])
        {
            case TagViewModel tag:
                FixTag(tag);
                break;
            case ChapterViewModel chapter:
                FixChapter(chapter);
                break;
        }

        return Task.FromResult(Response<object>.Success());
    }

    private static void FixTag(TagViewModel tag)
    {
        tag.Name = Encodings.Win1251.GetString(Encodings.Iso8859.GetBytes(tag.Name));
        tag.Value = Encodings.Win1251.GetString(Encodings.Iso8859.GetBytes(tag.Value));
    }

    private static void FixChapter(ChapterViewModel chapter)
    {
        chapter.Title = Encodings.Win1251.GetString(Encodings.Iso8859.GetBytes(chapter.Title));
    }
}