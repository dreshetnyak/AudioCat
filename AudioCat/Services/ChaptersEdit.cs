using AudioCat.ViewModels;
using System.Runtime.CompilerServices;

namespace AudioCat.Services;

internal static class ChaptersEdit
{
    public static void TrimStart(this IEnumerable<IMediaChapterViewModel> createdChapters, string textToTrim, bool isTrimExactText, bool isTrimCharsFromText, bool isTrimCaseSensitive)
    {
        if (string.IsNullOrEmpty(textToTrim))
            return;
        if (isTrimExactText)
            TrimExactStart(createdChapters, textToTrim, isTrimCaseSensitive);
        else if (isTrimCharsFromText)
            TrimCharsStart(createdChapters, textToTrim);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TrimExactStart(IEnumerable<IMediaChapterViewModel> createdChapters, string textToTrim, bool isTrimCaseSensitive)
    {
        foreach (var chapter in createdChapters)
        {
            if (chapter.Title.StartsWith(textToTrim, isTrimCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase))
                chapter.Title = chapter.Title[textToTrim.Length..];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TrimCharsStart(IEnumerable<IMediaChapterViewModel> createdChapters, string textToTrim)
    {
        foreach (var chapter in createdChapters)
        {
            var title = chapter.Title;
            for (var i = 0; i < title.Length; ++i)
            {
                var ch = title[i];
                if (textToTrim.Contains(ch))
                    continue;
                chapter.Title = title[i..];
                break;
            }
        }
    }

    public static void TrimEnd(this IEnumerable<IMediaChapterViewModel> createdChapters, string textToTrim, bool isTrimExactText, bool isTrimCharsFromText, bool isTrimCaseSensitive)
    {
        if (string.IsNullOrEmpty(textToTrim))
            return;
        if (isTrimExactText)
            TrimExactEnd(createdChapters, textToTrim, isTrimCaseSensitive);
        else if (isTrimCharsFromText)
            TrimCharsEnd(createdChapters, textToTrim);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TrimExactEnd(IEnumerable<IMediaChapterViewModel> createdChapters, string textToTrim, bool isTrimCaseSensitive)
    {
        foreach (var chapter in createdChapters)
        {
            if (chapter.Title.EndsWith(textToTrim, isTrimCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase))
                chapter.Title = chapter.Title[..^textToTrim.Length];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TrimCharsEnd(IEnumerable<IMediaChapterViewModel> createdChapters, string textToTrim)
    {
        foreach (var chapter in createdChapters)
        {
            var title = chapter.Title;
            for (var i = title.Length - 1; i >= 0; --i)
            {
                var ch = title[i];
                if (textToTrim.Contains(ch))
                    continue;
                chapter.Title = title[..(i + 1)];
                break;
            }
        }
    }

    public static void Replace(this IEnumerable<IMediaChapterViewModel> createdChapters, string replaceWhatText, string replaceWithText, bool isTrimCaseSensitive)
    {
        if (string.IsNullOrEmpty(replaceWhatText))
            return;
        foreach (var chapter in createdChapters) 
            chapter.Title = chapter.Title.Replace(replaceWhatText, replaceWithText, isTrimCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
    }

    public static void AddToStart(this IEnumerable<IMediaChapterViewModel> createdChapters, string textToAdd, int textToAddSequenceStart, int sequenceSize)
    {
        if (string.IsNullOrEmpty(textToAdd))
            return;
        var chapterIndex = 0;
        foreach (var chapter in createdChapters)
        {
            chapter.Title = textToAdd.Replace("{}", (chapterIndex + textToAddSequenceStart).ToString(new string('0', sequenceSize))) + chapter.Title;
            chapterIndex++;
        }
    }

    public static void AddToEnd(this IEnumerable<IMediaChapterViewModel> createdChapters, string textToAdd, int textToAddSequenceStart, int sequenceSize)
    {
        if (string.IsNullOrEmpty(textToAdd))
            return;
        var chapterIndex = 0;
        foreach (var chapter in createdChapters)
        {
            chapter.Title += textToAdd.Replace("{}", (chapterIndex + textToAddSequenceStart).ToString(new string('0', sequenceSize)));
            chapterIndex++;
        }
    }
}