using com.foxmail.wyyuan1991.NRM.Common;
using com.foxmail.wyyuan1991.NRM.RailwayModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;

namespace com.foxmail.wyyuan1991.NRM.Simulator
{
    public class SimAnalysisor
    {
        public SimAnalysisor()
        {
            List<int> _highPriceProduct = new List<int>() { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200};
            this.IndexList.Add(new SimStatic()
            {
                Name = "平均购票人数",
                Cal = (PrimalArrivalList pal, SellingRecordList srl, ControlRecordList crl) =>
                {
                    return srl.Count;
                },
                IsAvg = true
            });
            this.IndexList.Add(new SimStatic()
            {
                Name = "平均离开人数",
                Cal = (PrimalArrivalList pal, SellingRecordList srl, ControlRecordList crl) =>
                {
                    return pal.Count - srl.Count;
                },
                IsAvg = true
            });
            this.IndexList.Add(new SimStatic()
            {
                Name = "换乘比例",
                Cal = (PrimalArrivalList pal, SellingRecordList srl, ControlRecordList crl) =>
                {
                    return (double)srl.Count(i => i.Product.Count > 1) / (double)srl.Count;
                },
                IsAvg = true
            });
            this.IndexList.Add(new SimStatic()
            {
                Name = "平均收入",
                Cal = (PrimalArrivalList pal, SellingRecordList srl, ControlRecordList crl) =>
                {
                    return srl.Sum(i => i.Product.Sum(j => j.Fare));
                },
                IsAvg = true
            });
            this.IndexList.Add(new SimStatic()
            {
                Name = "换乘收入占比",
                Cal = (PrimalArrivalList pal, SellingRecordList srl, ControlRecordList crl) =>
                {
                    return srl.Where(i => i.Product.Count > 1).Sum(j => j.Product.Sum(k => k.Fare)) / srl.Sum(i => i.Product.Sum(j => j.Fare));
                },
                IsAvg = true
            });
            this.IndexList.Add(new SimStatic()
            {
                Name = "高价格产品占比",
                Cal = (PrimalArrivalList pal, SellingRecordList srl, ControlRecordList crl) =>
                {
                    return srl.Where(i => i.Product.Count == 1&& _highPriceProduct.Contains(i.Product[0].ProID)).Sum(j => j.Product.Sum(k => k.Fare)) / srl.Sum(i => i.Product.Sum(j => j.Fare));
                },
                IsAvg = true
            });
            this.IndexList.Add(new SimStatic()
            {
                Name = "资源剩余比例",
                Cal = (PrimalArrivalList pal, SellingRecordList srl, ControlRecordList crl) =>
                {
                    double b = this.ResourceSpace.Sum(i => this.InitState.ResDic[i]);
                    double a = srl.Sum(i => i.Product.Sum(j => j.Count()));
                    return (b - a) / b;
                },
                IsAvg = true
            });
            //this.IndexList.Add(new SimStatic()
            //{
            //    Name = "剩余资源列表",
            //    TCal = (PrimalArrivalList pal, SellingRecordList srl, ControlRecordList crl) =>
            //    {
            //        List<double> list = new List<double>();
            //        foreach (IResource r in this.ResourceSpace)
            //        {
            //            list.Add(r.Capacity - srl.Sum(i => i.Product.Exists(j => j.Contains(r)) ? 1 : 0));
            //        }
            //        return list;
            //    },
            //    IsAvg = true
            //});
        }
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

        public IMarket MarketInfo { get; set; }
        public IResourceSet ResourceSpace { get; set; }
        public IProductSet ProSpace { get; set; }
        public MetaResouceState InitState { get; set; }

        public List<SimStatic> IndexList = new List<SimStatic>();

