
# Audio Cat Tool 

**Audio Cat Tool** is a utility for con**cat**enating audio files. It provides a user interface for [FFMpeg](https://ffmpeg.org/) CLI tools (which are required for proper functioning). The tool supports MP3 and AAC encodings, which can be packaged in various audio container formats. It does not re-encode the audio; instead, it performs demuxing and remuxing, ensuring there is no loss in audio quality. Additionally, it allows for the preservation of media tags and cover images. Tags can be edited, and cover images can be added from image files.

## Screenshot
![Screenshot](App.png)

## Version History

### AudioCat 2.1.0
New. Added a popup menu to the tags grid that has an option to fix the tags Cyrillic encoding.<br>
New. Now the app accepts a drop of directories.

<details>
<summary>Previous versions</summary>

### AudioCat v2.0.0
Bug. When adding some files the app would freeze on the probing stage.<br>
New. Added the switch for enabling or disabling media tags, also adding, deleting and moving them around.<br>
New. Now dragging files into the app also work with right Control.<br>
New. Now app accesps files from CLI, or if they are dropped to it.<br>
Changed the font and the font size for some UI elements.<br>
The code went through a significant refactoring.

### AudioCat v1.7.2
Bug. When adding files with very long names by drag-n-drop, no files will be added, no message would be shown. Now the error is handles and we are showing the message.

### AudioCat v1.7.1
Fix. The toolbar items is now locked in their places.<br>
Bug. When adding files by drag-n-drop while pressing Ctrl the tags and image was erroneously selected in the new files.

### AudioCat v1.7.0
New. Now file probing is done in parallel, that can significantly increase files addition speed.<br>
Bug. The duration and bitrate was shown for JPG files, now it is N/A.

### AudioCat v1.6.1
Bug. If the cover image was added to the list then it was deleted by the end of concatenation, not it is preserved.

### AudioCat v1.6.0
New. Now we can add image files along with audio files, those images will be attached as cover images.<br>
New. Now if files are dragged and Left Ctrl is pressed the files will be added without clearing existing files.<br>
Bug. The text values read from files was dispayed garbled if they was in a language different from English.<br>
Fix. If a bitrate was not available an empty value was shown, now it is 'N/A'.<br>
Bug. When adding files by a directory, only mp3 files were added.

### AudioCat v1.5.1
Bug. If a warning that is ignored was repeated more than one time the error dialog would show anyway.<br>
Bug. Progress bar calculation was done based on the total file size, that could result in a wrong progress if one of the streams was discarded, now it is based on the duration.<br>
Fix. If metedata had an encoding BOM error that was always resulting in a concatenation error, now we handle it.

### AudioCat v1.5.0
Fix. Now the save file extension is added according to the input files encoding.<br>
New. Added support for cases when a jpg cover image is erroneously present as a png.<br>
New. Now app checks if ffmpeg and ffprobe is accessible.<br>
New. Now can remove a file by pressing Delete, or move files with Ctrl+Up/Down.<br>
Bug. When adding several files or a directory the first file was always marked as a metadata source event if the source was already selected.<br>
Fix. Chapters information is now discarded, before it could be writing an incomplete chapters info. Chapters are not supported.<br>
Fix. Removed error repetition messages, otherwise they were interpreted as errors.<br>
Fix. Cancel button is hidden now since it is not implemented.<br>
Fix. If a cover image was invalid that could result in a failure to output a file, now we skip those images.

### AudioCat v1.4.1
Bug. Adding cover was causing the concatenation error dialog to pop-up with an empty error message.

### AudioCat v1.4.0
Bug. The escaping of the input file names was done incorrectly causing concatenation to fail in some cases.<br>
Fix. Corrected the algorithm of selection of the default tags and cover image when the files is added to the list.<br>
New. Now when the files are added they are properly sorted.<br>
New. Now when adding filed the first file that contains an audio stream define the expected encoding, the rest of the files is skipped if their encoding doesn't match.<br>
New. If files was skipped during addition a dialog will pop-up listing the files and reasons for skipping.<br>
New. When the files are added using drag-and-drop the first file is automatically selected.

### AudioCat v1.3.0
Now can add cover images to the output file.

### AudioCat v1.2.0
When saving the concatenated file, now the save dialog opens to the path of the first file.<br>
Now using -hide_banner.<br>
Tags source now can be deselected.<br>
Now the selected tags are written to the output file.<br>
Cancel button image had a cursor over it, replaced the images.<br>
Replaced the selected tags source icon from arrow to checkmark.<br>
Gray ckeckmark displayed if the file has tags.<br>
Now selecting files without tags as a tags source is not allowed.<br>
Now handling concatenation errors, displaying them and offering to delete the output.
</details>
