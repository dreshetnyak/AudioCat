using AudioCat.ViewModels;
using System.Collections.ObjectModel;

namespace AudioCat.Models;

public interface IConcatParams
{
    bool TagsEnabled { get; }
    bool ChaptersEnabled { get; }
    ObservableCollection<IMediaTagViewModel> OutputTags { get; }
    ObservableCollection<IMediaChapterViewModel> OutputChapters { get; }
}