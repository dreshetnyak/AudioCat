# Audio Cat Tool 

A tool for concatenating audio files.

AudioCat v1.6.1
===============
Bug. If the cover image was added to the list then it was deleted by the end of concatenation, not it is preserved.

AudioCat v1.6.0
===============
New. Now we can add image files along with audio files, those images will be attached as cover images. 
New. Now if files are dragged and Left Ctrl is pressed the files will be added without clearing existing files.
Bug. The text values read from files was dispayed garbled if they was in a language different from English.
Fix. If a bitrate was not available an empty value was shown, now it is 'N/A'.
Bug. When adding files by a directory, only mp3 files were added.

AudioCat v1.5.1
===============
Bug. If a warning that is ignored was repeated more than one time the error dialog would show anyway.
Bug. Progress bar calculation was done based on the total file size, that could result in a wrong progress if one of the streams was discarded, now it is based on the duration.
Fix. If metedata had an encoding BOM error that was always resulting in a concatenation error, now we handle it.

AudioCat v1.5.0
===============
Fix. Now the save file extension is added according to the input files encoding.
New. Added support for cases when a jpg cover image is erroneously present as a png.
New. Now app checks if ffmpeg and ffprobe is accessible.
New. Now can remove a file by pressing Delete, or move files with Ctrl+Up/Down.
Bug. When adding several files or a directory the first file was always marked as a metadata source event if the source was already selected.
Fix. Chapters information is now discarded, before it could be writing an incomplete chapters info. Chapters are not supported.
Fix. Removed error repetition messages, otherwise they were interpreted as errors.
Fix. Cancel button is hidden now since it is not implemented.
Fix. If a cover image was invalid that could result in a failure to output a file, now we skip those images.

AudioCat v1.4.1
===============
Bug. Adding cover was causing the concatenation error dialog to pop-up with an empty error message.

AudioCat v1.4.0
===============
Bug. The escaping of the input file names was done incorrectly causing concatenation to fail in some cases.
Fix. Corrected the algorithm of selection of the default tags and cover image when the files is added to the list.
New. Now when the files are added they are properly sorted.
New. Now when adding filed the first file that contains an audio stream define the expected encoding, the rest of the files is skipped if their encoding doesn't match.
New. If files was skipped during addition a dialog will pop-up listing the files and reasons for skipping.
New. When the files are added using drag-and-drop the first file is automatically selected.

AudioCat v1.3.0
===============
Now can add cover images to the output file.

AudioCat v1.2.0
===============
When saving the concatenated file, now the save dialog opens to the path of the first file.
Now using -hide_banner.
Tags source now can be deselected.
Now the selected tags are written to the output file.
Cancel button image had a cursor over it, replaced the images.
Replaced the selected tags source icon from arrow to checkmark.
Gray ckeckmark displayed if the file has tags. 
Now selecting files without tags as a tags source is not allowed.
Now handling concatenation errors, displaying them and offering to delete the output.


Backlog
=======

Allow adding images.
Generate chapters. Options to generate from file names, from tags as well as passing through existing chapters.
Pin down tags, streams and chapters.
Tags, streams and chapters make the presence of content visible.
Adjust the margin on the right of expanders.

General logs view
Multi select for moving multiple files
Add support for more audio files formats
More intelligent suggested file names
Configuration.
Globalization, Localization.
