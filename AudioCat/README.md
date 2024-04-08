# Audio Cat Tool 

A tool for concatenating audio files.

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
Auto-select the first file when adding by files selection or directory selection.
Subtitle streams
General logs view
Multi select for moving multiple files
Add support for more audio files formats
More intelligent suggested file names
Configuration.
Globalization, Localization.
