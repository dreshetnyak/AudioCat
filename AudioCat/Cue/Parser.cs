using System.Diagnostics;
using System.IO;
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
    
    public static async Task<IResponse<ICue>> Parse(string cueFileFullName)
    {
        var file = new FileInfo(cueFileFullName);
        if (!file.Exists)
            return Response<ICue>.Failure("File not found");

        var fileFound = false;
        var trackFound = false;
        var indexFound = false;
        var cueBuilder = new Builder();
        var fileBuilder = new FileBuilder();
        var trackBuilder = new TrackBuilder();

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
            
            switch (command)
            {
                case IFileCommand fileCommand:
                {
                    if (fileFound)
                    {
                        var addResponse = AddFileToCue();
                        if (addResponse.IsFailure)
                            return addResponse;
                    }
                    else
                        fileFound = true;

                    fileBuilder.SetName(fileCommand.File);
                    fileBuilder.SetType(fileCommand.Type);
                    break;
                }
                case ITrackCommand trackCommand when trackFound:
                    var trackResponse = trackBuilder.Build();
                    if (trackResponse.IsFailure)
                        return Response<ICue>.Failure(trackResponse.Message);
                    fileBuilder.Add(trackResponse.Data!);
                    trackBuilder.Clear();
                    trackBuilder.SetNumber(trackCommand.Number);
                    trackBuilder.SetType(trackCommand.Type);
                    indexFound = false;
                    break;
                case ITrackCommand trackCommand:
                    trackBuilder.SetNumber(trackCommand.Number);
                    trackBuilder.SetType(trackCommand.Type);
                    trackFound = true;
                    break;
                case IIndexCommand when indexFound:
                    return Response<ICue>.Failure("More than one INDEX command specified in the TRACK command");
                case IIndexCommand indexCommand:
                    trackBuilder.SetIndex(Index.From(indexCommand));
                    indexFound = true;
                    break;
                case ITitleCommand titleCommand when fileFound:
                    trackBuilder.SetTitle(titleCommand.Title);
                    break;
                case ITitleCommand titleCommand:
                    cueBuilder.SetTitle(titleCommand.Title);
                    break;
                case IPerformerCommand performerCommand when fileFound:
                    trackBuilder.SetPerformer(performerCommand.Performer);
                    break;
                case IPerformerCommand performerCommand:
                    cueBuilder.SetPerformer(performerCommand.Performer);
                    break;
                case ISongwriterCommand songwriterCommand when fileFound:
                    trackBuilder.SetSongwriter(songwriterCommand.Songwriter);
                    break;
                case ISongwriterCommand songwriterCommand:
                    cueBuilder.SetSongwriter(songwriterCommand.Songwriter);
                    break;
                case ITagCommand tagCommand when fileFound:
                    trackBuilder.Add(Tag.From(tagCommand));
                    break;
                case ITagCommand tagCommand:
                    cueBuilder.Add(Tag.From(tagCommand));
                    break;
            }
        }

        if (!fileFound)
            return Response<ICue>.Failure("No FILE command found in the cue file");

        var response = AddFileToCue();
        return response.IsSuccess 
            ? cueBuilder.Build()
            : response;

        IResponse<ICue> AddFileToCue()
        {
            if (!trackFound)
                return Response<ICue>.Failure("No TRACK command specified in the FILE command");
            if (!indexFound)
                return Response<ICue>.Failure("No INDEX command specified in the TRACK command");

            var trackResponse = trackBuilder.Build();
            if (trackResponse.IsFailure)
                return Response<ICue>.Failure(trackResponse.Message);
            fileBuilder.Add(trackResponse.Data!);

            var fileResponse = fileBuilder.Build();
            if (fileResponse.IsFailure)
                return Response<ICue>.Failure(fileResponse.Message);
            cueBuilder.Add(fileResponse.Data!);

            trackBuilder.Clear();
            fileBuilder.Clear();
            trackFound = false;
            indexFound = false;
            return Response<ICue>.Success();
        }
    }
}