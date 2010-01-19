﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using MediaInfoWrapper;

namespace BluRip
{
    public partial class MainForm : Form
    {
        private UserSettings settings = new UserSettings();
        private string settingsPath = "";
        private List<TitleInfo> titleList = new List<TitleInfo>();

        private List<string> videoTypes = new List<string>();
        private List<string> ac3AudioTypes = new List<string>();
        private List<string> dtsAudioTypes = new List<string>();

        private Thread titleInfoThread = null;
        private Thread demuxThread = null;
        private Thread indexThread = null;
        private Thread encodeThread = null;
        private Thread subtitleThread = null;
        private Thread muxThread = null;

        private Process pc = new Process();
        private Process pc2 = new Process();

        private string title = "BluRip 1080p v0.3.6 © _hawk_/PPX";

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

                dtsAudioTypes.Add("DTS");
                dtsAudioTypes.Add("DTS Master Audio");
                dtsAudioTypes.Add("DTS Express");

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

        public delegate void MsgHandler(string msg);

        private void Message(string msg)
        {
            try
            {
                if (richTextBoxLog.Disposing) return;
                if (richTextBoxLog.IsDisposed) return;
                if (this.richTextBoxLog.InvokeRequired)
                {
                    MsgHandler mh = new MsgHandler(Message);
                    this.Invoke(mh, new object[] { msg });
                }
                else
                {
                    richTextBoxLog.AppendText("[" + DateTime.Now.ToString() + "] " + msg + "\n");
                    richTextBoxLog.ScrollToCaret();
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
        void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    sb.Append(e.Data + "\r\n");
                    Message(e.Data.Replace("\b","").Trim());
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
                    Message(e.Data.Replace("\b", "").Trim());
                }
            }
            catch (Exception)
            {
            }
        }

        private void GetSubStreamInfo(string path, string streamNumber, List<TitleInfo> result)
        {
            try
            {
                Message("");
                Message("Getting title info...");
                Message("");
                sb.Remove(0, sb.Length);
                pc2 = new Process();
                pc2.StartInfo.FileName = settings.eac3toPath;
                pc2.StartInfo.Arguments = "\"" + path + "\" " + streamNumber + ")";
                
                pc2.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                pc2.ErrorDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                pc2.StartInfo.UseShellExecute = false;
                pc2.StartInfo.CreateNoWindow = true;
                pc2.StartInfo.RedirectStandardError = true;
                pc2.StartInfo.RedirectStandardOutput = true;

                Message("Command: " + pc2.StartInfo.FileName + pc2.StartInfo.Arguments);

                if (!pc2.Start())
                {
                    Message("Error starting eac3to.exe");
                    return;
                }

                string res = "";                
                pc2.BeginOutputReadLine();
                pc2.BeginErrorReadLine();
                pc2.WaitForExit();
                pc2.Close();
                res = sb.ToString();
                res = res.Replace("\b", "");

                string[] tmp = res.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = tmp[i].Trim();
                }

                if (res.Trim() == "")
                {
                    Message("Failed to get stream infos");
                    return;
                }

                TitleInfo ti = new TitleInfo();

                if (tmp[0][0] == '-')
                {
                    int length = 0;
                    for (int i = 0; i < tmp[0].Length; i++)
                    {
                        if (tmp[0][i] == '-')
                        {
                            length++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    tmp[0] = tmp[0].Substring(length, tmp[0].Length - length);
                    tmp[0] = tmp[0].Trim();
                    ti.desc = tmp[0];
                }

                for (int i = 0; i < tmp.Length; i++)
                {
                    if (Regex.IsMatch(tmp[i], "^[0-9].*:"))
                    {
                        StreamInfo sr = new StreamInfo();
                        sr.desc = tmp[i];
                        if (i < tmp.Length - 1)
                        {
                            if (!Regex.IsMatch(tmp[i + 1], "^[0-9].*:"))
                            {
                                sr.addInfo = tmp[i + 1];                                

                            }
                        }

                        int pos = tmp[i].IndexOf(':');
                        string substr = tmp[i].Substring(0, pos);
                        sr.number = Convert.ToInt32(substr);

                        substr = tmp[i].Substring(pos + 1).Trim();
                        string[] tmpInfo = substr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tmpInfo.Length > 0)
                        {
                            sr.typeDesc = tmpInfo[0];
                            if (tmpInfo[0] == "Chapters")
                            {
                                sr.streamType = StreamType.Chapter;
                            }
                            else if (videoTypes.Contains(tmpInfo[0]))
                            {
                                sr.streamType = StreamType.Video;
                            }
                            else if (ac3AudioTypes.Contains(tmpInfo[0]))
                            {
                                sr.streamType = StreamType.Audio;
                                if (tmpInfo.Length > 1)
                                {
                                    sr.language = tmpInfo[1].Trim();
                                }
                            }
                            else if (dtsAudioTypes.Contains(tmpInfo[0]))
                            {
                                sr.streamType = StreamType.Audio;
                                if (tmpInfo.Length > 1)
                                {
                                    sr.language = tmpInfo[1].Trim();
                                }
                            }
                            else if (tmpInfo[0] == "Subtitle (PGS)")
                            {
                                sr.streamType = StreamType.Subtitle;
                                if (tmpInfo.Length > 1)
                                {
                                    sr.language = tmpInfo[1].Trim();
                                }
                            }
                            else
                            {
                                sr.streamType = StreamType.Unknown;
                            }
                        }
                        else
                        {
                            sr.typeDesc = "Unknown";
                            sr.streamType = StreamType.Unknown;
                        }

                        ti.streams.Add(sr);
                    }
                }

                result.Add(ti);
            }
            catch (Exception)
            {
            }
        }

