using com.foxmail.wyyuan1991.NRM.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;

namespace com.foxmail.wyyuan1991.NRM.Simulator
{
    /// <summary>
    /// 售票记录  旅客来源市场，调试使用，实际中无法观测
    /// </summary>
    public class SellingRecord : IXmlSerializable
    {
        /// <summary>
        /// 对应原始到达记录
        /// </summary>
        public PrimalArrival PA { get; set; }
        /// <summary>
        /// 到达时间
        /// </summary>
        public int ArriveTime { get; set; }
        /// <summary>
        /// 购买产品
        /// </summary>
        public List<IProduct> Product { get; set; }
        public List<Ticket> Tickets { get; set; }

        public SellingRecord()
        {
           
        }

        #region 实现XML序列化
        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }
        public void ReadXml(XmlReader reader)
        {
            
        }
        public void WriteXml(XmlWriter writer)
        {
            if (Product != null)
            {
                writer.WriteAttributeString("Time", ArriveTime.ToString());
                string s = "";
                foreach (var p in Product)
                {
                    s += p.ProID + ",";
                }
                s = s.Remove(s.Length - 1, 1); //删除最后的逗号
                writer.WriteAttributeString("Product", s);//多个product
            }
        }
        #endregion

        public string Print()
        {
            string s = ArriveTime.ToString()+";";

            if (Product != null)
            {
                foreach (IProduct p in Product)
                {
                    int i = Product.IndexOf(p);
                    if(i<Product.Count-1)
                    {
                        s += p.ProID + ",";
                    }else
                    {
                        s += p.ProID + ";";
                    }
                  
                }
            }
            if (Tickets != null)
            {
                foreach (Ticket t in Tickets)
                {
                    int i = Tickets.IndexOf(t);

                    foreach (IMetaResource imr in t.MetaResList)
                    {
                        s += imr.Name + ",";
                    }                   
                }
            }
                return s;
        }
        public override string ToString()
        {
            string s = String.Format("[{0}]时刻到达了一位[{1}]市场的旅客,",
                ArriveTime, PA.IndexOfMS);
            if (Product == null)
            {
                s += "他[未购买]产品";
            }
            else
            {
                s += "他购买了:";
                foreach (IProduct p in Product)
                {
                    s += p.Description + ",";
                }
            }
            return s;
        }
    }
    /// <summary>
    /// 销售列表
    /// </summary>
    public class SellingRecordList : List<SellingRecord>, IXmlSerializable
    {
        /// <summary>
        /// 对应原始需求到达列表
        /// </summary>
        public PrimalArrivalList PAL { get; set; }

        #region 实现XML序列化
        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }
        public void ReadXml(XmlReader reader)
        {

        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("PAL_ID", this.PAL.PAListID.ToString());
            writer.WriteAttributeString("Revenue", Revenue().ToString());

            XmlSerializer serializer = new XmlSerializer(typeof(SellingRecord));
            foreach (SellingRecord sr in this)
            {
                serializer.Serialize(writer, sr);
            }
        }
        #endregion

        public double Revenue()
        {
            return this.Sum(i => i.Product == null ? 0 : i.Product.Sum(k => k.Fare));
        }

        public void AddRecord(int time, PrimalArrival pa, List<IProduct> pro, List<Ticket> tickets)
        {

            SellingRecord sr = new SellingRecord()
            {
                ArriveTime = time,
                PA = pa,
                Product = pro,
                Tickets = tickets
            };
            this.Add(sr);
        }

        //输出文件
        public void WriteToFile(string FilePath)
        {
            FileStream fs = new FileStream(FilePath, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            //写文件头
            sw.WriteLine("[ID={0}]", this.PAL.PAListID);

            foreach (SellingRecord sr in this)
            {
                if (sr.Product != null)
                {
                    sw.WriteLine(sr.Print());
                }
            }
            sw.Close();
            fs.Close();
        }
        public void WritetoXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("SellingRecordList");
            writer.WriteAttributeString("PAL_ID", this.PAL.PAListID.ToString());
            writer.WriteAttributeString("Revenue", this.Revenue().ToString());
            foreach (SellingRecord sr in this)
            {
                if (sr.Product == null) continue;
                writer.WriteStartElement("SellingRecord");
                writer.WriteAttributeString("Time", sr.ArriveTime.ToString());
                string s = "";
                foreach (var p in sr.Product)
                {
                    s += p.ProID + ",";
                }
                s = s.Remove(s.Length - 1, 1); //删除最后的逗号
                writer.WriteAttributeString("Product", s);//多个product
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

    }

    public enum ProductState { Closed = 0, Open = 1 };
    /// <summary>
    /// 控制记录，默认全都关闭
    /// </summary>
    public class ControlRecord : IXmlSerializable
    {
        public int Time { get; set; }
        public IProduct Pro { get; set; }
        public ProductState Sta { get; set; }

        #region 实现XML序列化
        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }
        public void ReadXml(XmlReader reader)
        {
            
        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Time", Time.ToString());
            writer.WriteAttributeString("Product", Pro.ProID.ToString());
            writer.WriteAttributeString("State", Sta.ToString());
        }
        #endregion

        public string Print()
        {
            return Time.ToString() + ";" + Pro.ProID.ToString() + ";" + (Sta == ProductState.Open ? "1" : "0");
        }
       

    }
    public class ControlRecordList : List<ControlRecord>, IXmlSerializable
    {
        private int curTime = 0;
        private List<IProduct> curOpenProducts = new List<IProduct>();
        public void UpdateOpenProducts(int time, List<IProduct> list)
        {
            if (time < curTime)
            {
                throw new Exception("时间顺序错误！");
            }
            foreach (IProduct p in list.Except(curOpenProducts))
            {
                curOpenProducts.Add(p);
                this.Add(new ControlRecord()
                {
                    Time = time,
                    Pro = p,
                    Sta = ProductState.Open
                });
            }

            List<IProduct> temp = new List<IProduct>();
            foreach (IProduct p in curOpenProducts.Except(list))
            {
                this.Add(new ControlRecord()
                {
                    Time = time,
                    Pro = p,
                    Sta = ProductState.Closed
                });
                temp.Add(p);
            }
            foreach (IProduct p in temp)
            {
                curOpenProducts.Remove(p);
            }
            this.curTime = time;
        }

        /// <summary>
        /// 对应原始需求到达列表
        /// </summary>
        public PrimalArrivalList PAL { get; set; }

        #region 实现XML序列化
        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }
        public void ReadXml(XmlReader reader)
        {
           
        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("PAL_ID", this.PAL.PAListID.ToString());
            XmlSerializer serializer = new XmlSerializer(typeof(ControlRecord));
            foreach (ControlRecord cr in this)
            {
                serializer.Serialize(writer, cr);
            }
        }
        #endregion
        //输出文件
        public void WriteToFile(string FilePath)
        {
            FileStream fs = new FileStream(FilePath, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            //写文件头
            sw.WriteLine("[ID={0}]", this.PAL.PAListID);

            foreach (ControlRecord cr in this)
            {
                sw.WriteLine(cr.Print());
            }
            sw.Close();
            fs.Close();
        }
        public void WritetoXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("ControlRecordList");
            writer.WriteAttributeString("PAL_ID", this.PAL.PAListID.ToString());
            foreach (ControlRecord cr in this)
            {
                writer.WriteStartElement("ControlRecord");
                writer.WriteAttributeString("Time", cr.Time.ToString());
                writer.WriteAttributeString("Product", cr.Pro.ProID.ToString());
                writer.WriteAttributeString("State", cr.Sta.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }

    /// <summary>
    /// 一次仿真的结果
    /// </summary>
    public class SimRecodData
    {
        public SimRecodData()
        {
            SRD = new SellingRecordData();
            CRD = new ControlRecordData();
        }
        public SellingRecordData SRD {get;set;}
        public ControlRecordData CRD { get; set; }
        //XML文件操作
        public void SaveToXml(string _FileDirectory)
        {
            //判断文件路径是否存在，不存在则创建文件夹 
            if (!System.IO.Directory.Exists(_FileDirectory))
            {
                System.IO.Directory.CreateDirectory(_FileDirectory);//不存在就创建目录 
            } 

            XmlSerializer serializer = new XmlSerializer(typeof(SellingRecordData));
            TextWriter writer = new StreamWriter(_FileDirectory+"SellingRecord.xml",false);
            serializer.Serialize(writer, SRD);
            writer.Close();

            serializer = new XmlSerializer(typeof(ControlRecordData));
            writer = new StreamWriter(_FileDirectory + "ControlRecord.xml",false);
            serializer.Serialize(writer, CRD);
            writer.Close();
        }
    }
    public class SellingRecordData:IXmlSerializable
    {
        private XmlReader _reader;   
        public double AverageRevenue { get; set; }

        public HashSet<SellingRecordList> SrData = new HashSet<SellingRecordList>();
        public void Add(SellingRecordList list)
        {
            this.SrData.Add(list);
        }
        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }
        public void ReadXml(XmlReader reader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SellingRecordList));

            reader.ReadStartElement("SellingRecordData");

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                SellingRecordList sdi = serializer.Deserialize(reader) as SellingRecordList;
                this.SrData.Add(sdi);
            }
            reader.ReadEndElement();
        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("AverageRevenue", AverageRevenue.ToString());

            XmlSerializer serializer = new XmlSerializer(typeof(SellingRecordList));      
            foreach (SellingRecordList pa in this.SrData)
            {
                serializer.Serialize(writer, pa);
            }
        }
    }
    public class ControlRecordData:IXmlSerializable
    {

        public HashSet<ControlRecordList> CrData = new HashSet<ControlRecordList>();
        public void Add(ControlRecordList list)
        {
            this.CrData.Add(list);
        }
        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }
        public void ReadXml(XmlReader reader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ControlRecordList));
            reader.ReadStartElement("ControlRecordList");

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                ControlRecordList crl = serializer.Deserialize(reader) as ControlRecordList;
                this.Add(crl);
            }
            reader.ReadEndElement();
        }
        public void WriteXml(XmlWriter writer)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ControlRecordList));
            foreach (ControlRecordList pa in this.CrData)
            {
                serializer.Serialize(writer, pa);
            }
        }
    }  
}
