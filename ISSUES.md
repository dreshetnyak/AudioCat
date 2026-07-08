# AudioCat — Known Issues

Issue-tracking log for defects surfaced by a multi-model code review (26 AI models) and verified by a skeptical multi-agent audit. The original log listed 100 issues; a second skeptical line-by-line code verification (2026-07-01) closed **33 of them** as factually wrong, unreachable-hypothetical, pure style with no functional consequence, or expected work-in-progress on the intentionally incomplete player feature. This file now lists the **67 surviving issues (41 `real`, 26 `partial`)**, all confirmed present in the current source. Original issue numbers are kept (gaps = closed issues); `Ref` is the canonical id.

_Generated 2026-06-21 · pruned after code verification 2026-07-01 · upstream analysis: `ISSUES_REVIEW_Analysis.md` / `ISSUES_REVIEW_Matrix.csv`._

**Status:** ☐ Open · ☑ Fixed · ◌ Won't fix / by design (edit the box as issues are resolved)  
**Class:** `real` = genuine defect · `partial` = minor / cosmetic / dead-code / latent  
**Imp:** reviewer-weighted importance 1–8 · **Find:** # of 26 models that reported it · **Ref:** canonical id in the analysis  
**Present:** all 67 remaining issues re-verified present on 2026-07-01 (prior ⚠ markers resolved; #28 in particular was previously dismissed as "not present" but is confirmed real).  
**Note:** the player / chapters-wizard playback feature is under active development. Player issues below marked "latent" are genuine defects in already-written player code that will fire once playback is wired up; observations that merely said "not wired up yet" were closed as expected WIP.
**Update (2026-07-08):** the chapters-wizard playback wiring is complete (`chapters-player-wiring`). The open player issues were re-checked against the rewritten AudioFilePlayer/ChaptersPlayer: #29, #33, #34, #36, #40, #47, #59 were fixed by the rewrite (dated fix notes added); #28 survived and became live-reachable, then was fixed same day (Play() now guards on the device state); #64 and #70 survive unchanged (still no-impact API/latent concerns).

Closed on 2026-07-01 (removed from this file): C50, C66, C29, C52, C42, C126, C105, C124, C37, C40, C39, C77, C41, C104, C75, C68, C103, C101, C121, C36, C51, C94, C95, C100, C69, C82, C83, C84, C86, C87, C97, C98, C99.

---

## Index

| # | S | Sev | Imp | Cls | Pres | Ref | Issue |
|--:|:-:|:----|:---:|:---:|:----:|:----|:------|
| 1 | ☑ | High | 8 | real | yes | C21 | CUE Build() uses AsReadOnly() on backing list (shares storage); parser Clear() then empties built Tracks/Tags |
| 2 | ☑ | Medium | 7 | real | yes | C01 | Concatenation Cancel passes CancellationToken.None to Concatenate (cancel button inert) |
| 3 | ☑ | High | 7 | real | yes | C06 | Self-join deadlock: player disposed from inside its own PeriodicInvoker callback (EventInvokerTask.Wait waits on itself) |
| 4 | ☑ | High | 7 | real | yes | C07 | Re-entrant/lock-order deadlock in ChaptersPlayer: Sync semaphore held while disposing player |
| 5 | ☑ | High | 7 | real | yes | C04 | Culture-sensitive parsing of ffprobe/ffmpeg numbers (Extensions ToDecimal/ToInt/ToLong/SecondsToTimeSpan, FFmpegStats bitrate, FFmpegService timestamp) |
| 6 | ☑ | Medium | 7 | real | yes | C20 | CUE value-less REM sub-command -> IndexOutOfRangeException (CommandFactory CreateCueRemCommand indexes past end) |
| 9 | ☑ | High | 6 | real | yes | C09 | Task.Run(OnGenerateChapters) in ctor: race with field init AND mutates bound ObservableCollection off UI thread |
| 10 | ◌ | Medium | 6 | real | yes | C05 | ChaptersFactory.CreateFromExisting InvalidOperationException on chapters with null start/end time (EndTime!.Value) |
| 11 | ☑ | Medium | 6 | real | yes | C10 | Chapters wizard leaks subscriptions to singleton ScanForSilenceCommand (event leak + ghost dialogs on reopen) |
| 12 | ☑ | Low | 6 | real | yes | C15 | TextBlockFormatter loop corrupts/truncates warning text (processedBytes not reset; unconditional append eats char + emits stray bracket) |
| 13 | ☑ | High | 5 | real | yes | C02 | Process.Run does not kill ffmpeg child on cancellation (orphaned process) |
| 14 | ☑ | Low | 5 | real | yes | C03 | FFmpegStats.ExtractValue searches end-token from index 0 -> negative span / latent throw (not triggered by current -vn progress lines) |
| 15 | ☑ | Medium | 5 | real | yes | C18 | Temp files leaked on exception/cancellation (no finally; catch block cleans nothing; per-file remux concat lists) |
| 16 | ☑ | Medium | 5 | real | yes | C12 | Startup ContinueWith chain not unwrapped -> async steps run out of order |
| 17 | ☑ | Medium | 5 | real | yes | C47 | CommandBase.Execute is async void and swallows all exceptions -> generic 'Unknown error', no diagnostics |
| 18 | ☑ | Low | 5 | real | yes | C11 | ChaptersPlayer never disposed (semaphore/subscription/device handle leak per wizard open) |
| 19 | ☑ | Low | 5 | real | yes | C17 | Intermediate temp output file (outputToFile) never deleted even on success (two-step Vorbis / cover-image path) |
| 20 | ☑ | Medium | 5 | real | yes | C14 | EnableUserEntryOnStartup re-enables UI even when ffmpeg/ffprobe missing |
| 21 | ☑ | Low | 5 | real | yes | C26 | Silence at end of file (open silence_start with no silence_end) not captured / dropped on cancellation of BlockingCollection |
| 24 | ☑ | Medium | 5 | real | yes | C22 | Multi-file silence scan uses relative file-end timestamp instead of absolute (startTime+fileDuration) |
| 25 | ☑ | Medium | 5 | real | yes | C49 | CreateMetadataFile failure silently swallowed -> tags/chapters dropped with no user feedback |
| 26 | ☑ | Low | 5 | partial | yes | C64 | CUE parser rejects tracks with more than one INDEX command (pregap INDEX 00 + INDEX 01) |
| 27 | ☑ | Medium | 5 | real | yes | C70 | Adding a file clears manually entered output tags when files have no chapters (OutputTags.Clear on !ChaptersExist) |
| 28 | ☑ | Low | 5 | real | yes | C28 | Pause() does not update CurrentState -> Play() within 100ms poll window no-ops (stays paused) |
| 29 | ☑ | Low | 4 | real | yes | C27 | Duplicate pollers after pause->resume (PeriodicInvoker.Start not idempotent; Play() reruns Start) |
| 31 | ☑ | Medium | 4 | partial | yes | C13 | Startup continuations run on thread-pool thread and touch UI state / enumerate Files during population |
| 32 | ☑ | Low | 4 | partial | yes | C114 | Pervasive silent catch blocks (~40) hide genuine failures |
| 33 | ☑ | Low | 4 | partial | yes | C31 | Global timeline offsets use metadata Duration; null/image durations corrupt seek offsets & file-transition |
| 34 | ☑ | Low | 4 | real | yes | C33 | Disposed-object race in periodic status update (ObjectDisposedException reading reader/device after Dispose) |
| 35 | ☑ | Low | 4 | real | yes | C55 | DoNotInvokeFilesCollectionChangedEvent flag never reset if exception thrown during add (no try/finally) |
| 36 | ☑ | Low | 4 | real | yes | C78 | PeriodicInvoker.Dispose blocks calling/UI thread on EventInvokerTask.Wait() |
| 38 | ☑ | Low | 4 | real | yes | C73 | Incomplete command-line escaping for metadata tag values (only escapes double-quote, not backslash) |
| 39 | ☑ | Low | 3 | real | yes | C48 | GenerateTempOutputFileFrom returns empty string after 3 failures -> empty path passed to ffmpeg |
| 40 | ☑ | Low | 3 | partial | yes | C32 | Unused Sync semaphore in AudioFilePlayer / unsynchronized AudioFileReader access / dispose ordering |
| 41 | ☑ | Low | 3 | real | yes | C59 | Tags DataGrid double-click: insert-at-selection branch dead / only adds when grid empty (guard returns early when Items.Count>0) |
| 42 | ☑ | Low | 3 | partial | yes | C72 | Command-line argument quoting: embedded double-quotes in paths/tags break ffmpeg argument tokenization |
| 43 | ☑ | Medium | 3 | real | yes | C19 | Unrecoverable-remux abort is dead code (RemuxFiles always returns non-null Data; if(Data==null) unreachable) |
| 46 | ☑ | Low | 3 | not-a-bug | yes | C16 | Leading silence interval starting at 0.0s dropped (TimeSpan.Zero used as both sentinel and value) |
| 47 | ☑ | Low | 3 | partial | yes | C34 | Chapter advance only steps one chapter per position tick (skips across multiple short chapters) |
| 50 | ☐ | Low | 3 | partial | yes | C44 | RedirectStandardInput set but never written (potential hang if ffmpeg ever prompts) |
| 51 | ☐ | Low | 3 | partial | yes | C45 | Process callback overload drains only one of stdout/stderr -> pipe-buffer stall deadlock risk |
| 52 | ☐ | Low | 3 | real | yes | C46 | Process.Run read loop catch{break;} swallows real IO errors as EOF (truncated output / misclassification) |
| 53 | ☐ | Low | 3 | partial | yes | C65 | GetCueFileCommand returns Success even when some/all CUE files fail to parse (silent partial data loss) |
| 54 | ☐ | Low | 3 | partial | yes | C76 | Concurrent add operations mutate shared file collection / DoNotInvoke flag without synchronization |
| 55 | ☐ | Medium | 3 | real | yes | C89 | AddPathCommand blocks UI thread enumerating directory (.ToArray on EnumerateFiles AllDirectories) |
| 56 | ☐ | Medium | 3 | partial | yes | C96 | Files.GetFilesFromDirectory unbounded recursion (symlink/junction loop -> StackOverflow) |
| 57 | ☐ | Low | 3 | partial | yes | C108 | IsAccessible relies on StartsWith('ffmpeg version')/('ffprobe version') - fragile for custom builds |
| 58 | ☐ | Low | 3 | partial | yes | C109 | CreateFilesListFile returns empty string if source file missing -> malformed concat command |
| 59 | ☑ | Low | 3 | real | yes | C38 | Chapter gap causes transient position mis-reporting (stale ActiveChapter during gap) |
| 61 | ☐ | Low | 3 | real | yes | C74 | CUE Generator does not escape quotes in emitted values |
| 63 | ☐ | Trivial | 2 | partial | yes | C43 | Process Verb=runas: dead/misleading config (ignored when UseShellExecute=false; no elevation occurs) |
| 64 | ☐ | Low | 2 | partial | yes | C30 | AudioFilePlayer.SetPosition ignores its filePath parameter |
| 65 | ☐ | Trivial | 2 | partial | yes | C54 | DoNotInvokeOutputChaptersCountChangedEvent set true (not false) on last loop iteration |
| 68 | ☐ | Low | 2 | partial | yes | C61 | AAC/MP3 encoder builders misplace 'k' kilobit suffix (attaches to cutoff) - dead code (no callers) |
| 69 | ☐ | Trivial | 2 | partial | yes | C56 | Double sort of file names (GetMediaFiles then ProbeFiles re-sorts) |
| 70 | ☐ | Low | 2 | partial | yes | C35 | MeteringSampleProvider issues: StreamVolume MaxSampleValues buffer forwarded by reference / subscription never removed |
| 74 | ☐ | Low | 2 | partial | yes | C106 | LastIndexOf(ReadOnlySpan,char,startIndex) extension has non-standard semantics (startIndex as lower bound) |
| 75 | ☐ | Low | 2 | partial | yes | C110 | OnToggleChaptersEnabled does not re-enable chapters after manual disable + later codec change |
| 76 | ☐ | Low | 2 | partial | yes | C125 | Files.cs empty catch on Exists / COMException swallowed in MainWindow drop handler |
| 77 | ☐ | Low | 2 | partial | yes | C25 | CreateFromIntervals overlapping intervals -> negative-duration chapter (reachable today via multi-file silence scan, see #24) |
| 78 | ☐ | Low | 2 | real | yes | C63 | TRACK 00 accepted by parser but rejected by TrackBuilder as 'missing the number' (0 overloaded as unset) |
| 81 | ☐ | Trivial | 2 | real | yes | C127 | O(n^2) reassembly of remuxed files into original order (SortRemuxedFiles brute force) |
| 84 | ☐ | Trivial | 2 | partial | yes | C88 | -update true flag (image2 muxer specific) used in audio remux command |
| 87 | ☐ | Trivial | 1 | real | yes | C58 | SelectCodec misleading comment ('Acceptable codec has not been selected yet' on the selected branch) |
| 88 | ☐ | Trivial | 1 | real | yes | C60 | Duplicate 'End' column header in chapters grid (third column labeled End but binds Duration) |
| 89 | ☐ | Low | 1 | partial | yes | C62 | CUE Generator re-emits plain REM comment with doubled REM token (dead code; ToCommands has no callers) |
| 90 | ☐ | Low | 1 | partial | yes | C85 | DurationConverter returns 'N/A' for zero-length (TimeSpan.Zero) files |

---

## Issues

### 1. CUE Build() uses AsReadOnly() on backing list (shares storage); parser Clear() then empties built Tracks/Tags

`☑ Fixed` (2026-07-06) · **Severity** High · **Importance** 8 · **Class** real · **Finders** 3 · **Ref** C21 · **Present** yes

**Location:** AudioCat/Cue/Builder.cs:42-43; AudioCat/Cue/FileBuilder.cs:25-40; AudioCat/Cue/TrackBuilder.cs:37-56; AudioCat/Cue/Parser.cs:94-108,131-135

**Description:** The CUE builders return their results by wrapping their mutable backing `List<T>` with `List.AsReadOnly()`, which produces a `ReadOnlyCollection<T>` that is a live VIEW over the same list, not a copy. `CueImpl`/`CueFile`/`CueTrack` therefore hold references to the builder's internal lists. The Parser reuses a single `FileBuilder`/`TrackBuilder` instance and, immediately after `Build()`, calls `Clear()` (Parser.cs:104-105,135) which invokes `Tracks.Clear()`/`Tags.Clear()` on those very lists. The misleading "Do not remove ToArray() here, it is intended to make a copy" comments confirm a copy was intended but `AsReadOnly()` does not copy.

**Impact:** Every parsed CUE sheet yields tracks whose `Tags` are emptied and files whose `Tracks` are emptied (the final `AddFileToCue` at Parser.cs:63 clears after building, affecting even single-FILE cues). Consumers of `ICue` from GetCueFileCommand silently receive cue data with zero tracks/tags - effective data loss on CUE import.

**Repro:** Use the "Get Cue File" action to import any .cue sheet with tracks/REM tags. The returned ICue objects have empty CueFile.Tracks and empty CueTrack.Tags because the builders' Clear() emptied the shared backing lists after Build().

**Suggested fix:** Materialize a real copy in each Build(), e.g. `new List<T>(Tags).AsReadOnly()` or `[..Tags]`, so the returned collection does not share storage with the reused builder.

---

### 2. Concatenation Cancel passes CancellationToken.None to Concatenate (cancel button inert)

`☑ Fixed` (2026-07-06) · **Severity** Medium · **Importance** 7 · **Class** real · **Finders** 11 · **Ref** C01 · **Present** yes

**Fix note (2026-07-06):** Cts.Token now passed to Concatenate; ffmpeg child killed (entire tree) and awaited on cancellation in Process.Run; Cancel button added next to the progress bar (outside the IsUserEntryEnabled-disabled scope); cancelled run deletes the partial output file and reports "Cancelled".

**Location:** AudioCat/Commands/ConcatenateCommand.cs:45,67,86-90; wired at AudioCat/ViewModels/MainViewModel.cs:426

**Description:** ConcatenateCommand.Command() creates a CancellationTokenSource (Cts, line 45) and exposes Cancel() which calls Cts?.Cancel() (line 88, bound to the UI Cancel command at MainViewModel.cs:426). However, the call to MediaFileToolkitService.Concatenate(...) at line 67 passes CancellationToken.None instead of Cts.Token. Cts.Token is referenced nowhere, so the token source is completely disconnected from the running operation, even though the downstream pipeline (FFmpegService.Concatenate -> Process.Run -> WaitForExitAsync/ReadLineAsync, all parameter-named ctx) fully threads and honors a real token.

**Impact:** Pressing the Cancel button during concatenation has no effect; the operation cannot be interrupted and runs to completion regardless. Note: even with the correct token, Process.Run only unblocks the await on cancel and does not Kill the FFmpeg child process, so a complete fix needs process termination too.

**Repro:** 1. Add several large audio files. 2. Click Concatenate and choose an output path. 3. While FFmpeg is running, click Cancel. Observe the operation continues to completion instead of stopping.

**Suggested fix:** Pass Cts.Token (not CancellationToken.None) to Concatenate at line 67; ideally also Kill the ffmpeg child process on cancellation in Process.Run.

---

### 3. Self-join deadlock: player disposed from inside its own PeriodicInvoker callback (EventInvokerTask.Wait waits on itself)

`☑ Fixed` (2026-07-06) · **Severity** High · **Importance** 7 · **Class** real · **Finders** 8 · **Ref** C06 · **Present** yes

**Fix note (2026-07-06):** PeriodicInvoker now detects self-dispose via a per-instance AsyncLocal set inside EventInvokerLoop (flows into the callback, false for outside callers). Dispose/DisposeAsync entered from within the loop's own callback skip the self-join wait; the loop exits on its own after the callback returns (Cts already cancelled) and Cts disposal is deferred to a task continuation. Outside callers keep the original blocking-wait behavior. Also defuses the deadlock half of #4, which stays open for its design point (don't dispose the player from its own event callback).

**Location:** AudioCat/Services/PeriodicInvoker.cs:28-40 (Dispose/EventInvokerTask.Wait); AudioCat/Services/AudioFilePlayer.cs:116-125,177-186 (Dispose, OnPlaybackStateUpdate); AudioCat/Services/ChaptersPlayer.cs:114-125,141-186,188-241 (StopInternal, OnPlaybackPositionChanged/OnPlaybackStateChanged)

**Description:** PeriodicInvoker runs its callback (AudioFilePlayer.OnPlaybackStateUpdate) on the EventInvokerTask loop thread. That callback synchronously sets CurrentState/CurrentPosition, which raise PlaybackStateChanged/PlaybackPositionChanged on the same thread. ChaptersPlayer subscribes to these and, on chapter/file end, calls StopInternal() -> AudioFilePlayer.Dispose() -> PlayerStatusInvoker.Dispose(), whose synchronous Dispose() calls EventInvokerTask.Wait() (PeriodicInvoker.cs:34). Since this executes inside the EventInvokerTask itself, the task waits on its own completion, a self-join deadlock. (ChaptersPlayer.OnPlaybackPositionChanged line 170 and OnPlaybackStateChanged line 204 both reach this self-dispose path.)

**Impact:** When wired, the playback loop thread would block forever inside Dispose at end-of-chapter/end-of-file, hanging the player and leaking the WaveOut/AudioFileReader resources, with the UI never receiving stop/progress updates. Currently latent because the audio player playback path is not yet hooked up.

**Repro:** Latent - the only ChaptersPlayer.Play()/Stop() call sites are commented out in CreateChaptersViewModel.cs:646-648, so no event-driven self-dispose occurs at runtime yet. Once playback is wired: start chapter playback, let it reach the last chapter's EndTime or the last file's natural stop; the position/state event fires StopInternal -> player.Dispose -> PeriodicInvoker.Dispose -> EventInvokerTask.Wait() on its own thread and hangs.

**Suggested fix:** Make PeriodicInvoker.Dispose non-blocking from within its own callback (detect/skip self-join, or dispose async via DisposeAsync without Wait), or raise player events on a different thread / defer disposal off the invoker thread (e.g. post to dispatcher/threadpool).

**Audit note:** Defect logic is present but the trigger (ChaptersPlayer.Play) is commented out in CreateChaptersViewModel.cs:646-648, so it is latent, not yet runtime-reachable.

---

### 4. Re-entrant/lock-order deadlock in ChaptersPlayer: Sync semaphore held while disposing player

`☑ Fixed` (2026-07-06) · **Severity** High · **Importance** 7 · **Class** real · **Finders** 6 · **Ref** C07 · **Present** yes

**Fix note (2026-07-06):** ChaptersPlayer no longer disposes the player synchronously from its own event callbacks: StopInternal/file-transition now unsubscribe, Pause() the device for immediate silence, and defer AudioFilePlayer.Dispose to the thread pool (DisposePlayer). The fix also covered a second live flavor of the same Sync re-entrancy found during the fix: PeriodicInvoker.Start ran the loop's first callback synchronously on the caller's thread, so Play(chapter, offset) at a mid-file position fired PlaybackPositionChanged into OnPlaybackPositionChanged → Sync.Wait() on the thread already holding Sync (UI freeze on first play); Start now launches the loop via Task.Run. AudioFilePlayer.Dispose/DisposeAsync additionally join the status poller first, before disposing device/reader, so the deferred outside-thread dispose is race-free — this also fixes the main path of #34 (left open pending its own verification).

**Location:** AudioCat/Services/ChaptersPlayer.cs:123,141-186,188-241 (StopInternal disposing ActivePlayer from event callbacks); AudioCat/Services/AudioFilePlayer.cs:124,177-192 (Dispose -> PlayerStatusInvoker.Dispose, events fired from periodic callback); AudioCat/Services/PeriodicInvoker.cs:18-40 (EventInvokerTask.Wait self-join)

**Description:** AudioFilePlayer raises PlaybackPositionChanged/PlaybackStateChanged from its PeriodicInvoker callback (OnPlaybackStateUpdate), which executes inside PeriodicInvoker.EventInvokerLoop on a thread-pool thread. ChaptersPlayer handles those events (OnPlaybackPositionChanged line 141, OnPlaybackStateChanged line 188) by taking the non-reentrant Sync semaphore and, when a chapter/file ends, calling StopInternal() (lines 170/204), which disposes ActivePlayer (line 123). AudioFilePlayer.Dispose() calls PlayerStatusInvoker.Dispose() which blocks on EventInvokerTask.Wait() (PeriodicInvoker.cs:34). Because the event was raised synchronously from within that same EventInvokerTask, the task ends up joining on itself — a self-join deadlock; the Sync semaphore is never released, also wedging every other ChaptersPlayer method.

**Impact:** When playback reaches the end of the final chapter or a file ends naturally, the playback worker thread deadlocks (task waiting on itself) and ChaptersPlayer.Sync is left permanently held, hanging all subsequent Play/Stop/Dispose calls and leaking the audio device. This code is latent: ChaptersPlayer/AudioFilePlayer are implemented but the player is not yet fully wired into the UI.

**Repro:** Latent - ChaptersPlayer is not yet wired to the UI. Once wired: start chapter playback and let it run to the end of the last chapter (or let a non-final file play to its natural end); OnPlaybackPositionChanged/OnPlaybackStateChanged calls StopInternal->player.Dispose->PlayerStatusInvoker.Dispose->EventInvokerTask.Wait(), which is the very task running the callback, deadlocking the thread with Sync held.

**Suggested fix:** Do not dispose the player synchronously from its own event callback; defer disposal (post to dispatcher/queue) or make PeriodicInvoker.Dispose non-blocking / detect self-join so EventInvokerTask.Wait() is never called from within the callback while Sync is held.

---

### 5. Culture-sensitive parsing of ffprobe/ffmpeg numbers (Extensions ToDecimal/ToInt/ToLong/SecondsToTimeSpan, FFmpegStats bitrate, FFmpegService timestamp)

`☑ Fixed` (2026-07-06) · **Severity** High · **Importance** 7 · **Class** real · **Finders** 4 · **Ref** C04 · **Present** yes

**Fix note (2026-07-06):** all listed call sites now parse with CultureInfo.InvariantCulture; decimal/double sites use NumberStyles.Float (drops AllowThousands, so "123.456" can never silently parse as 123456 on group-separator cultures, and tolerates ffprobe exponent notation like 1.4e-05). Covers Extensions ToDecimal/ToInt/ToLong/SecondsToTimeSpan, FFmpegStats.GetBitrate, FFmpegService.TryGetTime. GetSpeed was already invariant; GetSize/GetTime parse digit-only integers and were left as is.

**Location:** AudioCat/Extensions.cs:14,16,18,92-93 (ToDecimal/ToInt/ToLong/SecondsToTimeSpan); AudioCat/FFmpeg/FFmpegStats.cs:46 (GetBitrate); AudioCat/FFmpeg/FFmpegService.cs:122 (TryGetTime)

**Description:** FFmpeg/ffprobe always emit numbers with '.' as the decimal separator and no thousands grouping, but several parsers read them with the OS locale. Extensions.ToDecimal/ToInt/ToLong/SecondsToTimeSpan (Extensions.cs:14,16,18,92-93) pass CultureInfo.CurrentCulture, and these feed FFprobeMediaFile/Stream/Chapter parsing of duration/start_time/bit_rate/sample_rate/channels (e.g. FFprobeMediaFile.cs:43-45). FFmpegStats.GetBitrate (FFmpegStats.cs:46) and FFmpegService.TryGetTime (FFmpegService.cs:122) likewise call double.TryParse with no culture/NumberStyles override. Notably FFmpegStats.GetSpeed (line 54) was already fixed to use CultureInfo.InvariantCulture, confirming the surrounding calls are inconsistent and culture-dependent. NumberStyles.Number/Float also allows a thousands separator, so on cultures where ',' is the group separator a string like "1,234" could be silently mis-parsed.

**Impact:** On systems whose locale uses ',' as the decimal separator (German, French, Russian, etc.), ffprobe values like duration="123.456" fail to parse and return null, so durations, bitrates, start times, sample rates and parsed chapter times are lost or zero; progress timestamps from ffmpeg ("time=00:01:23.45") also fail, breaking the progress bar. The app would show missing/incorrect metadata and broken progress for any non-invariant decimal locale.

**Repro:** Set Windows region/format to one using comma decimal separator (e.g. German). Run AudioCat, add an audio file: Duration/Bitrate columns show empty/zero because "123.456" fails CurrentCulture decimal.TryParse; during concatenation the progress (TryGetTime parsing "time=...") fails to advance.

**Suggested fix:** Parse all ffprobe/ffmpeg numerics with CultureInfo.InvariantCulture and NumberStyles.Float/Integer (no AllowThousands) in Extensions ToDecimal/ToInt/ToLong/SecondsToTimeSpan, FFmpegStats.GetBitrate, and FFmpegService.TryGetTime.

**Re-audit (2026-07-01):** core defect confirmed; one impact correction — TryGetTime (FFmpegService.cs:122) parses silencedetect `silence_start`/`silence_end` values, not concat progress, and the progress `time=HH:MM:SS` stamp is parsed culture-safely from integer components in FFmpegStats.GetTime. On comma-decimal locales the breakage is metadata/durations/chapter times and the silence scan — not the progress bar.

---

### 6. CUE value-less REM sub-command -> IndexOutOfRangeException (CommandFactory CreateCueRemCommand indexes past end)

`☑ Fixed` (2026-07-06) · **Severity** Medium · **Importance** 7 · **Class** real · **Finders** 2 · **Ref** C20 · **Present** yes

**Fix note (2026-07-06):** CreateCueRemCommand now guards before indexing: when valueStartIdx == valueSpan.Length (no value after the sub-command), it returns TagCommand(firstLiteral, "") — consistent with CreateCueTagCommand's empty-value handling. Covers `REM COMMENT` with and without trailing whitespace.

**Location:** AudioCat/Cue/CommandFactory.cs:193-209 (specifically line 206-207); helpers AudioCat/Extensions.cs:281-289 (SkipWhitespace) and 270-278 (SkipNonWhitespace)

**Description:** CreateCueRemCommand parses `REM <SUBCMD> <value>` lines. When the first literal after REM is all-uppercase/underscore (IsSubCommand returns true) but there is no value following it, line 206 computes `valueStartIdx = valueSpan.SkipWhitespace(firstLiteralEndIdx)`. With no trailing whitespace/value, both firstLiteralEndIdx and the result equal valueSpan.Length. Line 207 then unconditionally evaluates `valueSpan[valueStartIdx]`, indexing one past the end of the span, throwing IndexOutOfRangeException. The line is read via ReadLineAsync (terminator stripped), so a typical line like `REM COMMENT` with no trailing space hits this.

**Impact:** A CUE sheet containing a value-less REM sub-command (e.g. `REM COMMENT`) throws IndexOutOfRangeException during Cue.Parser.Parse. It is caught by GetCueFileCommand's try/catch, so the app does not crash, but the entire CUE import is aborted and the user sees a generic "Files selection error" dialog instead of the file being parsed.

**Repro:** Create a .cue file whose body includes a line `REM COMMENT` (or any uppercase token after REM with no value and no trailing whitespace). Use the Add Cue File command (GetCueFileCommand) and select that file. Parsing throws IndexOutOfRangeException, surfacing as an error dialog and aborting import.

**Suggested fix:** In CreateCueRemCommand, guard line 207: if valueStartIdx >= valueSpan.Length, return a REM TagCommand with empty value (or treat firstLiteral as a value-less sub-command) before indexing.

---

### 9. Task.Run(OnGenerateChapters) in ctor: race with field init AND mutates bound ObservableCollection off UI thread

`☑ Fixed` (2026-07-06) · **Severity** High · **Importance** 6 · **Class** real · **Finders** 10 · **Ref** C09 · **Present** yes

**Fix note (2026-07-06):** removed the mid-ctor `_ = Task.Run(OnGenerateChapters)`; OnGenerateChapters() is now called synchronously at the end of the ctor, after all its dependencies (CreatedChapters, Files, TagNames, CueFiles) are initialized and on the UI thread — same path the Generate button already uses. Eliminates the null-field race, the off-thread mutation of the bound ObservableCollection, and the swallowed fire-and-forget exceptions (an exception during initial generation now propagates; the known throw source there is #10, tracked separately).

**Location:** AudioCat/ViewModels/CreateChaptersViewModel.cs:626 (fire), 628 & 658 (field init), 756-787 (OnGenerateChapters), 789-796 (UpdateChapters)

**Description:** The constructor fires `_ = Task.Run(OnGenerateChapters)` at line 626, before the get-only auto-properties it depends on are assigned: `CreatedChapters = []` (628) and `Files = files.AsReadOnly()` (658). Both are non-nullable default-null reference properties, so the pool thread can read them while still null. `OnGenerateChapters` reads `Files`/`SelectedChapterSource` and calls `UpdateChapters`, which invokes `CreatedChapters.Clear()` and `CreatedChapters.Add()` (789-796). That ObservableCollection is bound to a WPF DataGrid, yet it is mutated from a thread-pool thread with no `Dispatcher.Invoke`. The task is fire-and-forget so any thrown exception is swallowed/unobserved.

**Impact:** Two failure modes: (a) NullReferenceException if the worker reaches CreatedChapters/Files before the ctor assigns them; (b) once assigned, mutating the DataGrid-bound ObservableCollection off the UI thread throws NotSupportedException ("collection changed on a different thread"). Because the task is discarded, the wizard silently shows no auto-generated chapters on open instead of crashing visibly.

**Repro:** Add audio files in the main window, then open the Create Chapters wizard (CreateChaptersWindow -> CreateChaptersViewModel constructed). The ctor immediately spawns Task.Run(OnGenerateChapters); depending on scheduling it hits a null CreatedChapters/Files (NRE) or mutates the bound collection off-thread (NotSupportedException). Exception is unobserved; chapters list comes up empty.

**Suggested fix:** Assign Files/CreatedChapters first, then dispatch generation to the UI thread (Dispatcher.InvokeAsync) at the end of the ctor, awaiting/observing the task instead of fire-and-forget.

---

### 10. ChaptersFactory.CreateFromExisting InvalidOperationException on chapters with null start/end time (EndTime!.Value)

`◌ Won't fix` (2026-07-06) · **Severity** Medium · **Importance** 6 · **Class** real · **Finders** 5 · **Ref** C05 · **Present** yes

**Won't fix note (2026-07-06):** verified unreachable in practice. ffprobe always runs libavformat's `compute_chapters_end()` (via avformat_find_stream_info, in the codebase since ~2011), which backfills any chapter end that is AV_NOPTS_VALUE with the next chapter's start, else the (possibly estimated) file duration, worst case `end = start` — never emitted as N/A/absent. Chapters missing a start are dropped by the matroska demuxer, and MP3 ID3v2 CHAP / MP4 chapters structurally always carry both. Empirically could not construct a repro on the installed ffmpeg (2026 git build): OGG `CHAPTERxxx=` vorbis comments (start-only by design), a binary-patched MKA with ChapterTimeEnd removed, and the same MKA with no Duration element all came back from ffprobe with both start_time and end_time filled. The `!.Value` deref only fires with an ancient (pre-2011) or broken ffprobe, and the app already requires a working toolchain. A defensive HasValue guard remains an option if the code is ever touched, but there is no reachable defect.

**Location:** AudioCat/Services/ChaptersFactory.cs:130-137 (deref at 132); source nullability AudioCat/FFmpeg/FFprobeMediaChapter.cs:16-17,39-40; Models/IMediaFile.cs:36-37

**Description:** CreateFromExisting iterates each source chapter and computes duration as `sourceChapter.EndTime!.Value - sourceChapter.StartTime!.Value` (line 132), using the null-forgiving operator on two nullable TimeSpan? properties. These properties are populated in FFprobeMediaChapter.Create from the optional ffprobe XML attributes start_time/end_time via null-conditional access plus SecondsToTimeSpan(), so either can legitimately be null when the attribute is absent or unparseable. Accessing .Value on a null Nullable<TimeSpan> throws InvalidOperationException ("Nullable object must have a value"). Note the sibling methods guard file.Duration.HasValue but this loop has no such guard for chapter start/end.

**Impact:** Selecting "Existing" as the chapter source in the Create Chapters wizard for a file whose embedded chapters lack a start_time or end_time crashes chapter generation with an unhandled InvalidOperationException, aborting the wizard (potentially crashing the app since the call site at CreateChaptersViewModel.cs:780 has no try/catch).

**Repro:** Add an audio file containing embedded chapters where at least one chapter is missing the start_time or end_time attribute in ffprobe output, open the Create Chapters wizard, and choose chapter source "Existing" -> CreateFromExisting runs and throws on EndTime!.Value/StartTime!.Value.

**Suggested fix:** Guard with HasValue (e.g. if (!sourceChapter.StartTime.HasValue || !sourceChapter.EndTime.HasValue) skip or default duration) before subtracting, and/or wrap the call site in try/catch like CreateFromCueFiles.

---

### 11. Chapters wizard leaks subscriptions to singleton ScanForSilenceCommand (event leak + ghost dialogs on reopen)

`☑ Fixed` (2026-07-06) · **Severity** Medium · **Importance** 6 · **Class** real · **Finders** 3 · **Ref** C10 · **Present** yes

**Fix note (2026-07-06):** verified real (all claims matched code), fixed by making CreateChaptersViewModel IDisposable: Dispose unsubscribes Starting/Finished from the singleton ScanForSilenceCommand and also disposes the per-VM ChaptersPlayer (which was never disposed anywhere — related leak). CreateChaptersCommand now declares the VM with `using var`, so teardown runs deterministically after ShowDialog returns regardless of how the window closes. Transient-command alternative rejected: CreateChaptersCommand is itself a singleton, so a DI-transient ScanForSilenceCommand would be captured once and remain effectively singleton.

**Location:** AudioCat/ViewModels/CreateChaptersViewModel.cs:665-668, 847-879; AudioCat/Commands/CreateChaptersCommand.cs:15-16; AudioCat/App.xaml.cs:25-26; AudioCat/Windows/CreateChaptersWindow.xaml.cs:22-26

**Description:** ScanForSilenceCommand and CreateChaptersCommand are both registered as DI singletons (App.xaml.cs:25-26). CreateChaptersCommand.Command constructs a fresh CreateChaptersViewModel on every wizard open, passing the same singleton scanForSilence instance. The VM constructor subscribes scanForSilence.Starting += OnScanForSilenceStarting and .Finished += OnScanForSilenceFinished (CreateChaptersViewModel.cs:665-666) but never unsubscribes: the VM is not IDisposable, and the window-close handler (CreateChaptersWindow.xaml.cs:22-26) only calls CancelScanForSilence, never tears down the handlers. Each closed wizard VM therefore stays rooted via the singleton's event invocation list.

**Impact:** Memory leak: every closed wizard VM (plus its Files, CreatedChapters, ChaptersPlayer) is retained forever by the singleton command. Worse correctness bug: after opening the wizard N times, a single silence scan fires OnScanForSilenceFinished on all N stale VMs, each of which can pop a MessageBox error dialog (line 866) for an already-closed window and mutate discarded collections via UpdateChapters.

**Repro:** 1. Open the Create Chapters wizard, close it. 2. Reopen and run a silence scan (e.g. one that fails). OnScanForSilenceFinished fires on every accumulated stale VM, producing duplicate error MessageBoxes; all closed VMs remain in memory.

**Suggested fix:** Make CreateChaptersViewModel IDisposable, unsubscribe Starting/Finished on window close; or pass a per-open (transient) ScanForSilenceCommand instance instead of the singleton.

---

### 12. TextBlockFormatter loop corrupts/truncates warning text (processedBytes not reset; unconditional append eats char + emits stray bracket)

`☑ Fixed` (2026-07-06) · **Severity** Low · **Importance** 6 · **Class** real · **Finders** 3 · **Ref** C15 · **Present** yes

**Fix note (2026-07-06):** verified real, incl. the re-audit's progressive-skip finding (processedBytes accumulated across iterations, keeping only chars at offsets 0, 1, 3, 6, 10, …). Fixed in OnFormattedTextChanged: each marker branch now `continue`s after setting processedBytes to the marker length (no fall-through append of the stray `[`), and the plain-char path sets `processedBytes = 1` instead of incrementing the stale value.

**Location:** AudioCat/Converters/TextBlockFormatter.cs:57-78

**Description:** OnFormattedTextChanged's parse loop is broken in two ways. (1) When a `[b]`/`[/b]` marker matches at readOffset, the code sets processedBytes = marker length but then unconditionally falls through to `output.Append(input[readOffset]); processedBytes++;` (lines 73-74) -- so it appends the marker's first char (a stray `[`) to the output AND advances readOffset by markerLength+1, skipping one real text character past the marker. (2) processedBytes is never reset to 0 in the no-match branch; it retains its prior value, so after a marker iteration the next plain-char iteration advances by stalePrev+1 instead of 1, skipping additional characters. The intended logic clearly required a `continue` after consuming a marker and a per-iteration reset of processedBytes.

**Impact:** The bold/italic/underline warning text (e.g. the chapters CHAPTERS_WARNING bound to OutputWarning) renders corrupted: a stray `[` is injected and the character immediately following each `[b]`/`[/b]`/`[u]`/`[/u]` marker is dropped, so "WARNING!" shows as "[ARNING!" etc. Cosmetic only; no data loss or crash.

**Repro:** Enable chapters, generate chapters, then change file order so IsChaptersFilesOrderChanged() is true (MainViewModel.cs:689). OutputWarning is set to CHAPTERS_WARNING which contains [b]/[u] markers; the warning TextBlock (MainWindow.xaml:1407) renders garbled text with stray brackets and missing characters.

**Suggested fix:** After consuming a start/end marker, set readOffset past the marker and `continue` (skip the append); reset processedBytes=0 each iteration and only `processedBytes++` once for the plain-char append path.

**Re-audit (2026-07-01):** worse than described — `processedBytes` accumulates across ALL iterations (never reset), so even plain marker-free stretches are skipped progressively (chars kept at offsets 0, 1, 3, 6, 10, …); the entire warning text renders garbled, not just characters adjacent to markers.

---

### 13. Process.Run does not kill ffmpeg child on cancellation (orphaned process)

`☑ Fixed` (2026-07-06) · **Severity** High · **Importance** 5 · **Class** real · **Finders** 8 · **Ref** C02 · **Present** yes

**Fix note (2026-07-06):** both Process.Run overloads now catch OperationCanceledException from WaitForExitAsync, call process.Kill(entireProcessTree: true), await real exit with an uncancellable token (releasing the output-file write locks), join the output-reader tasks, then rethrow. Covers concat, remux, silence scan, and probe cancellation.

**Location:** AudioCat/Services/Process.cs:11-35 (both Run overloads); used by AudioCat/FFmpeg/FFmpegService.cs:164,217,399 (Concatenate/Remux) and cancelled via AudioCat/Commands/ConcatenateCommand.cs:86-88

**Description:** Both Process.Run overloads spawn an ffmpeg.exe child process and await process.WaitForExitAsync(ctx). When the CancellationToken is signaled, WaitForExitAsync throws OperationCanceledException and the enclosing `using` block disposes the System.Diagnostics.Process object. Process.Dispose() only releases the managed wrapper/handles; it does NOT terminate the underlying OS process. There is no process.Kill()/Kill(entireProcessTree:true) call anywhere on the cancellation path, so the ffmpeg.exe subprocess is left running.

**Impact:** Cancelling a concatenation (or remux/silence scan) leaves an orphaned ffmpeg.exe running to completion in the background, consuming CPU/disk and keeping a write lock on the partially-written output file, so the app's cleanup (delete-empty-output at FFmpegService.cs:238) and any retry can fail with file-in-use errors. Repeated cancels accumulate orphaned processes.

**Repro:** Add several large audio files, start Concatenate, then press Cancel (ConcatenateCommand.Cancel -> Cts.Cancel()). Observe in Task Manager that ffmpeg.exe continues running and the temp/output file remains locked until that ffmpeg finishes on its own.

**Suggested fix:** On cancellation/exception in Process.Run, call process.Kill(entireProcessTree: true) (e.g. try/finally or ctx.Register) before disposing, then await exit.

**Re-audit (2026-07-01):** confirmed (no process.Kill anywhere in the codebase), but the concat repro cannot currently fire: ConcatenateCommand passes CancellationToken.None (see #2), so concat cancel is inert. The orphan occurs today via silence-scan cancellation (ScanForSilenceCommand → FFmpegService.cs:60) and probe cancellation — and will affect concatenation as soon as #2 is fixed.

---

### 14. FFmpegStats.ExtractValue searches end-token from index 0 -> negative span / throws on time= progress lines

`☑ Fixed` (2026-07-06) · **Severity** Low (downgraded from High, see re-audit note) · **Importance** 5 · **Class** real · **Finders** 7 · **Ref** C03 · **Present** yes

**Fix note (2026-07-06):** validated: the search-from-index-0 flaw is real in code, but the claimed exception/"concatenation broken" impact is contrived — confirmed the re-audit: all FFmpegStats feeds (OnConcatStatus, RemuxFile) are gated by IsErrorMessage and come from `-vn` invocations whose stats lines contain no `.`/`x` before `time=`/`speed=`; the only video-stream invocation (AddImages cover-art pass) has no `-stats` and no line callback. Fixed as hardening: ExtractValue now searches the end token from startOffset (`message.IndexOf(end, startOffset, ...)`), so a hypothetical dotted field before `time=` parses correctly instead of throwing; the existing `< 0` check covers end-token-only-before-start, and the space-skip loop cannot overshoot since no end token starts with a space.

**Location:** AudioCat/FFmpeg/FFmpegStats.cs:59-80 (bug at line 70/78); called from GetTime line 27; consumed in FFmpeg/FFmpegService.cs:261 (OnConcatStatus) and 412

**Description:** ExtractValue locates the end token via message.IndexOf(end, ...) starting from index 0 of the whole message (line 70) instead of searching from startOffset (the position just after the start token). For GetTime the end token is "." (line 27). A real FFmpeg progress line such as "frame=100 fps=0.0 q=-1.0 size=...kB time=00:00:10.50 bitrate=..." contains "." characters in earlier fields (fps=0.0, q=-1.0, or size value) that precede "time=". Thus endOffset lands before startOffset, and message.AsSpan(startOffset, endOffset - startOffset) at line 78 is called with a negative length, throwing ArgumentOutOfRangeException. The same flaw can affect GetSpeed (end="x", e.g. an "x" in an earlier path/codec token) though GetTime is the reliable trigger.

**Impact:** The FFmpegStats constructor computes Time in a field initializer, so the exception is thrown while parsing nearly every concat progress line. It propagates through OnConcatStatus into the outer catch at FFmpegService.cs:245, aborting the operation with a "Concatenation exception" message instead of producing the output file. Concatenation is effectively broken whenever FFmpeg emits a standard progress line with a decimal before time=.

**Repro:** Add 2+ audio files and run Concatenate. As soon as FFmpeg emits a status line containing "fps=0.0"/"q=-1.0"/decimal size before "time=", new FFmpegStats(status) throws ArgumentOutOfRangeException, surfacing as "Concatenation exception" and aborting the merge.

**Suggested fix:** Search for end token starting at startOffset (message.IndexOf(end, startOffset, ...)) and guard endOffset >= startOffset before slicing the span.

**Re-audit (2026-07-01):** the search-from-index-0 flaw is real, but the claimed impact ("concatenation effectively broken") is wrong — every ffmpeg invocation that feeds FFmpegStats uses `-vn` (audio-only), so its stats lines are `size= time= bitrate= speed=` with no `frame=/fps=/q=` fields and no '.'/'x' preceding the start tokens; the exception does not fire in practice. Latent/fragile parsing defect only — severity downgraded High → Low.

---

### 15. Temp files leaked on exception/cancellation (no finally; catch block cleans nothing; per-file remux concat lists)

`☑ Fixed` (2026-07-06) · **Severity** Medium · **Importance** 5 · **Class** real · **Finders** 5 · **Ref** C18 · **Present** yes

**Fix note (2026-07-06):** all temp artifacts (list files incl. per-file remux lists, metadata file, extracted images, remuxed files, intermediate outputs) are now created inside a per-run directory (%TEMP%\AudioCat\<guid>, see Services/TempDirectory.cs) which Concatenate deletes recursively in a finally block; app startup sweeps leftover per-run directories from crashed instances.

**Location:** AudioCat/FFmpeg/FFmpegService.cs:130-249 (cleanup at 231-243 inside try; catch at 245-249); temp creators GenerateTempOutputFileFrom:429-444, CreateFilesListFile:447-484, CreateMetadataFile:489-525, ExtractImages:700-737, RemuxFile:392-414

**Description:** In Concatenate(), all temp artifacts (list file, metadata file, extracted cover images, per-file remuxed temp files, and the intermediate temp output file) are created inside the try block, and the only deletion logic lives in the "Delete Temporary Files" region (lines 231-243) at the very end of that same try. There is no finally block. The catch block (lines 245-249) only emits an error message and returns, deleting nothing. Any exception or cancellation (OperationCanceledException from a cancelled Process.Run, ffmpeg failure surfaced as an exception, image embed/remux throwing) thrown between temp creation and the cleanup region aborts before deletion runs, so every temp file produced up to that point is orphaned in %TEMP%. RemuxFiles only deletes its temp outputs on an "unrecoverable" error path (302-303) and leaks the per-file remux concat list files created in RemuxFile (CreateFilesListFile at 395 is never deleted) regardless.

**Impact:** Cancelling a concatenation or hitting any mid-run failure leaves random-named temp files (concat lists, metadata, extracted images, remuxed audio copies which can be large) accumulating in the user's TEMP directory, never cleaned up by the app. Over repeated/cancelled runs this can consume significant disk space.

**Repro:** Add several audio files, start Concatenate, then cancel via the CancellationToken (or use files that trigger a remux+failure); the linked Process.Run throws OperationCanceledException, control skips the cleanup region and lands in catch, and the listFile/metadataFile/extracted images/remuxed temp files remain in %TEMP%. Also: every successful remux pass leaks the per-file concat list created at line 395.

**Suggested fix:** Track all temp paths and delete them in a finally block (and clean up the leaked per-file remux list in RemuxFile).

---

### 16. Startup ContinueWith chain not unwrapped -> async steps run out of order

`☑ Fixed` (2026-07-06) · **Severity** Medium · **Importance** 5 · **Class** real · **Finders** 4 · **Ref** C12 · **Present** yes

**Fix note (2026-07-06):** mechanism confirmed real (Task<Task> without Unwrap, steps overlap) but both impact claims refuted: chapters DO populate on CLI startup because AddMediaFiles clears the suppression flag before adding the last file, so OnFilesCollectionChanged fires once with all files present and regenerates OutputChapters (AddOutputChaptersOnStartup was fully redundant and no-oped in the race); user entry is already enabled at VerifyMediaFileServiceIsAccessible on success, by design. The issue's suggested fix (Unwrap/sequential await of the existing steps) would have introduced a duplicate-chapters bug: AddOutputChaptersOnStartup appends without Clear() or an OutputChaptersSource guard, so running it after OnFilesCollectionChanged doubles every chapter. Fixed instead by replacing the chain with a single awaited InitializeAsync (VerifyMediaFileServiceIsAccessible → AddCliFilesOnStartup → EnableUserEntryOnStartup) and deleting the redundant AddOutputChaptersOnStartup; EnableUserEntryOnStartup kept for the ffmpeg-missing error path.

**Location:** AudioCat/ViewModels/MainViewModel.cs:438-441 (continuation methods at 495-567)

**Description:** The startup chain `VerifyMediaFileServiceIsAccessible().ContinueWith(AddCliFilesOnStartup).ContinueWith(AddOutputChaptersOnStartup).ContinueWith(EnableUserEntryOnStartup)` chains `async Task M(Task _)` delegates without calling `.Unwrap()`. `Task.ContinueWith(Func<Task,Task>)` returns a `Task<Task>` that completes as soon as the continuation delegate RETURNS its inner Task — i.e., at the method's first incomplete `await` — not when the async work actually finishes. So `AddOutputChaptersOnStartup` (line 528) can run while `AddCliFilesOnStartup` (line 504) is still awaiting `MediaFilesService.AddMediaFiles` and `Files` is still empty/partial, making `Files.ChaptersExist()` (line 530) false and returning early. `EnableUserEntryOnStartup` likewise can run before prior steps complete.

**Impact:** When the app is launched with command-line/file-association arguments pointing to chaptered audio files, the embedded chapters are silently not imported into OutputChapters because the chapter-creation step races ahead of file loading. User entry may also be enabled before startup work completes.

**Repro:** Launch AudioCat with file arguments containing embedded chapters (e.g. open-with / drag files onto AudioCat.exe, or `AudioCat.exe file_with_chapters.m4b`). Observe chapters do not auto-populate OutputChapters. Timing-dependent but reliably reproduces when probing/loading takes longer than the synchronous prefix of the prior continuation.

**Suggested fix:** Replace the ContinueWith chain with a single `async Task InitializeAsync()` that awaits each step in order (`_ = InitializeAsync();`), or add `.Unwrap()` after each ContinueWith.

---

### 17. CommandBase.Execute is async void and swallows all exceptions -> generic 'Unknown error', no diagnostics

`☑ Fixed` (2026-07-06) · **Severity** Medium · **Importance** 5 · **Class** real · **Finders** 4 · **Ref** C47 · **Present** yes

**Fix note (2026-07-06):** verified real as written. Kept `async void` (required by ICommand.Execute; the catch-all is what prevents an app crash) but the bare swallow is replaced: `catch (Exception ex)` now sets `Response<object>.Failure(ex.Message)` (matching the idiom concrete commands use) so ConcatErrorWindow shows the actual reason instead of "Unknown error", and `Debug.WriteLine(ex)` records type + stack in debug output (no logging framework exists in the app). An escaped `OperationCanceledException` maps to quiet `Success()` — user-initiated cancel should not pop an error dialog.

**Location:** AudioCat/Commands/CommandBase.cs:19-37 (catch block 28-31); surfaced at AudioCat/ViewModels/MainViewModel.cs:614-627

**Description:** CommandBase.Execute is declared `async void` and wraps the awaited Command(parameter) call in a bare `try { ... } catch { /* ignore */ }`. Any exception thrown by a command's Command() implementation (or by OnStarting) is silently swallowed; `response` then stays at its initial seed value `Response<object>.Failure("Unknown error")`. No exception message, type, or stack trace is captured or logged anywhere. Because Execute is `async void`, the exception is also not propagatable to callers. Most concrete commands (AddFiles, Concatenate, ScanForSilence, etc.) do internally catch and return Failure(ex.Message), but any exception escaping those internal handlers is lost.

**Impact:** When an unhandled exception escapes a command body, the user is shown the generic, useless message "Unknown error" (e.g. via ConcatErrorWindow in OnConcatFinished) with no diagnostics, and nothing is logged for the developer. This makes such failures effectively undebuggable.

**Repro:** Trigger any code path inside a command's Command() that throws an exception not caught by that command's own try/catch (e.g. an exception in OnStarting handlers or an unguarded path). The Finished handler receives the seed response and the UI displays "Unknown error" with no detail; no log entry is produced.

**Suggested fix:** In the catch, capture the exception and build Response.Failure(ex.Message/ToString()) (and log it) instead of `/* ignore */`.

---

### 18. ChaptersPlayer never disposed (semaphore/subscription/device handle leak per wizard open)

`☑ Fixed` (2026-07-06) · **Severity** Low · **Importance** 5 · **Class** real · **Finders** 4 · **Ref** C11 · **Present** yes

**Fix note (2026-07-06):** resolved by the #11 fix: CreateChaptersViewModel is now IDisposable, its Dispose calls ChaptersPlayer.Dispose(), and CreateChaptersCommand declares the VM with `using var`, so disposal runs deterministically after ShowDialog returns regardless of close path — matching this issue's suggested fix. The device/file-handle portion of the leak remains latent by design: the player is unfinished work in progress (PlayPause wiring commented out at CreateChaptersViewModel.cs:643-650, ChaptersPlayer.Play never called, ActivePlayer always null). Remaining playback-path problems for when wiring completes are tracked in #34 and #36.

**Location:** AudioCat/Services/ChaptersPlayer.cs:8-39,292-305; AudioCat/ViewModels/CreateChaptersViewModel.cs:561,640; AudioCat/Windows/CreateChaptersWindow.xaml.cs:22-26

**Description:** `ChaptersPlayer` correctly implements `IDisposable` (its `Dispose` at :292 unsubscribes `CreatedChapters.CollectionChanged`, stops any active player, and disposes its `SemaphoreSlim Sync`). However the owner, `CreateChaptersViewModel`, constructs a `ChaptersPlayer` at :640 but does not implement `IDisposable` and has no Dispose path. `CreateChaptersWindow.OnWindowClosing` (:22) only cancels an in-flight silence scan and never disposes the VM or its player. Consequently `ChaptersPlayer.Dispose()` is never called for the lifetime of the app.

**Impact:** Each time the chapters wizard is opened, a `SemaphoreSlim` and a live `CreatedChapters.CollectionChanged` subscription are leaked (the subscription is harmless since it points at the disposed VM's own collection, but the semaphore handle and the rooted player object accumulate). The audio-device/file-handle portion of the leak is latent because playback (`Play`) is never invoked - the `PlayPause` wiring is commented out (:642-649), so `ActivePlayer` is always null.

**Repro:** Latent in part - the semaphore/object leak occurs now: open and close the Create Chapters wizard repeatedly; each instance's `ChaptersPlayer` (with its `SemaphoreSlim`) is never disposed and is retained for the process lifetime. The device/file-handle leak cannot occur until `ChaptersPlayer.Play` is wired up (currently commented out).

**Suggested fix:** Make `CreateChaptersViewModel : IDisposable`, dispose `ChaptersPlayer` in its `Dispose`, and call `viewModel.Dispose()` from `CreateChaptersWindow.OnWindowClosing`/after the dialog closes.

---

### 19. Intermediate temp output file (outputToFile) never deleted even on success (two-step Vorbis / cover-image path)

`☑ Fixed` (2026-07-06) · **Severity** Low · **Importance** 5 · **Class** real · **Finders** 4 · **Ref** C17 · **Present** yes

**Fix note (2026-07-06):** intermediate outputToFile files are now created inside the per-run temp directory (see #15 fix) and removed with it in Concatenate's finally block on success, failure, and cancellation.

**Location:** AudioCat/FFmpeg/FFmpegService.cs:154-156, 210-213, 225, 231-243

**Description:** In FFmpegService.Concatenate, when hasImages or twoStepsConcat is true, outputToFile is assigned a freshly created temp file in Path.GetTempPath() via GenerateTempOutputFileFrom (line 154-156, and again at 211-213 for the Vorbis two-step path). These intermediate temp files are inputs to the next step (CreateFilesListFile at 210, or AddImages at 225 which copies into the real outputFileName), but the "Delete Temporary Files" region (lines 234-241) only deletes listFile, metadataFile, extracted images, remuxed files, and outputFileName when it is zero-length. It never deletes outputToFile when outputToFile != outputFileName, and the catch block (245-249) cleans up nothing.

**Impact:** On every successful (or failed) concatenation of OGG Vorbis with tags, or any codec carrying an embedded cover image, one or two full-size audio temp files are orphaned in the system temp directory, growing unbounded across runs until the user manually clears %TEMP%. No correctness impact on the output file; it is a disk-space/temp-clutter leak.

**Repro:** Concatenate either (a) OGG Vorbis files that carry tags (triggers twoStepsConcat), or (b) any audio files that have an embedded cover image (hasImages). After completion, inspect %TEMP%: a GUID-named file with the output extension (the full concatenated audio) remains; in the images+two-step case two such temp files remain.

**Suggested fix:** In the cleanup region (and catch block), delete outputToFile when outputToFile != outputFileName, after AddImages has consumed it.

---

### 20. EnableUserEntryOnStartup re-enables UI even when ffmpeg/ffprobe missing

`☑ Fixed` (2026-07-06) · **Severity** Medium · **Importance** 5 · **Class** real · **Finders** 3 · **Ref** C14 · **Present** yes

**Fix note (2026-07-06):** verified real and still present after the #16 refactor (line refs/ContinueWith mechanism stale, effect identical with sequential awaits). Impact partly overstated: Concatenate could never actually be clicked — without ffprobe no file survives probing, Files stays empty, IsConcatenateEnabled stays false; real impact was active Add Files/Add Path buttons dumping every add into SkippedFilesWindow. Fixed by making VerifyMediaFileServiceIsAccessible return Task&lt;bool&gt; and gating InitializeAsync on it: on failure the UI now stays cleanly disabled and CLI-passed files are not pointlessly probed. EnableUserEntryOnStartup deleted as redundant — the success path already enables user entry inside VerifyMediaFileServiceIsAccessible, and nothing disables it between there and startup end.

**Location:** AudioCat/ViewModels/MainViewModel.cs:438-441 (ContinueWith chain), 495-502 (VerifyMediaFileServiceIsAccessible), 564-567 (EnableUserEntryOnStartup)

**Description:** The startup task chain `VerifyMediaFileServiceIsAccessible().ContinueWith(AddCliFilesOnStartup).ContinueWith(AddOutputChaptersOnStartup).ContinueWith(EnableUserEntryOnStartup)` always runs the final continuation. `VerifyMediaFileServiceIsAccessible` only sets `IsUserEntryEnabled = true` when `MediaFileToolkitService.IsAccessible()` succeeds; when ffmpeg/ffprobe are missing it merely shows a MessageBox and leaves the UI disabled. However `EnableUserEntryOnStartup` (line 564-567) unconditionally sets `IsUserEntryEnabled = true` via the dispatcher, with no check of the verification result, defeating the intended gate. (Note: `ContinueWith` continuations run regardless of antecedent state since the antecedents complete normally; there is no exception path that would cancel them.)

**Impact:** When FFmpeg/ffprobe are not on PATH, the app shows the "tools required" error yet still enables all user-entry controls, letting the user add files and click Concatenate against a non-functional toolchain, producing confusing downstream failures instead of a cleanly disabled UI.

**Repro:** Remove ffmpeg.exe/ffprobe.exe from PATH and launch AudioCat. The error MessageBox appears, but after dismissing it the input controls are enabled (IsUserEntryEnabled becomes true) because EnableUserEntryOnStartup runs unconditionally.

**Suggested fix:** Thread the verification result (e.g. return Task<bool> or store a field) and only set IsUserEntryEnabled = true in EnableUserEntryOnStartup when accessibility succeeded.

---

### 21. Silence at end of file (open silence_start with no silence_end) not captured / dropped on cancellation of BlockingCollection

`☑ Fixed` (2026-07-06) · **Severity** Low · **Importance** 5 · **Class** real · **Finders** 3 · **Ref** C26 · **Present** yes

**Fix note (2026-07-06):** Validation split the issue in two. The headline mechanism ("silence_start at EOF never gets a silence_end, open startTime never flushed") is bogus on any non-ancient FFmpeg: verified empirically with the app's exact scan command on an MP3 with 3s trailing silence — FFmpeg (fixed ~4.3, 2020) prints `silence_end` at stream end, so the interval always closes and the suggested "flush pending startTime at total duration" is unnecessary. The secondary cancel-without-drain race is real but narrow: Process.Run awaits its stderr reader to EOF, so all lines are queued before `cts.CancelAsync()`, but items the processor had not yet Taken were abandoned (loop guard + `Take(ctx)` abort on cancel), and the at-risk tail lines are exactly the final silencedetect lines including the EOF `silence_end`. Fixed by replacing cancellation with `statusQueue.CompleteAdding()` in a `finally` and awaiting the processor, which now drains via `GetConsumingEnumerable()` (linked CTS removed as pointless). Also fixes a latent leak on the exception path where the processor stayed blocked in `Take` on a never-cancelled token and then hit the disposed queue. The separate leading-silence-at-0 sentinel bug observed during testing is tracked in #46.

**Location:** AudioCat/FFmpeg/FFmpegService.cs:45-108 (esp. 60-64 cancel-after-run, 81-107 IntervalsProcessor)

**Description:** In ScanForSilence, statuses are pushed to a BlockingCollection consumed by IntervalsProcessor on a background task. The processor only emits an Interval when a "silence_end:" line is parsed (line 97-105); a "silence_start:" with no matching "silence_end" just keeps startTime set and never gets flushed. After Process.Run returns (line 60), the code immediately calls cts.CancelAsync() (line 62), which causes the processor's loop guard (line 84) and statusQueue.Take(ctx) (line 86) to abort. There is no completion/drain step (no CompleteAdding + drain-to-empty), so any status lines still buffered in the queue at cancellation, plus any final open silence interval (silence_start at/near EOF without a processed silence_end), are dropped.

**Impact:** A silence region at the very end of a file (trailing silence) can be missed from the returned intervals, so the chapters wizard's silence-based splitting omits or mis-places the last chapter boundary. Also a race exists where the last few buffered detect lines are discarded on the post-run cancellation, making results non-deterministic for silence near EOF.

**Repro:** Open the Create Chapters wizard, add an audio file that ends with a silent passage (silence running to EOF), and run Scan For Silence. The trailing silence interval (and occasionally the last detected interval) is not present in the produced intervals/chapter boundaries.

**Suggested fix:** After Process.Run, call statusQueue.CompleteAdding() and let the processor drain remaining items (loop on TryTake/GetConsumingEnumerable until empty) instead of cancelling, and flush any pending open startTime as a final interval ending at the file's total duration.

---

### 24. Multi-file silence scan uses relative file-end timestamp instead of absolute (startTime+fileDuration)

`☑ Fixed` (2026-07-07) · **Severity** Medium · **Importance** 5 · **Class** real · **Finders** 1 · **Ref** C22 · **Present** yes

**Fix note (2026-07-07):** ScanForSilenceCommand now computes `fileEndTime = startTime + fileDuration` and uses that absolute timestamp for the file-boundary interval, keeping multi-file silence-scan intervals monotonic on the concatenated timeline.

**Location:** AudioCat/Commands/ScanForSilenceCommand.cs:46-49 (esp. line 48); consumed in AudioCat/Services/ChaptersFactory.cs:145-157 (CreateFromIntervals)

**Description:** In ScanForSilenceCommand.Command, per-file silence intervals are correctly shifted to the absolute timeline via AddFileIntervals (line 73: startTime + fileInterval.Start/End). But the file-boundary marker added on line 48, `intervals.Add(new Interval(file.FilePath, fileDuration, fileDuration))`, passes the RELATIVE file duration as both Start and End instead of the absolute end `startTime + fileDuration`. The Interval ctor signature is (fileFullName, startTime, endTime), so for the 2nd+ file this boundary marker lands at a relative offset rather than its true position on the concatenated timeline. ChaptersFactory.CreateFromIntervals consumes these intervals in order treating interval.Start/End as absolute, so the bad marker corrupts the running startTime (interval.End - startTime can be negative and `startTime += interval.End - startTime` collapses the timeline backward).

**Impact:** For any multi-file silence scan, chapter boundaries derived from the file-end markers of the 2nd and later files are placed at wrong (relative) times, yielding negative/zero-length or misaligned chapters and a corrupted chapter timeline. Single-file scans are unaffected since startTime is zero (relative == absolute).

**Repro:** Open Create Chapters wizard with two or more audio files loaded, run Scan For Silence. Resulting chapters from silence: the boundary at each file transition (after the first file) is computed from the relative file duration, producing misplaced/negative-span chapters instead of contiguous absolute-timeline boundaries.

**Suggested fix:** On ScanForSilenceCommand.cs line 48 use absolute end: `new Interval(file.FilePath, startTime + fileDuration, startTime + fileDuration)`.

---

### 25. CreateMetadataFile failure silently swallowed -> tags/chapters dropped with no user feedback

`☑ Fixed` (2026-07-07) · **Severity** Medium · **Importance** 5 · **Class** real · **Finders** 1 · **Ref** C49 · **Present** yes

**Fix note (2026-07-07):** CreateMetadataFile now deletes any partial temp metadata file and rethrows an IOException on non-cancellation write failures, so Concatenate reports the existing "Concatenation exception" error through ConcatErrorWindow instead of silently producing output without requested metadata.

**Location:** AudioCat/FFmpeg/FFmpegService.cs:489-525 (catch at 519-524); caller FFmpegService.cs:140,150-162

**Description:** CreateMetadataFile builds a temp FFMETADATA file for output tags/chapters. Its general catch block (lines 519-524) swallows any non-cancellation exception (IO error, disk full, access denied, encoding failure) and returns "". This empty-string return is indistinguishable from the legitimate "no metadata to write" path (lines 497-498 also return ""). The caller (Concatenate, line 150-162) treats "" purely as "no metadata", omits the -i metadata arg, and proceeds with concatenation; no OnError/Error event or status is ever raised on the failure path.

**Impact:** If writing the metadata temp file fails at runtime, the output file is produced WITHOUT the user-configured tags and chapters, while the operation reports success. The user gets a silently degraded output (lost metadata) with no warning or error.

**Repro:** Enable tags and/or chapters for the output, then make the metadata temp write fail (e.g. fill the temp drive or deny write access to %TEMP%) and run Concatenate. The output is created without tags/chapters and no error is shown. Hard to hit deterministically under normal conditions, but reachable via IO/disk failures.

**Suggested fix:** On the catch path, surface the failure (await OnError / propagate) or return a distinct failure sentinel so Concatenate can warn the user instead of silently dropping metadata.

---

### 26. CUE parser rejects tracks with more than one INDEX command (pregap INDEX 00 + INDEX 01)

`☑ Fixed` (2026-07-07) · **Severity** Low (downgraded from Medium after corpus scan) · **Importance** 5 · **Class** partial · **Finders** 1 · **Ref** C64 · **Present** yes

**Audit note (2026-07-07):** Scanned `cue-sample` corpus: 244 `.cue` files, 6,879 tracks, 6,879 INDEX commands, 0 tracks with multiple INDEX commands, and 0 non-`INDEX 01` commands. So this is a valid CUE-spec compatibility edge case, but not evidenced by the audiobook-style sample set.

**Fix note (2026-07-07):** Parser now accepts multiple INDEX commands per track. It prefers `INDEX 01` as the stored track start, preserves prior behavior for files with only a non-01 index, and ignores extra subindexes instead of failing the whole CUE parse.

**Location:** AudioCat/Cue/Parser.cs:74 (and supporting state: Context.IndexFound line 30, ProcessIndexCommand lines 152-157, TrackBuilder.SetIndex single-index storage)

**Description:** In Parser.ProcessCommand, the switch arm `IIndexCommand when context.IndexFound => Response<ICue>.Failure("More than one INDEX command specified in the TRACK command")` (line 74) treats any second INDEX within a TRACK as a fatal parse error. After the first INDEX, ProcessIndexCommand sets context.IndexFound=true (line 155), so a subsequent INDEX line aborts the whole parse. The CUE spec permits multiple INDEX commands per track — most commonly `INDEX 00` (pregap) plus `INDEX 01` (track start), and up to 99 subindexes. The TrackBuilder/IIndex model also only stores a single index, so the design assumes one INDEX per track.

**Impact:** Loading any standard CUE sheet that uses a pregap (INDEX 00 before INDEX 01) or subindexes fails entirely with "More than one INDEX command specified in the TRACK command", so valid real-world CUE files are rejected and chapter/cue import does not work for them. Reachable at runtime via GetCueFileCommand.

**Repro:** 1. Create a .cue file with a track containing two INDEX lines, e.g. `INDEX 00 00:00:00` then `INDEX 01 00:02:00`. 2. Trigger the Get CUE file action (GetCueFileCommand -> Cue.Parser.Parse). 3. Parsing returns Failure and the CUE is rejected instead of being loaded.

**Suggested fix:** Accept multiple INDEX commands: keep the first INDEX 01 (or the lowest/primary index) as the track start and ignore/store additional indexes (pregap INDEX 00, subindexes) instead of failing.

---

### 27. Adding a file clears manually entered output tags when files have no chapters (OutputTags.Clear on !ChaptersExist)

`☑ Fixed` (2026-07-08) · **Severity** Medium · **Importance** 5 · **Class** real · **Finders** 1 · **Ref** C70 · **Present** yes

**Fix note (2026-07-08):** removed `OutputTags.Clear()` + early `return` from the no-chapters branch of OnFilesCollectionChanged; the chapters-regeneration block became `else if`, and both paths now fall through to the existing `if (OutputTags.Count == 0) SelectOutputTagsOnFilesLoad()`. User-entered tags survive file add/remove; as a bonus, chapter-less file lists now auto-populate output tags from the first tagged file (previously unreachable — the early return skipped population). Emptying the file list still resets tags via ClearOutput().

**Location:** AudioCat/ViewModels/MainViewModel.cs:729-736 (OnFilesCollectionChanged); populated via SelectOutputTagsOnFilesLoad 767-777 and SetTo 575

**Description:** OnFilesCollectionChanged fires on every file add/remove. When the file set contains no chapters (the common case for most plain audio files), the guard at line 729 (`Files.Count == 0 || !Files.ChaptersExist()`) is taken and line 735 unconditionally calls `OutputTags.Clear()` before returning. Because OutputTags is the user-editable output metadata collection (populated initially from the first tagged file and editable in the UI), every subsequent file addition wipes it, even after the user has manually entered/edited tag values. There is no check distinguishing user-modified tags from auto-loaded ones.

**Impact:** User-entered output tags are silently discarded whenever another file is added (or one removed) to a chapter-less file list, causing loss of manually entered metadata and forcing re-entry. Affects the common workflow of building up a file list then tagging, or tagging then adding more files.

**Repro:** 1. Add an audio file with no embedded chapters. 2. Expand Output Tags and manually enter/edit tag values (e.g. Title/Album). 3. Add (or remove) another file. 4. Observe OutputTags is cleared - the manually entered tags are gone.

**Suggested fix:** Do not Clear() OutputTags in the no-chapters branch; only clear chapter-related state, or guard the clear so user-modified tags are preserved.

---

### 28. Pause() does not update CurrentState -> Play() within 100ms poll window no-ops (stays paused)

`☑ Fixed` (2026-07-08) · **Severity** Low · **Importance** 5 · **Class** real · **Finders** 1 · **Ref** C28 · **Present** yes

**Location:** AudioCat/Services/AudioFilePlayer.cs:148-189 (Play 148-154, Pause 156-157, CurrentState setter 64-74, OnPlaybackStateUpdate 177-186)

**Description:** Pause() (line 156-157) only calls OutputDevice.Pause() and never updates CurrentState; state is resynced only by the 100ms PeriodicInvoker poll (OnPlaybackStateUpdate, 177-186). Play() (148-154) early-returns when CurrentState == Playing. After a Pause(), CurrentState remains a stale Playing until the next poll tick, so a Play() call within that ~100ms window hits the guard and returns without ever calling OutputDevice.Play() — the play press is silently dropped and playback stays paused until the user presses play a second time (after the poll has resynced state to Paused).

**Impact:** Rapid pause→play leaves the player paused and the press is swallowed. Also, PlaybackStateChanged fires up to ~100ms late since imperative calls don't set state eagerly. Latent: AudioFilePlayer is not yet wired into the UI.

**Repro:** Latent until playback is wired (PlayPause wiring commented out at CreateChaptersViewModel.cs:642-649). Once wired: play, pause, then press play again within ~100ms — playback stays paused.

**Suggested fix:** Set CurrentState eagerly in Play()/Pause(), or guard Play() on OutputDevice.PlaybackState instead of the polled CurrentState.

**Re-audit (2026-07-01):** the earlier audit-note dismissal ("Play() never no-ops", Present=no) was itself wrong — it overlooked that the stale *Playing* state after Pause() is precisely what blocks Play(). Mechanism re-verified line-by-line; the defect is real as originally titled.

**Status note (2026-07-08):** survived the player wiring and is no longer latent. Pause() still does not update CurrentState (poller-only resync, 100ms) and Play() still guards on the polled CurrentState; the live path is ChaptersPlayer.Pause → Resume (ActivePlayer.Play()) within the poll window: the device resume is silently dropped while the engine reports Playing, leaving the transport UI in "playing" with paused audio until the user toggles again.

**Fix note (2026-07-08):** Play() now guards on `OutputDevice.PlaybackState` (updated synchronously by NAudio in Play/Pause) instead of the polled `CurrentState`, so a resume within the poll window is no longer swallowed. CurrentState is deliberately left poller-only: setting it eagerly would suppress the poller's change detection and force raising PlaybackStateChanged under Sync (lock-order inversion the poller design avoids). Pause() unchanged — NAudio's Pause() already no-ops unless playing. UI state label still lags ≤100ms; that cosmetic lag is by design, the dropped-resume defect is gone.

---

### 29. Duplicate pollers after pause->resume (PeriodicInvoker.Start not idempotent; Play() reruns Start)

`☑ Fixed` (2026-07-08) · **Severity** Low · **Importance** 4 · **Class** real · **Finders** 7 · **Ref** C27 · **Present** yes

**Fix note (2026-07-08):** fixed at the caller during the player wiring (feature `chapters-player-wiring`): AudioFilePlayer.Play now starts the status poller at most once per player lifetime via an `IsStatusPollerStarted` flag guarded by `Sync` ("Start the poller exactly once; PeriodicInvoker.Start spawns a new loop on every call"); pause→resume reuses the already-running poller, and ChaptersPlayer.Resume resumes the live player instead of creating a new one. PeriodicInvoker.Start itself is still not idempotent, but no code path can invoke it twice on the same instance.

**Location:** AudioCat/Services/PeriodicInvoker.cs:11-16; AudioCat/Services/AudioFilePlayer.cs:148-157

**Description:** PeriodicInvoker.Start() unconditionally assigns EventInvokerTask = EventInvokerLoop(Cts.Token) with no guard for an already-running loop; it neither checks nor cancels a prior task. AudioFilePlayer.Play() (line 148) only early-returns when CurrentState == Playing, then calls PlayerStatusInvoker.Start() before OutputDevice.Play(). After Pause() (line 156) the polled state becomes Paused, so a subsequent Play() passes the guard and calls Start() again, spawning a second concurrent EventInvokerLoop sharing the same Cts. EventInvokerTask only references the latest loop, so the earlier orphaned poller is untracked and Dispose()/DisposeAsync() awaits only one of them.

**Impact:** Each pause->resume cycle adds another 100ms poller loop firing OnPlaybackStateUpdate, causing duplicate PlaybackStateChanged/PlaybackPositionChanged events and growing CPU/event churn; on dispose, orphaned loops are not awaited (only Cts.Cancel signals them). Currently latent because the player is not yet wired to the UI.

**Repro:** Latent - AudioFilePlayer is consumed only by ChaptersPlayer, whose Play/Pause is invoked from a PlayPause command that is commented out (CreateChaptersViewModel.cs:642-649). Once wired: Play, then Pause, then Play again -> a second PeriodicInvoker loop runs concurrently with the first.

**Suggested fix:** Make Start() idempotent (no-op if EventInvokerTask is already running/not completed), or have Play() call Start() only when transitioning from Stopped, using OutputDevice.Play() to resume from Paused.

**Audit note:** Defect is real in code but currently unreachable: ChaptersPlayer (the only consumer of AudioFilePlayer.Play/Pause) is instantiated in CreateChaptersViewModel.cs:640, but the PlayPause command that would invoke Play()/Pause() is commented out (lines 642-649, "TODO Playback wiring"). Hence latent.

---

### 31. Startup continuations run on thread-pool thread and touch UI state / enumerate Files during population

`☑ Fixed` (2026-07-08) · **Severity** Medium · **Importance** 4 · **Class** partial · **Finders** 4 · **Ref** C13 · **Present** yes

**Location:** AudioCat/ViewModels/MainViewModel.cs:438-441 (continuation chain), 495-502 (VerifyMediaFileServiceIsAccessible, sets IsUserEntryEnabled off-thread at 499), 504-526 (AddCliFilesOnStartup), 528-562 (AddOutputChaptersOnStartup, enumerates Files at 530 & 538)

**Description:** The constructor fires a startup pipeline `VerifyMediaFileServiceIsAccessible().ContinueWith(AddCliFilesOnStartup).ContinueWith(AddOutputChaptersOnStartup).ContinueWith(EnableUserEntryOnStartup)`. ContinueWith uses the default TaskScheduler, so every continuation body runs on a thread-pool thread, not the UI dispatcher. Two concrete problems: (1) the continuation delegates are `async Task` but the chain is not `.Unwrap()`ed, so each ContinueWith fires when the previous async method merely reaches its first await (returns its outer Task<Task>), NOT when its inner async work completes - thus AddOutputChaptersOnStartup begins while AddCliFilesOnStartup's still-pending `MediaFilesService.AddMediaFiles` is dispatching `Files.Add(...)` onto the UI thread. AddOutputChaptersOnStartup then enumerates the live ObservableCollection `Files` off-thread via `Files.ChaptersExist()` (530) and `Files.AsReadOnly()` (538) concurrently with those UI-thread inserts. (2) VerifyMediaFileServiceIsAccessible sets `IsUserEntryEnabled = true` (499) directly from a pool thread, raising a cascade of PropertyChanged events off the UI thread. Note the actual collection writes inside AddMediaFiles are correctly marshalled via Dispatcher.InvokeAsync (MediaFilesService.cs:157,186-195), so the collection is never *written* cross-thread - only read/enumerated.

**Impact:** When the app is launched with file/directory CLI arguments, the off-thread enumeration of `Files` in AddOutputChaptersOnStartup can race with UI-thread `Files.Add` inserts and throw `InvalidOperationException` (collection modified during enumeration), which is silently swallowed by the surrounding try/catch and may drop auto-detected chapters. Without CLI args, AddClireturn early and the chapter step finds an empty list, so the race is not hit; the off-thread PropertyChanged from IsUserEntryEnabled is tolerated by WPF's automatic binding marshalling.

**Repro:** Launch the app with one or more audio files/directories as command-line arguments (e.g. associate files and open, or `AudioCat.exe a.mp3 b.mp3 c.mp3` with files that contain chapters). The probing/Files.Add work dispatched by AddCliFilesOnStartup overlaps the thread-pool enumeration in AddOutputChaptersOnStartup; under timing pressure this throws collection-modified and the chapters are not populated (exception swallowed). Without CLI args this path is not reachable.

**Suggested fix:** Replace the ContinueWith chain with a single `async void`/awaited startup method that awaits each step in sequence, and marshal any Files reads and IsUserEntryEnabled writes onto the dispatcher (or run the whole pipeline on the UI context).

**Re-verification (2026-07-08):** root cause confirmed real in the pre-refactor code (un-unwrapped ContinueWith chain, default scheduler), but two ticket corrections. (1) The headline collection-modified race was the *improbable* symptom: AddOutputChaptersOnStartup fired at AddCliFilesOnStartup's first suspension (`await AddMediaFiles`), before any probing finished or any dispatcher-marshaled `Files.Add` landed — so `Files.ChaptersExist()` saw an empty list and returned early. Dominant behavior was CLI-startup chapters deterministically never populating, no exception involved; the enumeration race needed the pool continuation delayed past the start of probing inserts. (2) Sub-claim about `IsUserEntryEnabled = true` running off-thread at line 499 was false — VerifyMediaFileServiceIsAccessible was invoked directly from the UI-thread ctor and its await resumed on the captured dispatcher context; the continuation that did run on a pool thread (EnableUserEntryOnStartup) already marshaled via InvokeAsync.

**Fix note (2026-07-08):** fixed by the startup refactor that landed with the player wiring. The ContinueWith chain is replaced by `_ = InitializeAsync()` — sequential `await`s launched from the UI-thread constructor with no `ConfigureAwait(false)`, so every continuation resumes on the dispatcher. AddOutputChaptersOnStartup and EnableUserEntryOnStartup are deleted: output chapters are now populated by OnFilesCollectionChanged (fires on the UI thread since all `Files` mutations are dispatcher-marshaled in MediaFilesService), and IsUserEntryEnabled is set inside VerifyMediaFileServiceIsAccessible on the UI thread. No off-thread `Files` reads or UI-state writes remain on the startup path.

---

### 32. Pervasive silent catch blocks (~40) hide genuine failures

`☑ Fixed` (2026-07-08) · **Severity** Low · **Importance** 4 · **Class** partial · **Finders** 3 · **Ref** C114 · **Present** yes

**Re-verification (2026-07-08):** the ~40-catch headline was inflated; the audit note's concession is confirmed site-by-site. All FFmpegService `/* ignore */` catches wrap best-effort temp-file deletes or task joins on cancel paths; PeriodicInvoker's are dispose-cleanup plus one deliberate keep-the-poller-alive catch around the periodic callback; App.xaml.cs wraps optional encoding-provider registration. None are defects. The one serious claim — CommandBase.Execute's empty catch reducing every unexpected command exception to an undisplayed "Unknown error" — was accurate at HEAD but had already been fixed in the working tree by the player-wiring work: `OperationCanceledException` now finishes quietly as Success, and the general catch does `Debug.WriteLine(ex)` and returns `Failure(ex.Message)`, so the real message reaches the Finished response. Residual gaps found: OnCreateChaptersFinished silently returned on IsFailure, and AddCliFilesOnStartup's catch was bare `/* ignore */`. (AddFiles/AddPath/MoveFile/FixEncoding have no Finished subscriber, but they surface their own errors via dialogs internally; only unexpected exceptions pass unshown, now at least Debug-logged by CommandBase.)

**Fix note (2026-07-08):** residual gaps closed. OnCreateChaptersFinished now shows a MessageBox on a failure response instead of silently returning — CreateChaptersCommand itself only ever returns Success, so a failure there is by definition an unexpected exception surfaced by CommandBase and must be shown. AddCliFilesOnStartup's catch now does `Debug.WriteLine(ex)` (kept non-fatal deliberately: CLI-passed files failing to load should not crash startup). The ticket's suggested fix (introduce a DI logging facility) was evaluated and rejected as disproportionate for a small desktop app with no field-diagnostics requirement; `Debug.WriteLine` in CommandBase covers development use, and a logger can be added later if field diagnosis is ever needed.

**Location:** AudioCat/Commands/CommandBase.cs:28-31; AudioCat/ViewModels/MainViewModel.cs:524-525, 557-561; AudioCat/App.xaml.cs:33-34; AudioCat/FFmpeg/FFmpegService.cs:76,198,208,308,335,440,522,732; AudioCat/Services/PeriodicInvoker.cs:23,35-39,49-53

**Description:** CommandBase.Execute (CommandBase.cs:28) wraps every user command (AddFiles, Concatenate, MoveFile, FixEncoding, CreateChapters, ScanForSilence, etc.) in try/catch with an empty body, so any unexpected exception inside a command handler is swallowed and only the pre-seeded "Unknown error" Response is surfaced via OnFinished. MainViewModel.cs:524 swallows all errors from startup file loading (AddMediaFiles via command-line args), and :557 catches into a generic chapter-clear with no diagnostics. Across the app there is no logging facility (no logger in App.xaml.cs DI), so none of the ~40 catch blocks record anything. The remaining catches in FFmpegService/PeriodicInvoker are mostly best-effort temp-file deletion and dispose, which are legitimately ignorable.

**Impact:** Genuine but non-cancellation exceptions in a command (e.g. an unexpected bug in concat/probe orchestration) are silently absorbed: the UI re-enables with no error shown and no log, making field diagnosis very hard. Most other swallowed catches are harmless cleanup, so overall user-facing impact is moderate, not data-loss.

**Repro:** Make a command handler throw an unexpected (non-OperationCanceled) exception not already converted to a Response - e.g. force AddMediaFiles to throw during startup arg processing. App continues with no message box, no log entry; CommandBase callers just see a generic "Unknown error" Response that is typically not displayed.

**Suggested fix:** Introduce a logger (DI) and at minimum log exceptions in CommandBase.Execute and the MainViewModel startup catches; narrow other catches to expected types or add trace logging.

**Audit note:** Confirmed-but-partial: ~35-40 empty/comment-only catch blocks exist across the codebase, but the "hide genuine failures" claim only holds for a subset. The majority are defensible best-effort patterns (temp-file File.Delete cleanup, IDisposable.Dispose in PeriodicInvoker, OperationCanceled/TaskCanceled ignores, optional encoding-provider registration). The meaningfully risky ones are the central command dispatcher and the startup handlers, which swallow ALL exceptions with no logging anywhere in the app (no logger is registered in DI).

---

### 33. Global timeline offsets use metadata Duration; null/image durations corrupt seek offsets & file-transition

`☑ Fixed` (2026-07-08) · **Severity** Low · **Importance** 4 · **Class** partial · **Finders** 3 · **Ref** C31 · **Present** yes

**Fix note (2026-07-08):** resolved by the ChaptersPlayer rewrite during the player wiring, exactly per the suggested fix: the constructor builds a `PlayableFiles` list that skips `file.IsImage` entries and files whose `Duration` is null, and ALL timeline math (offset accumulation, TryFindFile, seeks, natural file rollover) operates only on that list with each entry's known non-null duration. `Duration ?? TimeSpan.Zero` coalescing is gone; image/unknown-duration entries can no longer collapse subsequent offsets.

**Location:** AudioCat/Services/ChaptersPlayer.cs:209,269-290 (TryFindFile loop uses Files[i].Duration ?? TimeSpan.Zero; OnPlaybackStateChanged file-transition at line 209 same); source of nulls: AudioCat/ViewModels/MediaFileViewModel.cs:79,85 and AudioCat/FFmpeg/FFprobeMediaFile.cs:44

**Description:** ChaptersPlayer builds a global playback timeline by summing per-file metadata Duration values, using `Files[i].Duration ?? TimeSpan.Zero` in both TryFindFile (line 274) and the natural file-transition handler OnPlaybackStateChanged (line 209). MediaFileViewModel.Duration is null for image/cover files (MediaFileViewModel.cs:79) and can also be null when ffprobe's format element omits the duration attribute (FFprobeMediaFile.cs:44, SecondsToTimeSpan on a missing value). Treating a null/zero duration as a zero-length file means accumulated offsets for all subsequent files collapse, so fileGlobalOffset and the seek positionInFile (globalPosition - fileGlobalOffset) become wrong, and the next-file offset computed at transition is short by the missing file's real length.

**Impact:** When a playlist contains an image-only entry or a file whose duration ffprobe did not report, seek offsets and file-to-file transitions are mis-aligned: playback jumps to the wrong file/position and chapter boundary detection drifts. Currently latent because ChaptersPlayer is not instantiated/wired into the UI yet.

**Repro:** Latent - once playback is wired: add an audio file then a cover-image entry (Duration null) ahead of another audio file, start chapter playback, and observe that offsets past the image are off by the image being treated as zero-length and that file transitions land at the wrong position.

**Suggested fix:** Exclude image/cover entries from the timeline and treat a null Duration as an error/skip (or probe actual stream duration) rather than coalescing to TimeSpan.Zero.

**Re-audit (2026-07-01):** the earlier claim that ChaptersPlayer "is never constructed anywhere" is stale — it IS constructed at CreateChaptersViewModel.cs:640; the defect is latent only because Play/Stop invocation (PlayPause) is commented out at :642-649.

---

### 34. Disposed-object race in periodic status update (ObjectDisposedException reading reader/device after Dispose)

`☑ Fixed` (2026-07-08) · **Severity** Low · **Importance** 4 · **Class** real · **Finders** 2 · **Ref** C33 · **Present** yes

**Fix note (2026-07-08):** fixed as predicted in the #4 fix note and re-verified after the wiring. AudioFilePlayer.Dispose/DisposeAsync now join the status poller FIRST (PlayerStatusInvoker.Dispose before touching device/reader), then dispose OutputDevice/reader under the `Sync` lock; OnPlaybackStateUpdate takes `Sync` and exits on `IsDisposed`, so no callback can read a disposed NAudio object. A second, unguarded reader the original issue didn't cover — WaveOutEvent's own playback thread calling Read() (its Dispose does not join that thread) — was closed on 2026-07-08 with `DisposalGuardedSampleProvider`, which serializes reads against reader disposal and reports end-of-stream once disposed (that race surfaced live as MediaFoundation COMException 0x8000FFFF "Catastrophic failure" on rapid waveform seeks).

**Location:** AudioCat/Services/AudioFilePlayer.cs:116-125 (Dispose order), 177-186 (OnPlaybackStateUpdate); AudioCat/Services/PeriodicInvoker.cs:18-40

**Description:** The PeriodicInvoker callback AudioFilePlayer.OnPlaybackStateUpdate (lines 177-186) reads OutputDevice.PlaybackState and AudioFileReader.CurrentTime every 100 ms on the EventInvokerTask thread. AudioFilePlayer.Dispose() (116-125) disposes OutputDevice (122) and AudioFileReader (123) BEFORE disposing PlayerStatusInvoker (124). Because the invoker is stopped/awaited last, a callback already in flight (or one that fires between the device dispose and the invoker dispose) reads the just-disposed NAudio OutputDevice/AudioFileReader, producing an ObjectDisposedException. The same ordering applies in DisposeAsync (127-146). The exception is swallowed by PeriodicInvoker's empty catch (PeriodicInvoker.cs:23), so it is masked rather than fatal.

**Impact:** Once playback is wired, disposing/stopping a playing AudioFilePlayer can race the 100 ms status loop into accessing disposed NAudio objects; the exception is caught and ignored, so practically a discarded tick rather than a crash. Currently latent and unreachable because the player is not yet hooked up to the UI.

**Repro:** Latent - ChaptersPlayer.Play/Stop are never invoked (CreateChaptersViewModel.cs:646-648 commented out), so AudioFilePlayer is never instantiated at runtime. After wiring: start playback, then Dispose/Stop the player while the 100 ms invoker tick is mid-flight to make OnPlaybackStateUpdate read the disposed OutputDevice/AudioFileReader.

**Suggested fix:** In AudioFilePlayer.Dispose/DisposeAsync, dispose PlayerStatusInvoker first (cancel+await the loop) before disposing OutputDevice and AudioFileReader.

**Audit note:** Defect is real in committed code but latent: the player is not yet wired (CreateChaptersViewModel.cs:646-648 are commented out, no caller invokes ChaptersPlayer.Play/Stop), so it cannot be hit at runtime today.

---

### 35. DoNotInvokeFilesCollectionChangedEvent flag never reset if exception thrown during add (no try/finally)

`☑ Fixed` (2026-07-08) · **Severity** Low · **Importance** 4 · **Class** real · **Finders** 2 · **Ref** C55 · **Present** yes

**Re-verification (2026-07-08):** structure claim accurate (no try/finally, reset only in last-iteration lambda), but three corrections. (1) The ticket's named trigger is self-defeating: MainViewModel.OnFilesCollectionChanged checks the flag first and returns, so while the flag is true the handler is inert and cannot throw; it only runs on the last Add, where the lambda has already set the flag false. It is the only code subscriber to Files.CollectionChanged — remaining throw sources while suppressed are WPF CollectionView internals (exotic) or dispatcher shutdown (app exiting, moot). (2) "DataGrid stops updating" is false — rows update via ItemsSource binding, which does not consult the flag; only MainViewModel derived state (totals, codec, chapter regen, button enables) goes stale. (3) "Until restart" is false — the next successful AddMediaFiles resets the flag at its own last iteration, self-healing. Probability near zero; kept the fix as cheap hardening.

**Fix note (2026-07-08):** loop wrapped in try/finally that resets the flag — exception insurance only. The ticket's suggested fix as written ("set before the loop and reset in a finally block or after the loop") was rejected: it would leave the flag true during the last Add, swallowing the single batch notification the design deliberately fires, so derived UI state would never refresh after any add. The last-iteration in-lambda reset is kept for the normal path; the finally only matters on the exception path (added files then go un-notified until the next collection change — acceptable vs a stuck flag).

**Location:** AudioCat/Services/MediaFilesService.cs:181-192

**Description:** In AddMediaFiles, MediaFilesContainer.DoNotInvokeFilesCollectionChangedEvent is set to true (line 182) before the loop that dispatches each file Add to the UI thread. It is only reset to false inside the InvokeAsync lambda during the final iteration (line 188-189). There is no try/finally guarding the flag, so if any dispatched Add throws (e.g. a CollectionChanged subscriber in MainViewModel.OnFilesCollectionChanged throws), the loop is cancelled, or the Dispatcher shuts down before the last item is processed, the flag is never reset and remains true.

**Impact:** If an exception interrupts the add loop before the last file, the suppression flag stays true for the rest of the app session: MainViewModel.cs:694 checks this flag and silently drops all subsequent Files CollectionChanged notifications, so the DataGrid and dependent UI state stop updating until restart.

**Repro:** Latent/edge - requires an exception in a dispatched Files.Add (e.g. a throwing CollectionChanged handler) or Dispatcher shutdown mid-add. Under normal operation the last iteration always resets the flag, so it is hard to trigger deterministically.

**Suggested fix:** Set the flag once before the loop and reset it in a finally block (or after the loop completes) rather than inside the last-iteration lambda.

---

### 36. PeriodicInvoker.Dispose blocks calling/UI thread on EventInvokerTask.Wait()

`☑ Fixed` (2026-07-08) · **Severity** Low · **Importance** 4 · **Class** real · **Finders** 2 · **Ref** C78 · **Present** yes

**Fix note (2026-07-08):** resolved during the player wiring by moving disposal off the UI thread rather than making Dispose non-blocking: ChaptersPlayer.DisposePlayer pauses the device for immediate silence and defers AudioFilePlayer.Dispose to the thread pool (`Task.Run`), and every player teardown path (Stop, seek file-swap, rollover, error recovery, ChaptersPlayer.Dispose from the wizard close) goes through DisposePlayer. AudioFilePlayer is the only PeriodicInvoker consumer, so no UI-thread caller of PeriodicInvoker.Dispose exists. The blocking join for outside callers is kept deliberately — it is what makes teardown race-free (see #34) — and the self-join case is detected and skipped (see #3).

**Location:** AudioCat/Services/PeriodicInvoker.cs:28-40 (Wait at :34); callers AudioFilePlayer.cs:124, ChaptersPlayer.cs:123,226

**Description:** PeriodicInvoker.Dispose() (line 34) calls Cts.Cancel() then synchronously blocks the calling thread on EventInvokerTask.Wait(). The blocking is bounded by any in-flight callback: `await Task.Delay(interval, ctx)` (line 24) sits OUTSIDE the try/catch, so once cancelled the Delay faults the loop task immediately (the TaskCanceledException is swallowed by Dispose's own catch at :35) — but if a callback is mid-execution, Wait() blocks until it finishes. Callbacks fire ChaptersPlayer event handlers that take the Sync semaphore and open audio files, so they can be slow.

**Impact:** When the player is eventually wired, disposing a player from the UI thread (e.g. closing the chapters window / ChaptersPlayer.Dispose -> AudioFilePlayer.Dispose -> PlayerStatusInvoker.Dispose) blocks the UI thread for the remainder of any in-flight callback (which may include audio-file opens/event handlers). Currently latent: the player is not invoked at runtime since PlayPause is commented out.

**Repro:** Latent - audio player not wired (CreateChaptersViewModel.PlayPause commented out, 642-648). Once enabled: start playback then close the chapters window on the UI thread while a status callback is mid-flight; Dispose blocks on EventInvokerTask.Wait() until the callback returns.

**Suggested fix:** Make Dispose non-blocking (fire-and-forget the cancel, or have callers use DisposeAsync); avoid sync .Wait() on the UI thread.

**Re-audit (2026-07-01):** the originally claimed "plus up to one interval (100 ms)" mechanism was wrong — cancellation faults the Delay immediately since it is outside the try/catch; the block is bounded by in-flight callbacks only. Distinct from the C06 self-join deadlock: C78 is the plain synchronous-block-on-Wait when Dispose is invoked from an external (UI) thread.

---

### 38. Incomplete command-line escaping for metadata tag values (only escapes double-quote, not backslash)

`☑ Fixed` (2026-07-08) · **Severity** Low · **Importance** 4 · **Class** real · **Finders** 1 · **Ref** C73 · **Present** yes

**Re-verification (2026-07-08):** present and real (code drifted to FFmpegService.cs:680; raw ProcessStartInfo.Arguments confirmed at Process.cs:124, UseShellExecute=false, so MSVCRT tokenization applies), with three corrections. (1) The "AC\DC" example is wrong: under CRT rules a backslash not adjacent to a quote is literal, so interior backslashes are benign; the breaking inputs are only a value ending in `\` (trailing backslash swallows the closing quote and slurps the remaining flags plus output path into the metadata value) or containing `\"`. (2) The suggested fix `Replace("\\","\\\\")` is actively harmful — blanket-doubling corrupts the benign case (`AC\DC` would parse back as `AC\\DC`). (3) The ticket missed that `tag.Name` was entirely unescaped and unfiltered on this path — a name with a space, quote or `=` breaks tokenization the same way (the chapter path filters names; this one didn't). Probability low: requires embedded cover art carrying a non-`comment` stream tag with a breaking value; failure mode is a failed AddImages step with a confusing aggregated error, not silent corruption.

**Fix note (2026-07-08):** added `EscapeCliArgValue` implementing canonical Win32 argument escaping — backslash runs are doubled only when they precede a double quote or the end of the value (our closing quote), quotes become `\"`, interior backslashes pass through untouched. Tag names are now sanitized with `FilterPrintable().Trim()` (matching the chapter path) and tags whose name still contains a space, quote or `=` are dropped rather than emitted as a broken argument. File paths on the same command line were reviewed and left as-is (cannot contain `"`, cannot end in `\` — they are files).

**Location:** AudioCat/FFmpeg/FFmpegService.cs:664 (GetMetadataQuery); contrast with FilterMetadataValue at lines 585-607 (esp. line 598)

**Description:** In GetMetadataQuery (line 664) image-stream metadata tags are emitted directly onto the FFmpeg command line as `-metadata:s:v:{i} {tag.Name}="{tag.Value.Replace("\"","\\\"")}"`. The value is only escaped for double-quote; backslashes are passed through verbatim. Since the whole string becomes ProcessStartInfo.Arguments (Services/Process.cs:100) and is parsed by the Windows C-runtime rules, a backslash sequence is interpreted specially — a value ending in a backslash collapses the following closing quote into an escaped `\"`, breaking argument tokenization. The main tag/chapter path writes to a metadata file and DOES escape backslash (FilterMetadataValue, line 598), so this command-line path is inconsistent and under-escaped.

**Impact:** When concatenating files whose embedded cover-art streams carry tag values containing backslashes (especially a trailing backslash, e.g. a Windows path or "AC\DC"), the FFmpeg arguments get mis-tokenized, causing the AddImages step to fail or to write a corrupted/mangled tag onto the output cover image. Only affects outputs where source files contain embedded images with backslash-bearing stream tags, so impact is narrow but real.

**Repro:** Add an input audio file with embedded cover art whose video-stream metadata tag value ends in a backslash; concatenate; AddImages mis-tokenizes args and FFmpeg errors or mistags.

**Suggested fix:** Escape backslash before quote in line 664: `tag.Value.Replace("\\","\\\\").Replace("\"","\\\"")`, or route image-stream tags through a file-based metadata mechanism like FilterMetadataValue.

---

### 39. GenerateTempOutputFileFrom returns empty string after 3 failures -> empty path passed to ffmpeg

`☑ Fixed` (2026-07-08) · **Severity** Low · **Importance** 3 · **Class** real · **Finders** 6 · **Ref** C48 · **Present** yes

**Re-verification (2026-07-08):** code defect present exactly as described (drifted to FFmpegService.cs:441-456; callers :159, :212, :408, no empty check), but two corrections. (1) The repro cannot reach the defect: an unwritable %TEMP% fails earlier at TempDirectory.Create (:140) or CreateFilesListFile (:142, bare FileStream) — both land in the outer catch as a clean "Concatenation exception" before GenerateTempOutputFileFrom is ever called; same shielding in RemuxFile (:407 precedes :408). Actually hitting the "" return requires temp writes to START failing in the window between the list file succeeding and the zero-byte write, then fail 3x — a millisecond-scale race, not a static disk-full condition. (2) The "cleanup FileInfo on ''" claim is false — cleanup constructs FileInfo(outputFileName) (:251), the user's real path; no FileInfo is ever built on outputToFile. Consequences if hit are loud (ffmpeg errors aggregated via OnError), not corrupting.

**Fix note (2026-07-08):** GenerateTempOutputFileFrom now throws IOException on exhaustion (preserving the last exception as message + inner) instead of returning "", matching CreateMetadataFile's existing pattern. Preferred over empty-string checks at three call sites: all callers already run inside try/catch that surfaces the message (Concatenate's outer catch directly; RemuxFile via Task.WhenAll rethrow in RemuxFiles into the same catch), so the confusing ffmpeg empty-output error becomes a clear "Failed to create a temporary output file: <reason>".

**Location:** AudioCat/FFmpeg/FFmpegService.cs:429-444 (definition); callers at 154-156, 211-213, 396

**Description:** GenerateTempOutputFileFrom tries 3 times to create a zero-byte temp file via File.WriteAllBytesAsync; on total failure it swallows all exceptions and returns "" (line 443). All three call sites use the result directly with no empty-string check: outputToFile = await GenerateTempOutputFileFrom(...) at lines 155 and 212, and line 396 in RemuxFile. That value is substituted verbatim into the ffmpeg command via GetFFmpegArgs as -update true "{outputToFile}", yielding -update true "".

**Impact:** If temp-file creation fails (disk full, temp dir permissions/quota), ffmpeg is launched with an empty output path, failing with a confusing error; subsequent steps (AddImages, second-step concat, cleanup FileInfo on "") also misbehave. Real but rare since it only triggers when temp writes fail 3x; the surrounding try/catch will turn it into a generic "Concatenation exception" message.

**Repro:** Latent-ish: make %TEMP% unwritable (full disk or denied permissions), add OGG Vorbis files with tags (twoStepsConcat) or any codec with an embedded cover image (hasImages), then run Concatenate. GenerateTempOutputFileFrom returns "" and ffmpeg gets an empty output path.

**Suggested fix:** Have GenerateTempOutputFileFrom throw (or return a failure Result) after 3 attempts instead of "", or guard callers to abort with a clear error when the path is empty.

---

### 40. Unused Sync semaphore in AudioFilePlayer / unsynchronized AudioFileReader access / dispose ordering

`☑ Fixed` (2026-07-08) · **Severity** Low · **Importance** 3 · **Class** partial · **Finders** 4 · **Ref** C32 · **Present** yes

**Fix note (2026-07-08):** all three sub-claims addressed by the player wiring. The dead SemaphoreSlim was replaced with a `System.Threading.Lock Sync` that is actually acquired by every member touching shared state (Play, Pause, SetVolume, SetPosition, OnPlaybackStateUpdate, Dispose/DisposeAsync), so the UI-thread `CurrentTime` seek write and the poller's `CurrentTime` read are serialized. Dispose ordering inverted per the suggested fix: the status poller is joined first, then device/reader disposed under `Sync` (see #34 for the additional WaveOutEvent-thread read guard).

**Location:** AudioCat/Services/AudioFilePlayer.cs:58,97,121,132 (Sync declared/created/disposed but never acquired); AudioFilePlayer.cs:162-169 (SetPosition) and 177-186 (OnPlaybackStateUpdate) unsynchronized AudioFileReader access; PeriodicInvoker.cs:18-26 (background thread)

**Description:** The `Sync` SemaphoreSlim field is constructed in the ctor (line 97) and disposed in both Dispose (line 121) and DisposeAsync (line 132), but is never used with Wait/Release anywhere in the class - it guards nothing (dead synchronization primitive). Meanwhile `AudioFileReader.CurrentTime` is read on a thread-pool thread by `OnPlaybackStateUpdate` (invoked by PeriodicInvoker's loop, which resumes on a background thread after `Task.Delay`) while `SetPosition` writes `AudioFileReader.CurrentTime` from the caller's (UI) thread, with no locking. NAudio's AudioFileReader is not thread-safe, so concurrent get/set of CurrentTime races on the underlying stream position. Dispose ordering also tears down OutputDevice/AudioFileReader before stopping PlayerStatusInvoker (line 124/135 last), so the still-running timer callback can touch already-disposed objects during teardown.

**Impact:** Concurrent seek (SetPosition) and the 100ms status poll can corrupt the reader's stream position or throw, and disposing while the invoker is mid-callback can hit a disposed AudioFileReader/OutputDevice. Latent: ChaptersPlayer constructs AudioFilePlayer but the actual Play/SetPosition calls are commented out (CreateChaptersViewModel.cs:646-648) and the player isn't in DI, so it is not reachable at runtime yet.

**Repro:** Latent - the audio player path is not wired up (ChaptersPlayer.Play/Stop calls are commented out in CreateChaptersViewModel; AudioFilePlayer not registered in DI). Once wired: start playback, then call SetPosition while the 100ms PlayerStatusInvoker poll reads CurrentTime concurrently, or dispose the player mid-playback, to trigger the race.

**Suggested fix:** Either use the Sync semaphore to guard all AudioFileReader access (SetPosition and OnPlaybackStateUpdate) or remove it as dead code; and stop/dispose PlayerStatusInvoker first in Dispose/DisposeAsync before disposing the reader/output device.

---

### 41. Tags DataGrid double-click: insert-at-selection branch dead / only adds when grid empty (guard returns early when Items.Count>0)

`☑ Fixed` (2026-07-08) · **Severity** Low · **Importance** 3 · **Class** real · **Finders** 3 · **Ref** C59 · **Present** yes

**Location:** AudioCat/Windows/MainWindow.xaml.cs:134-142

**Description:** In OnTagsDataGridMouseDoubleClick the guard at line 136 returns early whenever `dataGrid.Items.Count > 0`. Because the body only executes when the grid is empty (Items.Count == 0), `dataGrid.SelectedIndex` is always -1 at line 138, so the `if (dataGrid.SelectedIndex >= 0) tags.Insert(...)` branch is unreachable dead code. Only the `else tags.Add(...)` path can ever run, and only on an empty grid. The intent (insert a new tag at the selected/double-clicked row) is inverted by the guard.

**Impact:** Double-clicking the tags DataGrid adds a new tag only when the grid is empty; once any tag exists, double-clicking (including on an existing row) does nothing, and tags can never be inserted at a selected position via this gesture. Cosmetic/usability annoyance, not data loss; the Insert key handler remains a workaround.

**Repro:** Run app, open the output tags grid. Double-click the empty grid -> one tag is added. Add/keep at least one tag, then double-click any tag row -> nothing happens (no insert, no add).

**Suggested fix:** Remove `|| dataGrid.Items.Count > 0` from the guard so the SelectedIndex insert branch becomes reachable on a populated grid.

**Re-verification (2026-07-08):** Code facts confirmed: guard bails on `Items.Count > 0`, body runs only on an empty grid, `SelectedIndex` is then always -1, so the Insert branch is dead (no `NewItemPlaceholder` distortion — the ItemsSource item type `IMediaTagViewModel` is an interface, so WPF coerces row-adding off and `Items.Count` equals the tag count). Two ticket corrections: (1) "intent inverted by the guard" is wrong — Name/Value are editable template columns (MainWindow.xaml:1238, :1262) and double-click on a populated grid is the standard begin-cell-edit gesture; the guard deliberately protects it, and the dead Insert branch is copy-paste residue from the Key.Insert handler, not a broken feature. (2) The suggested fix is a regression: removing the guard would insert a blank tag row on every double-click made to edit a cell. No usability gap exists — the Insert key adds/inserts rows on a populated grid.

**Fix note (2026-07-08):** Dead-code cleanup only, behavior unchanged. Body collapsed to the single reachable path `tags.Add(new TagViewModel())`, empty-grid condition folded into the pattern match (`Items.Count: 0`), and a comment documents the seed-first-tag intent and why the guard must stay (MainWindow.xaml.cs:134-142). Ticket's suggested fix rejected as described above.

---

### 42. Command-line argument quoting: embedded double-quotes in paths/tags break ffmpeg argument tokenization

`☑ Resolved by #38` (2026-07-08) · **Severity** Low · **Importance** 3 · **Class** partial · **Finders** 3 · **Ref** C72 · **Present** no (exposure eliminated by the #38 fix)

**Location:** AudioCat/Services/Process.cs:93-111 (Arguments string); AudioCat/FFmpeg/FFmpegService.cs:657-664 (image metadata cmdline), 616,638,664 (quoted interpolation)

**Description:** Process.Run passes a single concatenated argument string to ProcessStartInfo.Arguments instead of using ArgumentList, so all quoting is manual. Most file paths are safe because Windows forbids `"` in filenames and media paths are routed through the single-quoted concat list file (EscapeFileListFilePath). The real exposure is GetMetadataQuery (FFmpegService.cs:657-664), which copies image-stream tag values onto the command line: tag.Value is escaped only with Replace("\"","\\\"") — which breaks for values ending in a backslash (producing `\"` that closes the quote) — and tag.Name (line 664) is interpolated with no escaping at all.

**Impact:** A source image stream carrying a metadata tag whose value ends in a backslash, or whose name/value contains a double-quote, mistokenizes the ffmpeg argument list, causing the AddImages step to fail or write wrong metadata. Practical likelihood is low because it only affects embedded cover-art metadata copied from existing files, and ordinary file paths cannot contain quotes on Windows.

**Repro:** Latent/edge - Add an audio file whose embedded cover-art image stream has a tag value ending in `\` (or containing a `"`), mark it as cover source, and concatenate; the cover-image embedding (AddImages) ffmpeg call gets corrupted arguments. Hard to trigger with normal files since such tag values are rare.

**Suggested fix:** Switch Process.Run to ProcessStartInfo.ArgumentList (per-arg, no manual quoting), or at minimum escape tag.Name and fix trailing-backslash handling in tag.Value at FFmpegService.cs:657-664.

**Audit note:** All ffmpeg/ffprobe invocations build a single argument STRING (Process.cs:100 `Arguments = arguments`) rather than using ArgumentList, and callers naively wrap values in `\"{x}\"`. However the headline claim about "paths" is largely moot: on Windows `"` is an illegal filename character, and actual media paths are written into the concat list file (single-quoted + escaped via EscapeFileListFilePath at FFmpegService.cs:486), not placed on the command line. The genuinely exposed spot is image-stream metadata copied onto the command line (FFmpegService.cs:657-664): `tag.Value` is escaped only by `Replace("\"","\\\"")` (which mishandles a value ending in a backslash, e.g. `foo\` -> `foo\"` corrupts tokenization under CommandLineToArgvW), and `tag.Name` (line 664) is emitted entirely unescaped. UseShellExecute=false so cmd.exe metacharacters are not a concern, only CommandLineToArgvW quoting.

**Re-verification (2026-07-08):** The sole genuine exposure this ticket names — GetMetadataQuery's `tag.Value` naive Replace escaping and unescaped `tag.Name` — is exactly the code the #38 fix replaced today: values now go through `EscapeCliArgValue` (correct CRT backslash-run/quote escaping including the trailing-backslash case, FFmpegService.cs:697-719) and names are sanitized/dropped if they contain space, quote, or `=` (:683-685). Ticket line refs :657-664 are stale (now :679-686). All remaining command-line interpolations audited (:26, :53, :409, :433-438, :630, :656, :805, :835) — every one is a file path, safe under CommandLineToArgvW/CRT tokenization: `"` is an illegal Windows filename character, a file path never ends in `\` (always ends with a filename), and UNC leading `\\` is literal inside quotes. A bare tag name ending in `\` precedes `=`, not a quote, so it is also literal. No exploitable exposure remains.

**Resolution note (2026-07-08):** Closed as resolved by the #38 fix; no additional code change. The suggested `ArgumentList` refactor was rejected as disproportionate: argument strings include pre-formed multi-token fragments (`encodingCommand` from Settings), so the switch would require tokenizing those — real regression risk against zero remaining exposure.

---

### 43. Unrecoverable-remux abort is dead code (RemuxFiles always returns non-null Data; if(Data==null) unreachable)

`☑ Fixed` (2026-07-08) · **Severity** Medium · **Importance** 3 · **Class** real · **Finders** 2 · **Ref** C19 · **Present** yes

**Location:** AudioCat/FFmpeg/FFmpegService.cs:190-194 (guard), 302-313 (RemuxFiles return), 339-354 (SortRemuxedFiles)

**Description:** In Concatenate, after calling RemuxFiles the code checks `if (remuxResponse.Data == null)` (line 190) to detect an unrecoverable remux error and abort the loop. But RemuxFiles always sets Data to a non-null value: both return paths (line 311 Success and line 312 Failure) pass `sortedRemuxedFiles`, the result of SortRemuxedFiles, which always returns `sortedFiles.AsReadOnly()` (line 353) - never null. On the unrecoverable path (line 302-303), DeleteAllTempFiles deletes the temp files but RemuxFiles still returns those (now-deleted) paths as Data, so the `Data == null` guard is unreachable dead code and the intended abort never fires.

**Impact:** When remuxing hits an unrecoverable error (a temp file missing or zero-length), the temp files are deleted but the abort at line 192-193 never executes; concatenation continues with stale/deleted file paths in the list file, producing a corrupt or empty output or a confusing downstream FFmpeg error instead of the intended clean "unrecoverable error, aborting" message.

**Repro:** Concatenate a set where at least one file fails to remux such that its temp output is missing or zero bytes (IsUnrecoverableError returns true). DeleteAllTempFiles runs, but RemuxFiles returns non-null Data so the abort branch is skipped; concat proceeds against deleted temp paths.

**Suggested fix:** On the unrecoverable path, return Response.Failure(message) with null Data (or have RemuxFiles signal abort via a separate flag/empty collection), and have Concatenate check that instead of Data==null.

**Re-verification (2026-07-08):** Core claim confirmed (line refs shifted: guard now FFmpegService.cs:195-199, RemuxFiles returns :322-324, SortRemuxedFiles :351-366). Both return paths pass `sortedRemuxedFiles`, which `SortRemuxedFiles` always materializes via `AsReadOnly()` — never null — and `Response<T>.Failure(data, message)` (Result.cs:86) does store Data, so the `Data == null` guard was unreachable. Trigger is probable: `GenerateTempOutputFileFrom` pre-creates zero-byte files, so any per-file remux failing before writing output leaves 0 bytes and `IsUnrecoverableError` fires. One impact correction: "producing a corrupt or empty output" is overstated — the rerun ffmpeg dies opening the first missing input during concat-demuxer setup, before writing output, and the loop is bounded (`remuxedFiles != null` breaks on iteration 2). Actual impact was one wasted ffmpeg run plus a confusing "No such file or directory" error in place of the intended clean abort message; surviving partial output is identical to what the intended abort leaves. Side observation (pre-existing, all error-break paths incl. the intended abort): `break` still falls through to the Second-Step/Attach-Images regions — out of scope here.

**Fix note (2026-07-08):** Per ticket's first suggested option: the unrecoverable path in `RemuxFiles` now returns `Response<ReadOnlyCollection<string>>.Failure(errors.ToString())` — Data stays null — right after `DeleteAllTempFiles`, with a comment explaining why Data must remain null. The existing `Data == null` abort guard in Concatenate is now live: the user sees the remux errors followed by "Remuxing failed with an unrecoverable error, aborting." and no ghost rerun against deleted temp files occurs. Recoverable-errors path (all temp outputs present and non-empty) is unchanged: Failure with Data, concatenation proceeds with remuxed files.

---

### 46. Leading silence interval starting at 0.0s dropped (TimeSpan.Zero used as both sentinel and value)

`☑ Closed — works as intended` (2026-07-08) · **Severity** Low · **Importance** 3 · **Class** not-a-bug (behavior correct, mechanism accidental) · **Finders** 2 · **Ref** C16 · **Present** yes (mechanism) / no (defect)

**Location:** AudioCat/FFmpeg/FFmpegService.cs:81-108 (IntervalsProcessor)

**Description:** In IntervalsProcessor, the local `startTime` (initialized to TimeSpan.Zero, line 83) is overloaded as both the "no silence interval currently active" sentinel and as a parsed start value. When ffmpeg's silencedetect emits `silence_start: 0` (silence beginning at the file's very start), TryGetTime parses it to TimeSpan.Zero and line 93 assigns it, but `startTime` remains equal to the sentinel. On the subsequent `silence_end:` line, the guard `if (startTime == TimeSpan.Zero)` at line 90 is still true, so control re-enters the start-detection branch (lines 92-94) instead of recording the interval; the silence_end line contains no silence_start, so nothing is captured and the interval is silently lost.

**Impact:** A silence region that begins exactly at 0.0s in a file is dropped from the detected intervals list, so silence-based chapter creation (CreateChapters wizard) misses a leading-silence split point. Real-world likelihood is moderate since true silence at t=0 (and parsing to exactly Zero) is uncommon, but when it occurs the chapter boundaries are wrong.

**Repro:** In the Create Chapters wizard, run "Scan for silence" on a file whose audio begins with a silent passage long enough to trigger silencedetect at silence_start: 0. The leading silence interval will be absent from the results, shifting/omitting the expected chapter boundary.

**Suggested fix:** Track active state with a nullable (TimeSpan? startTime) or a separate bool flag instead of comparing against TimeSpan.Zero.

**Re-verification (2026-07-08):** Mechanism confirmed exactly as described (FFmpegService.cs:82-108): `silence_start: 0` parses to `TimeSpan.Zero`, collides with the sentinel, and the following `silence_end` line re-enters start detection so the interval is dropped. Probability is actually higher than the ticket estimates: ScanForSilenceCommand scans each file separately and offsets results (ScanForSilenceCommand.cs:39-50), so every file's own t=0 is exposed, not just the global timeline start. Minor parser note: `TryGetTime` skips a leading minus, so negative timestamps (e.g. `silence_start: -0.011`) parse as positive — slightly wrong value, but such intervals are not dropped; only exact 0 hits the sentinel.

**Resolution (2026-07-08):** Closed as works-as-intended, no code change. Silence detection exists to find chapter boundaries; a leading-silence interval is irrelevant in every position: chapter 1 must start at 0 for full timeline coverage, and mid-list files already get their chapter break from the zero-length boundary interval appended at each file end (ScanForSilenceCommand.cs:49). Moreover, keeping the interval would make CreateFromIntervals (ChaptersFactory.cs:145-157) emit a zero-duration chapter that lands unfiltered in the wizard grid — i.e. the ticket's suggested fix would regress the UI. The dropped interval is the desired outcome; the only residual criticism is that the correctness is accidental (sentinel collision) rather than explicit, noted here for future maintainers.

---

### 47. Chapter advance only steps one chapter per position tick (skips across multiple short chapters)

`☑ Fixed` (2026-07-08) · **Severity** Low · **Importance** 3 · **Class** partial · **Finders** 2 · **Ref** C34 · **Present** yes

**Fix note (2026-07-08):** the incremental single-step advance no longer exists — the ChaptersPlayer rewrite derives the active chapter from scratch on every position tick via `FindChapter(globalPosition)` (first chapter whose [StartTime, EndTime) contains the position; no index stepping, no per-tick +1 limit). A tick that jumps over any number of short chapters lands directly on the chapter actually containing the position, and ChapterChanged fires once with that chapter.

**Location:** AudioCat/Services/ChaptersPlayer.cs:141-186 (advance logic 151-162); instantiated but unwired at AudioCat/ViewModels/CreateChaptersViewModel.cs:640-649

**Description:** In OnPlaybackPositionChanged, chapter advancement only ever considers nextChapterIndex = ActiveChapterIndex + 1. On each position tick it checks whether globalPosition has reached the single next chapter's StartTime and, if so, steps forward exactly one chapter. There is no loop, so when a position update jumps over several short chapters (globalPosition already past multiple chapter boundaries within one tick), it advances by only one chapter instead of to the chapter actually containing globalPosition. The reported ActiveChapter, ChapterChanged event, and chapterPosition then lag the real playback position until subsequent ticks catch up one chapter at a time.

**Impact:** During playback of sequences containing chapters shorter than the position-update interval, the displayed current chapter and chapter-relative position would trail the true position, and ChapterChanged would fire stale chapters. Currently latent: ChaptersPlayer is constructed but never started (Play/Stop commands are commented-out TODO dummies in CreateChaptersViewModel), so OnPlaybackPositionChanged is never invoked at runtime.

**Repro:** Latent - ChaptersPlayer.Play() is never called; the PlayPause command wiring is commented out in CreateChaptersViewModel.cs:642-649, so position-change events that drive the advance logic never fire. Would manifest once playback is wired and a chapter shorter than the player's tick interval is played.

**Suggested fix:** Replace the single-step check with a while loop that advances ActiveChapterIndex while globalPosition >= CreatedChapters[next].StartTime (firing ChapterChanged only for the final landing chapter).

---

### 50. RedirectStandardInput set but never written (potential hang if ffmpeg ever prompts)

`☐ Open` · **Severity** Low · **Importance** 3 · **Class** partial · **Finders** 2 · **Ref** C44 · **Present** yes

**Location:** AudioCat/Services/Process.cs:93-111 (RedirectStandardInput=true at line 104); consumers in AudioCat/FFmpeg/FFmpegService.cs (Process.Run calls, e.g. lines 27,60,164,217,399)

**Description:** CreateProcess sets RedirectStandardInput = true for every ffmpeg/ffprobe process, but no code path ever accesses process.StandardInput to write to or close the stream. Both Process.Run overloads only read StandardOutput/StandardError and await WaitForExitAsync. If a child process ever blocks waiting on stdin, the parent would await WaitForExitAsync indefinitely. In practice every ffmpeg command line in FFmpegService.cs includes -y (overwrite without confirmation), so the most common prompt (output file exists) cannot occur, making an actual hang unlikely. Note also Verb="runas" is set alongside UseShellExecute=false, where Verb is ignored.

**Impact:** Latent/low risk: with -y on all commands ffmpeg does not prompt, so no hang is observed today. The redirected-but-unused stdin is a fragility: a future command without -y, or an ffprobe edge case prompting on stdin, would cause the operation to hang until the CancellationToken cancels (e.g. user cancels concatenation).

**Repro:** Latent - all ffmpeg invocations pass -y so no stdin prompt is triggered; would require adding a command path that omits -y (or an interactive ffmpeg/ffprobe prompt) to actually hang.

**Suggested fix:** Set RedirectStandardInput = false (and pass -nostdin to ffmpeg); also drop the no-op Verb="runas".

---

### 51. Process callback overload drains only one of stdout/stderr -> pipe-buffer stall deadlock risk

`☐ Open` · **Severity** Low · **Importance** 3 · **Class** partial · **Finders** 2 · **Ref** C45 · **Present** yes

**Location:** AudioCat/Services/Process.cs:11-18 (Run callback overload) and ReadOutStream:37-63; cf. correct dual-drain overload at 20-35

**Description:** CreateProcess (lines 93-111) redirects BOTH stdout and stderr (RedirectStandardOutput=true, RedirectStandardError=true). The callback overload Run (lines 11-18) only spawns ONE reader via ReadOutStream, which reads either StandardError OR StandardOutput per the outputType arg, never both. All current callers (Concatenate 164/217, RemuxFile 399, ScanForSilence 60) pass OutputType.Error, so the child process's stdout pipe is never drained. If a redirected pipe's OS buffer (~64KB on Windows) fills, the child blocks on write while the parent blocks on WaitForExitAsync — classic deadlock. The other overload (20-35) correctly drains both streams concurrently.

**Impact:** Latent. With the current invocations ffmpeg always writes its muxed output to a file and emits only progress/log text on stderr (which is drained), so stdout stays empty and the deadlock is not reachable today. It becomes a real hang if any caller ever uses an ffmpeg command that emits significant data on stdout (e.g. `-progress pipe:1` or piping output to pipe:1).

**Repro:** Latent - no current caller produces substantial stdout while reading only stderr. To trigger: call the callback Run overload with OutputType.Error against an ffmpeg command that writes >~64KB to stdout (e.g. output to pipe:1); the process hangs at WaitForExitAsync once the stdout pipe buffer fills.

**Suggested fix:** In the callback Run overload, also start a draining reader for the non-selected stream (read both stdout and stderr concurrently) before awaiting WaitForExitAsync.

---

### 52. Process.Run read loop catch{break;} swallows real IO errors as EOF (truncated output / misclassification)

`☐ Open` · **Severity** Low · **Importance** 3 · **Class** real · **Finders** 2 · **Ref** C46 · **Present** yes

**Location:** AudioCat/Services/Process.cs:44-63 (ReadOutStream) and 65-91 (ReadOutputStream); both catch blocks at 56-59 and 83-86

**Description:** Both stream-reading loops in Process.Run wrap `textReader.ReadLineAsync(ctx)` in a try with a bare `catch { break; }`. Any exception thrown while reading the process's stdout/stderr (e.g. IOException from a broken/closed pipe, decoding error) is swallowed and treated identically to a clean EOF (`line == null`), exiting the loop and returning normally. Only OperationCanceledException is re-surfaced, and only because the trailing `ctx.ThrowIfCancellationRequested()` re-checks the token after the loop; every other IO fault is misclassified as end-of-output. The string-returning overload (line 65) thus returns whatever partial text was accumulated so far.

**Impact:** A transient read error during ffprobe (Probe, line 27) silently yields a truncated XML document that FFprobeMediaFile then parses, producing incomplete/incorrect media metadata with no error surfaced; for ffmpeg stderr progress loops it silently stops parsing progress. Failures are misreported as success rather than raised. Low frequency in practice since pipes rarely fault mid-read, but real and silent when it happens.

**Repro:** Latent/rare - requires an actual IO fault on the redirected stdout/stderr pipe mid-read (e.g. process crash closing the pipe, or decode error). Hard to trigger deterministically at runtime; under normal completion the loop exits via the legitimate `line == null` EOF path, so no user-visible bug in the common case.

**Suggested fix:** Catch only OperationCanceledException to break/rethrow; let other exceptions propagate (or wrap and rethrow) so genuine IO errors are not misclassified as EOF.

---

### 53. GetCueFileCommand returns Success even when some/all CUE files fail to parse (silent partial data loss)

`☐ Open` · **Severity** Low · **Importance** 3 · **Class** partial · **Finders** 2 · **Ref** C65 · **Present** yes

**Location:** AudioCat/Commands/GetCueFileCommand.cs:20-33; consumer AudioCat/ViewModels/CreateChaptersViewModel.cs:836-843

**Description:** In GetCueFileCommand.Command, the foreach loop parses each selected .cue file via Cue.Parser.Parse. On a parse failure it shows a Yes/No "Abort adding?" MessageBox. If the user clicks No, the failed file is silently skipped and the loop continues; the method ultimately returns Response.Success(cueFiles.AsReadOnly()) containing only the subset that parsed (line 33). If the user clicks Yes, it returns Response.Success() with NO data payload (line 29). The command never returns Failure for parse errors, and the consumer OnGetQueueFileFinished (line 839) treats absent/empty/partial data as a normal no-op after clearing CueFiles.

**Impact:** When some of multiple selected CUE files fail to parse and the user chooses not to abort, the wizard loads only the successfully parsed subset with no further warning, so chapters are generated from incomplete CUE data (silent partial loss). The Yes/abort path returns success-with-no-data, which clears the existing CueFiles list, discarding previously loaded cues without any error indication.

**Repro:** Open Create Chapters wizard, invoke Cue Files load, select two .cue files (one malformed), click No on the error dialog: only the valid cue loads, malformed silently dropped, command returns Success.

**Suggested fix:** Track parse failures; after the loop return Failure (or surface a summary) when any file failed, and on abort return Failure instead of empty Success so the handler does not silently clear/partially populate CueFiles.

---

### 54. Concurrent add operations mutate shared file collection / DoNotInvoke flag without synchronization

`☐ Open` · **Severity** Low · **Importance** 3 · **Class** partial · **Finders** 2 · **Ref** C76 · **Present** yes

**Location:** AudioCat/Services/MediaFilesService.cs:151-198 (AddMediaFiles); AudioCat/Windows/MainWindow.xaml.cs:65-101 (OnDataGridDrop/AddDragFiles); AudioCat/Services/MediaFilesContainer.cs:11; AudioCat/ViewModels/MainViewModel.cs:692-695 (OnFilesCollectionChanged guard)

**Description:** `AddMediaFiles` is re-entrant and shares the single mutable `IMediaFilesContainer.Files` collection plus the non-volatile `DoNotInvokeFilesCollectionChangedEvent` bool, with no synchronization. The drag-drop path (`OnDataGridDrop`) launches `AddDragFiles` via `Task.Run` on a thread-pool thread that is NOT gated by `CommandBase.CanBeExecuted`, and it sets the mitigating `IsUserEntryEnabled=false` flag only after the task has already started. If a second add (another drop, or the Add Files/Add Path command in the gap before the flag propagates) overlaps, two `AddMediaFiles` runs interleave: one run sets `DoNotInvokeFilesCollectionChangedEvent=false` on its last item while the other is mid-batch, so the suppression flag (used as a plain on/off, not a nesting counter) is corrupted. The non-dispatcher reads of `files` for codec/duplicate detection (lines 159-178) also observe a concurrently mutating collection.

**Impact:** Overlapping add operations can prematurely re-enable or wrongly leave-on the `OnFilesCollectionChanged` suppression flag, causing the DataGrid/derived state (TotalSize, TotalDuration, SelectedCodec, chapter warnings) to either fire a storm of partial-update events or skip updates entirely, leaving stale UI. Duplicate/codec-consistency checks may also race against the growing collection. The collection content is not corrupted (additions are UI-thread serialized).

**Repro:** Drag-and-drop a large set of files onto the grid (starts a long background probe via Task.Run) and, within the brief window before IsUserEntryEnabled is applied, immediately drag-drop a second set (or click Add Files). The two AddMediaFiles batches interleave; observe the DoNotInvoke flag flipping false mid-batch and inconsistent grid/total updates. Hard to hit reliably due to timing/UI-flag mitigation.

**Suggested fix:** Serialize all add entry points behind one shared async lock (SemaphoreSlim) or a single app-level busy gate, and replace the bool suppression flag with a reentrancy counter scoped to a single batch.

**Audit note:** Partial: the ObservableCollection itself is not corrupted because every `files.Add` is marshaled to the UI thread via `uiDispatcher.InvokeAsync`. The genuine, unsynchronized races are (a) the shared `DoNotInvokeFilesCollectionChangedEvent` bool used as a non-reentrant suppression flag, and (b) the read-modify pattern over `files` (GetAudioCodec/GetDuplicates/SelectedFile) across overlapping adds. `IsUserEntryEnabled` partially mitigates but is set late and from a worker thread, leaving a window.

---

### 55. AddPathCommand blocks UI thread enumerating directory (.ToArray on EnumerateFiles AllDirectories)

`☐ Open` · **Severity** Medium · **Importance** 3 · **Class** real · **Finders** 1 · **Ref** C89 · **Present** yes

**Location:** AudioCat/Commands/AddPathCommand.cs:21 (invoked via AudioCat/Commands/CommandBase.cs:19-37 Execute)

**Description:** In AddPathCommand.Command, line 21 calls Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToArray() synchronously. CommandBase.Execute is an async void ICommand handler that awaits Command on the UI thread, and there is no Task.Run or await before this enumeration, so the entire recursive directory walk (materialized eagerly by .ToArray()) plus the subsequent Files.Sort run on the WPF UI thread before the first await at AddMediaFiles (line 24).

**Impact:** When a user selects a folder with a large/deep directory tree (or one on a slow/network drive), the UI freezes (no rendering, no input) until the full recursive enumeration completes. The app appears hung during that window.

**Repro:** Run app, use the Add Path action, choose a folder containing many files across deeply nested subdirectories (or a slow network share). Observe the window become unresponsive while EnumerateFiles(...AllDirectories).ToArray() walks the tree, before file probing starts.

**Suggested fix:** Wrap the EnumerateFiles().ToArray() and Files.Sort calls in await Task.Run(...) so directory enumeration runs off the UI thread.

---

### 56. Files.GetFilesFromDirectory unbounded recursion (symlink/junction loop -> StackOverflow)

`☐ Open` · **Severity** Medium · **Importance** 3 · **Class** partial · **Finders** 1 · **Ref** C96 · **Present** yes

**Location:** AudioCat/Services/Files.cs:33-75 (mutual recursion GetFilesFromDirectories<->GetFilesFromDirectory); callers MainWindow.xaml.cs:89, MainViewModel.cs:517

**Description:** GetFilesFromDirectory enumerates subdirectories via Directory.EnumerateDirectories (SearchOption.TopDirectoryOnly) then recurses through GetFilesFromDirectories with no visited-set, no depth limit, and no reparse-point (symlink/junction) detection. On Windows, EnumerateDirectories follows directory junctions and symbolic links, so a junction/symlink cycle (e.g. C:\a\b -> C:\a) produces infinite mutual recursion. Because every level awaits Task.Yield(), frames accumulate on the heap rather than the call stack, so the practical outcome is unbounded memory growth and an unending traversal rather than a StackOverflowException.

**Impact:** Dropping (or passing as startup arg) a folder that contains a directory junction/symlink loop causes the app to hang indefinitely while consuming ever-growing memory, eventually OutOfMemory; the UI thread awaits the traversal so the window appears frozen.

**Repro:** Create a junction loop: `mklink /J C:\test\loop C:\test`. Run AudioCat and drag the C:\test folder onto the window (or pass it as a command-line arg). The recursive enumeration never terminates and memory climbs without bound.

**Suggested fix:** Skip directories whose FileAttributes include ReparsePoint, and/or track visited canonical full paths in a HashSet with a max-depth guard; convert to iterative traversal.

**Audit note:** Defect real but title's failure mode imprecise: due to `await Task.Yield()` at every level, recursion frames go on the heap (async state machines), not the synchronous call stack, so the crash is unbounded memory growth / infinite hang (eventual OutOfMemory), not a true StackOverflowException. Reachable via folder drag-drop (MainWindow.xaml.cs:89) and startup args (MainViewModel.cs:517).

---

### 57. IsAccessible relies on StartsWith('ffmpeg version')/('ffprobe version') - fragile for custom builds

`☐ Open` · **Severity** Low · **Importance** 3 · **Class** partial · **Finders** 1 · **Ref** C108 · **Present** yes

**Location:** AudioCat/FFmpeg/FFmpegService.cs:796-823 (checks at 806 and 815)

**Description:** IsAccessible() validates tool availability by running "ffmpeg -version" / "ffprobe -version" and asserting the captured output begins with the literal prefixes via response.StartsWith("ffmpeg version") (line 806) and response.StartsWith("ffprobe version") (line 815). This is a brittle exact-prefix string match rather than a check on the process exit code or a substring/regex match. Custom-compiled FFmpeg builds, forks, or wrapper scripts/shims (or any banner that prints a build/copyright line first, or leading whitespace) can emit a first line that does not start with these exact tokens, even though the binaries are fully functional.

**Impact:** Users with a non-standard FFmpeg/ffprobe build or a wrapper on PATH get a false "tool is not found" failure on startup; VerifyMediaFileServiceIsAccessible reports failure and the app surfaces a missing-tools error despite the tools working. Affects only edge-case installations; official builds always emit the expected prefix, so most users are unaffected.

**Repro:** Put on PATH an ffmpeg/ffprobe whose -version output does not begin exactly with "ffmpeg version"/"ffprobe version" (e.g. a wrapper script that prints its own banner first, or a custom build). Launch AudioCat; startup accessibility check returns Failure and the app reports the tool as not found even though it runs fine.

**Suggested fix:** Use the process exit code and/or a case-insensitive Contains("version")/regex match (e.g. trim and match "ffmpeg version") instead of an exact StartsWith on the raw output.

---

### 58. CreateFilesListFile returns empty string if source file missing -> malformed concat command

`☐ Open` · **Severity** Low · **Importance** 3 · **Class** partial · **Finders** 1 · **Ref** C109 · **Present** yes

**Location:** AudioCat/FFmpeg/FFmpegService.cs:473-484 (defect), 210-217 (unguarded caller)

**Description:** CreateFilesListFile(string file) returns "" when the source file does not exist (lines 475-477). Its sole caller is the second step of the two-step concat path (Vorbis with metadata) at line 210, where it is passed `outputToFile` produced by the first concat step. The empty return is not checked: line 215 calls GetFFmpegArgs(codec, listFile="", ...), which emits `-f concat -safe 0 -i "" ...`, a malformed FFmpeg command. The first-step loop only verifies stderr is empty (line 169) before breaking; it never confirms the output file was actually created, so a silent no-output-but-no-error first step would slip through.

**Impact:** In the two-step Vorbis-with-tags path, if the first concat step yields no output file while reporting no error, the second step runs FFmpeg with an empty input path and fails, producing a confusing error and no output instead of a clear diagnostic. Normal operation produces the temp file so this is an edge case, not hit in typical runs.

**Repro:** Latent/edge-case - requires the two-step concat path (Vorbis output with chapters/tags enabled) where FFmpeg's first concat step exits cleanly (empty stderr) yet leaves no temp output file, so CreateFilesListFile(outputToFile) returns "" and the second-step command is built with -i "". Not reproducible via the standard happy path.

**Suggested fix:** After line 210 check listFile=="" (or verify outputToFile exists/non-empty before second step) and abort with a clear error via OnError.

**Audit note:** Defect exists but is narrow/edge-case. The string overload of CreateFilesListFile is only invoked at line 210 (two-step Vorbis concat path); its empty-string return is not guarded by the caller. The other RemuxFile path (line 395) uses the IEnumerable overload, not this one, so it is unaffected.

**Re-audit (2026-07-01):** even narrower than described — GenerateTempOutputFileFrom pre-creates outputToFile on disk (line 437) and an ffmpeg failure never deletes it, so the exists-check stays true; only external deletion of the temp file (or a temp-path failure, which instead throws ArgumentException in `new FileInfo("")`) can reach the empty-string return.

---

### 59. Chapter gap causes transient position mis-reporting (stale ActiveChapter during gap)

`☑ Fixed` (2026-07-08) · **Severity** Low · **Importance** 3 · **Class** real · **Finders** 1 · **Ref** C38 · **Present** yes

**Fix note (2026-07-08):** fixed by the same rewrite as #47, matching the suggested fix: `FindChapter(globalPosition)` returns null when no chapter's [StartTime, EndTime) contains the position, so inside a gap `ActiveChapter` becomes null, `ChapterChanged` fires the null transition (documented behavior: "including transitions to and from null"), and position events carry a null chapter — the wizard unflags the playing row instead of showing the stale, already-ended chapter with an over-range chapter position.

**Location:** AudioCat/Services/ChaptersPlayer.cs:141-186 (advance logic 152-173, progress report 175-180); instantiated but unwired at AudioCat/ViewModels/CreateChaptersViewModel.cs:640-649

**Description:** OnPlaybackPositionChanged only advances ActiveChapter when globalPosition >= nextChapter.StartTime, and only stops when past EndTime of the LAST chapter (lines 152-173). If chapters are non-contiguous (a gap exists where one chapter's EndTime is earlier than the next chapter's StartTime), then while playback is inside that gap globalPosition is past the current chapter's EndTime but still below the next chapter's StartTime. During that window ActiveChapter stays the stale, already-ended chapter, and PlaybackProgress (line 180) is fired with that stale chapter and a chapterPosition (globalPosition - chapterStart, line 176-177) that exceeds the chapter's real duration.

**Impact:** During inter-chapter gaps the UI would briefly show the previous (ended) chapter as active and a chapter-relative position that overruns its duration, until playback reaches the next chapter's start. Purely a transient progress/position display glitch; no audio or data impact. Currently latent because the player is not hooked to any UI command.

**Repro:** Latent - ChaptersPlayer is instantiated but its Play/Stop are never invoked (PlayPause command commented out, CreateChaptersViewModel.cs:642-649). If wired: load files producing non-contiguous chapters (e.g. silence-scan chapters with gaps), play a chapter, and observe stale ActiveChapter / over-range chapter position while position is within a gap before the next chapter's StartTime.

**Suggested fix:** When globalPosition exceeds ActiveChapter.EndTime but is below nextChapter.StartTime (gap), clear/null the active chapter or suppress chapter-relative progress until the next chapter's StartTime is reached.

**Audit note:** Defect logic is present, but ChaptersPlayer is not yet wired to UI (the PlayPause RelayCommand calling Play/Stop is commented out in CreateChaptersViewModel.cs:642-649), so it cannot be triggered at runtime today. Marked present=yes for the code logic, but it is latent/unreachable.

---

### 61. CUE Generator does not escape quotes in emitted values

`☐ Open` · **Severity** Low · **Importance** 3 · **Class** real · **Finders** 1 · **Ref** C74 · **Present** yes

**Location:** AudioCat/Cue/Generator.cs:8-12 (TITLE/PERFORMER/SONGWRITER), 19-20 (FILE name), 38-42 (track TITLE/PERFORMER/SONGWRITER)

**Description:** Generator.ToCommands emits quoted CUE fields via raw string interpolation, e.g. $"TITLE \"{cue.Title}\"", $"FILE \"{file.Name}\"", and the track-level TITLE/PERFORMER/SONGWRITER lines, with no escaping of double-quote characters inside the values. Any value containing a " (e.g. an album titled The "Best" Hits) produces a malformed line like TITLE "The "Best" Hits". CUE format has no standard escape for embedded quotes, but the emitted output prematurely terminates the quoted token and the trailing content is mis-tokenized. The codebase's own Parser/CommandFactory would not round-trip such a line correctly.

**Impact:** Generated CUE sheets with quote-containing titles/performers/filenames would be malformed and mis-parsed by CUE readers (including AudioCat's own Parser). Currently has no user-facing effect because ToCommands is never invoked anywhere in the app (CUE generation is not wired up) — purely latent.

**Repro:** Latent - Generator.ToCommands has no callers; the application only parses CUE files and never writes them, so the unescaped output is never produced at runtime.

**Suggested fix:** Sanitize/escape quoted field values before interpolation (e.g. replace embedded double quotes or strip them) in a shared helper used by all quoted emit sites once ToCommands is wired up.

**Audit note:** Defect is genuinely present in the code, but the affected method Generator.ToCommands has no callers anywhere in the repo (app only parses CUE, never writes), so it is latent/dead-code.

---

### 63. Process Verb=runas: claimed to force UAC elevation on every ffmpeg/ffprobe call (note: ignored when UseShellExecute=false)

`☐ Open` · **Severity** Trivial · **Importance** 2 · **Class** partial · **Finders** 7 · **Ref** C43 · **Present** yes (as dead/misleading config; the originally claimed UAC elevation does not occur)

**Location:** AudioCat/Services/Process.cs:97-110 (Verb at :108, UseShellExecute at :102)

**Description:** CreateProcess builds a ProcessStartInfo with both UseShellExecute = false (line 102) and Verb = "runas" (line 108). The Verb property is only applied by Windows when UseShellExecute is true (it routes through ShellExecuteEx); with UseShellExecute = false the process is created directly via CreateProcess and Verb is silently ignored. Consequently the "runas" verb has zero runtime effect — no elevation, no UAC prompt — contrary to the issue title. It is leftover/misleading dead configuration.

**Impact:** No functional impact: ffmpeg and ffprobe launch normally at the caller's privilege level with no UAC prompt. The only cost is reader confusion/misleading code suggesting elevation is intended.

**Repro:** Latent - not reachable as described; Verb is ignored under UseShellExecute=false, so no UAC prompt or elevation can be triggered at runtime. The named bug does not manifest.

**Suggested fix:** Remove the unused `Verb = "runas"` line (and the unused RedirectStandardInput) to eliminate misleading dead config.

**Audit note:** The literal `Verb = "runas"` is present at AudioCat/Services/Process.cs:108, but its claimed runtime effect (forcing UAC elevation on every ffmpeg/ffprobe launch) does NOT occur. ProcessStartInfo.Verb is only honored when UseShellExecute = true; here UseShellExecute = false (line 102), so the .NET runtime ignores Verb entirely. No UAC prompt is ever raised. The defect is therefore dead/misleading config, not an actual elevation bug — matching the prior 'partial' verdict.

---

### 64. AudioFilePlayer.SetPosition ignores its filePath parameter

`☐ Open` · **Severity** Low · **Importance** 2 · **Class** partial · **Finders** 5 · **Ref** C30 · **Present** yes

**Location:** AudioCat/Services/AudioFilePlayer.cs:47,162-169 (callers: AudioCat/Services/ChaptersPlayer.cs:94,234)

**Description:** `SetPosition(string filePath, TimeSpan position)` (AudioFilePlayer.cs:162-169) never reads its `filePath` parameter; it clamps `position` and applies it to the single `AudioFileReader` opened at construction (`Create(audioFile)`). The signature implies the call can target a specific file, but an `AudioFilePlayer` instance is bound to exactly one file. Callers in ChaptersPlayer (:94, :234) currently pass the path of the very file that `ActivePlayer` was created from, so the wrong-stream scenario does not actually occur today.

**Impact:** Misleading API surface only: a hypothetical caller passing a different path would silently seek the already-open stream instead of erroring. No incorrect behavior in current call sites since they always pass the active player's own file. The ChaptersPlayer/player UI is not fully wired yet, so the effect is latent.

**Repro:** Latent - no observable runtime fault; current callers always pass the active player's own file. The parameter is dead. To "trigger" the misleading behavior one would have to call SetPosition with a path other than the loaded file, which the codebase never does.

**Suggested fix:** Drop the unused `filePath` parameter from `IAudioFilePlayer.SetPosition`/impl and callers, or assert it matches the loaded file.

---

### 65. DoNotInvokeOutputChaptersCountChangedEvent set true (not false) on last loop iteration

`☐ Open` · **Severity** Trivial · **Importance** 2 · **Class** partial · **Finders** 5 · **Ref** C54 · **Present** yes

**Location:** AudioCat/ViewModels/MainViewModel.cs:537-554 (AddOutputChaptersOnStartup) and 742-760 (OnFilesCollectionChanged)

**Description:** In both AddOutputChaptersOnStartup and the files-collection-changed handler, DoNotInvokeOutputChaptersCountChangedEvent is set true BEFORE the add loop (lines 537/742). Inside the loop, on the last iteration (index == newChapters.Count - 1) the code sets the flag to true AGAIN (lines 542/748) — a copy/paste typo where the apparent intent was to set it false so the final OutputChapters.Add would re-fire the OutputChaptersCount notification. The redundant assignment is a no-op because the flag is already true. Functionally harmless because the finally block (lines 549/755) resets it to false and an explicit OnPropertyChanged(nameof(OutputChaptersCount)) is called right after (lines 554/760), so the count UI updates regardless.

**Impact:** No observable runtime effect: OutputChaptersCount is still correctly refreshed via the explicit OnPropertyChanged after the loop. The line is dead/redundant code reflecting an aborted attempt to re-enable the notification mid-loop; it neither suppresses nor double-fires the binding.

**Repro:** Latent - the redundant assignment sets an already-true flag and is overridden by the finally block plus an explicit OnPropertyChanged; no user-visible behavior differs whether the line reads true or false.

**Suggested fix:** Delete the redundant in-loop `if (index == newChapters.Count - 1)` assignment in both methods (or set it to false if mid-loop re-enable was actually intended).

**Audit note:** Present but inert: the redundant `= true` is a real copy/paste defect, yet it has no functional consequence because the flag is already true and the surrounding finally + explicit OnPropertyChanged compensate. Severity downgraded to Trivial/dead-code.

---

### 68. AAC/MP3 encoder builders misplace 'k' kilobit suffix (attaches to cutoff) - dead code (no callers)

`☐ Open` · **Severity** Low · **Importance** 2 · **Class** partial · **Finders** 3 · **Ref** C61 · **Present** yes

**Location:** AudioCat/FFmpeg/AacEncoderArgs.cs:21; AudioCat/FFmpeg/Mp3EncoderArgs.cs:22-23 (BuildCutOff at AacEncoderArgs.cs:25-26 and Mp3EncoderArgs.cs:27-28)

**Description:** The CBR/ABR branches of Build() interpolate the kilobit suffix as `$"-b:a {Bitrate}{BuildCutOff()}k"`. BuildCutOff() returns `" -cutoff {CutOff}"` (or empty). So when CutOff > 0, e.g. Bitrate=128, CutOff=15000, the result is `-b:a 128 -cutoff 15000k` — the `k` lands on the cutoff value (making it invalid for ffmpeg) and the `-b:a` bitrate is left without its `k` unit. The VBR branch is unaffected since it carries no `k`.

**Impact:** No runtime impact today: AacEncoderArgs and Mp3EncoderArgs are marked "work in progress" and their CreateVbr/CreateCbr/CreateAbr/Build members have zero callers (concatenation uses Settings.GetEncodingCommand, which emits `-c copy`/`-c:a flac`). Latent — if these builders are ever wired in with a nonzero cutoff, ffmpeg would reject the malformed `-cutoff Nk` argument and the bitrate would lack its unit.

**Repro:** Latent - encoder builder classes are unused work-in-progress code with no callers.

**Suggested fix:** Move the suffix before the cutoff in all three CBR/ABR branches: `$"-b:a {Bitrate}k{BuildCutOff()}"` (and `-abr 1 -b:a {Bitrate}k{BuildCutOff()}`).

---

### 69. Double sort of file names (GetMediaFiles then ProbeFiles re-sorts)

`☐ Open` · **Severity** Trivial · **Importance** 2 · **Class** partial · **Finders** 3 · **Ref** C56 · **Present** yes

**Location:** AudioCat/Services/MediaFilesService.cs:59,99; AudioCat/Services/Files.cs:8-11

**Description:** GetMediaFiles sorts the incoming fileNames via Files.Sort into sortedFiles (line 59), then passes that already-sorted list to ProbeFiles, which immediately calls Files.Sort again on it (line 99). Files.Sort uses a stable OrderBy with a natural-numeric key computed by running DigitRegex().Replace (zero-padding every digit run to 4 chars) over each filename, so the second sort recomputes identical keys and yields an identical order. The re-sort is pure redundant work with no effect on the result.

**Impact:** No correctness or ordering bug; the result is identical. The only consequence is wasted CPU: the per-filename regex replace and OrderBy run twice per file-add operation, negligible for typical file counts.

**Repro:** Latent (performance-only) - Add files via AddFiles; GetMediaFiles sorts then ProbeFiles sorts the same list again. Observable only as redundant regex/sort work, not as wrong behavior.

**Suggested fix:** Remove the Files.Sort call in ProbeFiles (line 99) and iterate the passed-in (already sorted) fileNames directly.

---

### 70. MeteringSampleProvider issues: StreamVolume MaxSampleValues buffer forwarded by reference / subscription never removed

`☐ Open` · **Severity** Low · **Importance** 2 · **Class** partial · **Finders** 2 · **Ref** C35 · **Present** yes

**Location:** AudioCat/Services/AudioFilePlayer.cs:101 (also StreamVolumeEventArgs at :27-30); consumer wiring absent in AudioCat/Services/ChaptersPlayer.cs:127-139

**Description:** At AudioFilePlayer.cs:101 the StreamVolume handler forwards NAudio's `e.MaxSampleValues` array directly into a new StreamVolumeEventArgs without copying. NAudio's MeteringSampleProvider reuses the same internal `maxSamples` float[] across callbacks, so any subscriber that retains the array would observe it being overwritten on the next metering interval. Separately, the `MeteringProvider.StreamVolume += (lambda)` subscription at line 101 is never unsubscribed (unlike the explicit subscribe/unsubscribe pattern in ChaptersPlayer.SubscribeToPlayer/UnsubscribeFromPlayer), and ChaptersPlayer never even subscribes to PlaybackVolume.

**Impact:** No current runtime impact: PlaybackVolume has no subscribers (ChaptersPlayer wires only state/position/error), so the aliased buffer is never observed and the un-removed lambda is collected with the player it is part of. Latent risk only — if a future volume-meter UI stores MaxSampleValues, it would see garbled/mutated levels.

**Repro:** Latent - PlaybackVolume event is never subscribed (ChaptersPlayer.SubscribeToPlayer omits it) and the player is not yet wired into UI; the aliased array is never retained by any consumer, and provider/lambda share the player's lifetime so the missing unsubscribe leaks nothing.

**Suggested fix:** Copy the array when forwarding: `new StreamVolumeEventArgs((float[])e.MaxSampleValues.Clone())`; optionally store the lambda in a field and detach it in Dispose for consistency.

**Audit note:** Both sub-claims are real but minor. The buffer-aliasing concern is genuine per NAudio semantics but latent (no consumer of PlaybackVolume, and current forwarder doesn't retain the array). The missing unsubscribe is cosmetic since provider and player share lifetime.

---

### 74. LastIndexOf(ReadOnlySpan,char,startIndex) extension has non-standard semantics (startIndex as lower bound)

`☐ Open` · **Severity** Low · **Importance** 2 · **Class** partial · **Finders** 1 · **Ref** C106 · **Present** yes

**Location:** AudioCat/Extensions.cs:259-268 (definition); AudioCat/Cue/CommandFactory.cs:249 (sole caller)

**Description:** The custom extension `LastIndexOf(this ReadOnlySpan<char> span, char ch, int startIndex)` loops `for (var i = span.Length - 1; i >= startIndex; i--)`, i.e. it treats `startIndex` as the lower bound of the search range and searches the whole tail of the span downward to that bound. This collides with the well-known BCL signature `string.LastIndexOf(char, int)`, where the int argument is the starting position and the search proceeds downward from it. The semantics are therefore inverted relative to the familiar overload. Its only caller, `GetQuotedValueFullString`, passes `startIndex=1` on a span that always begins with a quote (guaranteed at CommandFactory.cs:241) to locate the closing quote, which the custom semantics happen to satisfy correctly.

**Impact:** No current runtime defect: the single CUE-parsing caller relies on and matches the custom semantics. The risk is latent maintenance confusion - a future caller assuming BCL `LastIndexOf` semantics (search downward from startIndex) would get wrong results, since this implementation can return indices above startIndex.

**Repro:** Latent - the only caller (CommandFactory.GetQuotedValueFullString) is correct; the wrong-results scenario requires adding a new caller that expects standard BCL semantics. Demonstration: "abcba".AsSpan().LastIndexOf('b', 0) returns 1, whereas standard semantics searching downward from index 0 would return -1.

**Suggested fix:** Rename to convey lower-bound semantics (e.g. LastIndexOfFrom / LastIndexOfInRange) or document it clearly to avoid collision with BCL LastIndexOf(char, int).

**Audit note:** Present as a naming/semantics hazard, but the sole caller uses it correctly so there is no current functional bug.

---

### 75. OnToggleChaptersEnabled does not re-enable chapters after manual disable + later codec change

`☐ Open` · **Severity** Low · **Importance** 2 · **Class** partial · **Finders** 1 · **Ref** C110 · **Present** yes

**Location:** AudioCat/ViewModels/MainViewModel.cs:600-606 (OnToggleChaptersEnabled), 691-717 (OnFilesCollectionChanged / ChaptersWasDisabledByCodec)

**Description:** The boolean flag ChaptersWasDisabledByCodec is the only thing that distinguishes "chapters auto-disabled because the codec doesn't support them" from "user manually disabled chapters". It is set true in OnFilesCollectionChanged (line 709) and used (line 711-715) to auto re-enable chapters when a later file change restores a chapter-supporting codec. However the manual toggle handler OnToggleChaptersEnabled (lines 600-606) only flips ChaptersEnabled and never reads or resets ChaptersWasDisabledByCodec. As a result the flag's value can become stale relative to the user's manual intent, so the auto re-enable logic does not honor a manual disable that happened around a codec change.

**Impact:** If a user adds files whose codec does not support chapters (chapters auto-disabled, flag set) and then swaps to files with a chapter-supporting codec, chapters are silently re-enabled even though the user may have intended them off; conversely a manual re-enable while the flag is set leaves the flag stale. Purely a UI-state/checkbox annoyance, not a data-loss issue.

**Repro:** Manually uncheck chapters first, then add OGG/WAV/FLAC files (CodecsThatDoesNotSupportChapters) — line 709 sets ChaptersWasDisabledByCodec unconditionally even though chapters were already off by user choice. Remove those files: the auto re-enable at 711-715 turns chapters back on, overriding the earlier manual disable.

**Suggested fix:** In OnToggleChaptersEnabled, clear ChaptersWasDisabledByCodec = false whenever the user manually changes the state so the codec-round-trip auto re-enable respects manual intent.

**Re-audit (2026-07-01):** the original repro was wrong — a manual toggle-off made AFTER the unsupported files are removed IS honored (the empty-list event re-enables chapters and clears the flag before the user toggles). The broken case is a manual disable made BEFORE adding unsupported-codec files, per the corrected repro above.

---

### 76. Files.cs empty catch on Exists / COMException swallowed in MainWindow drop handler

`☐ Open` · **Severity** Low · **Importance** 2 · **Class** partial · **Finders** 1 · **Ref** C125 · **Present** yes

**Location:** AudioCat/Services/Files.cs:16-20 (IsDirectory empty catch); AudioCat/Windows/MainWindow.xaml.cs:65-101 (OnDataGridDrop COMException handler + AddDragFiles empty catch)

**Description:** Files.IsDirectory (Files.cs:18-19) wraps Directory.Exists/File.GetAttributes in a try/catch that swallows every exception and returns false, so a path that throws PathTooLongException/UnauthorizedAccessException is silently classified as "not a directory". In MainWindow, OnDataGridDrop (lines 67-80) has a dedicated COMException 0x8007007A (path-too-long) handler that shows a MessageBox, but it only wraps e.Data.GetData and the Task.Run launch; the actual file enumeration and probing happen on a background thread inside AddDragFiles, whose body is wrapped in its own catch { /* ignore */ } (lines 95-96). Any exception (including path-too-long) raised during GetFilesFromDirectories/AddMediaFiles is therefore swallowed on the worker thread and never reaches the COMException handler.

**Impact:** When a user drops a file/folder with an over-long path or an inaccessible directory, the app silently does nothing: no files added, no error, and the path-too-long MessageBox at MainWindow.xaml.cs:76 never fires because that error surfaces on the background thread where it is discarded. Users get no feedback that the drop failed.

**Repro:** Drop a folder/file whose full path exceeds MAX_PATH (or an access-denied directory) onto the file DataGrid without long-path support enabled. GetFilesFromDirectories/AddMediaFiles throws on the worker thread, is caught by AddDragFiles' empty catch, and nothing visible happens (the COMException path-too-long dialog does not appear).

**Suggested fix:** Catch specific exceptions (or move the COMException/path-too-long handling) inside AddDragFiles and surface a message via Dispatcher; in IsDirectory log/let unexpected exceptions through rather than blanket-swallowing.

---

### 77. CreateFromIntervals overlapping intervals -> negative-duration chapter

`☐ Open` · **Severity** Low · **Importance** 2 · **Class** partial · **Finders** 1 · **Ref** C25 · **Present** yes (reachable today — see re-audit note)

**Location:** AudioCat/Services/ChaptersFactory.cs:145-157 (CreateFromIntervals), 178-198 (CreateChapter); fed by AudioCat/Commands/ScanForSilenceCommand.cs:32-50

**Description:** CreateFromIntervals builds each chapter as duration = interval.Start - startTime, then advances startTime to interval.End (via startTime += interval.End - startTime). Thus chapter N's duration equals interval[N].Start - interval[N-1].End. If intervals were not monotonic (i.e. interval[N].Start < interval[N-1].End, an overlap), duration is negative. CreateChapter then computes endTime = startTime.Add(duration) with no clamp, yielding a chapter whose EndTime < StartTime and a negative calculatedEnd-calculatedStart span. There is no validation or Max(0,...) guard anywhere in the path.

**Impact:** Produces malformed chapters (End before Start, negative span) that corrupt the chapters wizard display and generated metadata/CUE. Reachable at runtime today: in a multi-file silence scan, ScanForSilenceCommand.cs:48 appends each file's end sentinel as `new Interval(file.FilePath, fileDuration, fileDuration)` WITHOUT the cumulative startTime offset (unlike AddFileIntervals at line 73), so the interval sequence is non-monotonic and CreateFromIntervals emits negative-duration chapters — same root cause as #24 (C22).

**Repro:** Open the Create Chapters wizard with two or more files and run a silence scan: the un-offset file-end sentinels make the interval sequence non-monotonic and negative-duration chapters are produced.

**Suggested fix:** Guard in CreateFromIntervals/CreateChapter: skip or clamp when interval.Start <= startTime (duration <= 0) so no negative-duration chapter is emitted.

**Re-audit (2026-07-01):** the earlier "not reachable at runtime" verdict was wrong — the multi-file silence scan supplies non-monotonic intervals today via the un-offset end sentinel (ScanForSilenceCommand.cs:48). Fixing #24 (C22) removes the current trigger, but the missing guard here should still be added.

---

### 78. TRACK 00 accepted by parser but rejected by TrackBuilder as 'missing the number' (0 overloaded as unset)

`☐ Open` · **Severity** Low · **Importance** 2 · **Class** real · **Finders** 1 · **Ref** C63 · **Present** yes

**Location:** AudioCat/Cue/CommandFactory.cs:122; AudioCat/Cue/TrackBuilder.cs:21,29,39-40,49

**Description:** CommandFactory.CreateTrackCommand (line 122) only rejects a TRACK number when it fails int.TryParse or is < 0, so `TRACK 00` parses successfully into a TrackCommand with Number=0. TrackBuilder reuses 0 as the "unset" sentinel: Number defaults to 0, Clear() resets it to 0 (line 49), and Build() (lines 39-40) treats Number == 0 as "missing the number" and returns Failure("The track is missing the number"). Thus a valid, parser-accepted track number of 0 is indistinguishable from an unset track and is rejected at build time.

**Impact:** Any CUE sheet containing a `TRACK 00 ...` entry fails the entire parse with "The track is missing the number" (the failure propagates up through Parser.AddFileToCue/ProcessTrackCommandWhenTrackFound and aborts the whole cue), even though such files are syntactically valid. Real-world impact is low since track numbering conventionally starts at 01, but a 0-numbered track is silently impossible.

**Repro:** Load/parse a CUE sheet whose track line is `TRACK 00 AUDIO` (followed by a valid INDEX) via Cue/Parser.cs. The parse returns failure "The track is missing the number" instead of producing a track with Number 0.

**Suggested fix:** Use a sentinel that cannot be a valid number (e.g. -1 / nullable int) for "unset", or track set-state with a bool flag, instead of overloading 0.

---

### 81. O(n^2) reassembly of remuxed files into original order (SortRemuxedFiles brute force)

`☐ Open` · **Severity** Trivial · **Importance** 2 · **Class** real · **Finders** 1 · **Ref** C127 · **Present** yes

**Location:** AudioCat/FFmpeg/FFmpegService.cs:339-354 (called from line 305)

**Description:** SortRemuxedFiles restores the original media-file ordering after parallel remuxing by nesting two loops: an outer loop over mediaFiles (n entries) and an inner loop scanning the entire remuxedFiles ConcurrentBag (also up to n entries) to find the reference-equal IMediaFileViewModel, breaking on first match. This is a brute-force O(n*m) = O(n^2) reassembly. The ordering is needed because remuxedFiles is populated out-of-order by the AsParallel() remux tasks (lines 288-298). A dictionary keyed by the view-model (or by stable index) would make it O(n).

**Impact:** Purely a performance/algorithmic-complexity concern; output is correct. For the realistic file counts in an audiobook-concat scenario (tens, occasionally low hundreds of files) the quadratic cost is negligible compared to the FFmpeg remux subprocess time, so there is no perceptible user-facing impact.

**Repro:** Latent performance only - add a very large number of files (e.g. thousands) requiring remux (RemuxOnErrors path), trigger Concatenate; the O(n^2) sort runs but stays dwarfed by remux/concat time, so no observable slowdown in practice.

**Suggested fix:** Build a Dictionary<IMediaFileViewModel,string> (or order by a stored index) from remuxedFiles, then project mediaFiles through it for O(n) reordering.

---

### 84. -update true flag (image2 muxer specific) used in audio remux command

`☐ Open` · **Severity** Trivial · **Importance** 2 · **Class** partial · **Finders** 1 · **Ref** C88 · **Present** yes

**Location:** AudioCat/FFmpeg/FFmpegService.cs:397 (RemuxFile), 421-422 and 425-426 (GetFFmpegArgs)

**Description:** The `-update true` flag is appended to every audio concat/remux FFmpeg command string: the single-file remux at line 397, and all four output-arg branches of GetFFmpegArgs (lines 421, 422, 425, 426). `-update` is a private muxer option of the `image2` muxer (correctly used at line 755 where `-f image2` is present). These audio commands write mp3/m4a/ogg/opus/flac/etc. via audio muxers and never set `-f image2`, so the option has no meaning there. FFmpeg currently tolerates the unrecognized option (the app works in manual testing), but it is cargo-culted from the image-extraction command.

**Impact:** No functional impact today; FFmpeg ignores the meaningless option for audio output, so concatenation/remux succeed. The flag is misleading and brittle: a future FFmpeg version that treats unknown output options as fatal could break all concat/remux operations.

**Repro:** Latent/cosmetic - Add audio files and concatenate; FFmpeg runs with `-update true` but ignores it, so output is correct. No observable defect with current FFmpeg versions.

**Suggested fix:** Remove `-update true` from the four audio command strings in RemuxFile and GetFFmpegArgs; keep it only on the image2 extraction command at line 755.

---

### 87. SelectCodec misleading comment ('Acceptable codec has not been selected yet' on the selected branch)

`☐ Open` · **Severity** Trivial · **Importance** 1 · **Class** real · **Finders** 4 · **Ref** C58 · **Present** yes

**Location:** AudioCat/Services/MediaFilesService.cs:114

**Description:** In SelectCodec, the inline comment "Acceptable codec has not been selected yet" sits on the `if (selectedCodec != "")` branch. That condition is true exactly when an acceptable codec HAS already been selected (non-empty), and the branch validates the current file against it via HasStreamWithCodec. The comment describes the opposite situation (the fall-through at line 118 where `selectedCodec == ""` and a codec gets picked via GetCodecName), so it is placed on the wrong branch and is misleading. Pure comment defect; the logic itself is correct.

**Impact:** No runtime effect; behavior is correct. Only a maintainability/readability hazard that could mislead a future developer reading SelectCodec.

**Repro:** Latent - comment-only defect, no runtime behavior change.

**Suggested fix:** Move/reword the comment: the `if (selectedCodec != "")` branch means a codec was already selected; place the "not selected yet" note on the fall-through assignment at line 118.

---

### 88. Duplicate 'End' column header in chapters grid (third column labeled End but binds Duration)

`☐ Open` · **Severity** Trivial · **Importance** 1 · **Class** real · **Finders** 2 · **Ref** C60 · **Present** yes

**Location:** AudioCat/Windows/CreateChaptersWindow.xaml:89-98

**Description:** In the Output Chapters DataGrid, two consecutive DataGridTextColumns both declare Header="End". The third column (line 90) correctly binds EndTime, but the fourth column (line 95) is also labeled "End" while binding Duration (with ConverterParameter=Trim). The fourth header is mislabeled; it should read "Duration".

**Impact:** Cosmetic only: the chapters wizard grid shows two adjacent columns titled "End", one of which actually displays each chapter's Duration. Users see a duplicate header and cannot tell the Duration column from the End column. No functional/data effect.

**Repro:** Open the Create Chapters window with chapters loaded; observe the Output Chapters grid has two columns headed "End" (the second of which displays trimmed Duration values).

**Suggested fix:** Change the fourth column's Header from "End" to "Duration" at CreateChaptersWindow.xaml:95.

---

### 89. CUE Generator re-emits plain REM comment with doubled REM token (dead code; ToCommands has no callers)

`☐ Open` · **Severity** Low · **Importance** 1 · **Class** partial · **Finders** 1 · **Ref** C62 · **Present** yes

**Location:** AudioCat/Cue/Generator.cs:13-14 (cue-level) and :43-46 (track-level); root cause in AudioCat/Cue/CommandFactory.cs:193-204 (CreateCueRemCommand)

**Description:** When the CUE parser reads a plain REM comment (e.g. `REM This is a comment`), CommandFactory.CreateCueRemCommand stores it as a TagCommand with Name = Command.REM ("REM") and Value = the comment text (Generator.cs root cause at CommandFactory.cs:201-204, the non-sub-command branch). Generator.ToCommands then re-emits cue-level tags as `$"REM {tag.Name} {tag.Value}"` (line 14), producing `REM REM This is a comment` — a doubled REM token. The track-level loop (lines 43-46) has the equivalent problem for plain REM comments stored at track level. Structured REM sub-commands like `REM GENRE Rock` round-trip correctly because their Name is the sub-command, not "REM".

**Impact:** A CUE round-trip (parse then regenerate) would corrupt plain REM comment lines by duplicating the REM keyword. No user-facing impact today because Generator.ToCommands has zero callers — the app only parses CUE sheets and writes them via Cue/Builder, never via Generator. Latent dead-code defect.

**Repro:** Latent - Generator.ToCommands has no callers anywhere in the codebase (app parses CUE and generates output through other paths). To observe: call cue.ToCommands() on an ICue parsed from a sheet containing a plain `REM <comment>` line; output line is `REM REM <comment>`.

**Suggested fix:** In Generator emit plain REM tags as `REM {tag.Value}` when tag.Name == "REM", else `REM {tag.Name} {tag.Value}` (or delete the unused ToCommands).

---

### 90. DurationConverter returns 'N/A' for zero-length (TimeSpan.Zero) files

`☐ Open` · **Severity** Low · **Importance** 1 · **Class** partial · **Finders** 1 · **Ref** C85 · **Present** yes

**Location:** AudioCat/Converters/DurationConverter.cs:9-12

**Description:** DurationConverter.Convert uses the property pattern `value is TimeSpan { TotalSeconds: > 0 } time`, so any TimeSpan whose TotalSeconds equals exactly 0 (including TimeSpan.Zero) falls through to the "N/A" branch instead of formatting as "00:00:00". A genuinely zero-length value and a non-TimeSpan/null value are therefore rendered identically. This is most visible for chapter/interval StartTime bindings (MainWindow.xaml lines 1036/1348 and CreateChaptersWindow.xaml line 87), where the first chapter's StartTime is normally TimeSpan.Zero.

**Impact:** The first chapter's start time (00:00:00), and any truly zero-duration file or interval, display as "N/A" in the DataGrids rather than "00:00:00", which is cosmetically misleading. Cosmetic only; no functional/data effect on concatenation.

**Repro:** Open the app, load files and create/display chapters (Create Chapters wizard or the chapters grid in MainWindow). The first chapter's StartTime column shows "N/A" instead of "00:00:00".

**Suggested fix:** Change guard to `value is TimeSpan { TotalSeconds: >= 0 } time` (or `value is TimeSpan time`) so zero formats as 00:00:00.
