﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace BluRip
{
    [XmlInclude(typeof(ExtraFileInfo))]
    [XmlInclude(typeof(VideoFileInfo))]
    [XmlInclude(typeof(SubtitleFileInfo))]
    public class TitleInfo
    {
        public TitleInfo() { }

        public TitleInfo(TitleInfo orig)
        {
            this.desc = orig.desc;
            this.streams.Clear();
            foreach (StreamInfo si in orig.streams)
            {
                this.streams.Add(new StreamInfo(si));
            }
        }

        public string desc = "";

        public List<StreamInfo> streams = new List<StreamInfo>();

        public static bool SaveStreamInfoFile(TitleInfo ti, string filename)
        {
            MemoryStream ms = null;
            FileStream fs = null;
            XmlSerializer xs = null;

            try
            {
                ms = new MemoryStream();
                fs = new FileStream(filename, FileMode.Create, FileAccess.Write);

                xs = new XmlSerializer(typeof(TitleInfo));
                xs.Serialize(ms, ti);
                ms.Seek(0, SeekOrigin.Begin);

                fs.Write(ms.ToArray(), 0, (int)ms.Length);
                ms.Close();
                fs.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (ms != null) ms.Close();
                if (fs != null) fs.Close();
            }
        }

        public static bool LoadSettingsFile(ref TitleInfo ti, string filename)
        {
            MemoryStream ms = null;

            try
            {
                if (!File.Exists(filename)) return false;
                byte[] data = File.ReadAllBytes(filename);
                XmlSerializer xs = new XmlSerializer(typeof(TitleInfo));
                ms = new MemoryStream(data);
                ms.Seek(0, SeekOrigin.Begin);

                ti = (TitleInfo)xs.Deserialize(ms);
                ms.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (ms != null) ms.Close();
            }
        }
    }

    public enum StreamType
    {
        Unknown,
        Chapter,
        Video,
        Audio,
        Subtitle
    }

    public class ExtraFileInfo
    {
        public ExtraFileInfo() { }

        public string filename = "";
    }

    public class VideoFileInfo : ExtraFileInfo
    {
        public VideoFileInfo() { }

        public string encodeAvs = "";
        public string fps = "";
        public string encodedFile = "";
    }

    public class SubtitleFileInfo : ExtraFileInfo
    {
        public SubtitleFileInfo() { }

        public string normalSub = "";
        public string normalIdx = "";

        public string forcedSub = "";
        public string forcedIdx = "";
    }

    public class StreamInfo
    {
        public StreamInfo() { }

        public StreamInfo(StreamInfo orig)
        {
            this.addInfo = orig.addInfo;
            this.desc = orig.desc;
            this.filename = orig.filename;
            this.language = orig.language;
            this.number = orig.number;
            this.selected = orig.selected;
            this.streamType = orig.streamType;
            this.typeDesc = orig.typeDesc;
        }

        public int number = 0;

        public string typeDesc = "";

        public string desc = "";

        public string addInfo = "";

        public string language = "";

        public StreamType streamType = StreamType.Unknown;

        public bool selected = false;

        public string filename = "";
        
        public ExtraFileInfo extraFileInfo = new ExtraFileInfo();
    }
}