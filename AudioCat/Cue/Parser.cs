using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using AudioCat.Models;

namespace AudioCat.Cue;

internal sealed class Parser
{
    [DebuggerDisplay("Number: {Number,nq}; Time: {Time,nq}")]
    private sealed class Index : IIndex
    {
        public int Number { get; private init; }
        public TimeSpan Time { get; private init; } 
        public static IIndex From(IIndexCommand indexCommand) => new Index { Number = indexCommand.Number, Time = indexCommand.Time };
    }

    [DebuggerDisplay("Name: {Name}; Value: {Value,nq}")]
    private sealed class Tag : ITag
    {
        public string Name { get; private init; } = "";
        public string Value { get; private init; } = "";
        public static ITag From(ITagCommand tagCommand) => new Tag { Name = tagCommand.Name, Value = tagCommand.Value };
    }

    private sealed class Context
    {
        public bool FileFound { get; set; }
        public bool TrackFound { get; set; }
        public bool IndexFound { get; set; }
        public Builder CueBuilder { get; } = new();
        public FileBuilder FileBuilder { get; } = new();
        public TrackBuilder TrackBuilder { get; } = new();
    }

    public static async Task<IResponse<ICue>> Parse(string cueFileFullName)
    {
        var file = new FileInfo(cueFileFullName);
        if (!file.Exists)
            return Response<ICue>.Failure("File not found");

        var context = new Context();

        await using var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var streamReader = new StreamReader(fileStream);
        while (await streamReader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var commandResponse = CommandFactory.Create(line);
            if (commandResponse.IsFailure)
                return Response<ICue>.Failure(commandResponse.Message);
            var command = commandResponse.Data!;
            var processCommandResponse = ProcessCommand(context, command);
            if (processCommandResponse.IsFailure)
                return processCommandResponse;
        }

        if (!context.FileFound)
            return Response<ICue>.Failure("No FILE command found in the cue file");

        var response = AddFileToCue(context);
        return response.IsSuccess 
            ? context.CueBuilder.Build()
            : response;
    }

    private static IResponse<ICue> ProcessCommand(Context context, object command) => command switch
    {
        IFileCommand fileCommand => ProcessFileCommand(context, fileCommand),
        ITrackCommand trackCommand when context.TrackFound => ProcessTrackCommandWhenTrackFound(context, trackCommand),
        ITrackCommand trackCommand => ProcessTrackCommand(context, trackCommand),
        IIndexCommand when context.IndexFound => Response<ICue>.Failure("More than one INDEX command specified in the TRACK command"),
        IIndexCommand indexCommand => ProcessIndexCommand(context, indexCommand),
        ITitleCommand titleCommand when context.FileFound => ProcessTitleCommandWhenFileFound(context, titleCommand),
        ITitleCommand titleCommand => ProcessTitleCommand(context, titleCommand),
        IPerformerCommand performerCommand when context.FileFound => ProcessPerformerCommandWhenFileFound(context, performerCommand),
        IPerformerCommand performerCommand => ProcessPerformerCommand(context, performerCommand),
        ISongwriterCommand songwriterCommand when context.FileFound => ProcessSongwriterCommandWhenFileFound(context, songwriterCommand),
        ISongwriterCommand songwriterCommand => ProcessSongwriterCommand(context, songwriterCommand),
        ITagCommand tagCommand when context.FileFound => ProcessTagCommandWhenTagFound(context, tagCommand),
        ITagCommand tagCommand => ProcessTagCommand(context, tagCommand),
        _ => Response<ICue>.Success()
    };

