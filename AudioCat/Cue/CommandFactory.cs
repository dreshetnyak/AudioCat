using AudioCat.Models;

namespace AudioCat.Cue;

internal static class CommandFactory
{
    #region Internal Types
    private sealed class FileCommand(string fileName, string fileType) : IFileCommand
    {
        public string File { get; } = fileName;
        public string Type { get; } = fileType;
        public override string ToString() => Type != ""
            ? $"{Command.FILE} \"{File}\" {Type}"
            : $"{Command.FILE} \"{File}\"";
    }

    private sealed class TrackCommand(int number, string trackType) : ITrackCommand
    {
        public int Number { get; } = number;
        public string Type { get; } = trackType;
        public override string ToString() => Type != ""
            ? $"{Command.TRACK} {Number:00} {Type}"
            : $"{Command.TRACK} {Number:00}";
    }

    private sealed class IndexCommand(int number, TimeSpan time) : IIndexCommand
    {
        public int Number { get; } = number;
        public TimeSpan Time { get; } = time;
        public override string ToString() => $"{Command.INDEX} {Number:00} {(int)Time.TotalMinutes:00}:{Time.Seconds:00}:{Time.Milliseconds / 1000.0 * 75:00}";
    }

    private sealed class TitleCommand(string title) : ITitleCommand
    {
        public string Title { get; } = title;
        public override string ToString() => $"{Command.TITLE} \"{Title}\"";
    }

    private sealed class PerformerCommand(string performer) : IPerformerCommand
    {
        public string Performer { get; } = performer;
        public override string ToString() => $"{Command.PERFORMER} \"{Performer}\"";
    }

    private sealed class SongwriterCommand(string songwriter) : ISongwriterCommand
    {
        public string Songwriter { get; } = songwriter;
        public override string ToString() => $"{Command.SONGWRITER} \"{Songwriter}\"";
    }

    private sealed class TagCommand(string tag, string value) : ITagCommand
    {
        public string Name => tag;
        public string Value { get; } = value;
        public override string ToString() => Value != "" ? $"{Name} {Value}" : Name;
    }
    #endregion

    public static IResponse<object> Create(string line)
    {
        var lineSpan = line.AsSpan();
        var startIndex = lineSpan.SkipWhitespace();
        if (startIndex == line.Length)
            return Response<object>.Failure("The line does not contain any command");
        var commandName = lineSpan[startIndex..lineSpan.SkipNonWhitespace(startIndex)];
        var valueStart = lineSpan.SkipWhitespace(startIndex + commandName.Length);
        var commandValue = valueStart != lineSpan.Length
            ? lineSpan[valueStart..]
            : [];

        if (commandName.Equals(Command.FILE, StringComparison.OrdinalIgnoreCase))
            return CreateFileCommand(commandValue);
        if (commandName.Equals(Command.TRACK, StringComparison.OrdinalIgnoreCase))
            return CreateTrackCommand(commandValue);
        if (commandName.Equals(Command.INDEX, StringComparison.OrdinalIgnoreCase))
            return CreateIndexCommand(commandValue);
        if (commandName.Equals(Command.TITLE, StringComparison.OrdinalIgnoreCase))
            return CreateCueTitleCommand(commandValue);
        if (commandName.Equals(Command.PERFORMER, StringComparison.OrdinalIgnoreCase))
            return CreateCuePerformerCommand(commandValue);
        if (commandName.Equals(Command.SONGWRITER, StringComparison.OrdinalIgnoreCase))
            return CreateCueSongwriterCommand(commandValue);
        if (commandName.Equals(Command.REM, StringComparison.OrdinalIgnoreCase))
            return CreateCueRemCommand(commandValue);
        return CreateCueTagCommand(commandName, commandValue);
    }

    private static IResponse<object> CreateFileCommand(ReadOnlySpan<char> valueSpan)
    {
        if (valueSpan.IsEmpty)
            return Response<object>.Failure("Invalid FILE command, doesn't contain any data");

        #region Get File Name
        var quotedValue = valueSpan[0] == '\"';
        var fileNameSpan = quotedValue
            ? GetQuotedValueFullString(valueSpan)
            : GetNoSpaceValue(valueSpan);
        if (fileNameSpan.IsEmpty)
            return Response<object>.Failure("Invalid FILE command, bad file name");
        #endregion

        #region Get File Type
        var fileTypeStartIndex = fileNameSpan.Length;
        if (quotedValue)
            fileTypeStartIndex += 2;
        fileTypeStartIndex = valueSpan.SkipWhitespace(fileTypeStartIndex);
        var fileTypeSpan = fileTypeStartIndex != valueSpan.Length
            ? valueSpan[fileTypeStartIndex..]
            : [];
        #endregion

        return Response<object>.Success(new FileCommand(fileNameSpan.ToString(), fileTypeSpan.TrimEnd().ToString()));
    }

    private static IResponse<object> CreateTrackCommand(ReadOnlySpan<char> valueSpan)
    {
        if (valueSpan.IsEmpty)
            return Response<object>.Failure("Invalid TRACK command, doesn't contain any data");

        #region Get Track Number
        var numberSpan = GetNoSpaceValue(valueSpan);
        if (numberSpan.IsEmpty || !int.TryParse(numberSpan, out var trackNumber) || trackNumber < 0)
            return Response<object>.Failure("Invalid TRACK command, bad track number");
        #endregion

        #region Get Track Type
        var trackTypeStartIndex = valueSpan.SkipWhitespace(numberSpan.Length);
        var trackTypeSpan = trackTypeStartIndex != valueSpan.Length
            ? valueSpan[trackTypeStartIndex..]
            : [];
        #endregion

        return Response<object>.Success(new TrackCommand(trackNumber, trackTypeSpan.TrimEnd().ToString()));
    }

