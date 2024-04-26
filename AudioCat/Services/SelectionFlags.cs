using AudioCat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioCat.ViewModels;

namespace AudioCat.Services;

internal static class SelectionFlags
{
    public static (bool metadataSelected, bool coverSelected) GetFrom(IEnumerable<MediaFileViewModel> mediaFiles) 
    {
        var metadataSelected = false;
        var coverSelected = false;

        foreach (var file in mediaFiles)
        {
            if (file is { HasTags: true, IsTagsSource: true })
                metadataSelected = true;
            if (file is { HasCover: true, IsCoverSource: true })
                coverSelected = true;
            if (metadataSelected && coverSelected)
                break;
        }

        return (metadataSelected, coverSelected);
    }
}