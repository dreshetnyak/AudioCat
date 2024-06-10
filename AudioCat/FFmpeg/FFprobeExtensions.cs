using System.Xml.Linq;
using AudioCat.Models;

namespace AudioCat.FFmpeg;

internal static class FFprobeExtensions
{
    public static IReadOnlyList<IMediaTag> GetTags(this XElement? parentElement)
    {
        if (parentElement is not { HasElements: true })
            return [];
        var tagsContainerElement = parentElement.Element("tags");
        if (tagsContainerElement is not { HasElements: true })
            return [];

        var tags = new List<IMediaTag>();
        foreach (var tagElement in tagsContainerElement.Elements("tag"))
        {
            var key = tagElement.Attribute("key")?.Value;
            if (string.IsNullOrEmpty(key))
                continue;
            var value = tagElement.Attribute("value")?.Value ?? "";
            tags.Add(new MediaTag(key, value));
        }

        return tags;
    }
}