using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;

namespace com.foxmail.wyyuan1991.NRM.Simulator
{
    /// <summary>
    /// 原始需求到达记录
    /// </summary>
    public class PrimalArrival : IXmlSerializable
    {
        /// <summary>
        /// 到达时间
        /// </summary>
        public int ArriveTime { get; set; }
        /// <summary>
        /// 旅客所属子市场
        /// </summary>
        public int IndexOfMS { get; set; }
        //public IMarketSegment MS { get; set; }

        public double ChoosingParam { get; set; }

        public PrimalArrival()
        {

        }
        public PrimalArrival(string s)
        {
            string[] ss = s.Split(new char[] { ',' });
            this.ArriveTime = Convert.ToInt32(ss[0]);
            this.IndexOfMS = Convert.ToInt32(ss[1]);
            this.ChoosingParam = Convert.ToDouble(ss[2]);
        }

        #region 实现XML序列化
        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }
        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();
            ArriveTime = Convert.ToInt32(reader.GetAttribute("Time"));
            IndexOfMS = Convert.ToInt32(reader.GetAttribute("MS"));
            reader.Read();
        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Time", ArriveTime.ToString());
            writer.WriteAttributeString("MS", IndexOfMS.ToString());
        }
        #endregion

        public override string ToString()
        {
            return this.ArriveTime + "," + IndexOfMS+","+ ChoosingParam;
        }
    }
    /// <summary>
    /// 原始需求到达列表
    /// </summary>
    public class PrimalArrivalList : List<PrimalArrival>, IXmlSerializable
    {
        public int PAListID { get; set; }
        public int TimeHorizon { get; set; }

        #region 实现XML序列化
        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }
        public void ReadXml(XmlReader reader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PrimalArrival));
            this.PAListID = Convert.ToInt32(reader.GetAttribute("ID"));
            reader.ReadStartElement("PrimalArrivalList");
            //reader.MoveToContent();          
            //this.PAListID = Convert.ToInt32(reader.ReadAttributeValue());

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                PrimalArrival sdi = serializer.Deserialize(reader) as PrimalArrival;
                this.Add(sdi);
            }
            reader.ReadEndElement();
        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("ID", this.PAListID.ToString());
            XmlSerializer serializer = new XmlSerializer(typeof(PrimalArrival));
            foreach (PrimalArrival pa in this)
            {
                serializer.Serialize(writer, pa);
            }
        }
        #endregion
        public void WriteToXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("PrimalArrivalList ");
            writer.WriteAttributeString("ID", this.PAListID.ToString());
            foreach (PrimalArrival pa in this)
            {
                //加入子元素
                writer.WriteStartElement("PrimalArrival");
                writer.WriteAttributeString("Time", pa.ArriveTime.ToString());
                writer.WriteAttributeString("MS", pa.IndexOfMS.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        public void WriteToFile(string FilePath)
        {
            FileStream fs = new FileStream(FilePath, FileMode.Append);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            foreach (PrimalArrival pa in this)
            {
                sw.Write(pa.ToString() + ";");
            }
            sw.Close();
            fs.Close();
        }

        //读入/输出文件
        public void WriteToArrFile(string FilePath)
        {
            FileStream fs = new FileStream(FilePath, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            //写文件头
            sw.WriteLine("[ID={0},TimeHorizon={1}]", this.PAListID, this.TimeHorizon);

            foreach (PrimalArrival pa in this)
            {
                sw.WriteLine(pa.ToString());
            }
            sw.Close();
            fs.Close();
        }
        public void ReadFromArrFile(string FilePath)
        {
            //c#文件流读文件 
            using (FileStream fsRead = new FileStream(FilePath, FileMode.Open))
            {
                StreamReader sr = new StreamReader(fsRead, System.Text.Encoding.Default);
                //读文件头
                string s = sr.ReadLine();
                s = s.Substring(1, s.Length - 2);
                string[] ss = s.Split(new char[] { ',', '=' });
                if (ss[0] == "ID") this.PAListID = Convert.ToInt32(ss[1]);
                if (ss[2] == "TimeHorizon") this.TimeHorizon = Convert.ToInt32(ss[3]);
                while (!sr.EndOfStream)
                {
                    this.Add(new PrimalArrival(sr.ReadLine()));
                }
            }
        }
    }
    /// <summary>
    /// 原始需求到达数据
    /// </summary>
    public class PrimalArrivalData : IXmlSerializable
    {
        private XmlReader _reader;
        public HashSet<PrimalArrivalList> Data = new HashSet<PrimalArrivalList>();

        //XML文件操作
        public void LoadFromXml(string _FilePath)
        {
            var xrs = new XmlReaderSettings();
            xrs.IgnoreComments = true;
            xrs.IgnoreWhitespace = true;

            XmlSerializer serializer = new XmlSerializer(typeof(PrimalArrivalList));
            TextReader reader = new StreamReader(_FilePath);
            XmlReader r = XmlReader.Create(reader, xrs);
            r.Read();
            if (r.NodeType == XmlNodeType.XmlDeclaration)
            {
                r.Skip();
            }
            this.ReadXml(r);
        }
        public void SaveToXml(string _FilePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PrimalArrivalData));
            TextWriter writer = new StreamWriter(_FilePath);
            serializer.Serialize(writer, this);
            writer.Close();
        }

        public void ReadXml(string _FilePath)
        {
            this._reader = XmlReader.Create(_FilePath);
        }
        public bool ReadNextPAL()
        {
            //TODO:从XML文件中顺序读取下一个PAL
            //找到第一个PrimalArrivalList标记
            if (_reader.NodeType == XmlNodeType.Element &&
                    _reader.Name == "PrimalArrivalList") return true;
            while (_reader.Read())
            {
                if (_reader.NodeType == XmlNodeType.Element &&
                    _reader.Name == "PrimalArrivalList")
                {
                    return true;
                }
            }
            return false;
        }
        public PrimalArrivalList GetCurPAL()
        {
            PrimalArrivalList pal = new PrimalArrivalList();
            pal.PAListID = Convert.ToInt32(_reader.GetAttribute("ID"));
            while (_reader.Read())
            {
                if (_reader.NodeType == XmlNodeType.Element &&
                                 _reader.Name == "PrimalArrival")
                {
                    pal.Add(new PrimalArrival()
                    {
                        ArriveTime = Convert.ToInt32(_reader.GetAttribute("Time")),
                        IndexOfMS = Convert.ToInt32(_reader.GetAttribute("MS"))
                    });
                }
                else if (_reader.NodeType == XmlNodeType.Element &&
                                _reader.Name == "PrimalArrivalList")
                {
                    break;
                }
            }
            return pal;
        }
        public void WriteToXml(string _FilePath)
        {
            XmlTextWriter writer = new XmlTextWriter(_FilePath, null);
            //写入根元素
            writer.WriteStartElement("PrimalArrivalData");
            foreach (PrimalArrivalList pal in this.Data)
            {
                writer.WriteStartElement("PrimalArrivalList ");
                writer.WriteAttributeString("ID", pal.PAListID.ToString());
                foreach (PrimalArrival pa in pal)
                {
                    //加入子元素
                    writer.WriteStartElement("PrimalArrival");
                    writer.WriteAttributeString("Time", pa.ArriveTime.ToString());
                    writer.WriteAttributeString("MS", pa.IndexOfMS.ToString());
                    writer.WriteEndElement();
                }
                //关闭根元素，并书写结束标签
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            //将XML写入文件并且关闭XmlTextWriter
            writer.Close();
        }

        #region IXmlSerializable
        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }
        public void ReadXml(XmlReader reader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PrimalArrivalList));

            reader.ReadStartElement("PrimalArrivalData");

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                PrimalArrivalList sdi = serializer.Deserialize(reader) as PrimalArrivalList;
                this.Data.Add(sdi);
            }
            reader.ReadEndElement();
        }
        public void WriteXml(XmlWriter writer)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PrimalArrivalList));
            foreach (PrimalArrivalList pa in this.Data)
            {
                serializer.Serialize(writer, pa);
            }
        }
        #endregion
    }

}