        public void PrintHead(string outpupath)
        {
            //输出
            StreamWriter sw = new StreamWriter(outpupath, true);
            sw.Write("\r\nArr文件地址,");
            foreach (SimStatic s in IndexList)
            {
                if (IndexList.IndexOf(s) < IndexList.Count - 1)
                {
                    sw.Write("{0},", s.Name);
                }
                else
                {
                    sw.Write("{0}", s.Name);
                }
            }
            sw.Close();
        }
        public void Dowork(string arrpath, string srpath, string ctlpath, string outpupath)
        {
            DirectoryInfo arrFolder = new DirectoryInfo(arrpath);
            DirectoryInfo srFolder = new DirectoryInfo(srpath);
            DirectoryInfo ctlFolder = new DirectoryInfo(ctlpath);

            FileInfo[] sr = srFolder.GetFiles();
            FileInfo[] ctl = ctlFolder.GetFiles();

            Dictionary<SimStatic, double> dic = new Dictionary<SimStatic, double>();
            Dictionary<SimStatic, List<double>> listDic = new Dictionary<SimStatic, List<double>>();
            foreach (SimStatic s in IndexList)
            {
                if (s.Cal != null)
                {
                    dic.Add(s, 0);
                }
                else if (s.TCal != null)
                {
                    listDic.Add(s, new List<double>());
                }
            }

            int n = 0;
            int total = arrFolder.GetFiles("*.arr").Length;
            int step = (int)Math.Ceiling((double)arrFolder.GetFiles("*.arr").Length / 10000);
            Console.Write("进度 000.00%");
            foreach (FileInfo file in arrFolder.GetFiles("*.arr"))
            {
                Task ta = factory.StartNew(() =>
                {
                    PrimalArrivalList pal = new PrimalArrivalList();
                    pal.ReadFromArrFile(file.FullName);

                    FileInfo srFile = sr.FirstOrDefault(i => i.Name == file.Name.Substring(0, file.Name.IndexOf('.')) + ".sr");
                    FileInfo ctlFile = ctl.FirstOrDefault(i => i.Name == file.Name.Substring(0, file.Name.IndexOf('.')) + ".cr");

                    ControlRecordList crl = GenCrl(ctlFile, pal);
                    SellingRecordList srl = GenSrl(srFile, pal);

                    lock (dic)
                    {
                        foreach (SimStatic s in IndexList)
                        {
                            if (s.Cal != null)
                            {
                                dic[s] += s.Cal(pal, srl, crl);
                            }
                            else if (s.TCal != null)
                            {
                                List<double> temp = s.TCal(pal, srl, crl);
                                for (int i = 0; i < temp.Count; i++)
                                {
                                    if (listDic[s].Count <= i) listDic[s].Add(0);
                                    listDic[s][i] += temp[i];
                                }
                            }
                        }
                    }

                    if (n++ % step == 0)
                    {
                        lock (arrFolder)
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
            //输出
            StreamWriter sw = new StreamWriter(outpupath, true);
            sw.Write("\r\n"+arrFolder.FullName + ",");
            foreach (SimStatic s in IndexList)
            {
                if (s.Cal != null)
                {
                    if (s.IsAvg)
                    {
                        dic[s] = dic[s] / (double)total;
                    }
                    if (IndexList.IndexOf(s) < IndexList.Count - 1)
                    {
                        sw.Write("{0},", dic[s]);
                    }
                    else
                    {
                        sw.Write("{0}", dic[s]);
                    }
                }
                else if (s.TCal != null)
                {
                    if (s.IsAvg)
                    {
                        for (int i = 0; i < listDic[s].Count; i++)
                        {
                            listDic[s][i] = listDic[s][i] / (double)total;
                        }
                    }
                    sw.WriteLine();
                    sw.WriteLine(s.Name);
                    for (int i = 0; i < listDic[s].Count; i++)
                    {
                        sw.WriteLine("{0},", listDic[s][i]);
                    }
                }
            }
            sw.Close();
        }

        private SellingRecordList GenSrl(FileInfo srFile, PrimalArrivalList pal)
        {
            SellingRecordList srl = new SellingRecordList();
            //c#文件流读文件            
            using (FileStream fsRead = srFile.OpenRead())
            {
                StreamReader sr = new StreamReader(fsRead, System.Text.Encoding.Default);
                //读文件头
                string s = sr.ReadLine();
                s = s.Substring(1, s.Length - 2);
                string[] ss = s.Split(new char[] { ',', '=' });
                if (ss[0] == "ID") srl.PAL = pal;
                while (!sr.EndOfStream)
                {
                    s = sr.ReadLine();
                    ss = s.Split(new char[] { ';' });
                    string[] sss = ss[1].Split(new char[] { ',' });
                    SellingRecord sellrecord = new SellingRecord();
                    sellrecord.Product = new List<IProduct>();
                    sellrecord.ArriveTime = Convert.ToInt32(ss[0]);
                    foreach (string ssss in sss)
                    {
                        sellrecord.Product.Add(this.ProSpace.FirstOrDefault(i => i.ProID == Convert.ToInt32(ssss)));
                    }
                    srl.Add(sellrecord);
                }
            }
            return srl;
        }
        private ControlRecordList GenCrl(FileInfo crFile, PrimalArrivalList pal)
        {
            ControlRecordList crl = new ControlRecordList();
            //c#文件流读文件            
            using (FileStream fsRead = crFile.OpenRead())
            {
                StreamReader sr = new StreamReader(fsRead, System.Text.Encoding.Default);
                //读文件头
                string s = sr.ReadLine();
                s = s.Substring(1, s.Length - 2);
                string[] ss = s.Split(new char[] { ',', '=' });
                if (ss[0] == "ID") crl.PAL = pal;
                while (!sr.EndOfStream)
                {
                    s = sr.ReadLine();
                    ss = s.Split(new char[] { ';' });

                    ControlRecord controlRecord = new ControlRecord();
                    controlRecord.Time = Convert.ToInt32(ss[0]);
                    controlRecord.Pro = this.ProSpace.FirstOrDefault(i => i.ProID == Convert.ToInt32(ss[1]));
                    controlRecord.Sta = ss[2] == "1" ? ProductState.Open : ProductState.Closed;
                    crl.Add(controlRecord);
                }
            }
            return crl;
        }
    }

    public delegate double CalMethod(PrimalArrivalList pal, SellingRecordList srl, ControlRecordList crl);
    public delegate List<double> ListCalMethod(PrimalArrivalList pal, SellingRecordList srl, ControlRecordList crl);
    public class SimStatic
    {
        public string Name { get; set; }
        public CalMethod Cal { get; set; }
        public ListCalMethod TCal { get; set; }
        public bool IsAvg { get; set; }
    }
}
