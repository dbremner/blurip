v0.5.2 (2010/12/31)

> - French translation<br>
<blockquote>- Video preview with vlc plugin (needs installed vlc >= 1.1.5 + activeX plugin)<br>
- Progress bar for encode/mux with Windows 7 taskbar support<br>
- Always recreate .avs on encode<br>
- New encoding profiles<br>
- Option to edit CropInfo<br>
- Hardcode subs with SupTitle.dll<br>
- Support for 3D blurays<br>
- New plugin system<br>
- Changed default 'settings.xml' location (AppData\Roaming\BluRip)<br>
- Get language tags for tracks with no prefered language<br>
- Always delete index files if older then video<br>
- Option to add additional AC3 track for all DTS tracks<br>
- Lots of fixes & minor enhancements<br></blockquote>

v0.5.1 (2010/08/16)<br>
<br>
<blockquote>- Some minor GUI fixes (don't reset position on language change etc)<br>
- Option to disable mkvmerge header compression (for audio + video tracks)<br>
- Resize method selection<br>
- Edit functions for demuxed stream list<br>
- Option to edit AutoCrop values (crop, addborders, resize) by hand<br>
- Lots of minor bugfixes<br>
- Show expected size/bitrate in mainwindow when 2-pass profile selected<br>
- Compatible with latest DGDecNV<br></blockquote>

v0.5.0 (2010/07/20)<br>
<blockquote>- Complete GUI redesign<br>
- Skin support<br>
- Multilanguage support (english + german)<br>
- Support for PGS subtitles<br>
- Lots of minor bugfixes<br>
- Optional path for encoded video<br>
- Shutdown possible when not using queue<br>
- Prevent standby when running<br>
- Process step selection<br>
- Preferred language selection for audio and subtitles<br>
- Option to set forced flag in .mkv for first forced track<br>
- Compatible with latest 64-bit x264 build<br>
- Switched to .NET 4.0<br></blockquote>

v0.4.9<br>
<blockquote>- Improved subtitle handling<br>
- Improved index generation handling<br>
- Fixed bitrate calculation<br>
- Option to mux lowres subtitles (for popcorn players)<br>
- Fixed PCM track handling<br>
- Updated MediaInfo.dll<br></blockquote>

v0.4.8<br>
<blockquote>- Split almost all processing steps into seperate DLLs<br>
- Support for 64 bit x264 using avs2yuv<br>
- Improved subtitle processing (faster/split tracks that contain normal&forced subs into 2 seperate tracks)<br>
- Added bitrate calculator for 2-pass profiles (not using any overhead calculation yet)<br>
- Added a lot of new default profiles<br>
- Advanced options for audio & video tracks (on stream selection tab)<br>
<blockquote>- Set output format for each audio track<br>
- Include additionally AC3 track<br>
- Disable autocrop/set framerate/resolution manually<br>
</blockquote>- Support for PCM tracks (AC3 by default)<br>
- Only needs .NET 2.0<br></blockquote>

v0.4.7<br>
<blockquote>- Code rewrite to allow gui redesign later<br>
- Fixed some bugs in stream selection<br>
- Delete index files if "Delete demuxed files" is checked<br></blockquote>

v0.4.6.1<br>
<blockquote>- Removed check for CuvidServer.exe<br></blockquote>

v0.4.6<br>
<blockquote>- Show return codes of all external tools<br>
- Do not select AC3 surround tracks if other tracks are available<br>
- Use DGMultiSource instead of CuvidServer<br>
- If "delete input files" is selected, index files are deleted<br>
- Added LoadPlugin command to avs files<br>
- Updated MediaInfo.dll<br>
- Moved shutdown checkbox from tab control to mainform (to allow changes while queue is running)<br>
- set chapter language correctly<br>
- Set framerate based on string parsing if mediainfo fails<br></blockquote>

v0.4.5<br>
<blockquote>- DGDecNv support<br>
- Option to use untouched audio<br>
- Option to shutdown after queue<br>
- Option to convert DTS to AC3<br>
- More subtitle mux/copy options<br>
- Recognize more Ac3 types<br></blockquote>

v0.4.4<br>
<blockquote>- Load/save projects<br>
- Queue system<br>
- Changed avs input source selection<br></blockquote>

v0.4.3<br>
<blockquote>- Option to save log to file (right click in log window)<br>
- Option to start AutoCrop window minimized<br>
- Show encoding progress in title/taskbar icon text<br>
- Fix for empty profiles on first start<br>
- Option to only mux forced subs into mkv<br>
- Option to copy all subs to 'Subs' folder<br>
- Option to copy all subs but forced to 'Subs' folder<br></blockquote>

v0.4.2<br>
<blockquote>- Added option to downsample AC3 & DTS tracks<br>
- Added DTS Hi-Res to dts audio tracks<br>
- Resized GUI (smaller)<br>
- Added some file existence checks<br></blockquote>

v0.4.1<br>
<blockquote>- Added AviSynth profiles<br>
- Support for muxing untouched video<br>
- Support for 720p resizing<br></blockquote>

v0.4.0<br>
<blockquote>- Added support for 2-pass profiles<br>
- Bugfix: Subtitles were not muxed<br>
- Bugfix: Stream number for subtitles in Sub folder not incremented<br></blockquote>

v0.3.9<br>
<blockquote>- Added support for <b>.m2ts files</b><br></blockquote>

v0.3.8<br>
<blockquote>- Option to use -core for DTS-HD tracks<br>
- Quotes around all filenames<br></blockquote>

v0.3.7<br>
<blockquote>- Removed MediaInfoWrapper - used interface form MediaInfoDll<br>
- Fixed typo for crop modes<br>
- Log windows for each step<br>
- Copy subtitles in same order as in streamlist<br></blockquote>

v0.3.6<br>
<blockquote>- First public release<br>