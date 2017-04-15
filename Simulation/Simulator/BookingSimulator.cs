using MathNet.Numerics.Random;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks.Schedulers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using com.foxmail.wyyuan1991.NRM.Common;
using System;

namespace com.foxmail.wyyuan1991.NRM.Simulator
{
    /// <summary>
    /// 购票行为仿真器
    /// </summary>
    public class BookingSimulator
    {
        #region Parallel 并行计算支持
        // Create a scheduler 
        protected LimitedConcurrencyLevelTaskScheduler lcts;
        protected CancellationTokenSource cts = new CancellationTokenSource();
        protected List<Task> tasks = new List<Task>();
        // Create a TaskFactory and pass it our custom scheduler. 
        protected TaskFactory factory;
        private int m_NumOfThreads = 2;
        public int NumOfThreads
        {
            get { return m_NumOfThreads; }
            set
            {
                m_NumOfThreads = value;
                lcts = new LimitedConcurrencyLevelTaskScheduler(m_NumOfThreads);
                factory = new TaskFactory(lcts);
            }
        }
        #endregion 
        //增加配置输入输出等信息

        System.Random rng = SystemRandomSource.Default;//随机数发生器

        public IMarket MarketInfo { get; set; }
        public IResourceSet ResourceSpace { get; set; }
        public IController Controller { get; set; }
        public ResouceState InitState { get; set; }

        #region SetOut
        private TextWriter m_TextWriter;
        private void print(string str)
        {
            if (this.m_TextWriter != null)
            {
                m_TextWriter.WriteLine(str);
            }
        }
        private void print(string format, params object[] arg)
        {
            if (this.m_TextWriter != null)
            {
                m_TextWriter.WriteLine(format, arg);
            }
        }
        public TextWriter SimTextWriter
        {
            get
            {
                return m_TextWriter;
            }
            set
            {
                m_TextWriter = value;
            }
        }//输出
        #endregion

        public void BatchProcess(PrimalArrivalData Pad, string path)
        {
            //判断文件路径是否存在，不存在则创建文件夹 
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);//不存在就创建目录 
            }
            XmlTextWriter writer1 = new XmlTextWriter(path + "SellingRecord.xml", null);
            XmlTextWriter writer2 = new XmlTextWriter(path + "ControlRecord.xml", null);

            //写入根元素
            writer1.WriteStartElement("SellingRecordData");
            writer2.WriteStartElement("ControlRecordData");

            //TODO 限制任务数量
            while (Pad.ReadNextPAL())
            {
                PrimalArrivalList pal = Pad.GetCurPAL();
                Task ta = factory.StartNew(() =>
                {
                    SellingRecordList Srlist;
                    ControlRecordList Crlist;
                    Process(pal, out Srlist, out Crlist);
                    lock (writer1)
                    {
                        Srlist.WritetoXml(writer1);
                    }
                    lock (writer2)
                    {
                        Crlist.WritetoXml(writer2);
                    }
                    //SimData.CRD.Add(Crlist);
                    //SimData.SRD.Add(Srlist);
                }, cts.Token);
                tasks.Add(ta);
                if (tasks.Count >= 128)
                {
                    Task.WaitAll(tasks.ToArray());
                    tasks.Clear();
                }
            }
            Task.WaitAll(tasks.ToArray());
            tasks.Clear();

