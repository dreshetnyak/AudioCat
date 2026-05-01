# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AudioCat is a WPF desktop application (.NET 10, C# 14, Windows-only) that concatenates audio files via FFmpeg CLI. It does **not re-encode** audio — it demuxes and remuxes streams to preserve quality. Supports MP3, AAC, OPUS, OGG Vorbis, WMA, WAV, and FLAC.

FFmpeg (`ffmpeg.exe` and `ffprobe.exe`) must be accessible on PATH for the app to function.

## Build Commands

```powershell
dotnet build                          # Debug build
dotnet build --configuration Release  # Release build
dotnet run --project AudioCat         # Run the app
```

No test projects exist — testing is done manually by running the application.

## Architecture

**Pattern:** MVVM with Microsoft.Extensions.DependencyInjection. All services and commands are registered in `App.xaml.cs`.

**Main Data Flow:**
1. User drops/selects files → `AddFilesCommand` → `MediaFilesService.AddMediaFiles()`
2. Files probed in parallel via `FFmpegService.Probe()` → ffprobe XML output parsed by `FFprobeMediaFile`
3. Results populate `MainViewModel.Files` (ObservableCollection bound to the DataGrid)
4. User configures output codec, tags, chapters
5. `ConcatenateCommand` → `FFmpegService.Concatenate()` → FFmpeg performs demux/remux

**Key Files:**
- `App.xaml.cs` — DI container setup; all registrations are here
- `MainViewModel.cs` — central state: file list, selected codec, output tags/chapters, UI enable states
- `Settings.cs` — supported codecs, file filters, FFmpeg encoding commands, error-handling rules (errors to skip/retry)
- `FFmpeg/FFmpegService.cs` — wraps FFmpeg/ffprobe CLI; all subprocess calls go through here
- `FFmpeg/FFprobeMediaFile.cs` — parses ffprobe XML into `IMediaFile` objects
- `Models/IMediaFile.cs` — core interfaces: `IMediaFile`, `IMediaStream`, `IMediaChapter`, `IMediaTag`
- `Commands/` — one class per user action (AddFiles, Concatenate, MoveFile, FixEncoding, CreateChapters, ScanForSilence)
- `Services/MediaFilesService.cs` — file loading, validation, parallel probing orchestration
- `Cue/` — CUE sheet parsing (`Parser.cs`), generation (`Generator.cs`), builder, and types

**ViewModels:**
- `MediaFileViewModel` — wraps `IMediaFile` for display in the DataGrid
- `CreateChaptersViewModel` — drives the chapters wizard window
- `ChapterViewModel`, `TagViewModel` — individual item view models

**WPF Converters** (`Converters/`): duration, bitrate, file size formatting, visibility logic, tag concatenation for display.

## Key Implementation Details

**FLAC concatenation** uses re-encoding (FLAC→FLAC) instead of `concat` demuxer because FFmpeg's concat doesn't adjust DTS timestamps for FLAC, causing "non-monotonically increasing DTS" errors. FLAC is lossless so re-encoding is acceptable.

**Chapters** are not supported for OGG, WAV, and FLAC formats (disabled in `Settings.cs`).

**Error handling**: `Settings.cs` defines lists of FFmpeg stderr patterns that should be ignored, trigger a retry with remuxing, or are fatal. Check there before adding new error handling.

**File codec consistency**: When files are added, the first file's audio stream codec defines the expected codec for all subsequent files. Files with a different codec are rejected with a message.

**Concurrency**: File probing runs in parallel (`Task.Run` with `AsParallel`). Concatenation is async with a `CancellationToken`. The UI stays responsive via `Dispatcher.Invoke` for progress updates, implemented through `PeriodicInvoker`.

**Tags format**: Tags are written to a temporary metadata file (avoiding shell escaping issues). The file is UTF-8 without BOM. Certain characters in tag values must be escaped — see `FFmpegService` for the escaping logic.

## NuGet Dependencies

- `Microsoft.Extensions.DependencyInjection` (v10.0.7) — IoC container
- `NAudio` (v2.3.0) — audio playback in the UI (not used for concatenation)