        private bool GetStreamInfo(string path, List<TitleInfo> result)
        {
            try
            {
                sb.Remove(0, sb.Length);
                result.Clear();
                Message("Getting playlist info...");
                Message("");
                pc = new Process();
                pc.StartInfo.FileName = settings.eac3toPath;
                pc.StartInfo.Arguments = "\"" + path + "\"";
                
                pc.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                pc.ErrorDataReceived += new DataReceivedEventHandler(OutputDataReceived);

                pc.StartInfo.UseShellExecute = false;
                pc.StartInfo.CreateNoWindow = true;
                pc.StartInfo.RedirectStandardError = true;
                pc.StartInfo.RedirectStandardOutput = true;

                Message("Command: " + pc.StartInfo.FileName + pc.StartInfo.Arguments);

                if (!pc.Start())
                {
                    Message("Error starting eac3to.exe");
                    return false;
                }

                pc.BeginOutputReadLine();
                pc.BeginErrorReadLine();
                pc.WaitForExit();
                pc.Close();

                Message("");
                Message("Done.");

                string res = sb.ToString();
                
                res = res.Replace("\b", "");

                string[] tmp = res.Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = tmp[i].Trim();
                }
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (Regex.IsMatch(tmp[i], "^[0-9].*\\)"))
                    {
                        string[] tmp2 = tmp[i].Split(new char[] { ')' }, StringSplitOptions.RemoveEmptyEntries);
                        GetSubStreamInfo(path, tmp2[0], result);
                    }
                }
                