            writer1.WriteEndElement();
            writer2.WriteEndElement();
            //将XML写入文件并且关闭XmlTextWriter
            writer1.Close();
            writer2.Close();
        }
        public void BatchProcess(string inputDir, string outputDir)
        {
            DirectoryInfo folder = new DirectoryInfo(inputDir);

            int n = 0;
            int total = folder.GetFiles("*.arr").Length;
            int step = (int)Math.Ceiling((double)folder.GetFiles("*.arr").Length / 10000);
            Console.Write("进度 000.00%");
            foreach (FileInfo f in folder.GetFiles("*.arr"))
            {
                FileInfo file = f;//防止在VS2010下报错
                PrimalArrivalList pal = new PrimalArrivalList();
                pal.ReadFromArrFile(file.FullName);
                Task ta = factory.StartNew(() =>
                {
                    SellingRecordList Srlist;
                    ControlRecordList Crlist;
                    Process(pal, out Srlist, out Crlist);
                    //判断文件路径是否存在，不存在则创建文件夹 
                    if (!System.IO.Directory.Exists(outputDir + @"\Sell"))
                    {
                        System.IO.Directory.CreateDirectory(outputDir + @"\Sell");//不存在就创建目录 
                    }
                    Srlist.WriteToFile(outputDir + @"\Sell\" + pal.PAListID + ".sr");
                    if (!System.IO.Directory.Exists(outputDir + @"\Control"))
                    {
                        System.IO.Directory.CreateDirectory(outputDir + @"\Control");//不存在就创建目录 
                    }
                    Crlist.WriteToFile(outputDir + @"\Control\" + pal.PAListID + ".cr");
                    if (n++ % step == 0)
                    {
                        lock (folder)
                        {
                            Console.SetCursorPosition(Console.CursorLeft - 7, Console.CursorTop);
                            Console.Write("{0}%", String.Format("{0:000.00}", Math.Round(((double)n / (double)total), 4) * 100));
                        }
                    }
                }, cts.Token);
                tasks.Add(ta);
            }
            Task.WaitAll(tasks.ToArray());
            tasks.Clear();
            Console.SetCursorPosition(Console.CursorLeft - 7, Console.CursorTop);
            Console.WriteLine("100.00 %  完成！");
        }
        public void Process(string inputDir, string outputDir)
        {
            DirectoryInfo folder = new DirectoryInfo(inputDir);

            int n = 0;
            int total = folder.GetFiles("*.arr").Length;
            int step = (int)Math.Ceiling((double)folder.GetFiles("*.arr").Length / 10000);
            Console.Write("进度 000.00%");
            foreach (FileInfo f in folder.GetFiles("*.arr"))
            {
                FileInfo file = f;//防止在VS2010下报错
                PrimalArrivalList pal = new PrimalArrivalList();
                pal.ReadFromArrFile(file.FullName);

                SellingRecordList Srlist;
                ControlRecordList Crlist;
                Process(pal, out Srlist, out Crlist);
                //判断文件路径是否存在，不存在则创建文件夹 
                if (!System.IO.Directory.Exists(outputDir + @"\Sell"))
                {
                    System.IO.Directory.CreateDirectory(outputDir + @"\Sell");//不存在就创建目录 
                }
                Srlist.WriteToFile(outputDir + @"\Sell\" + pal.PAListID + ".sr");
                if (!System.IO.Directory.Exists(outputDir + @"\Control"))
                {
                    System.IO.Directory.CreateDirectory(outputDir + @"\Control");//不存在就创建目录 
                }
                Crlist.WriteToFile(outputDir + @"\Control\" + pal.PAListID + ".cr");
                if (n++ % step == 0)
                {
                    lock (folder)
                    {
                        Console.SetCursorPosition(Console.CursorLeft - 7, Console.CursorTop);
                        Console.Write("{0}%", String.Format("{0:000.00}", Math.Round(((double)n / (double)total), 4) * 100));
                    }
                }
            }

            Console.SetCursorPosition(Console.CursorLeft - 7, Console.CursorTop);
            Console.WriteLine("100.00 %  完成！");
        }
        public bool Process(PrimalArrivalList arr, out SellingRecordList Srlist, out ControlRecordList Crlist)
        {
            IConOL conol = Controller.GenConOL();
            Srlist = new SellingRecordList();
            Crlist = new ControlRecordList();
            Srlist.PAL = arr; Crlist.PAL = arr;

            ResouceState rs = new ResouceState(InitState);
            if (conol != null) conol.Update(rs);
            List<IProduct> openProductList = Controller.OpenProductList(0, rs, conol);
            Crlist.UpdateOpenProducts(0, openProductList);
            for (int i = 0; i < arr.Count; i++)
            {
                //生成开放产品集
                openProductList = Controller.OpenProductList(arr[i].ArriveTime, rs, conol);
                //模拟旅客购票
                List<IProduct> pro = (MarketInfo[arr[i].IndexOfMS] as IChoiceAgent).Select(openProductList, rng.NextDouble());
                if (pro != null)
                {
                    //出票
                    List<Ticket> tickets = Controller.PrintTickets(rs, pro, conol);
                    //更新资源状态
                    rs.UpdateAfterSelling(tickets);
                    if(conol!=null)conol.Update(rs);
                    //记录产品情况
                    Crlist.UpdateOpenProducts(arr[i].ArriveTime, openProductList);
                    //加入SellingRecordList
                    Srlist.AddRecord(arr[i].ArriveTime, arr[i], pro,tickets);
                }
            }
            return true;
        }
    }
}