    private static IResponse<ICue> AddFileToCue(Context context)
    {
        if (!context.TrackFound)
            return Response<ICue>.Failure("No TRACK command specified in the FILE command");
        if (!context.IndexFound)
            return Response<ICue>.Failure("No INDEX command specified in the TRACK command");

        var trackResponse = context.TrackBuilder.Build();
        if (trackResponse.IsFailure)
            return Response<ICue>.Failure(trackResponse.Message);
        context.FileBuilder.Add(trackResponse.Data!);

        var fileResponse = context.FileBuilder.Build();
        if (fileResponse.IsFailure)
            return Response<ICue>.Failure(fileResponse.Message);
        context.CueBuilder.Add(fileResponse.Data!);

        context.TrackBuilder.Clear();
        context.FileBuilder.Clear();
        context.TrackFound = false;
        context.IndexFound = false;
        return Response<ICue>.Success();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResponse<ICue> ProcessFileCommand(Context context, IFileCommand fileCommand)
    {
        if (context.FileFound)
        {
            var addResponse = AddFileToCue(context);
            if (addResponse.IsFailure)
                return addResponse;
        }
        else
            context.FileFound = true;

        context.FileBuilder.SetName(fileCommand.File);
        context.FileBuilder.SetType(fileCommand.Type);
        return Response<ICue>.Success();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResponse<ICue> ProcessTrackCommandWhenTrackFound(Context context, ITrackCommand trackCommand)
    {
        var trackResponse = context.TrackBuilder.Build();
        if (trackResponse.IsFailure)
            return Response<ICue>.Failure(trackResponse);
        context.FileBuilder.Add(trackResponse.Data!);
        context.TrackBuilder.Clear();
        context.TrackBuilder.SetNumber(trackCommand.Number);
        context.TrackBuilder.SetType(trackCommand.Type);
        context.IndexFound = false;
        return Response<ICue>.Success();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResponse<ICue> ProcessTrackCommand(Context context, ITrackCommand trackCommand)
    {
        context.TrackBuilder.SetNumber(trackCommand.Number);
        context.TrackBuilder.SetType(trackCommand.Type);
        context.TrackFound = true;
        return Response<ICue>.Success();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResponse<ICue> ProcessIndexCommand(Context context, IIndexCommand indexCommand)
    {
        context.TrackBuilder.SetIndex(Index.From(indexCommand));
        context.IndexFound = true;
        return Response<ICue>.Success();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResponse<ICue> ProcessTitleCommandWhenFileFound(Context context, ITitleCommand titleCommand)
    {
        context.TrackBuilder.SetTitle(titleCommand.Title);
        return Response<ICue>.Success();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResponse<ICue> ProcessTitleCommand(Context context, ITitleCommand titleCommand)
    {
        context.CueBuilder.SetTitle(titleCommand.Title);
        return Response<ICue>.Success();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResponse<ICue> ProcessPerformerCommandWhenFileFound(Context context, IPerformerCommand performerCommand)
    {
        context.TrackBuilder.SetPerformer(performerCommand.Performer);
        return Response<ICue>.Success();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResponse<ICue> ProcessPerformerCommand(Context context, IPerformerCommand performerCommand)
    {
        context.CueBuilder.SetPerformer(performerCommand.Performer);
        return Response<ICue>.Success();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResponse<ICue> ProcessSongwriterCommandWhenFileFound(Context context, ISongwriterCommand songwriterCommand)
    {
        context.TrackBuilder.SetSongwriter(songwriterCommand.Songwriter);
        return Response<ICue>.Success();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResponse<ICue> ProcessSongwriterCommand(Context context, ISongwriterCommand songwriterCommand)
    {
        context.CueBuilder.SetSongwriter(songwriterCommand.Songwriter);
        return Response<ICue>.Success();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResponse<ICue> ProcessTagCommandWhenTagFound(Context context, ITagCommand tagCommand)
    {
        context.TrackBuilder.Add(Tag.From(tagCommand));
        return Response<ICue>.Success();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResponse<ICue> ProcessTagCommand(Context context, ITagCommand tagCommand)
    {
        context.CueBuilder.Add(Tag.From(tagCommand));
        return Response<ICue>.Success();
    }
}