    private static IResponse<object> CreateIndexCommand(ReadOnlySpan<char> valueSpan)
    {
        if (valueSpan.IsEmpty)
            return Response<object>.Failure("Invalid INDEX command, doesn't contain any data");

        #region Get Index Number
        var numberSpan = GetNoSpaceValue(valueSpan);
        if (numberSpan.IsEmpty || !int.TryParse(numberSpan, out var indexNumber) || indexNumber < 0)
            return Response<object>.Failure("Invalid INDEX command, bad index number");
        #endregion

        #region Get Index Time
        var timeStartIndex = valueSpan.SkipWhitespace(numberSpan.Length);
        var timeSpan = timeStartIndex != valueSpan.Length
            ? valueSpan[timeStartIndex..]
            : [];
        if (timeSpan.IsEmpty || !TryParseIndexTime(timeSpan, out var time))
            return Response<object>.Failure("Invalid INDEX command, bad track time");
        #endregion

        return Response<object>.Success(new IndexCommand(indexNumber, time));
    }

    private static bool TryParseIndexTime(ReadOnlySpan<char> timeSpan, out TimeSpan ts)
    {
        ts = TimeSpan.Zero;
        var minutesEndIndex = timeSpan.IndexOf(':');
        if (minutesEndIndex == -1)
            return false;
        var minutesSpan = timeSpan[..minutesEndIndex];
        if (minutesSpan.IsEmpty || !int.TryParse(minutesSpan, out var minutes) || minutes < 0)
            return false;

        var secondsEndIndex = timeSpan.IndexOf(':', minutesEndIndex + 1);
        if (secondsEndIndex == -1)
            return false;
        var secondsSpan = timeSpan[(minutesEndIndex + 1)..secondsEndIndex];
        if (secondsSpan.IsEmpty || !int.TryParse(secondsSpan, out var seconds) || seconds < 0)
            return false;

        var framesSpan = timeSpan[(secondsEndIndex + 1)..];
        if (framesSpan.IsEmpty || !int.TryParse(framesSpan, out var frames) || frames < 0)
            return false;

        ts = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds + frames / 75.0);
        return true;
    }

    private static IResponse<object> CreateCueTitleCommand(ReadOnlySpan<char> valueSpan) => 
        Response<object>.Success(new TitleCommand(GetValueFullString(valueSpan).Trim().ToString()));

    private static IResponse<object> CreateCuePerformerCommand(ReadOnlySpan<char> valueSpan) =>
        Response<object>.Success(new PerformerCommand(GetValueFullString(valueSpan).Trim().ToString()));

    private static IResponse<object> CreateCueSongwriterCommand(ReadOnlySpan<char> valueSpan) =>
        Response<object>.Success(new SongwriterCommand(GetValueFullString(valueSpan).Trim().ToString()));

    private static IResponse<object> CreateCueRemCommand(ReadOnlySpan<char> valueSpan)
    {
        if (valueSpan.IsEmpty)
            return Response<object>.Success(new TagCommand(Command.REM, ""));
       
        if (valueSpan[0] == '\"')
            return Response<object>.Success(new TagCommand(Command.REM, GetQuotedValueFullString(valueSpan).ToString()));
        
        var firstLiteralEndIdx = valueSpan.SkipNonWhitespace();
        var firstLiteral = valueSpan[..firstLiteralEndIdx];
        if (!IsSubCommand(firstLiteral))
            return Response<object>.Success(new TagCommand(Command.REM, valueSpan.Trim().ToString()));

        var valueStartIdx = valueSpan.SkipWhitespace(firstLiteralEndIdx);
        if (valueSpan[valueStartIdx] == '\"')
            return Response<object>.Success(new TagCommand(firstLiteral.ToString(), GetQuotedValueFullString(valueSpan[valueStartIdx..]).ToString()));
        
        return Response<object>.Success(new TagCommand(firstLiteral.ToString(),
            (valueSpan[valueStartIdx] != '\"' //TODO This is always true, should it be removed?
                ? valueSpan[valueStartIdx..].Trim() 
                : GetQuotedValueFullString(valueSpan[valueStartIdx..])).ToString()));

        static bool IsSubCommand(ReadOnlySpan<char> valueSpan)
        {
            foreach (var ch in valueSpan)
            {
                if (!(char.IsUpper(ch) || ch == '_'))
                    return false;
            }

            return true;
        }
    }

    private static IResponse<object> CreateCueTagCommand(ReadOnlySpan<char> tagName, ReadOnlySpan<char> valueSpan) => Response<object>.Success(!valueSpan.IsEmpty
        ? new TagCommand(tagName.ToString(), valueSpan.Trim().ToString())
        : new TagCommand(tagName.ToString(), ""));

    private static ReadOnlySpan<char> GetQuotedValue(ReadOnlySpan<char> valueSpan)
    {
        var endIdx = valueSpan.IndexOf('\"', 1);
        return endIdx != -1
            ? valueSpan[1..endIdx]
            : [];
    }

    private static ReadOnlySpan<char> GetNoSpaceValue(ReadOnlySpan<char> valueSpan) =>
        valueSpan[..valueSpan.SkipNonWhitespace()];

    private static ReadOnlySpan<char> GetValueFullString(ReadOnlySpan<char> valueSpan)
    {
        if (!valueSpan.IsEmpty)
            return valueSpan[0] == '\"'
                ? GetQuotedValueFullString(valueSpan)
                : valueSpan.Trim();
        return [];
    }

    private static ReadOnlySpan<char> GetQuotedValueFullString(ReadOnlySpan<char> valueSpan)
    {
        var endIdx = valueSpan.LastIndexOf('\"', 1);
        return endIdx != -1
            ? valueSpan[1..endIdx]
            : [];
    }
}