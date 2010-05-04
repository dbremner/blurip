﻿//BluRip - one click BluRay/m2ts to mkv converter
//Copyright (C) 2009-2010 _hawk_

//This program is free software; you can redistribute it and/or
//modify it under the terms of the GNU General Public License
//as published by the Free Software Foundation; either version 2
//of the License, or (at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

//Contact: hawk.ac@gmx.net

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using MediaInfoLib;

namespace BluRip
{
    public partial class MainForm : Form
    {
        private UserSettings settings = new UserSettings();
        private string settingsPath = "";
        
        private List<string> videoTypes = new List<string>();
        private List<string> ac3AudioTypes = new List<string>();
        private List<string> dtsAudioTypes = new List<string>();
                
        private Thread indexThread = null;

        private Process pc = new Process();

        public string title = "BluRip v0.4.8 © _hawk_";

        public MainForm()
        {
            InitializeComponent();
            try
            {
                videoTypes.Add("h264/AVC");
                videoTypes.Add("VC-1");
                videoTypes.Add("MPEG2");

                ac3AudioTypes.Add("TrueHD/AC3");
                ac3AudioTypes.Add("AC3");
                ac3AudioTypes.Add("AC3 Surround");
                ac3AudioTypes.Add("AC3 EX");
                ac3AudioTypes.Add("E-AC3");
                ac3AudioTypes.Add("RAW/PCM"); // convert to ac3 by default

                dtsAudioTypes.Add("DTS");
                dtsAudioTypes.Add("DTS Master Audio");
                dtsAudioTypes.Add("DTS Express");
                dtsAudioTypes.Add("DTS Hi-Res");
                dtsAudioTypes.Add("DTS ES"); // have to check if needed
                dtsAudioTypes.Add("DTS-ES");

                comboBoxX264Priority.Items.Clear();
                foreach (string s in Enum.GetNames(typeof(ProcessPriorityClass)))
                {
                    comboBoxX264Priority.Items.Add(s);
                }
            }
            catch (Exception)
            {
            }
        }

        private void MessageMain(string msg)
        {
            try
            {
                if (richTextBoxLogMain.Disposing) return;
                if (richTextBoxLogMain.IsDisposed) return;
                if (this.richTextBoxLogMain.InvokeRequired)
                {
                    MsgHandler mh = new MsgHandler(MessageMain);
                    this.Invoke(mh, new object[] { msg });
                }
                else
                {
                    richTextBoxLogMain.AppendText("[" + DateTime.Now.ToString() + "] " + msg + "\n");
                    richTextBoxLogMain.ScrollToCaret();
                }
            }
            catch (Exception)
            {
            }
        }

        private void MessageDemux(string msg)
        {
            try
            {
                if (richTextBoxLogDemux.Disposing) return;
                if (richTextBoxLogDemux.IsDisposed) return;
                if (this.richTextBoxLogDemux.InvokeRequired)
                {
                    MsgHandler mh = new MsgHandler(MessageDemux);
                    this.Invoke(mh, new object[] { msg });
                }
                else
                {
                    richTextBoxLogDemux.AppendText("[" + DateTime.Now.ToString() + "] " + msg + "\n");
                    richTextBoxLogDemux.ScrollToCaret();
                    MessageMain(msg);
                }
            }
            catch (Exception)
            {
            }
        }

        private void MessageCrop(string msg)
        {
            try
            {
                if (richTextBoxLogCrop.Disposing) return;
                if (richTextBoxLogCrop.IsDisposed) return;
                if (this.richTextBoxLogCrop.InvokeRequired)
                {
                    MsgHandler mh = new MsgHandler(MessageCrop);
                    this.Invoke(mh, new object[] { msg });
                }
                else
                {
                    richTextBoxLogCrop.AppendText("[" + DateTime.Now.ToString() + "] " + msg + "\n");
                    richTextBoxLogCrop.ScrollToCaret();
                    MessageMain(msg);
                }
            }
            catch (Exception)
            {
            }
        }

        private void MessageSubtitle(string msg)
        {
            try
            {
                if (richTextBoxLogSubtitle.Disposing) return;
                if (richTextBoxLogSubtitle.IsDisposed) return;
                if (this.richTextBoxLogSubtitle.InvokeRequired)
                {
                    MsgHandler mh = new MsgHandler(MessageSubtitle);
                    this.Invoke(mh, new object[] { msg });
                }
                else
                {
                    richTextBoxLogSubtitle.AppendText("[" + DateTime.Now.ToString() + "] " + msg + "\n");
                    richTextBoxLogSubtitle.ScrollToCaret();
                    MessageMain(msg);
                }
            }
            catch (Exception)
            {
            }
        }

        private void MessageEncode(string msg)
        {
            try
            {
                if (richTextBoxLogEncode.Disposing) return;
                if (richTextBoxLogEncode.IsDisposed) return;
                if (this.richTextBoxLogEncode.InvokeRequired)
                {
                    MsgHandler mh = new MsgHandler(MessageEncode);
                    this.Invoke(mh, new object[] { msg });
                }
                else
                {
                    richTextBoxLogEncode.AppendText("[" + DateTime.Now.ToString() + "] " + msg + "\n");
                    richTextBoxLogEncode.ScrollToCaret();
                    MessageMain(msg);
                }
            }
            catch (Exception)
            {
            }
        }

        private void MessageMux(string msg)
        {
            try
            {
                if (richTextBoxLogMux.Disposing) return;
                if (richTextBoxLogMux.IsDisposed) return;
                if (this.richTextBoxLogMux.InvokeRequired)
                {
                    MsgHandler mh = new MsgHandler(MessageMux);
                    this.Invoke(mh, new object[] { msg });
                }
                else
                {
                    richTextBoxLogMux.AppendText("[" + DateTime.Now.ToString() + "] " + msg + "\n");
                    richTextBoxLogMux.ScrollToCaret();
                    MessageMain(msg);
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonPath_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    textBoxPath.Text = fbd.SelectedPath;
                    settings.lastBluRayPath = fbd.SelectedPath;
                }
            }
            catch (Exception)
            {
            }
        }