                return true;
            }
            catch (Exception)
            {
                Message("Error parsing reply");
                return false;
            }
        }

        private void TitleInfoThread()
        {
            try
            {
                GetStreamInfo(textBoxPath.Text, titleList);
            }
            catch (Exception)
            {
            }
        }

        private void buttonGetStreamInfo_Click(object sender, EventArgs e)
        {
            try
            {
                this.Text = title + " [Getting stream info...]";

                progressBarMain.Visible = true;
                buttonAbort.Visible = true;

                richTextBoxLog.Clear();
                comboBoxTitle.Items.Clear();
                listBoxStreams.Items.Clear();

                titleInfoThread = new Thread(TitleInfoThread);
                titleInfoThread.Start();

                while (titleInfoThread.IsAlive)
                {
                    Application.DoEvents();
                    Thread.Sleep(5);
                }
                titleInfoThread = null;

                foreach (TitleInfo ti in titleList)
                {
                    comboBoxTitle.Items.Add(ti.desc);
                }
                if (titleList.Count > 0)
                {
                    comboBoxTitle.SelectedIndex = 0;
                }

                demuxedStreamList = new TitleInfo();
                UpdateDemuxedStreams();
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
                if (titleInfoThread != null) titleInfoThread = null;                
            }
            finally
            {
                progressBarMain.Visible = false;
                buttonAbort.Visible = false;
                this.Text = title;
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
                foreach (LanguagInfo li in settings.preferedLanguages)
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

                checkBoxCropDirectshow.Checked = settings.cropDirectshow;
                checkBoxEncodeDirectshow.Checked = settings.encodeDirectshow;
                comboBoxCropMode.SelectedIndex = settings.cropMode;

                comboBoxX264Priority.SelectedItem = Enum.GetName(typeof(ProcessPriorityClass),settings.x264Priority);

                textBoxTargetFolder.Text = settings.targetFolder;
                textBoxTargetfilename.Text = settings.targetFilename;
                textBoxMovieTitle.Text = settings.movieTitle;

                checkBoxDefaultAudioTrack.Checked = settings.defaultAudio;
                checkBoxDefaultSubtitleForced.Checked = settings.defaultSubtitleForced;
                checkBoxDefaultSubtitleTrack.Checked = settings.defaultSubtitle;
                checkBoxDeleteAfterEncode.Checked = settings.deleteAfterEncode;

                richTextBoxCommandsAfterResize.Clear();
                string[] tmp = settings.commandsAfterResize.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in tmp)
                {
                    richTextBoxCommandsAfterResize.Text += s.Trim() + "\r\n";
                }

                UpdateLanguage();
                UpdateEncodingSettings();
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
                    listBoxStreams.Items.Clear();
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
            foreach (LanguagInfo li in settings.preferedLanguages)
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
                listBoxStreams.BeginUpdate();
                int maxLength = 0;
                foreach (StreamInfo si in titleList[comboBoxTitle.SelectedIndex].streams)
                {                    
                    maxLength = Math.Max(maxLength, StreamTypeToString(si.streamType).Length);
                }

                foreach (StreamInfo si in titleList[comboBoxTitle.SelectedIndex].streams)
                {                    
                    string desc = "[ " + si.number.ToString("d3") + " ] - [ " + StreamTypeToString(si.streamType);
                    for (int i = 0; i < maxLength - StreamTypeToString(si.streamType).Length; i++) desc += " ";
                    desc += " ] - (" + si.desc + ")";
                    if (si.addInfo != "")
                    {
                        desc += " - (" + si.addInfo + ")";
                    }
                    listBoxStreams.Items.Add(desc);
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
                                            ac3List[index]++;
                                            si.selected = true;
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
                        
                        listBoxStreams.SetSelected(titleList[comboBoxTitle.SelectedIndex].streams.IndexOf(si),si.selected);
                        
                    }
                }
                listBoxStreams.EndUpdate();
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

        private void buttonAbort_Click(object sender, EventArgs e)
        {
            try
            {
                if (titleInfoThread != null)
                {
                    titleInfoThread.Abort();
                    titleInfoThread = null;
                }
                if (demuxThread != null)
                {
                    demuxThread.Abort();
                    demuxThread = null;
                }
                if (indexThread != null)
                {
                    indexThread.Abort();
                    indexThread = null;
                }
                if (subtitleThread != null)
                {
                    subtitleThread.Abort();
                    subtitleThread = null;
                }
                if (encodeThread != null)
                {
                    encodeThread.Abort();
                    encodeThread = null;
                }
                if (muxThread != null)
                {
                    muxThread.Abort();
                    muxThread = null;
                }

                try
                {
                    if (!pc.HasExited)
                    {
                        pc.Kill();
                    }
                }
                catch (Exception)
                {
                }

                try
                {
                    if (!pc2.HasExited)
                    {
                        pc2.Kill();
                    }
                }
                catch (Exception)
                {
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

        private void listBoxStreams_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                for (int i = 0; i < titleList[comboBoxTitle.SelectedIndex].streams.Count; i++)
                {
                    if (listBoxStreams.SelectedIndices.Contains(i)) titleList[comboBoxTitle.SelectedIndex].streams[i].selected = true;
                    else titleList[comboBoxTitle.SelectedIndex].streams[i].selected = false;
                }
            }
            catch (Exception)
            {
            }
        }

        private bool demuxThreadStatus = false;
        private void DemuxThread()
        {
            try
            {
                demuxThreadStatus = false;
                sb.Remove(0, sb.Length);                
                Message("Starting to demux...");
                Message("");
                pc = new Process();
                pc.StartInfo.FileName = settings.eac3toPath;
                pc.StartInfo.Arguments = "\"" + settings.lastBluRayPath + "\" ";

                string prefix = textBoxFilePrefix.Text;

                foreach (StreamInfo si in titleList[comboBoxTitle.SelectedIndex].streams)
                {
                    if (si.selected && si.streamType != StreamType.Unknown)
                    {
                        pc.StartInfo.Arguments += si.number.ToString() + ": \"" + settings.workingDir + "\\" + prefix + "_" + si.number.ToString("d3") + "_";
                        if (si.streamType == StreamType.Chapter)
                        {
                            pc.StartInfo.Arguments += "chapter.txt\" ";
                            si.filename = settings.workingDir + "\\" + prefix + "_" + si.number.ToString("d3") + "_chapter.txt";
                        }
                        else if (si.streamType == StreamType.Audio)
                        {
                            if (ac3AudioTypes.Contains(si.typeDesc))
                            {
                                pc.StartInfo.Arguments += "audio_ac3_" + si.language + ".ac3\" ";
                                si.filename = settings.workingDir + "\\" + prefix + "_" + si.number.ToString("d3") + "_audio_ac3_" + si.language + ".ac3";
                            }
                            else if (dtsAudioTypes.Contains(si.typeDesc))
                            {
                                pc.StartInfo.Arguments += "audio_dts_" + si.language + ".dts\" ";
                                si.filename = settings.workingDir + "\\" + prefix + "_" + si.number.ToString("d3") + "_audio_dts_" + si.language + ".dts";
                            }
                        }
                        else if (si.streamType == StreamType.Video)
                        {
                            pc.StartInfo.Arguments += "video.mkv\" ";
                            si.filename = settings.workingDir + "\\" + prefix + "_" + si.number.ToString("d3") + "_video.mkv";
                        }
                        else if (si.streamType == StreamType.Subtitle)
                        {
                            pc.StartInfo.Arguments += "subtitle_"+ si.language +".sup\" ";
                            si.filename = settings.workingDir + "\\" + prefix + "_" + si.number.ToString("d3") + "_subtitle_" + si.language + ".sup";
                        }
                    }
                }
                Message("Command: " + pc.StartInfo.FileName + pc.StartInfo.Arguments);
                pc.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                //pc.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

                pc.StartInfo.UseShellExecute = false;
                pc.StartInfo.CreateNoWindow = true;
                pc.StartInfo.RedirectStandardError = true;
                pc.StartInfo.RedirectStandardOutput = true;

                if (!pc.Start())
                {
                    Message("Error starting eac3to.exe");
                    return;
                }

                //pc.BeginErrorReadLine();
                pc.BeginOutputReadLine();
                
                pc.WaitForExit();
                pc.Close();

                demuxedStreamList = new TitleInfo();
                demuxedStreamList.desc = titleList[comboBoxTitle.SelectedIndex].desc;

                foreach (StreamInfo si in titleList[comboBoxTitle.SelectedIndex].streams)
                {
                    if (si.selected)
                    {
                        demuxedStreamList.streams.Add(new StreamInfo(si));
                    }
                }
                TitleInfo.SaveStreamInfoFile(demuxedStreamList, settings.workingDir + "\\" + prefix + "_streamInfo.xml");
                UpdateDemuxedStreams();
                Message("");
                Message("Done.");

                // sort streamlist
                TitleInfo tmpList = new TitleInfo();
                tmpList.desc = demuxedStreamList.desc;

                // chapter first
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Chapter)
                    {
                        tmpList.streams.Add(new StreamInfo(si));
                    }
                }
                // video
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Video)
                    {
                        tmpList.streams.Add(new StreamInfo(si));
                    }
                }
                // audio
                foreach (LanguagInfo li in settings.preferedLanguages)
                {
                    foreach (StreamInfo si in demuxedStreamList.streams)
                    {
                        if (si.streamType == StreamType.Audio)
                        {
                            if (si.language == li.language)
                            {
                                tmpList.streams.Add(new StreamInfo(si));
                            }
                        }
                    }
                }
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Audio)
                    {
                        if (!HasLanguage(si.language))
                        {
                            tmpList.streams.Add(new StreamInfo(si));
                        }
                    }
                }
                // subtitle
                foreach (LanguagInfo li in settings.preferedLanguages)
                {
                    foreach (StreamInfo si in demuxedStreamList.streams)
                    {
                        if (si.streamType == StreamType.Subtitle)
                        {
                            if (si.language == li.language)
                            {
                                tmpList.streams.Add(new StreamInfo(si));
                            }
                        }
                    }
                }
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Subtitle)
                    {
                        if (!HasLanguage(si.language))
                        {
                            tmpList.streams.Add(new StreamInfo(si));
                        }
                    }
                }
                demuxedStreamList = tmpList;
                TitleInfo.SaveStreamInfoFile(demuxedStreamList, settings.workingDir + "\\" + prefix + "_streamInfo.xml");
                UpdateDemuxedStreams();

                demuxThreadStatus = true;
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
            }
        }

        private TitleInfo demuxedStreamList = new TitleInfo();

        private void UpdateDemuxedStreams()
        {
            try
            {
                listBoxDemuxedStreams.Items.Clear();
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    listBoxDemuxedStreams.Items.Add(si.typeDesc + " - " + si.filename);
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
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Video)
                    {
                        filename = si.filename;
                        break;
                    }
                }

                string fps = "";

                try
                {                    
                    MediaInfo mi = new MediaInfo(filename);
                    if (mi.VideoCount > 0)
                    {                        
                        fps = mi.Video[0].FrameRate;
                    }
                }
                catch (Exception ex )
                {
                    Message("Error getting MediaInfo: " + ex.Message);
                    return;
                }

                sb.Remove(0, sb.Length);
                if (!settings.encodeDirectshow || !settings.cropDirectshow)
                {
                    Message("Starting to index...");
                    Message("");

                    pc = new Process();
                    pc.StartInfo.FileName = settings.ffmsindexPath;
                    pc.StartInfo.Arguments = "\"" + filename + "\"";

                    Message("Command: " + pc.StartInfo.FileName + pc.StartInfo.Arguments);
                    pc.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);

                    pc.StartInfo.UseShellExecute = false;
                    pc.StartInfo.CreateNoWindow = true;
                    pc.StartInfo.RedirectStandardError = true;
                    pc.StartInfo.RedirectStandardOutput = true;

                    if (!pc.Start())
                    {
                        Message("Error starting ffmsindex.exe");
                        return;
                    }

                    pc.BeginOutputReadLine();

                    pc.WaitForExit();
                    pc.Close();
                    Message("Indexing done!");
                }
                if (settings.cropDirectshow)
                {
                    File.WriteAllText(settings.workingDir + "\\" + settings.filePrefix + "_cropTemp.avs",
                        "DirectShowSource(\"" + filename + "\")");
                }
                else
                {
                    File.WriteAllText(settings.workingDir + "\\" + settings.filePrefix + "_cropTemp.avs",
                        "FFVideoSource(\"" + filename + "\")");
                }
                AutoCrop ac = new AutoCrop(settings.workingDir + "\\" + settings.filePrefix + "_cropTemp.avs", settings);
                if (ac.error)
                {
                    Message("Exception: " + ac.errorStr);
                    return;
                }
                ac.NrFrames = settings.nrFrames;
                ac.BlackValue = settings.blackValue;
                ac.ShowDialog();
                Message("Crop top: " + ac.cropTop.ToString());
                Message("Crop bottom: " + ac.cropBottom.ToString());
                if (ac.resize)
                {
                    Message("Resize to: " + ac.resizeX.ToString() + " x " + ac.resizeY.ToString());
                }
                if (ac.border)
                {
                    Message("Border top: " + ac.borderTop.ToString());
                    Message("Border bottom: " + ac.borderBottom.ToString());
                }

                string encode = "";
                if (settings.encodeDirectshow)
                {
                    encode = "DirectShowSource(\"" + filename + "\")\r\n";
                }
                else
                {
                    encode = "FFVideoSource(\"" + filename + "\")\r\n";
                }
                if (ac.cropTop != 0 || ac.cropBottom != 0)
                {
                    encode += "Crop(0," + ac.cropTop.ToString() + ",-0,-" + ac.cropBottom.ToString() + ")\r\n";
                    if (ac.resize)
                    {
                        encode += "LanczosResize(" + ac.resizeX.ToString() + "," + ac.resizeY.ToString() + ")\r\n";
                    }
                    if (ac.border)
                    {
                        encode += "AddBorders(0," + ac.borderTop + ",0," + ac.borderBottom + ")\r\n";
                    }
                }
                string[] tmp = settings.commandsAfterResize.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in tmp)
                {
                    encode += s.Trim() + "\r\n";
                }
                File.WriteAllText(settings.workingDir + "\\" + settings.filePrefix + "_encode.avs", encode);
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Video)
                    {
                        if (si.extraFileInfo.GetType() != typeof(VideoFileInfo))
                        {
                            si.extraFileInfo = new VideoFileInfo();
                        }

                        ((VideoFileInfo)si.extraFileInfo).encodeAvs = settings.workingDir + "\\" + settings.filePrefix + "_encode.avs";
                        ((VideoFileInfo)si.extraFileInfo).fps = fps;
                        ((VideoFileInfo)si.extraFileInfo).filename = "";
                    }
                }
                TitleInfo.SaveStreamInfoFile(demuxedStreamList, settings.workingDir + "\\" + settings.filePrefix + "_streamInfo.xml");
                UpdateDemuxedStreams();
                indexThreadStatus = true;
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
            }
        }

        private bool DoDemux()
        {
            try
            {
                this.Text = title + " [Demuxing...]";
                notifyIconMain.Text = this.Text;

                if (settings.workingDir == "")
                {
                    MessageBox.Show("Working dir not set", "Error");
                    return false;
                }
                if (comboBoxTitle.SelectedIndex == -1)
                {
                    MessageBox.Show("No title selected", "Error");
                    return false;
                }
                int videoCount = 0;
                int audioCount = 0;
                foreach (StreamInfo si in titleList[comboBoxTitle.SelectedIndex].streams)
                {
                    if (si.streamType == StreamType.Audio && si.selected)
                    {
                        audioCount++;
                    }
                    if (si.streamType == StreamType.Video && si.selected)
                    {
                        videoCount++;
                    }
                }
                if (audioCount < 1)
                {
                    MessageBox.Show("No audio streams selected", "Error");
                    return false;
                }
                if (videoCount != 1)
                {
                    MessageBox.Show("No video stream or more then one selected", "Error");
                    return false;
                }

                progressBarMain.Visible = true;
                buttonAbort.Visible = true;

                richTextBoxLog.Clear();

                demuxThread = new Thread(DemuxThread);
                demuxThread.Start();

                while (demuxThread.IsAlive)
                {
                    Application.DoEvents();
                    Thread.Sleep(5);
                }
                demuxThread = null;
                return demuxThreadStatus;
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
                return false;
            }
            finally
            {
                this.Text = title;
                notifyIconMain.Text = this.Text;
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
                    MessageBox.Show("No demuxed streams available", "Error");
                    return false;
                }

                richTextBoxLog.Clear();

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
                Message("Exception: " + ex.Message);
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
                            if (((VideoFileInfo)si.extraFileInfo).filename != "") return true;
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
                progressBarMain.Visible = true;
                buttonAbort.Visible = true;
                tabControlMain.Enabled = false;

                if (demuxedStreamList.streams.Count == 0)
                {
                    if (!DoDemux()) return;
                    if (!DoIndex()) return;
                    if (!DoSubtitle()) return;
                    if (!DoEncode()) return;
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
                    }
                    else
                    {
                        if (!DoMux()) return;
                    }
                }
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
            }
            finally
            {
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
                LanguagInfo li = new LanguagInfo();
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
                        settings.preferedLanguages[index] = new LanguagInfo(lf.li);
                        UpdateLanguage();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void checkBoxCropDirectshow_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.cropDirectshow = checkBoxCropDirectshow.Checked;
            }
            catch (Exception)
            {
            }
        }

        private void checkBoxEncodeDirectshow_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                settings.encodeDirectshow = checkBoxEncodeDirectshow.Checked;
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
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonDoDemux_Click(object sender, EventArgs e)
        {
            try
            {
                progressBarMain.Visible = true;
                buttonAbort.Visible = true;
                tabControlMain.Enabled = false;

                DoDemux();
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
            }
            finally
            {
                tabControlMain.Enabled = true;
                progressBarMain.Visible = false;
                buttonAbort.Visible = false;
            }
        }

        private void buttonDoIndex_Click(object sender, EventArgs e)
        {
            try
            {
                progressBarMain.Visible = true;
                buttonAbort.Visible = true;
                tabControlMain.Enabled = false;

                DoIndex();
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
            }
            finally
            {
                tabControlMain.Enabled = true;
                progressBarMain.Visible = false;
                buttonAbort.Visible = false;
            }
        }

        private void buttonDoEncode_Click(object sender, EventArgs e)
        {
            try
            {
                progressBarMain.Visible = true;
                buttonAbort.Visible = true;
                tabControlMain.Enabled = false;

                DoEncode();
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
            }
            finally
            {
                tabControlMain.Enabled = true;
                progressBarMain.Visible = false;
                buttonAbort.Visible = false;
            }
        }
                
        private bool DoEncode()
        {
            try
            {
                this.Text = title + " [Encoding...]";
                notifyIconMain.Text = this.Text;

                richTextBoxLog.Clear();

                encodeThread = new Thread(EncodeThread);
                encodeThread.Start();

                while (encodeThread.IsAlive)
                {
                    Application.DoEvents();
                    Thread.Sleep(5);
                }
                encodeThread = null;
                return true;
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
                return false;
            }
            finally
            {
                this.Text = title;
                notifyIconMain.Text = this.Text;
            }
        }

        private bool encodeThreadStatus = false;
        private void EncodeThread()
        {
            try
            {
                encodeThreadStatus = false;
                if (settings.workingDir == "")
                {
                    MessageBox.Show("Working dir not set", "Error");
                    return;
                }
                if (demuxedStreamList.streams.Count == 0)
                {
                    MessageBox.Show("No demuxed streams available", "Error");
                    return;
                }

                string filename = "";
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Video)
                    {
                        if (si.extraFileInfo.GetType() == typeof(VideoFileInfo))
                        {
                            filename = ((VideoFileInfo)si.extraFileInfo).encodeAvs;
                            break;
                        }
                    }
                }

                if (filename == "")
                {
                    MessageBox.Show("Encode avs not set - do index + autocrop first", "Error");
                    return;
                }

                int index = comboBoxEncodeProfile.SelectedIndex;
                if (index < 0)
                {
                    MessageBox.Show("Encoding profile not set", "Error");
                    return;
                }

                sb.Remove(0, sb.Length);
                Message("Starting to encode...");
                Message("");

                pc = new Process();
                pc.StartInfo.FileName = settings.x264Path;
                pc.StartInfo.Arguments = settings.encodingSettings[index].settings + " \"" + filename + "\" -o \"" + settings.workingDir +
                    "\\" + settings.filePrefix + "_video.mkv\"";

                Message("Command: " + pc.StartInfo.FileName + pc.StartInfo.Arguments);
                
                pc.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                pc.StartInfo.UseShellExecute = true;
                pc.StartInfo.CreateNoWindow = true;
                                                
                if (!pc.Start())
                {
                    Message("Error starting x264.exe");
                    return;
                }

                pc.PriorityClass = settings.x264Priority;

                while (!pc.HasExited)
                {
                    pc.Refresh();
                    string tmp = pc.MainWindowTitle;
                    int s = tmp.IndexOf('[');
                    int e = tmp.IndexOf(']');
                    if (s > 0 && e > 0 && e > s)
                    {
                        string fps = "";
                        string eta = "";
                        string substr = tmp.Substring(s + 1, e - s - 2);
                        
                        string[] tmpStr = tmp.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tmpStr.Length > 2)
                        {
                            fps = "- " + tmpStr[1];
                            eta = " - " + tmpStr[2];
                        }

                        this.Text = title + " [Encoding (" + substr + "% " + fps + eta + ")...]";
                        notifyIconMain.Text = title + " " + substr + "%"; ;
                    }
                    Thread.Sleep(1000);
                }
                

                pc.WaitForExit();
                pc.Close();
                Message("Encoding done!");
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Video)
                    {
                        if (si.extraFileInfo.GetType() == typeof(VideoFileInfo))
                        {
                            ((VideoFileInfo)si.extraFileInfo).encodedFile = settings.workingDir + "\\" + settings.filePrefix + "_video.mkv";
                            break;
                        }
                    }
                }
                TitleInfo.SaveStreamInfoFile(demuxedStreamList, settings.workingDir + "\\" + settings.filePrefix + "_streamInfo.xml");
                encodeThreadStatus = true;
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
            }
        }

        private void buttonDoSubtitle_Click(object sender, EventArgs e)
        {
            try
            {
                progressBarMain.Visible = true;
                buttonAbort.Visible = true;
                tabControlMain.Enabled = false;

                DoSubtitle();
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
            }
            finally
            {
                tabControlMain.Enabled = true;
                progressBarMain.Visible = false;
                buttonAbort.Visible = false;
            }
        }

        private bool subtitleThreadStatus = false;
        private void SubtitleThread()
        {
            try
            {
                subtitleThreadStatus = false;
                
                string fps = "";
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Video)
                    {
                        if (si.extraFileInfo.GetType() == typeof(VideoFileInfo))
                        {
                            fps = ((VideoFileInfo)si.extraFileInfo).fps;
                            break;
                        }
                    }
                }
                if (fps == "")
                {
                    MessageBox.Show("Framerate not set - do index + autocrop first", "Error");
                    return;
                }
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Subtitle)
                    {
                        si.extraFileInfo = new SubtitleFileInfo();
                        SubtitleFileInfo sfi = (SubtitleFileInfo)si.extraFileInfo;

                        string output = settings.workingDir + "\\" + Path.GetFileNameWithoutExtension(si.filename) +
                            "_complete.sub";

                        string outputIdx = settings.workingDir + "\\" + Path.GetFileNameWithoutExtension(si.filename) +
                            "_complete.idx";

                        sb.Remove(0, sb.Length);
                        Message("Starting to process subtitle...");
                        Message("");

                        pc = new Process();
                        pc.StartInfo.FileName = settings.javaPath;
                        pc.StartInfo.Arguments = "-jar " + settings.sup2subPath + " " +
                            si.filename + " " + output + " /fps:" + fps;

                        Message("Command: " + pc.StartInfo.FileName + pc.StartInfo.Arguments);
                        
                        pc.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);

                        pc.StartInfo.UseShellExecute = false;
                        pc.StartInfo.CreateNoWindow = true;
                        pc.StartInfo.RedirectStandardError = true;
                        pc.StartInfo.RedirectStandardOutput = true;

                        if (!pc.Start())
                        {
                            Message("Error starting java.exe");
                            return;
                        }

                        pc.BeginOutputReadLine();

                        pc.WaitForExit();
                        pc.Close();
                        Message("Processing done!");

                        if (File.Exists(output))
                        {
                            sfi.normalSub = output;
                        }
                        if (File.Exists(outputIdx))
                        {
                            sfi.normalIdx = outputIdx;
                        }
                        ////////////////////////////////////////////////////////

                        output = settings.workingDir + "\\" + Path.GetFileNameWithoutExtension(si.filename) +
                            "_onlyforced.sub";

                        outputIdx = settings.workingDir + "\\" + Path.GetFileNameWithoutExtension(si.filename) +
                            "_onlyforced.idx";

                        sb.Remove(0, sb.Length);
                        Message("Starting to process subtitle...");
                        Message("");

                        pc = new Process();
                        pc.StartInfo.FileName = settings.javaPath;
                        pc.StartInfo.Arguments = "-jar " + settings.sup2subPath + " " +
                            si.filename + " " + output + " /forced+ /fps:" + fps;

                        Message("Command: " + pc.StartInfo.FileName + pc.StartInfo.Arguments);

                        pc.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);

                        pc.StartInfo.UseShellExecute = false;
                        pc.StartInfo.CreateNoWindow = true;
                        pc.StartInfo.RedirectStandardError = true;
                        pc.StartInfo.RedirectStandardOutput = true;

                        if (!pc.Start())
                        {
                            Message("Error starting java.exe");
                            return;
                        }

                        pc.BeginOutputReadLine();

                        pc.WaitForExit();
                        pc.Close();
                        Message("Processing done!");

                        if (File.Exists(output))
                        {
                            sfi.forcedSub = output;
                        }
                        if (File.Exists(outputIdx))
                        {
                            sfi.forcedIdx = outputIdx;
                        }
                        try
                        {
                            if (sfi.normalIdx != "" && sfi.normalSub != "" && sfi.forcedIdx != "" && sfi.forcedSub != "")
                            {
                                FileInfo f1 = new FileInfo(sfi.normalSub);
                                FileInfo f2 = new FileInfo(sfi.forcedSub);
                                if (f1.Length == f2.Length)
                                {
                                    File.Delete(sfi.normalSub);
                                    File.Delete(sfi.normalIdx);
                                    sfi.normalSub = "";
                                    sfi.normalIdx = "";
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                TitleInfo.SaveStreamInfoFile(demuxedStreamList, settings.workingDir + "\\" + settings.filePrefix + "_streamInfo.xml");
                subtitleThreadStatus = true;
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
            }
        }

        private bool DoSubtitle()
        {
            try
            {
                if (demuxedStreamList.streams.Count == 0)
                {
                    MessageBox.Show("No demuxed streams available", "Error");
                    return false;
                }

                this.Text = title + " [Processing subtitles...]";
                notifyIconMain.Text = this.Text;
                richTextBoxLog.Clear();

                subtitleThread = new Thread(SubtitleThread);
                subtitleThread.Start();

                while (subtitleThread.IsAlive)
                {
                    Application.DoEvents();
                    Thread.Sleep(5);
                }
                subtitleThread = null;
                return subtitleThreadStatus;
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
                return false;
            }
            finally
            {
                this.Text = title;
                notifyIconMain.Text = this.Text;
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
                notifyIconMain.Text = this.Text;
                this.Hide();
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

        private string getShortLanguage(string language)
        {
            try
            {
                foreach (LanguagInfo li in settings.preferedLanguages)
                {
                    if (li.language == language) return li.languageShort;
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        private bool hasForcedSub(string language)
        {
            try
            {
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Subtitle && si.language == language)
                    {
                        if (si.extraFileInfo.GetType() == typeof(SubtitleFileInfo))
                        {
                            if (((SubtitleFileInfo)si.extraFileInfo).forcedIdx != "") return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool muxThreadStatus = false;
        private void MuxThread()
        {
            try
            {
                muxThreadStatus = false;
                int videoStream = 0;
                int audioStream = 0;
                int chapterStream = 0;
                int suptitleStream = 0;

                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Video)
                    {
                        if (si.extraFileInfo.GetType() == typeof(VideoFileInfo))
                        {
                            if (((VideoFileInfo)si.extraFileInfo).encodedFile != "")
                            {
                                if (File.Exists(((VideoFileInfo)si.extraFileInfo).encodedFile))
                                {
                                    videoStream++;
                                }
                            }
                        }
                    }
                    else if (si.streamType == StreamType.Audio)
                    {
                        if (File.Exists(si.filename))
                        {
                            audioStream++;
                        }
                    }
                    else if (si.streamType == StreamType.Chapter)
                    {
                        chapterStream++;
                    }
                    else if (si.streamType == StreamType.Subtitle)
                    {
                        suptitleStream++;
                    }
                }
                if (videoStream == 0)
                {
                    MessageBox.Show("No videostream or encoded video filename not set", "Error");
                    return;
                }
                if (audioStream == 0)
                {
                    MessageBox.Show("No audiostream or audio filename not set", "Error");
                    return;
                }
                if (chapterStream > 0)
                {
                    bool error = false;
                    foreach (StreamInfo si in demuxedStreamList.streams)
                    {
                        if (si.streamType == StreamType.Chapter)
                        {
                            if (!File.Exists(si.filename))
                            {
                                error = true;
                            }
                        }
                    }
                    if (error)
                    {
                        MessageBox.Show("Chapter file not found", "Error");
                        return;
                    }
                }
                if (suptitleStream > 0)
                {
                    bool error = false;
                    foreach (StreamInfo si in demuxedStreamList.streams)
                    {
                        if (si.streamType == StreamType.Subtitle)
                        {
                            if (si.extraFileInfo.GetType() == typeof(SubtitleFileInfo))
                            {
                                SubtitleFileInfo sfi = (SubtitleFileInfo)si.extraFileInfo;
                                if (sfi.forcedIdx != "")
                                {
                                    if (!File.Exists(sfi.forcedIdx)) error = true;
                                }
                                if (sfi.forcedSub != "")
                                {
                                    if (!File.Exists(sfi.forcedSub)) error = true;
                                }

                                if (sfi.normalIdx != "")
                                {
                                    if (!File.Exists(sfi.normalIdx)) error = true;
                                }
                                if (sfi.normalSub != "")
                                {
                                    if (!File.Exists(sfi.normalSub)) error = true;
                                }
                            }
                            else
                            {
                                error = false;
                            }                        }
                    }
                    if (error)
                    {
                        MessageBox.Show("Subtitle file(s) not found", "Error");
                        return;
                    }
                }

                sb.Remove(0, sb.Length);
                Message("Starting to mux...");
                Message("");

                pc = new Process();
                pc.StartInfo.FileName = settings.mkvmergePath;
                pc.StartInfo.Arguments = "--title \"" + settings.movieTitle + "\" -o " + settings.targetFolder + "\\" + settings.targetFilename + ".mkv ";

                // video + chapter
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Chapter)
                    {
                        pc.StartInfo.Arguments += "--chapters " + si.filename + " ";
                    }
                    else if (si.streamType == StreamType.Video)
                    {
                        pc.StartInfo.Arguments += ((VideoFileInfo)si.extraFileInfo).encodedFile + " ";
                    }                    
                }
                // audio
                bool defaultSet = false;
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Audio)
                    {
                        string st = "";
                        st = getShortLanguage(si.language);
                        if (st != "") pc.StartInfo.Arguments += "--language 0" + ":" + st + " ";
                        if (settings.preferedLanguages.Count > 0 && settings.preferedLanguages[0].language == si.language)
                        {
                            if (!defaultSet)
                            {
                                if (settings.defaultAudio)
                                {
                                    pc.StartInfo.Arguments += "--default-track 0 ";
                                }
                                defaultSet = true;
                            }
                        }
                        pc.StartInfo.Arguments += si.filename + " ";
                    }
                }
                // subtitle
                defaultSet = false;
                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Subtitle)
                    {
                        SubtitleFileInfo sfi = (SubtitleFileInfo)si.extraFileInfo;
                        if (settings.preferedLanguages.Count > 0 && settings.preferedLanguages[0].language == si.language)
                        {
                            if (!defaultSet)
                            {
                                if (settings.defaultSubtitle)
                                {
                                    if (!settings.defaultSubtitleForced)
                                    {
                                        pc.StartInfo.Arguments += "--default-track 0 ";
                                        defaultSet = true;
                                    }
                                    else
                                    {
                                        if (hasForcedSub(si.language))
                                        {
                                            if (sfi.forcedIdx != "")
                                            {
                                                pc.StartInfo.Arguments += "--default-track 0 ";
                                                defaultSet = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (!settings.defaultSubtitle)
                        {
                            pc.StartInfo.Arguments += "--default-track 0:0 ";
                        }
                        if (sfi.normalIdx != "")
                        {
                            pc.StartInfo.Arguments += sfi.normalIdx + " ";
                        }
                        else if (sfi.forcedIdx != "")
                        {
                            pc.StartInfo.Arguments += sfi.forcedIdx + " ";
                        }
                    }
                }

                Message("Command: " + pc.StartInfo.FileName + pc.StartInfo.Arguments);
                
                pc.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);

                pc.StartInfo.UseShellExecute = false;
                pc.StartInfo.CreateNoWindow = true;
                pc.StartInfo.RedirectStandardError = true;
                pc.StartInfo.RedirectStandardOutput = true;

                if (!pc.Start())
                {
                    Message("Error starting mkvmerge.exe");
                    return;
                }

                pc.BeginOutputReadLine();

                pc.WaitForExit();
                pc.Close();
                Message("Muxing done!");

                Message("Trying to copy subtitles...");
                try
                {
                    if (!Directory.Exists(settings.targetFolder + "\\Subs"))
                    {
                        Directory.CreateDirectory(settings.targetFolder + "\\Subs");
                    }
                }
                catch (Exception ex)
                {
                    Message("Exception: " + ex.Message);
                }

                foreach (StreamInfo si in demuxedStreamList.streams)
                {
                    if (si.streamType == StreamType.Subtitle)
                    {
                        if (si.extraFileInfo.GetType() == typeof(SubtitleFileInfo))
                        {
                            SubtitleFileInfo sfi = (SubtitleFileInfo)si.extraFileInfo;
                            try
                            {
                                string target = settings.targetFolder + "\\Subs\\" + settings.targetFilename;
                                if (sfi.normalIdx != "")
                                {
                                    File.Copy(sfi.normalIdx, target + "_" + si.number.ToString("d2") + "_" + si.language.ToLower() + ".idx");
                                    File.Copy(sfi.normalSub, target + "_" + si.number.ToString("d2") + "_" + si.language.ToLower() + ".sub");
                                }
                                else if (sfi.forcedIdx != "")
                                {
                                    File.Copy(sfi.forcedIdx, target + "_" + si.number.ToString("d2") + "_" + si.language.ToLower() + "_forced.idx");
                                    File.Copy(sfi.forcedSub, target + "_" + si.number.ToString("d2") + "_" + si.language.ToLower() + "_forced.sub");
                                }
                            }
                            catch (Exception ex)
                            {
                                Message("Exception: " + ex.Message);
                            }
                        }
                    }
                }
                Message("Done.");
                if (settings.deleteAfterEncode)
                {
                    Message("Deleting source files...");
                    foreach (StreamInfo si in demuxedStreamList.streams)
                    {
                        try
                        {
                            File.Delete(si.filename);
                            if (si.extraFileInfo.GetType() == typeof(VideoFileInfo))
                            {
                                File.Delete(((VideoFileInfo)si.extraFileInfo).encodedFile);
                                File.Delete(((VideoFileInfo)si.extraFileInfo).encodeAvs);
                            }
                            if (si.extraFileInfo.GetType() == typeof(SubtitleFileInfo))
                            {
                                SubtitleFileInfo sfi = (SubtitleFileInfo)si.extraFileInfo;
                                if (sfi.forcedIdx != "") File.Delete(sfi.forcedIdx);
                                if (sfi.forcedSub != "") File.Delete(sfi.forcedSub);
                                if (sfi.normalIdx != "") File.Delete(sfi.normalIdx);
                                if (sfi.normalSub != "") File.Delete(sfi.normalSub);
                            }
                        }
                        catch (Exception ex)
                        {
                            Message("Exception: " + ex.Message);
                        }
                    }
                    try
                    {
                        File.Delete(settings.workingDir + "\\" + settings.filePrefix + "_streamInfo.xml");
                        File.Delete(settings.workingDir + "\\" + settings.filePrefix + "_cropTemp.avs");
                    }
                    catch (Exception ex)
                    {
                        Message("Exception: " + ex.Message);
                    }
                    Message("Done.");
                }
                muxThreadStatus = true;
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
            }
        }

        private bool DoMux()
        {
            try
            {
                this.Text = title + " [Muxing...]";
                notifyIconMain.Text = this.Text;

                richTextBoxLog.Clear();

                muxThread = new Thread(MuxThread);
                muxThread.Start();

                while (muxThread.IsAlive)
                {
                    Application.DoEvents();
                    Thread.Sleep(5);
                }
                muxThread = null;
                return true;
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
                return false;
            }
            finally
            {
                this.Text = title;
                notifyIconMain.Text = this.Text;
            }
        }

        private void buttonDoMux_Click(object sender, EventArgs e)
        {
            try
            {
                progressBarMain.Visible = true;
                buttonAbort.Visible = true;
                tabControlMain.Enabled = false;

                DoMux();
            }
            catch (Exception ex)
            {
                Message("Exception: " + ex.Message);
            }
            finally
            {
                tabControlMain.Enabled = true;
                progressBarMain.Visible = false;
                buttonAbort.Visible = false;
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
                    LanguagInfo li = settings.preferedLanguages[index];
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
                    LanguagInfo li = settings.preferedLanguages[index];
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

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                settings.commandsAfterResize = richTextBoxCommandsAfterResize.Text;
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
    }
}