using AudioCat.ViewModels;

namespace AudioCat.Services;

internal static class SelectionFlags
{
    public static bool GetCoverSelectedFrom(IEnumerable<IMediaFileViewModel> mediaFiles) 
    {
        foreach (var file in mediaFiles)
        {
            if (file is { HasCover: true, IsCoverSource: true })
                return true;
        }

        return false;
    }
}