        static StringBuilder sb = new StringBuilder();
        void OutputDataReceivedMain(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    sb.Append(e.Data + "\r\n");
                    MessageMain(e.Data.Replace("\b","").Trim());
                }
            }
            catch (Exception)
            {
            }
        }

        void OutputDataReceivedDemux(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    sb.Append(e.Data + "\r\n");
                    MessageDemux(e.Data.Replace("\b", "").Trim());
                }
            }
            catch (Exception)
            {
            }
        }

        void OutputDataReceivedCrop(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    sb.Append(e.Data + "\r\n");
                    MessageCrop(e.Data.Replace("\b", "").Trim());
                }
            }
            catch (Exception)
            {
            }
        }

        void OutputDataReceivedSubtitle(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    sb.Append(e.Data + "\r\n");
                    MessageSubtitle(e.Data.Replace("\b", "").Trim());
                }
            }
            catch (Exception)
            {
            }
        }

        void OutputDataReceivedEncode(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    sb.Append(e.Data + "\r\n");
                    MessageEncode(e.Data.Replace("\b", "").Trim());
                }
            }
            catch (Exception)
            {
            }
        }

        void OutputDataReceivedMux(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    sb.Append(e.Data + "\r\n");
                    MessageMux(e.Data.Replace("\b", "").Trim());
                }
            }
            catch (Exception)
            {
            }
        }

        void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    sb.Append(e.Data + "\r\n");
                    MessageMain(e.Data.Replace("\b", "").Trim());
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonEac3toPath_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "eac3to.exe|eac3to.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {                    
                    textBoxEac3toPath.Text = ofd.FileName;
                    settings.eac3toPath = ofd.FileName;
                }
            }
            catch (Exception)
            {
            }
        }

        private void UpdateLanguage()
        {
            try
            {
                listBoxPreferedLanguages.Items.Clear();
                foreach (LanguageInfo li in settings.preferedLanguages)
                {
                    listBoxPreferedLanguages.Items.Add(li.language + " (" + li.translation + " - " + li.languageShort + ")");
                }
            }
            catch (Exception)
            {
            }
        }

        private void UpdateEncodingSettings()
        {
            try
            {
                listBoxX264Profiles.Items.Clear();
                comboBoxEncodeProfile.Items.Clear();
                foreach (EncodingSettings es in settings.encodingSettings)
                {
                    listBoxX264Profiles.Items.Add(es.desc);
                    comboBoxEncodeProfile.Items.Add(es.desc);
                }
                if (settings.lastProfile > -1 && settings.lastProfile < settings.encodingSettings.Count)
                {
                    comboBoxEncodeProfile.SelectedIndex = settings.lastProfile;
                }
            }
            catch (Exception)
            {
            }
        }

        private void UpdateAvisynthSettings()
        {
            try
            {
                listBoxAviSynthProfiles.Items.Clear();
                comboBoxAvisynthProfile.Items.Clear();
                foreach (AvisynthSettings avs in settings.avisynthSettings)
                {
                    listBoxAviSynthProfiles.Items.Add(avs.desc);
                    comboBoxAvisynthProfile.Items.Add(avs.desc);
                }
                if (settings.lastAvisynthProfile > -1 && settings.lastAvisynthProfile < settings.avisynthSettings.Count)
                {
                    comboBoxAvisynthProfile.SelectedIndex = settings.lastAvisynthProfile;
                }
            }
            catch (Exception)
            {
            }
        }

        private void UpdateFromSettings()
        {
            try
            {
                textBoxEac3toPath.Text = settings.eac3toPath;
                textBoxPath.Text = settings.lastBluRayPath;

                checkBoxAutoSelect.Checked = settings.useAutoSelect;
                checkBoxIncludeSubtitle.Checked = settings.includeSubtitle;
                checkBoxPreferDts.Checked = settings.preferDTS;
                checkBoxSelectChapters.Checked = settings.includeChapter;
                listBoxPreferedLanguages.Items.Clear();
                textBoxWorkingDir.Text = settings.workingDir;
                textBoxFfmsindexPath.Text = settings.ffmsindexPath;
                textBoxX264Path.Text = settings.x264Path;
                textBoxSup2subPath.Text = settings.sup2subPath;
                textBoxFilePrefix.Text = settings.filePrefix;
                textBoxJavaPath.Text = settings.javaPath;
                textBoxMkvmergePath.Text = settings.mkvmergePath;

                numericUpDownBlackValue.Value = settings.blackValue;
                numericUpDownNrFrames.Value = settings.nrFrames;

                comboBoxCropMode.SelectedIndex = settings.cropMode;

                comboBoxX264Priority.SelectedItem = Enum.GetName(typeof(ProcessPriorityClass),settings.x264Priority);

                textBoxTargetFolder.Text = settings.targetFolder;
                textBoxTargetfilename.Text = settings.targetFilename;
                textBoxMovieTitle.Text = settings.movieTitle;

                checkBoxDefaultAudioTrack.Checked = settings.defaultAudio;
                checkBoxDefaultSubtitleForced.Checked = settings.defaultSubtitleForced;
                checkBoxDefaultSubtitleTrack.Checked = settings.defaultSubtitle;
                checkBoxDeleteAfterEncode.Checked = settings.deleteAfterEncode;

                checkBoxUseCore.Checked = settings.dtsHdCore;
                checkBoxUntouchedVideo.Checked = settings.untouchedVideo;
                checkBoxResize720p.Checked = settings.resize720p;

                checkBoxUntouchedVideo_CheckedChanged(null, null);

                checkBoxDownmixAc3.Checked = settings.downmixAc3;
                checkBoxDownmixDts.Checked = settings.downmixDTS;

                if (settings.downmixAc3Index > -1 && settings.downmixAc3Index < comboBoxDownmixAc3.Items.Count) comboBoxDownmixAc3.SelectedIndex = settings.downmixAc3Index;
                if (settings.downmixDTSIndex > -1 && settings.downmixDTSIndex < comboBoxDownmixDts.Items.Count) comboBoxDownmixDts.SelectedIndex = settings.downmixDTSIndex;

                checkBoxDownmixAc3_CheckedChanged(null, null);
                checkBoxDownmixDts_CheckedChanged(null, null);

                checkBoxMinimizeCrop.Checked = settings.minimizeAutocrop;                                     
                
                comboBoxCropInput.SelectedIndex = settings.cropInput;
                comboBoxEncodeInput.SelectedIndex = settings.encodeInput;

                checkBoxUntouchedAudio.Checked = settings.untouchedAudio;

                comboBoxCopySubs.SelectedIndex = settings.copySubs;
                comboBoxMuxSubs.SelectedIndex = settings.muxSubs;

                textBoxDgindexnvPath.Text = settings.dgindexnvPath;

                checkBoxDtsToAc3.Checked = settings.convertDtsToAc3;

                textBoxX264x64Path.Text = settings.x264x64Path;
                textBoxAvs2yuvPath.Text = settings.avs2yuvPath;
                checkBoxUse64bit.Checked = settings.use64bit;

                UpdateLanguage();
                UpdateEncodingSettings();
                UpdateAvisynthSettings();
            }
            catch (Exception)
            {
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                CheckForIllegalCrossThreadCalls = false;

                this.Text = title;

                settingsPath = Application.StartupPath + "\\settings.xml";
                if (!File.Exists(settingsPath))
                {
                    UserSettings.SaveSettingsFile(settings, settingsPath);
                    UserSettings.LoadSettingsFile(ref settings, settingsPath);
                }
                else
                {
                    UserSettings.LoadSettingsFile(ref settings, settingsPath);
                }
                UpdateFromSettings();
            }
            catch (Exception)
            {
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                UserSettings.SaveSettingsFile(settings, settingsPath);
                if (!silent)
                {
                    if (MessageBox.Show("Are you sure?", "Exit BluRip", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        abortThreads();
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    abortThreads();
                }
            }
            catch (Exception)
            {
            }
        }

        private void comboBoxTitle_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBoxTitle.SelectedIndex > -1)
                {
                    checkedListBoxStreams.Items.Clear();
                    UpdateStreamList();
                }
            }
            catch (Exception)
            {
            }
        }

        private string StreamTypeToString(StreamType st)
        {
            if (st == StreamType.Audio)
            {
                return "AUDIO";
            }
            else if (st == StreamType.Chapter)
            {
                return "CHAPTER";
            }
            else if (st == StreamType.Subtitle)
            {
                return "SUBTITLE";
            }
            else if (st == StreamType.Unknown)
            {
                return "UNKNOWN";
            }
            else if (st == StreamType.Video)
            {
                return "VIDEO";
            }
            else
            {
                return "UNKNOWN";
            }
        }

        private bool HasLanguage(string s)
        {
            foreach (LanguageInfo li in settings.preferedLanguages)
            {
                if (li.language == s) return true;
            }
            return false;
        }

        private int LanguagIndex(string s)
        {
            int index = -1;
            for (int i = 0; i < settings.preferedLanguages.Count; i++)
            {
                if (settings.preferedLanguages[i].language == s)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        private void UpdateStreamList()
        {
            try
            {
                int maxLength = 0;
                foreach (StreamInfo si in titleList[comboBoxTitle.SelectedIndex].streams)
                {                    
                    maxLength = Math.Max(maxLength, StreamTypeToString(si.streamType).Length);
                }

                List<int> maxac3List = new List<int>();
                List<int> maxdtsList = new List<int>();

                for (int i = 0; i < settings.preferedLanguages.Count; i++)
                {
                    maxac3List.Add(0);
                    maxdtsList.Add(0);
                }
                foreach (StreamInfo si in titleList[comboBoxTitle.SelectedIndex].streams)
                {
                }

                int videoCount = 0;
                int chapterCount = 0;

                if (settings.useAutoSelect)
                {
                    List<int> ac3List = new List<int>();
                    List<int> dtsList = new List<int>();

                    for (int i = 0; i < settings.preferedLanguages.Count; i++)
                    {
                        ac3List.Add(0);
                        dtsList.Add(0);
                    }
                    foreach (StreamInfo si in titleList[comboBoxTitle.SelectedIndex].streams)
                    {
                        if (si.streamType == StreamType.Audio)
                        {
                            if (HasLanguage(si.language))
                            {
                                int index = LanguagIndex(si.language);
                                if (dtsAudioTypes.Contains(si.typeDesc))
                                {
                                    maxdtsList[index]++;
                                }
                                if (ac3AudioTypes.Contains(si.typeDesc))
                                {
                                    maxac3List[index]++;
                                }
                            }
                        }
                    }

                    foreach (StreamInfo si in titleList[comboBoxTitle.SelectedIndex].streams)
                    {
                        if (si.streamType == StreamType.Chapter)
                        {
                            if (settings.includeChapter && chapterCount == 0)
                            {
                                si.selected = true;
                                chapterCount++;
                            }
                        }
                        if (si.streamType == StreamType.Subtitle)
                        {
                            if (settings.includeSubtitle)
                            {
                                if (HasLanguage(si.language))
                                {
                                    si.selected = true;
                                }
                            }
                        }
                        if (si.streamType == StreamType.Audio)
                        {
                            if (HasLanguage(si.language))
                            {
                                int index = LanguagIndex(si.language);
                                if (settings.preferDTS)
                                {
                                    if (dtsAudioTypes.Contains(si.typeDesc))
                                    {
                                        if (dtsList[index] == 0)
                                        {
                                            dtsList[index]++;
                                            si.selected = true;
                                        }
                                    }
                                    if (ac3AudioTypes.Contains(si.typeDesc) && maxdtsList[index] == 0)
                                    {
                                        if (ac3List[index] == 0)
                                        {
                                            ac3List[index]++;
                                            si.selected = true;
                                        }
                                    }
                                }
                                else
                                {
                                    if (ac3AudioTypes.Contains(si.typeDesc))
                                    {   
                                        if (ac3List[index] == 0)
                                        {
                                            if (si.typeDesc == "AC3 Surround" && maxac3List.Count > 0)
                                            {
                                            }
                                            else
                                            {
                                                ac3List[index]++;
                                                si.selected = true;
                                            }
                                        }
                                    }
                                    if (dtsAudioTypes.Contains(si.typeDesc) && maxac3List[index] == 0)
                                    {
                                        if (dtsList[index] == 0)
                                        {
                                            dtsList[index]++;
                                            si.selected = true;
                                        }
                                    }
                                }
                            }
                        }
                        if (si.streamType == StreamType.Video)
                        {
                            if (si.desc.Contains("1080") && videoCount == 0)
                            {
                                si.selected = true;
                                videoCount++;
                            }
                        }
                    }
                }

                checkedListBoxStreams.Items.Clear();
                foreach (StreamInfo si in titleList[comboBoxTitle.SelectedIndex].streams)
                {
                    string desc = "[ " + si.number.ToString("d3") + " ] - [ " + StreamTypeToString(si.streamType);
                    for (int i = 0; i < maxLength - StreamTypeToString(si.streamType).Length; i++) desc += " ";
                    desc += " ] - ";
                    if (si.advancedOptions != null && si.advancedOptions.GetType() != typeof(AdvancedOptions)) desc += "AO* ";
                    desc += "(" + si.desc + ")";
                    if (si.addInfo != "")
                    {
                        desc += " - (" + si.addInfo + ")";
                    }

                    checkedListBoxStreams.Items.Add(desc);
                    checkedListBoxStreams.SetItemChecked(checkedListBoxStreams.Items.Count - 1, si.selected);
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonDeleteLanguage_Click(object sender, EventArgs e)
        {
            if (listBoxPreferedLanguages.SelectedIndex > -1)
            {
                settings.preferedLanguages.RemoveAt(listBoxPreferedLanguages.SelectedIndex);
                UpdateLanguage();
            }
        }

        private void checkBoxAutoSelect_CheckedChanged(object sender, EventArgs e)
        {
            settings.useAutoSelect = checkBoxAutoSelect.Checked;
        }

        private void checkBoxSelectChapters_CheckedChanged(object sender, EventArgs e)
        {
            settings.includeChapter = checkBoxSelectChapters.Checked;
        }

        private void checkBoxPreferDts_CheckedChanged(object sender, EventArgs e)
        {
            settings.preferDTS = checkBoxPreferDts.Checked;
        }

        private void checkBoxIncludeSubtitle_CheckedChanged(object sender, EventArgs e)
        {
            settings.includeSubtitle = checkBoxIncludeSubtitle.Checked;
        }

        private void abortThreads()
        {
            try
            {
                abort = true;
                if (sit != null)
                {
                    sit.Stop();
                    sit = null;
                }
                if (mit != null)
                {
                    mit.Stop();
                    mit = null;
                }
                if (dt != null)
                {
                    dt.Stop();
                    dt = null;
                }
                if (et != null)
                {
                    et.Stop();
                    et = null;
                }
                if (mt != null)
                {
                    mt.Stop();
                    mt = null;
                }
                if (st != null)
                {
                    st.Stop();
                    st = null;
                }
                if (indexThread != null)
                {
                    indexThread.Abort();
                    indexThread = null;
                }

                try
                {
                    pc.Kill();
                    pc.Close();
                }
                catch (Exception)
                {
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonAbort_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure?", "Abort all threads", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    abortThreads();
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonWorkingDir_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();

                try
                {
                    if (settings.workingDir != "")
                    {
                        DirectoryInfo di = Directory.GetParent(settings.workingDir);
                        fbd.SelectedPath = di.FullName;
                    }
                }
                catch (Exception)
                {
                }

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    textBoxWorkingDir.Text = fbd.SelectedPath;
                    settings.workingDir = fbd.SelectedPath;
                }
            }
            catch (Exception)
            {
            }
        }

        private bool indexThreadStatus = false;
        private void IndexThread()
        {
            try
            {
                indexThreadStatus = false;
                string filename = "";
                AdvancedVideoOptions avo = null;
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Video)
                    {
                        filename = si.filename;
                        if (si.advancedOptions != null && si.advancedOptions.GetType() == typeof(AdvancedVideoOptions)) avo = (AdvancedVideoOptions)si.advancedOptions;
                        break;
                    }
                }

                string fps = "";
                string resX = "";
                string resY = "";

                try
                {
                    MediaInfoLib.MediaInfo mi2 = new MediaInfoLib.MediaInfo();
                    mi2.Open(filename);
                    mi2.Option("Complete", "1");
                    string[] tmpstr = mi2.Inform().Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in tmpstr)
                    {
                        MessageCrop(s.Trim());
                    }
                    if (mi2.Count_Get(StreamKind.Video) > 0)
                    {
                        fps = mi2.Get(StreamKind.Video, 0, "FrameRate");
                        resX = mi2.Get(StreamKind.Video, 0, "Width");
                        resY = mi2.Get(StreamKind.Video, 0, "Height");
                    }
                    mi2.Close();
                }
                catch (Exception ex)
                {
                    MessageCrop("Error getting MediaInfo: " + ex.Message);
                    return;
                }

                if (avo != null && avo.disableFps)
                {
                    MessageCrop("Using manual fps - override MediaInfo value");
                    fps = avo.fps;
                }

                if (fps == "")
                {
                    MessageCrop("Error getting framerate");
                    foreach (StreamInfo si in demuxedStreamList.streams)
                    {
                        if (si.streamType == StreamType.Video)
                        {
                            if (si.desc.Contains("24 /1.001"))
                            {
                                MessageCrop("Assume fps is 23.976");
                                fps = "23.976";
                                break;
                            }
                            else if (si.desc.Contains("1080p24 (16:9)"))
                            {
                                MessageCrop("Assume fps is 24");
                                fps = "24";
                                break;
                            }
                            // add other framerates here
                        }
                    }
                    if (fps == "")
                    {
                        MessageCrop("Could not get framerate - please report log to developer");
                        return;
                    }
                }

                sb.Remove(0, sb.Length);
                CropInfo cropInfo = new CropInfo();
                if (!settings.untouchedVideo)
                {
                    if (settings.cropInput == 1 || settings.encodeInput == 1)
                    {
                        if (!File.Exists(filename + ".ffindex"))
                        {
                            MessageCrop("Starting to index...");
                            MessageCrop("");

                            pc = new Process();
                            pc.StartInfo.FileName = settings.ffmsindexPath;
                            pc.StartInfo.Arguments = "\"" + filename + "\"";

                            MessageCrop("Command: " + pc.StartInfo.FileName + pc.StartInfo.Arguments);
                            pc.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceivedCrop);

                            pc.StartInfo.UseShellExecute = false;
                            pc.StartInfo.CreateNoWindow = true;
                            pc.StartInfo.RedirectStandardError = true;
                            pc.StartInfo.RedirectStandardOutput = true;

                            if (!pc.Start())
                            {
                                MessageCrop("Error starting ffmsindex.exe");
                                return;
                            }

                            pc.BeginOutputReadLine();

                            pc.WaitForExit();
                            MessageCrop("ffmsindex return code: " + pc.ExitCode.ToString());
                            pc.Close();
                            MessageCrop("Indexing done!");
                        }
                        else
                        {
                            MessageCrop(filename + ".ffindex already exits");
                        }
                    }
                    else if (settings.cropInput == 2 || settings.encodeInput == 2)
                    {
                        string output = Path.ChangeExtension(filename, "dgi");

                        if (!File.Exists(output))
                        {
                            MessageCrop("Starting to index...");
                            MessageCrop("");

                            pc = new Process();
                            pc.StartInfo.FileName = settings.dgindexnvPath;
                            pc.StartInfo.Arguments = "-i \"" + filename + "\" -o \"" + output + "\" -e";

                            MessageCrop("Command: " + pc.StartInfo.FileName + pc.StartInfo.Arguments);
                            pc.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceivedCrop);

                            pc.StartInfo.UseShellExecute = false;
                            pc.StartInfo.CreateNoWindow = true;
                            pc.StartInfo.RedirectStandardError = true;
                            pc.StartInfo.RedirectStandardOutput = true;

                            if (!pc.Start())
                            {
                                MessageCrop("Error starting DGIndexNv.exe");
                                return;
                            }

                            pc.BeginOutputReadLine();

                            pc.WaitForExit();
                            MessageCrop("dgindexnv return code: " + pc.ExitCode.ToString());
                            pc.Close();
                            MessageCrop("Indexing done!");
                        }
                        else
                        {
                            MessageCrop(output + " already exists");
                        }
                    }

                    if (avo == null || !avo.disableAutocrop)
                    {
                        if (settings.cropInput == 0)
                        {
                            File.WriteAllText(settings.workingDir + "\\" + settings.filePrefix + "_cropTemp.avs",
                                "DirectShowSource(\"" + filename + "\")");
                        }
                        else if (settings.cropInput == 1)
                        {
                            string data = "";
                            string dlldir = Path.GetDirectoryName(settings.ffmsindexPath);
                            if (File.Exists(dlldir + "\\ffms2.dll"))
                            {
                                data = "LoadPlugin(\"" + dlldir + "\\ffms2.dll" + "\")\r\n";
                            }
                            data += "FFVideoSource(\"" + filename + "\")";
                            File.WriteAllText(settings.workingDir + "\\" + settings.filePrefix + "_cropTemp.avs", data);
                        }
                        else if (settings.cropInput == 2)
                        {
                            string output = Path.ChangeExtension(filename, "dgi");
                            string data = "";
                            string dlldir = Path.GetDirectoryName(settings.dgindexnvPath);
                            if (File.Exists(dlldir + "\\DGMultiDecodeNV.dll"))
                            {
                                data = "LoadPlugin(\"" + dlldir + "\\DGMultiDecodeNV.dll" + "\")\r\n";
                            }
                            data += "DGMultiSource(\"" + output + "\")";
                            File.WriteAllText(settings.workingDir + "\\" + settings.filePrefix + "_cropTemp.avs", data);
                        }
                        MessageCrop("Starting AutoCrop...");

                        AutoCrop ac = new AutoCrop(settings.workingDir + "\\" + settings.filePrefix + "_cropTemp.avs", settings, ref cropInfo);
                        if (cropInfo.error)
                        {
                            MessageCrop("Exception: " + cropInfo.errorStr);
                            return;
                        }

                        if (settings.minimizeAutocrop)
                        {
                            ac.WindowState = FormWindowState.Minimized;
                        }

                        ac.NrFrames = settings.nrFrames;
                        ac.BlackValue = settings.blackValue;
                        ac.ShowDialog();
                    }
                    else
                    {
                        cropInfo.border = avo.manualBorders;
                        cropInfo.borderBottom = avo.borderBottom;
                        cropInfo.borderTop = avo.borderTop;
                        cropInfo.resize = avo.manualResize;
                        cropInfo.resizeX = avo.sizeX;
                        cropInfo.resizeY = avo.sizeY;
                        cropInfo.error = false;
                        if (avo.manualCrop)
                        {
                            cropInfo.cropBottom = avo.cropBottom;
                            cropInfo.cropTop = avo.cropTop;
                        }
                        else
                        {
                            cropInfo.cropBottom = 0;
                            cropInfo.cropTop = 0;
                        }
                    }

                    MessageCrop("");
                    MessageCrop("Crop top: " + cropInfo.cropTop.ToString());
                    MessageCrop("Crop bottom: " + cropInfo.cropBottom.ToString());
                    if (cropInfo.border)
                    {
                        MessageCrop("Border top: " + cropInfo.borderTop.ToString());
                        MessageCrop("Border bottom: " + cropInfo.borderBottom.ToString());
                    }
                    if (cropInfo.resize)
                    {
                        MessageCrop("Resize to: " + cropInfo.resizeX.ToString() + " x " + cropInfo.resizeY.ToString());
                    }

                    string encode = "";
                    if (settings.encodeInput == 0)
                    {
                        encode = "DirectShowSource(\"" + filename + "\")\r\n";
                    }
                    else if (settings.encodeInput == 1)
                    {
                        string dlldir = Path.GetDirectoryName(settings.ffmsindexPath);
                        if (File.Exists(dlldir + "\\ffms2.dll"))
                        {
                            encode += "LoadPlugin(\"" + dlldir + "\\ffms2.dll" + "\")\r\n";
                        }
                        encode += "FFVideoSource(\"" + filename + "\")\r\n";
                    }
                    else if (settings.encodeInput == 2)
                    {
                        string output = Path.ChangeExtension(filename, "dgi");
                        string dlldir = Path.GetDirectoryName(settings.dgindexnvPath);
                        if (File.Exists(dlldir + "\\DGMultiDecodeNV.dll"))
                        {
                            encode += "LoadPlugin(\"" + dlldir + "\\DGMultiDecodeNV.dll" + "\")\r\n";
                        }
                        encode += "DGMultiSource(\"" + output + "\")\r\n";
                    }
                    if (cropInfo.cropTop != 0 || cropInfo.cropBottom != 0)
                    {
                        encode += "Crop(0," + cropInfo.cropTop.ToString() + ",-0,-" + cropInfo.cropBottom.ToString() + ")\r\n";
                        if (cropInfo.border)
                        {
                            encode += "AddBorders(0," + cropInfo.borderTop + ",0," + cropInfo.borderBottom + ")\r\n";
                        }
                        else
                        {
                            MessageCrop("Did not add AddBorders command");
                        }
                        if (cropInfo.resize)
                        {
                            encode += "LanczosResize(" + cropInfo.resizeX.ToString() + "," + cropInfo.resizeY.ToString() + ")\r\n";
                        }
                        else
                        {
                            MessageCrop("Did not add resize command");
                        }
                    }
                    int index = comboBoxAvisynthProfile.SelectedIndex;
                    if (index > -1 && index < settings.avisynthSettings.Count)
                    {
                        string[] tmp = settings.avisynthSettings[index].commands.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string s in tmp)
                        {
                            encode += s.Trim() + "\r\n";
                        }
                    }

                    File.WriteAllText(settings.workingDir + "\\" + settings.filePrefix + "_encode.avs", encode);

                    MessageCrop("");
                    MessageCrop("Encode avs:");
                    MessageCrop("");
                    string[] tmpstr2 = encode.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in tmpstr2)
                    {
                        MessageCrop(s);
                    }
                }
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Video)
                    {
                        if (si.extraFileInfo.GetType() != typeof(VideoFileInfo))
                        {
                            si.extraFileInfo = new VideoFileInfo();
                        }
                        if (!settings.untouchedVideo)
                        {
                            ((VideoFileInfo)si.extraFileInfo).encodeAvs = settings.workingDir + "\\" + settings.filePrefix + "_encode.avs";
                        }
                        ((VideoFileInfo)si.extraFileInfo).fps = fps;
                        if (cropInfo.resize)
                        {
                            ((VideoFileInfo)si.extraFileInfo).resX = cropInfo.resizeX.ToString();
                            ((VideoFileInfo)si.extraFileInfo).resY = cropInfo.resizeY.ToString();
                        }
                        else
                        {
                            int tmp = cropInfo.cropBottom + cropInfo.cropTop;
                            int y = Convert.ToInt32(resY) - tmp;
                            ((VideoFileInfo)si.extraFileInfo).resX = resX;
                            ((VideoFileInfo)si.extraFileInfo).resY = y.ToString();
                        }
                    }
                }
                TitleInfo.SaveStreamInfoFile(demuxedStreamList, settings.workingDir + "\\" + settings.filePrefix + "_streamInfo.xml");
                UpdateDemuxedStreams();
                indexThreadStatus = true;
            }
            catch (Exception ex)
            {
                MessageCrop("Exception: " + ex.Message);
            }
            finally
            {
                
            }
        }

        private bool DoIndex()
        {
            try
            {
                this.Text = title + " [Indexing & AutoCrop...]";
                notifyIconMain.Text = this.Text;

                if (demuxedStreamList.streams.Count == 0)
                {
                    MessageMain("No demuxed streams available");
                    if (!silent) MessageBox.Show("No demuxed streams available", "Error");
                    return false;
                }


                indexThread = new Thread(IndexThread);
                indexThread.Start();

                while (indexThread.IsAlive)
                {
                    Application.DoEvents();
                    Thread.Sleep(5);
                }
                indexThread = null;
                return indexThreadStatus;
            }
            catch (Exception ex)
            {
                MessageCrop("Exception: " + ex.Message);
                return false;
            }
            finally
            {
                this.Text = title;
                notifyIconMain.Text = this.Text;
            }
        }

        private bool hasFpsValue()
        {
            try
            {
                if (demuxedStreamList.streams.Count == 0)
                {
                    return false;
                }
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Video)
                    {
                        if (si.extraFileInfo.GetType() == typeof(VideoFileInfo))
                        {
                            if (((VideoFileInfo)si.extraFileInfo).fps != "") return true;
                        }
                        else return false;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool hasAvsValue()
        {
            try
            {
                if (demuxedStreamList.streams.Count == 0)
                {
                    return false;
                }
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Video)
                    {
                        if (si.extraFileInfo.GetType() == typeof(VideoFileInfo))
                        {
                            if (((VideoFileInfo)si.extraFileInfo).encodeAvs != "") return true;
                        }
                        else return false;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool hasOutputVideoValue()
        {
            try
            {
                if (demuxedStreamList.streams.Count == 0)
                {
                    return false;
                }
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Video)
                    {
                        if (si.extraFileInfo.GetType() == typeof(VideoFileInfo))
                        {
                            if (((VideoFileInfo)si.extraFileInfo).encodedFile != "") return true;
                        }
                        else return false;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void buttonStartConvert_Click(object sender, EventArgs e)
        {
            try
            {
                if (!checkComplete()) return;
                progressBarMain.Visible = true;
                buttonAbort.Visible = true;
                tabControlMain.Enabled = false;

                if (demuxedStreamList.streams.Count == 0)
                {
                    if (!DoDemux()) return;
                    if (!DoIndex()) return;
                    if (!DoSubtitle()) return;
                    if (!settings.untouchedVideo)
                    {
                        if (!DoEncode()) return;
                    }
                    if (!DoMux()) return;
                }
                else
                {
                    if (!hasOutputVideoValue())
                    {
                        if (!hasAvsValue() || !hasFpsValue())
                        {
                            if (!DoIndex()) return;
                        }
                        if (!DoSubtitle()) return;
                        if (!DoEncode()) return;
                        if (!DoMux()) return;
                    }
                    else
                    {
                        if (!DoMux()) return;
                    }                    
                }
            }
            catch (Exception ex)
            {
                MessageMain("Exception: " + ex.Message);
            }
            finally
            {
                SaveLog(richTextBoxLogMain.Text, settings.workingDir + "\\" + settings.targetFilename + "_completeLog.txt");
                tabControlMain.Enabled = true;
                progressBarMain.Visible = false;
                buttonAbort.Visible = false;
            }
        }

        private void buttonFfmsindexPath_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "ffmsindex.exe|ffmsindex.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    textBoxFfmsindexPath.Text = ofd.FileName;
                    settings.ffmsindexPath = ofd.FileName;
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonX264Path_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "x264.exe|x264.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    textBoxX264Path.Text = ofd.FileName;
                    settings.x264Path = ofd.FileName;
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonSup2subPath_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "BDSup2Sub.jar|BDSup2Sub.jar";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    textBoxSup2subPath.Text = ofd.FileName;
                    settings.sup2subPath = ofd.FileName;
                }
            }
            catch (Exception)
            {
            }
        }
        
        private void textBoxFilePrefix_TextChanged(object sender, EventArgs e)
        {
            try
            {
                settings.filePrefix = textBoxFilePrefix.Text;
            }
            catch (Exception)
            {
            }
        }

        private void buttonJavaPath_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "java.exe|java.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    textBoxJavaPath.Text = ofd.FileName;
                    settings.javaPath = ofd.FileName;
                }
            }
            catch (Exception)
            {
            }
        }

        private void numericUpDownNrFrames_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                settings.nrFrames = (int)numericUpDownNrFrames.Value;
            }
            catch (Exception)
            {
            }
        }

        private void numericUpDownBlackValue_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                settings.blackValue = (int)numericUpDownBlackValue.Value;
            }
            catch (Exception)
            {
            }
        }

        private void buttonLoadStreamInfo_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "*_streamInfo.xml|*.xml";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    TitleInfo.LoadSettingsFile(ref demuxedStreamList, ofd.FileName);
                    UpdateDemuxedStreams();
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonClearStreamInfoList_Click(object sender, EventArgs e)
        {
            try
            {
                demuxedStreamList = new TitleInfo();
                UpdateDemuxedStreams();
            }
            catch (Exception)
            {
            }
        }
        
        private void buttonAddLanguage_Click(object sender, EventArgs e)
        {
            try
            {
                LanguageInfo li = new LanguageInfo();
                li.language = "Language";
                li.languageShort = "la";
                li.translation = "Language";
                settings.preferedLanguages.Add(li);
                UpdateLanguage();
            }
            catch (Exception)
            {
            }
        }

        private void listBoxPreferedLanguages_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                int index = listBoxPreferedLanguages.SelectedIndex;
                if (index > -1)
                {
                    LanguageForm lf = new LanguageForm(settings.preferedLanguages[index]);
                    if (lf.ShowDialog() == DialogResult.OK)
                    {
                        settings.preferedLanguages[index] = new LanguageInfo(lf.li);
                        UpdateLanguage();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonAddX264_Click(object sender, EventArgs e)
        {
            try
            {
                EncodingSettings es = new EncodingSettings("Description", "Parameter");
                settings.encodingSettings.Add(es);
                UpdateEncodingSettings();
            }
            catch (Exception)
            {
            }
        }

        private void buttonDelX264_Click(object sender, EventArgs e)
        {
            try
            {
                int index = listBoxX264Profiles.SelectedIndex;
                if (index > -1)
                {
                    settings.encodingSettings.RemoveAt(index);
                    UpdateEncodingSettings();
                    if (index - 1 < settings.encodingSettings.Count) listBoxX264Profiles.SelectedIndex = index - 1;
                }
            }
            catch (Exception)
            {
            }
        }

        private void listBoxX264Profiles_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                int index = listBoxX264Profiles.SelectedIndex;
                if (index > -1)
                {
                    EncoderSettingsForm esf = new EncoderSettingsForm(settings.encodingSettings[index]);
                    if (esf.ShowDialog() == DialogResult.OK)
                    {
                        settings.encodingSettings[index] = new EncodingSettings(esf.es);
                        UpdateEncodingSettings();
                        listBoxX264Profiles.SelectedIndex = index;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonDoIndex_Click(object sender, EventArgs e)
        {
            try
            {
                if (!checkIndex()) return;
                progressBarMain.Visible = true;
                buttonAbort.Visible = true;
                tabControlMain.Enabled = false;

                DoIndex();
            }
            catch (Exception ex)
            {
                MessageCrop("Exception: " + ex.Message);
            }
            finally
            {
                tabControlMain.Enabled = true;
                progressBarMain.Visible = false;
                buttonAbort.Visible = false;
            }
        }

        private string avisynthLink = "http://sourceforge.net/projects/avisynth2/files/";
        private string haaliLink = "http://haali.su/mkv/";
        private string eac3toLink = "http://forum.doom9.org/showthread.php?t=125966";
        private string x264Link = "http://x264.nl/";
        private string bdsup2subLink = "http://forum.doom9.org/showthread.php?t=145277";
        private string ffmpegsrcLink = "http://code.google.com/p/ffmpegsource/downloads/list";
        private string javaLink = "http://java.com/downloads/";
        private string mkvtoolnixLink = "http://www.bunkus.org/videotools/mkvtoolnix/downloads.html";
        private string filterTweakerLink = "http://www.codecguide.com/windows7_preferred_filter_tweaker.htm";
        private string anydvdLink = "http://www.slysoft.com/de/anydvdhd.html";
        private string surcodeLink = "http://www.surcode.com/";
        private string dgdecnvLink = "http://neuron2.net/dgdecnv/dgdecnv.html";
        private string x264InfoLink = "http://mewiki.project357.com/wiki/X264_Settings";
        private string avs2yuvLink = "http://akuvian.org/src/avisynth/avs2yuv/";

        private void linkLabelAviSynth_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(avisynthLink);
            }
            catch (Exception)
            {
            }
        }

        private void linkLabelHaali_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(haaliLink);
            }
            catch (Exception)
            {
            }
        }

        private void linkLabelEac3to_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(eac3toLink);
            }
            catch (Exception)
            {
            }
        }

        private void linkLabelX264_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(x264Link);
            }
            catch (Exception)
            {
            }
        }

        private void linkLabelBDSup2sub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(bdsup2subLink);
            }
            catch (Exception)
            {
            }
        }

        private void linkLabelFFMpegSrc_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(ffmpegsrcLink);
            }
            catch (Exception)
            {
            }
        }

        private void linkLabelJava_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(javaLink);
            }
            catch (Exception)
            {
            }
        }

        private void linkLabelMkvtoolnix_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(mkvtoolnixLink);
            }
            catch (Exception)
            {
            }
        }

        private void buttonMkvmergePath_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "mkvmerge.exe|mkvmerge.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    textBoxMkvmergePath.Text = ofd.FileName;
                    settings.mkvmergePath = ofd.FileName;
                }
            }
            catch (Exception)
            {
            }
        }

        private void comboBoxX264Priority_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBoxX264Priority.SelectedIndex > -1)
                {
                    settings.x264Priority = (ProcessPriorityClass)Enum.Parse(typeof(ProcessPriorityClass), comboBoxX264Priority.Text);
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonMinimize_Click(object sender, EventArgs e)
        {
            try
            {
                notifyIconMain.Visible = true;
                this.Hide();
                if (this.Text.Length < 64)
                {
                    notifyIconMain.Text = this.Text;
                }
            }
            catch (Exception)
            {
            }
        }

        private void notifyIconMain_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                this.Show();
                notifyIconMain.Visible = false;
            }
            catch (Exception)
            {
            }
        }

        private void linkLabelFilterTweaker_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(filterTweakerLink);
            }
            catch (Exception)
            {
            }
        }

        private void buttonTargetfolder_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();

                try
                {
                    if (settings.targetFolder != "")
                    {
                        DirectoryInfo di = Directory.GetParent(settings.targetFolder);
                        fbd.SelectedPath = di.FullName;
                    }
                }
                catch (Exception)
                {
                }

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    textBoxTargetFolder.Text = fbd.SelectedPath;
                    settings.targetFolder = fbd.SelectedPath;
                }
            }
            catch (Exception)
            {
            }
        }

        private void textBoxTargetfilename_TextChanged(object sender, EventArgs e)
        {
            try
            {
                settings.targetFilename = textBoxTargetfilename.Text;
            }
            catch (Exception)
            {
            }
        }

        private void textBoxMovieTitle_TextChanged(object sender, EventArgs e)
        {
            try
            {
                settings.movieTitle = textBoxMovieTitle.Text;
            }
            catch (Exception)
            {
            }
        }
        
        private void buttonStreamUp_Click(object sender, EventArgs e)
        {
            try
            {
                int index = listBoxDemuxedStreams.SelectedIndex;
                if (index > 0)
                {
                    StreamInfo si = demuxedStreamList.streams[index];
                    demuxedStreamList.streams.RemoveAt(index);
                    demuxedStreamList.streams.Insert(index - 1, si);
                    UpdateDemuxedStreams();
                    listBoxDemuxedStreams.SelectedIndex = index - 1;
                    TitleInfo.SaveStreamInfoFile(demuxedStreamList, settings.workingDir + "\\" + settings.filePrefix + "_streamInfo.xml");
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonStreamDown_Click(object sender, EventArgs e)
        {
            try
            {
                int index = listBoxDemuxedStreams.SelectedIndex;
                if (index < demuxedStreamList.streams.Count - 1)
                {
                    StreamInfo si = demuxedStreamList.streams[index];
                    demuxedStreamList.streams.RemoveAt(index);
                    demuxedStreamList.streams.Insert(index + 1, si);
                    UpdateDemuxedStreams();
                    listBoxDemuxedStreams.SelectedIndex = index + 1;
                    TitleInfo.SaveStreamInfoFile(demuxedStreamList, settings.workingDir + "\\" + settings.filePrefix + "_streamInfo.xml");
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonLangUp_Click(object sender, EventArgs e)
        {
            try
            {
                int index = listBoxPreferedLanguages.SelectedIndex;
                if (index > 0)
                {
                    LanguageInfo li = settings.preferedLanguages[index];
                    settings.preferedLanguages.RemoveAt(index);
                    settings.preferedLanguages.Insert(index - 1, li);
                    UpdateLanguage();
                    listBoxPreferedLanguages.SelectedIndex = index - 1;
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonLangDown_Click(object sender, EventArgs e)
        {
            try
            {
                int index = listBoxPreferedLanguages.SelectedIndex;
                if (index < settings.preferedLanguages.Count - 1)
                {
                    LanguageInfo li = settings.preferedLanguages[index];
                    settings.preferedLanguages.RemoveAt(index);
                    settings.preferedLanguages.Insert(index + 1, li);
                    UpdateLanguage();
                    listBoxPreferedLanguages.SelectedIndex = index + 1;
                }
            }
            catch (Exception)
            {
            }
        }

        private void checkBoxDefaultAudioTrack_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.defaultAudio = checkBoxDefaultAudioTrack.Checked;
            }
            catch (Exception)
            {
            }
        }

        private void checkBoxDefaultSubtitleTrack_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.defaultSubtitle = checkBoxDefaultSubtitleTrack.Checked;
                if (settings.defaultSubtitle)
                {
                    checkBoxDefaultSubtitleForced.Enabled = true;
                }
                else
                {
                    checkBoxDefaultSubtitleForced.Enabled = false;
                }
            }
            catch (Exception)
            {
            }
        }

        private void checkBoxDefaultSubtitleForced_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.defaultSubtitleForced = checkBoxDefaultSubtitleForced.Checked;
            }
            catch (Exception)
            {
            }
        }

        private void comboBoxEncodeProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                settings.lastProfile = comboBoxEncodeProfile.SelectedIndex;
            }
            catch (Exception)
            {
            }
        }

        private void checkBoxDeleteAfterEncode_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.deleteAfterEncode = checkBoxDeleteAfterEncode.Checked;
            }
            catch (Exception)
            {
            }
        }

        private void linkLabelAnyDvd_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(anydvdLink);
            }
            catch (Exception)
            {
            }
        }

        private void comboBoxCropMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBoxCropMode.SelectedIndex > -1)
                {
                    settings.cropMode = comboBoxCropMode.SelectedIndex;
                }
            }
            catch (Exception)
            {
            }
        }

        private void clearLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (tabControlLog.SelectedTab == tabPageMainLog)
                {
                    richTextBoxLogMain.Clear();
                }
                else if (tabControlLog.SelectedTab == tabPageDemuxLog)
                {
                    richTextBoxLogDemux.Clear();
                }
                if (tabControlLog.SelectedTab == tabPageCropLog)
                {
                    richTextBoxLogCrop.Clear();
                }
                if (tabControlLog.SelectedTab == tabPageSubtitleLog)
                {
                    richTextBoxLogSubtitle.Clear();
                }
                if (tabControlLog.SelectedTab == tabPageEncodeLog)
                {
                    richTextBoxLogEncode.Clear();
                }
                if (tabControlLog.SelectedTab == tabPageMuxLog)
                {
                    richTextBoxLogMux.Clear();
                }
            }
            catch (Exception)
            {
            }
        }

        private void clearAllLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                richTextBoxLogMain.Clear();
                richTextBoxLogDemux.Clear();
                richTextBoxLogCrop.Clear();
                richTextBoxLogSubtitle.Clear();
                richTextBoxLogEncode.Clear();
                richTextBoxLogMux.Clear();
            }
            catch (Exception)
            {
            }
        }

        private void checkBoxUseCore_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.dtsHdCore = checkBoxUseCore.Checked;
            }
            catch (Exception)
            {
            }
        }        

        private void checkBoxUntouchedVideo_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.untouchedVideo = checkBoxUntouchedVideo.Checked;
                if (settings.untouchedVideo)
                {
                    checkBoxResize720p.Checked = false;
                    checkBoxResize720p.Enabled = false;
                }
                else
                {
                    checkBoxResize720p.Enabled = true;
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonAddAvisynth_Click(object sender, EventArgs e)
        {
            try
            {
                AvisynthSettings avs = new AvisynthSettings("Empty", "");
                settings.avisynthSettings.Add(avs);
                UpdateAvisynthSettings();
            }
            catch (Exception)
            {
            }
        }

        private void buttonDelAvisynth_Click(object sender, EventArgs e)
        {
            try
            {
                int index = listBoxAviSynthProfiles.SelectedIndex;
                if (index > -1)
                {
                    settings.avisynthSettings.RemoveAt(index);
                    UpdateAvisynthSettings();
                }
            }
            catch (Exception)
            {
            }
        }

        private void listBoxAviSynthProfiles_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                int index = listBoxAviSynthProfiles.SelectedIndex;
                if (index > -1)
                {
                    AvisynthSettingsForm avsf = new AvisynthSettingsForm(settings.avisynthSettings[index]);
                    if (avsf.ShowDialog() == DialogResult.OK)
                    {
                        settings.avisynthSettings[index] = new AvisynthSettings(avsf.avs);
                        UpdateAvisynthSettings();
                        listBoxAviSynthProfiles.SelectedIndex = index;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void checkBoxResize720p_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.resize720p = checkBoxResize720p.Checked;
            }
            catch (Exception)
            {
            }
        }

        private bool checkEac3to()
        {
            try
            {
                if (!File.Exists(settings.eac3toPath))
                {
                    MessageMain("eac3to path not set");
                    if(!silent) MessageBox.Show("eac3to path not set", "Error");
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool checkIndex()
        {
            try
            {
                if (settings.cropInput == 1 || settings.encodeInput == 1)
                {
                    if (!File.Exists(settings.ffmsindexPath))
                    {
                        MessageMain("ffmsindex path not set");
                        if (!silent) MessageBox.Show("ffmsindex path not set", "Error");
                        return false;
                    }
                }
                else if (settings.cropInput == 2 || settings.encodeInput == 2)
                {
                    if (!File.Exists(settings.dgindexnvPath))
                    {
                        MessageMain("DGIndexNv path not set");
                        if (!silent) MessageBox.Show("DGIndexNv path not set", "Error");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool checkBdsup2sub()
        {
            try
            {
                if (!File.Exists(settings.javaPath))
                {
                    MessageMain("java path not set");
                    if (!silent) MessageBox.Show("java path not set", "Error");
                    return false;
                }
                if (!File.Exists(settings.sup2subPath))
                {
                    MessageMain("BDsup2sub path not set");
                    if (!silent) MessageBox.Show("BDsup2sub path not set", "Error");
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool checkX264()
        {
            try
            {
                if (settings.use64bit)
                {
                    if (!File.Exists(settings.x264x64Path))
                    {
                        MessageMain("x264 64 bit path not set");
                        if (!silent) MessageBox.Show("x264 64 bit path not set", "Error");
                        return false;
                    }
                    if (!File.Exists(settings.avs2yuvPath))
                    {
                        MessageMain("avs2yuv path not set");
                        if (!silent) MessageBox.Show("avs2yuv path not set", "Error");
                        return false;
                    }
                }
                else
                {
                    if (!File.Exists(settings.x264Path))
                    {
                        MessageMain("x264 path not set");
                        if (!silent) MessageBox.Show("x264 path not set", "Error");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool checkMkvmerge()
        {
            try
            {
                if (!File.Exists(settings.mkvmergePath))
                {
                    MessageMain("mkvmerge path not set");
                    if (!silent) MessageBox.Show("mkvmerge path not set", "Error");
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool checkComplete()
        {
            try
            {
                if (!checkEac3to()) return false;
                int sup = 0;
                if (comboBoxTitle.SelectedIndex > -1)
                {
                    foreach (StreamInfo si in titleList[comboBoxTitle.SelectedIndex].streams)
                    {
                        if (si.streamType == StreamType.Subtitle)
                        {
                            sup++;
                        }
                    }
                }
                if (!checkIndex()) return false;
                if (sup > 0)
                {
                    if (!checkBdsup2sub()) return false;
                }
                if (!settings.untouchedVideo)
                {
                    if (!checkX264()) return false;
                }
                if (!checkMkvmerge()) return false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void checkBoxDownmixDts_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.downmixDTS = checkBoxDownmixDts.Checked;
                if (settings.downmixDTS)
                {
                    comboBoxDownmixDts.Enabled = true;
                }
                else
                {
                    comboBoxDownmixDts.Enabled = false;
                }
            }
            catch (Exception)
            {
            }
        }

        private void checkBoxDownmixAc3_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.downmixAc3 = checkBoxDownmixAc3.Checked;
                if (settings.downmixAc3)
                {
                    comboBoxDownmixAc3.Enabled = true;
                }
                else
                {
                    comboBoxDownmixAc3.Enabled = false;
                }
            }
            catch (Exception)
            {
            }
        }

        private void comboBoxDownmixDts_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBoxDownmixDts.SelectedIndex > -1) settings.downmixDTSIndex = comboBoxDownmixDts.SelectedIndex;
            }
            catch (Exception)
            {
            }
        }

        private void comboBoxDownmixAc3_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBoxDownmixAc3.SelectedIndex > -1) settings.downmixAc3Index = comboBoxDownmixAc3.SelectedIndex;
            }
            catch (Exception)
            {
            }
        }

        private void linkLabelSurcode_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(surcodeLink);
            }
            catch (Exception)
            {
            }
        }

        private void SaveLog(string log, string filename)
        {
            try
            {
                string[] lines = log.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                string tmp = "";
                foreach (string s in lines) tmp += s.Trim() + "\r\n";
                File.WriteAllText(filename, tmp);
            }
            catch (Exception)
            {
            }
        }

        private void SaveLog(string log)
        {
            try
            {
                string[] lines = log.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                string tmp = "";
                foreach(string s in lines) tmp += s.Trim() + "\r\n";
                SaveFileDialog sfd = new SaveFileDialog();                
                sfd.Filter = "Log file|*.txt";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, tmp);
                }
            }
            catch (Exception)
            {
            }
        }

        private void saveLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (tabControlLog.SelectedTab == tabPageMainLog)
                {   
                    SaveLog(richTextBoxLogMain.Text);
                }
                else if (tabControlLog.SelectedTab == tabPageDemuxLog)
                {
                    SaveLog(richTextBoxLogDemux.Text);
                }
                if (tabControlLog.SelectedTab == tabPageCropLog)
                {
                    SaveLog(richTextBoxLogCrop.Text);
                }
                if (tabControlLog.SelectedTab == tabPageSubtitleLog)
                {
                    SaveLog(richTextBoxLogSubtitle.Text);
                }
                if (tabControlLog.SelectedTab == tabPageEncodeLog)
                {
                    SaveLog(richTextBoxLogEncode.Text);
                }
                if (tabControlLog.SelectedTab == tabPageMuxLog)
                {
                    SaveLog(richTextBoxLogMux.Text);
                }
            }
            catch (Exception)
            {
            }
        }

        private void checkBoxMinimizeCrop_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.minimizeAutocrop = checkBoxMinimizeCrop.Checked;
            }
            catch (Exception)
            {
            }
        }

        private void buttonSaveProject_Click(object sender, EventArgs e)
        {
            try
            {
                if (demuxedStreamList.streams.Count == 0 && titleList.Count == 0)
                {
                    if (MessageBox.Show("Demuxed stream list/title list empty - continue anyway?", "Stream list empty", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return;
                    }
                }

                Project project = new Project(settings, demuxedStreamList, titleList, comboBoxTitle.SelectedIndex, m2tsList);
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "BluRip project (*.brp)|*.brp";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    Project.SaveProjectFile(project, sfd.FileName);
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonLoadProject_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "BluRip project (*.brp)|*.brp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Project project = new Project();
                    if (Project.LoadProjectFile(ref project, ofd.FileName))
                    {
                        settings = new UserSettings(project.settings);                        
                        titleList.Clear();
                        foreach (TitleInfo ti in project.titleList)
                        {
                            titleList.Add(new TitleInfo(ti));
                        }                        
                        UpdateTitleList();
                        comboBoxTitle.SelectedIndex = project.titleIndex;
                        demuxedStreamList = new TitleInfo(project.demuxedStreamList);

                        m2tsList.Clear();
                        foreach (string s in project.m2tsList)
                        {
                            m2tsList.Add(s);
                        }

                        UpdateFromSettings();
                        UpdateDemuxedStreams();                        
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private List<Project> projectQueue = new List<Project>();

        private void UpdateQueue()
        {
            try
            {
                listBoxQueue.Items.Clear();
                foreach (Project p in projectQueue)
                {
                    listBoxQueue.Items.Add("Title: " + p.settings.movieTitle);
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonQueueCurrent_Click(object sender, EventArgs e)
        {
            try
            {
                Project p = new Project(settings, demuxedStreamList, titleList, comboBoxTitle.SelectedIndex, m2tsList);
                projectQueue.Add(p);
                UpdateQueue();
            }
            catch (Exception)
            {
            }
        }

        private void buttonQueueDel_Click(object sender, EventArgs e)
        {
            try
            {
                int index = listBoxQueue.SelectedIndex;
                if (index > -1)
                {
                    projectQueue.RemoveAt(index);
                    UpdateQueue();
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonQueueUp_Click(object sender, EventArgs e)
        {
            try
            {
                int index = listBoxQueue.SelectedIndex;
                if (index > 0)
                {
                    Project p = projectQueue[index];
                    projectQueue.RemoveAt(index);
                    projectQueue.Insert(index - 1, p);
                    UpdateQueue();
                    listBoxQueue.SelectedIndex = index - 1;
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonQueueDown_Click(object sender, EventArgs e)
        {
            try
            {
                int index = listBoxQueue.SelectedIndex;
                if (index > -1 && index < projectQueue.Count - 1)
                {
                    Project p = projectQueue[index];
                    projectQueue.RemoveAt(index);
                    projectQueue.Insert(index + 1, p);
                    UpdateQueue();
                    listBoxQueue.SelectedIndex = index + 1;
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonQueueExisting_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "BluRip project (*.brp)|*.brp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Project project = new Project();
                    if (Project.LoadProjectFile(ref project, ofd.FileName))
                    {
                        projectQueue.Add(project);
                        UpdateQueue();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private bool silent = false;
        private bool abort = false;

        private void buttonProcessQueue_Click(object sender, EventArgs e)
        {
            try
            {
                silent = true;
                abort = false;
                
                foreach (Project project in projectQueue)
                {
                    richTextBoxLogCrop.Clear();
                    richTextBoxLogDemux.Clear();
                    richTextBoxLogEncode.Clear();
                    richTextBoxLogMain.Clear();
                    richTextBoxLogMux.Clear();
                    richTextBoxLogSubtitle.Clear();

                    MessageMain("Starting to process job " + (projectQueue.IndexOf(project) + 1).ToString() + "/" + projectQueue.Count.ToString());

                    if (!abort)
                    {
                        MessageMain("Processing project " + project.settings.movieTitle);
                        settings = new UserSettings(project.settings);
                        titleList.Clear();
                        foreach (TitleInfo ti in project.titleList)
                        {
                            titleList.Add(new TitleInfo(ti));
                        }
                        UpdateTitleList();
                        comboBoxTitle.SelectedIndex = project.titleIndex;
                        demuxedStreamList = new TitleInfo(project.demuxedStreamList);

                        m2tsList.Clear();
                        foreach (string s in project.m2tsList)
                        {
                            m2tsList.Add(s);
                        }

                        UpdateFromSettings();
                        UpdateDemuxedStreams();

                        buttonStartConvert_Click(null, null);
                    }
                    else
                    {
                        MessageMain("Queue canceled");
                    }
                    MessageMain("Job done.");
                }
                if (checkBoxShutDown.Checked)
                {
                    System.Diagnostics.Process.Start("ShutDown", "-s -f");
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                silent = false;
            }
        }

        private void comboBoxCropInput_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                settings.cropInput = comboBoxCropInput.SelectedIndex;
            }
            catch (Exception)
            {
            }
        }

        private void comboBoxEncodeInput_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                settings.encodeInput = comboBoxEncodeInput.SelectedIndex;
            }
            catch (Exception)
            {
            }
        }

        private void checkBoxUntouchedAudio_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.untouchedAudio = checkBoxUntouchedAudio.Checked;
                if (settings.untouchedAudio)
                {
                    checkBoxDownmixAc3.Checked = false;
                    checkBoxDownmixDts.Checked = false;
                    checkBoxUseCore.Checked = false;
                    checkBoxDtsToAc3.Checked = false;

                    checkBoxUseCore.Enabled = false;
                    checkBoxDownmixAc3.Enabled = false;
                    checkBoxDownmixDts.Enabled = false;
                    checkBoxDtsToAc3.Enabled = false;
                }
                else
                {
                    checkBoxUseCore.Enabled = true;
                    checkBoxDownmixAc3.Enabled = true;
                    checkBoxDownmixDts.Enabled = true;
                    checkBoxDtsToAc3.Enabled = true;
                    checkBoxDownmixDts_CheckedChanged(null, null);
                    checkBoxDownmixAc3_CheckedChanged(null, null);
                }
            }
            catch (Exception)
            {
            }
        }

        private void comboBoxMuxSubs_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                settings.muxSubs = comboBoxMuxSubs.SelectedIndex;
            }
            catch (Exception)
            {
            }
        }

        private void comboBoxCopySubs_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                settings.copySubs = comboBoxCopySubs.SelectedIndex;
            }
            catch (Exception)
            {
            }
        }

        private void linkLabelDGDecNv_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(dgdecnvLink);
            }
            catch (Exception)
            {
            }
        }

        private void buttonDgindexnvPath_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "DGIndexNv.exe|DGIndexNv.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    textBoxDgindexnvPath.Text = ofd.FileName;
                    settings.dgindexnvPath = ofd.FileName;
                }
            }
            catch (Exception)
            {
            }
        }

        private void checkBoxDtsToAc3_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.convertDtsToAc3 = checkBoxDtsToAc3.Checked;
            }
            catch (Exception)
            {
            }
        }

        private void checkedListBoxStreams_SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                titleList[comboBoxTitle.SelectedIndex].streams[checkedListBoxStreams.SelectedIndex].selected =
                    checkedListBoxStreams.GetItemChecked(checkedListBoxStreams.SelectedIndex);
            }
            catch (Exception)
            {
            }
        }

        private void checkedListBoxStreams_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                titleList[comboBoxTitle.SelectedIndex].streams[checkedListBoxStreams.SelectedIndex].selected =
                    checkedListBoxStreams.GetItemChecked(checkedListBoxStreams.SelectedIndex);
            }
            catch (Exception)
            {
            }
        }

        private void toolStripMenuItemDeleteAdvancedOptions_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBoxTitle.SelectedIndex > -1 && checkedListBoxStreams.SelectedIndex > -1)
                {
                    int index = checkedListBoxStreams.SelectedIndex;
                    titleList[comboBoxTitle.SelectedIndex].streams[index].advancedOptions = new AdvancedOptions();
                    UpdateStreamList();
                    checkedListBoxStreams.SelectedIndex = index;
                }
            }
            catch (Exception)
            {
            }
        }

        private void toolStripMenuItemAdvancedOptions_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBoxTitle.SelectedIndex > -1 && checkedListBoxStreams.SelectedIndex > -1)
                {
                    StreamInfo si = titleList[comboBoxTitle.SelectedIndex].streams[checkedListBoxStreams.SelectedIndex];
                    if (si.streamType == StreamType.Audio)
                    {
                        AdvancedAudioOptions aao = null;
                        if (si.advancedOptions.GetType() != typeof(AdvancedAudioOptions))
                        {
                            aao = new AdvancedAudioOptions();
                        }
                        else
                        {
                            aao = new AdvancedAudioOptions(si.advancedOptions);
                        }
                        AdvancedAudioOptionsEdit aaoe = new AdvancedAudioOptionsEdit(aao);
                        if (aaoe.ShowDialog() == DialogResult.OK)
                        {
                            int index = checkedListBoxStreams.SelectedIndex;
                            titleList[comboBoxTitle.SelectedIndex].streams[index].advancedOptions =
                                new AdvancedAudioOptions(aaoe.ao);
                            UpdateStreamList();
                            checkedListBoxStreams.SelectedIndex = index;
                        }
                    }
                    else if (si.streamType == StreamType.Video)
                    {
                        AdvancedVideoOptions avo = null;
                        if (si.advancedOptions.GetType() != typeof(AdvancedVideoOptions))
                        {
                            avo = new AdvancedVideoOptions();
                        }
                        else
                        {
                            avo = new AdvancedVideoOptions(si.advancedOptions);
                        }
                        AdvancedVideoOptionsEdit avoe = new AdvancedVideoOptionsEdit(avo);
                        if (avoe.ShowDialog() == DialogResult.OK)
                        {
                            int index = checkedListBoxStreams.SelectedIndex;
                            titleList[comboBoxTitle.SelectedIndex].streams[index].advancedOptions =
                                new AdvancedVideoOptions(avoe.ao);
                            UpdateStreamList();
                            checkedListBoxStreams.SelectedIndex = index;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonX264x64Path_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "x264.exe|x264.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    textBoxX264x64Path.Text = ofd.FileName;
                    settings.x264x64Path = ofd.FileName;
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonAvs2yuvPath_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "avs2yuv.exe|avs2yuv.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    textBoxAvs2yuvPath.Text = ofd.FileName;
                    settings.avs2yuvPath= ofd.FileName;
                }
            }
            catch (Exception)
            {
            }
        }

        private void checkBoxUse64bit_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.use64bit = checkBoxUse64bit.Checked;
            }
            catch (Exception)
            {
            }
        }

        private void linkLabelx264Info_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(x264InfoLink);
            }
            catch (Exception)
            {
            }
        }

        private void linkLabelavs2yuv_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(avs2yuvLink);
            }
            catch (Exception)
            {
            }
        }

        
    }
}
