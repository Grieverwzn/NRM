using com.foxmail.wyyuan1991.NRM.Data;
using com.foxmail.wyyuan1991.NRM.RailwayModel;
using com.foxmail.wyyuan1991.NRM.RailwaySolver;
using com.foxmail.wyyuan1991.NRM.Simulator;
using ILOG.CPLEX;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RailwayNRM
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            //args = new string[] { "1", @"F:\Data\2\Net1.xls", @"F:\Data\2\test.txt" };
            //args = new string[] { "2", @"C:\Users\YuanWuyang\Desktop\Net1.xls", "20", @"C:\Users\YuanWuyang\Desktop\testarr.xml" };
            //args = new string[] { "4", @"C:\Users\YuanWuyang\Desktop\Net1.xls", @"C:\Users\YuanWuyang\Desktop\testarr.xml", @"C:\Users\YuanWuyang\Desktop\test.txt", @"C:\Users\YuanWuyang\Desktop\BPC\" };

            //args = new string[] { "2.1", @"D:\Res\Case2\5-0.05-500.xls" ,@"D:\Res\Case2\5-0.05-500\bpc_5-0.05-500.txt" };
            //args = new string[] { "4", @"C:\Users\YuanWuyang\Desktop\Net1.xls", @"C:\Users\YuanWuyang\Desktop\testarr.xml", @"C:\Users\YuanWuyang\Desktop\test.txt", @"C:\Users\YuanWuyang\Desktop\BPC\" };
            //args = new string[] { "1.1", @"C:\Users\YuanWuyang\Desktop\Net1.xls", @"C:\Users\YuanWuyang\Desktop\test.txt", "8","CDLP,DD"};

            //args = new string[] { "3", @"D:\Res\Case4\5-0.01-500.xls", @"D:\Res\Case4\5-0.01-500\5-0.01-500arr.xml", @"D:\Res\Case2\5-0-500\bpc_5-0-500.txt", @"D:\Res\Case4\5-0.01-500\BPC-0\" };
            //,@"D:\Res\Case3\5-0.025-500\BPC\" };

            //args = new string[] { "2", @"F:\Data\1\500-0.05-150000.xls", "20", @"F:\Data\1\arr\" };
            //args = new string[] { "3", @"F:\Data\1\500-0.05-150000\500-0.05-150000.xls", @"D:\Res\1\500-0.05-150000\testarr.xml", @"D:\Res\1\500-0.05-150000\bpc_500-0.05-150000.txt", @"D:\Res\1\500-0.05-150000\BPC\" };
            args = new string[] { "4", @"F:\Data\1\500-0.05-150000.xls", @"D:\Res\1\arr\0.arr", @"D:\Res\1\500-0.05-150000\bpc_500-0.05-150000.txt", @"D:\Res\1\500-0.05-150000\BPC\" };
            /* 通过参数使用此程序 args[0]为选择功能
             * 1 计算得到一个投标价格控制策略 
             * 2 生成仿真序列
             * 3 投标价格控制仿真
             */
#endif
            if (args.Length == 0)
            {
                Console.WriteLine("仅支持参数启动！");
                Console.WriteLine("1 计算得到一个投标价格控制策略 ");
                Console.WriteLine("1.1 计算三种方式得到的结果");
                Console.WriteLine("2 生成仿真序列");
                Console.WriteLine("2.1 计算loadFactor");
                Console.WriteLine("3 投标价格控制仿真");
                Console.WriteLine("3.1 开放全部产品控制仿真");
                Console.WriteLine("4 batch 投标价格控制仿真");
                Console.WriteLine("4.1 batch 开放全部产品控制仿真");
                Console.WriteLine("按回车退出...");
                Console.ReadLine();
                return;
            }
            try
            {
                switch (args[0])
                {
                    case "1": computebpc(args); //
                        break;
                    case "1.1": computebpc_1_1(args); //
                        break;
                    case "2": genarrival(args);//
                        break;
                    case "2.1": loadfactor(args);//
                        break;
                    case "3": simBPC(args);//
                        break;
                    case "3.1": simOAC(args);//
                        break;
                    case "4": batchsimBPC(args);//
                        break;
                    case "4.1": batchsimOAC(args);//
                        break;
                    default:
                        {
                            Console.WriteLine("按回车退出...");
                            Console.ReadLine();
                        }
                        break;
                }
#if DEBUG
                Console.ReadLine();
#endif
            }
            catch (Exception e)
            {
                Console.WriteLine("出现错误:{0}", e.StackTrace);
                Console.ReadLine();
            }
        }


        //计算得到一个投标价格控制策略 
        //args[1]：数据表 .xls
        //args[2] : 输出 .txt
        static void computebpc(string[] args)
        {
            string path1 = args[1];
            string path2 = args[2];

            RailDataReader dg = new RailDataReader();
            dg.ReadXLS(path1);

            NRMDataAdapter da = new NRMDataAdapter();
            da.MarketInfo = dg.mar;
            da.ProSpace = dg.proset;
            da.ResSpace = dg.ResSet;
            da.pathList = dg.pathList;
            da.InitialState = da.CreateOrFind(da.GenInitialState());
            da.TimeHorizon = dg.TimeHorizon;

            RailwayNRMSolver_DD DDSolver = new RailwayNRMSolver_DD();
            DDSolver.NumOfThreads = 8;
            DDSolver.Tolerance = 1e-2;
            DDSolver.step = da.RS.Count;
            DDSolver.SolverTextWriter = Console.Out;
            DDSolver.Data = da;
            DDSolver.Solve();
            DDSolver.SaveBidPrice(path2);

            Report(DDSolver.RMPModel, Console.Out, 0, "BPC");
        }

        //args[1]：数据表 .xls
        //args[2] : 输出 .txt
        //args[2] : 最多使用线程数
        static void computebpc_1_1(string[] args)
        {
            RailDataReader dg = new RailDataReader();
            dg.ReadXLS(args[1]);

            NRMDataAdapter da = new NRMDataAdapter();
            da.MarketInfo = dg.mar;
            da.ProSpace = dg.proset;
            da.ResSpace = dg.ResSet;
            da.pathList = dg.pathList;
            da.InitialState = da.CreateOrFind(da.GenInitialState());
            da.TimeHorizon = dg.TimeHorizon;

            Console.WriteLine("--------问题生成结束--------");
            Console.WriteLine("--------资源数量:{0}--------", da.ResSpace.Count);
            Console.WriteLine("--------产品数量:{0}--------", da.ProSpace.Count);
            Console.WriteLine("--------路径数量:{0}--------", dg.pathList.Count);
            Console.WriteLine("--------市场数量:{0}--------", da.MarketInfo.Count);
            Console.WriteLine("--------时段数量:{0}--------", da.TimeHorizon);

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(args[2], false))
            {
                Stopwatch sw = new Stopwatch();
                if (args[4].IndexOf("CDLP") >= 0)
                {
                    RailwayNRMSolver_GCDLP d = new RailwayNRMSolver_GCDLP(); d.NumOfThreads = Convert.ToInt32(args[3]); d.Tolerance = 1e-2;
                    d.SolverTextWriter = Console.Out;
                    d.Data = da;
                    sw.Start();
                    d.Solve();
                    sw.Stop();
                    Report(d.RMPModel, file, sw.ElapsedMilliseconds, "CDLP");
                }
                if (args[4].IndexOf("CLP") >= 0)
                {
                    RailwayNRMSolver_CLP_v2 e = new RailwayNRMSolver_CLP_v2(); e.NumOfThreads = Convert.ToInt32(args[3]); e.Tolerance = 1e-2;
                    e.SolverTextWriter = Console.Out;
                    sw.Reset();
                    e.Data = da;
                    sw.Start();
                    e.Solve();
                    sw.Stop();
                    Report(e.RMPModel, file, sw.ElapsedMilliseconds, "CLP");
                }
                if (args[4].IndexOf("DD") >= 0)
                {
                    RailwayNRMSolver_DD f = new RailwayNRMSolver_DD(); f.NumOfThreads = Convert.ToInt32(args[3]); f.Tolerance = 1e-2; f.step = da.RS.Count;
                    sw.Reset();
                    f.SolverTextWriter = Console.Out;
                    f.Data = da;
                    sw.Start();
                    f.Solve();
                    sw.Stop();
                    Report(f.RMPModel, file, sw.ElapsedMilliseconds, "DD");
                }
            }
        }

        //args[1]：数据表 .xls
        //args[2] :  采集次数
        //args[3] :  输出文件 .xml
        static void genarrival(string[] args)
        {

            Console.WriteLine("{0}生成开始", System.DateTime.Now.ToLongTimeString());
            string path = args[3];
            RailDataReader dg = new RailDataReader();
            dg.ReadXLS(args[1]);

            NRMDataAdapter da = new NRMDataAdapter();
            da.MarketInfo = dg.mar;
            da.ProSpace = dg.proset;
            da.ResSpace = dg.ResSet;
            da.pathList = dg.pathList;
            da.InitialState = da.CreateOrFind(da.GenInitialState());
            da.TimeHorizon = dg.TimeHorizon;

            PrimalArrivalData pad = new PrimalArrivalData();//到达数据
            ArrivalSimulator arr = new ArrivalSimulator()  //到达序列生成器
            {
                MaxLamada = 0,
                TimeHorizon = dg.TimeHorizon,
                MarketInfo = dg.mar
            };

            //判断文件路径是否存在，不存在则创建文件夹 
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);//不存在就创建目录 
            }
            //XmlTextWriter writer = new XmlTextWriter(args[3], null);
            //writer.WriteStartElement("PrimalArrivalData");
            int num = 0;
            int total = Convert.ToInt32(args[2]);
            Parallel.For(0, total, (i, loopState) =>
            {
            //pad.Data.Add(arr.Gen(i));
            PrimalArrivalList pal = arr.Gen(i);
            pal.WriteToArrFile(path + "" + pal.PAListID + ".arr");
#if DEBUG
            
            if (num++ % 1 == 0)
            {
                Console.WriteLine("进度 {0}% 完毕", Math.Round((double)num / (double)total * 100), 2);
            }
                
#endif

                //lock (writer)
                //{
                //    pal.WriteToXml(writer);
                //}
            });
            //writer.WriteEndElement();
            //将XML写入文件并且关闭XmlTextWriter
            //writer.Close();

            Console.WriteLine("{0}生成结束", DateTime.Now.ToLongTimeString());

            // pad.WriteToXml(args[3]);

        }

        //args[1]：数据表 .xls
        //args[2] :  控制策略.txt
        private static void loadfactor(string[] args)
        {
            RailDataReader dg = new RailDataReader();
            dg.ReadXLS(args[1]);

            NRMDataAdapter data = new NRMDataAdapter()
            {
                MarketInfo = dg.mar,
                ProSpace = dg.proset,
                ResSpace = dg.ResSet,
                pathList = dg.pathList,
                TimeHorizon = dg.TimeHorizon
            };

            BidPriceController BPC = new BidPriceController()
            {
                DataAdapter = data
            };
            BPC.ReadFromTXT(args[2]);

            Console.WriteLine("{0}完成计算！LoadFactor:{1}", System.DateTime.Now.ToLongTimeString(), BPC.loadFactor());
        }

        //args[1]：数据表 .xls
        //args[2] :  到达序列 .xml
        //args[3] :  控制策略 .txt
        //args[4] :  输出目录 
        static void simBPC(string[] args)
        {
            RailDataReader dg = new RailDataReader();
            dg.ReadXLS(args[1]);

            NRMDataAdapter data = new NRMDataAdapter()
            {
                MarketInfo = dg.mar,
                ProSpace = dg.proset,
                ResSpace = dg.ResSet,
                pathList = dg.pathList,
                TimeHorizon = dg.TimeHorizon
            };
            data.InitialState = data.CreateOrFind(data.GenInitialState());

            PrimalArrivalData pad = new PrimalArrivalData();//到达数据
            pad.LoadFromXml(args[2]);

            SellingRecordList sr;//售票记录
            ControlRecordList cr;//控制记录
            SimRecodData SimData = new SimRecodData();

            BookingSimulator simulator = new BookingSimulator()
            {
                MarketInfo = dg.mar,
                ResourceSpace = dg.ResSet
            };

            BidPriceController BPC = new BidPriceController()
            {
                DataAdapter = data
            };
            BPC.ReadFromTXT(args[3]);

            //OpenAllStrategy OAS = new OpenAllStrategy()
            //{
            //    DataAdapter = data
            //};
            simulator.Controller = BPC;

            int num = 1;
            double exp = 0;
            foreach (PrimalArrivalList pa in pad.Data)
            {
                simulator.Process(pa, out sr, out cr);
                exp += sr.Revenue();
                SimData.CRD.Add(cr);
                SimData.SRD.Add(sr);
                num++;
            }
            SimData.SRD.AverageRevenue = exp / (num - 1);
            Console.WriteLine("{0}完成仿真！平均收益:{1}", System.DateTime.Now.ToLongTimeString(), SimData.SRD.AverageRevenue);
            SimData.SaveToXml(args[4]);
        }

        //args[1]：数据表 .xls
        //args[2] :  到达序列 .xml
        //args[3] :  输出目录 
        static void simOAC(string[] args)
        {
            RailDataReader dg = new RailDataReader();
            dg.ReadXLS(args[1]);

            NRMDataAdapter data = new NRMDataAdapter()
            {
                MarketInfo = dg.mar,
                ProSpace = dg.proset,
                ResSpace = dg.ResSet,
                pathList = dg.pathList,
                TimeHorizon = dg.TimeHorizon
            };
            data.InitialState = data.CreateOrFind(data.GenInitialState());

            PrimalArrivalData pad = new PrimalArrivalData();//到达数据
            pad.LoadFromXml(args[2]);

            SellingRecordList sr;//售票记录
            ControlRecordList cr;//控制记录
            SimRecodData SimData = new SimRecodData();
            BookingSimulator simulator = new BookingSimulator()
            {
                MarketInfo = dg.mar,
                ResourceSpace = dg.ResSet
            };

            OpenAllStrategy OAS = new OpenAllStrategy()
            {
                DataAdapter = data
            };
            simulator.Controller = OAS;

            int num = 1;
            double exp = 0;
            foreach (PrimalArrivalList pa in pad.Data)
            {
                simulator.Process(pa, out sr, out cr);
                exp += sr.Revenue();
                SimData.CRD.Add(cr);
                SimData.SRD.Add(sr);
                num++;
            }
            SimData.SRD.AverageRevenue = exp / (num - 1);
            Console.WriteLine("{0}完成仿真！平均收益:{1}", System.DateTime.Now.ToLongTimeString(), SimData.SRD.AverageRevenue);
            SimData.SaveToXml(args[3]);
        }

        static void batchsimBPC(string[] args)
        {
            Console.WriteLine("{0}仿真开始", System.DateTime.Now.ToLongTimeString());
            RailDataReader dg = new RailDataReader();
            dg.ReadXLS(args[1]);

            NRMDataAdapter data = new NRMDataAdapter()
            {
                MarketInfo = dg.mar,
                ProSpace = dg.proset,
                ResSpace = dg.ResSet,
                pathList = dg.pathList,
                TimeHorizon = dg.TimeHorizon
            };
            data.InitialState = data.CreateOrFind(data.GenInitialState());

            //PrimalArrivalList pal = new PrimalArrivalList();
            //pal.ReadFromArrFile(@"F:\Data\1\arr\1.arr");

            PrimalArrivalData pad = new PrimalArrivalData();//到达数据           
            pad.ReadXml(args[2]);
            BookingSimulator simulator = new BookingSimulator()
            {
                MarketInfo = dg.mar,
                ResourceSpace = dg.ResSet,
                NumOfThreads = 4
            };

            BidPriceController BPC = new BidPriceController()
            {
                DataAdapter = data
            };
            BPC.ReadFromTXT(args[3]);
            simulator.Controller = BPC;

            simulator.BatchProcess(pad, args[4]);
            Console.WriteLine("{ 0}仿真结束", System.DateTime.Now.ToLongTimeString());
            //SimData.SRD.AverageRevenue = SimData.SRD.SrData.Sum(i=>i.Revenue()) / SimData.SRD.SrData.Count;
            //Console.WriteLine("{0}完成仿真！平均收益:{1}", System.DateTime.Now.ToLongTimeString(), SimData.SRD.AverageRevenue);
            //SimData.SaveToXml(args[4]);
        }
        static void batchsimOAC(string[] args)
        {
            Console.WriteLine("{0}仿真开始", System.DateTime.Now.ToLongTimeString());
            RailDataReader dg = new RailDataReader();
            dg.ReadXLS(args[1]);

            NRMDataAdapter data = new NRMDataAdapter()
            {
                MarketInfo = dg.mar,
                ProSpace = dg.proset,
                ResSpace = dg.ResSet,
                pathList = dg.pathList,
                TimeHorizon = dg.TimeHorizon
            };
            data.InitialState = data.CreateOrFind(data.GenInitialState());

            PrimalArrivalData pad = new PrimalArrivalData();//到达数据
            pad.LoadFromXml(args[2]);

            BookingSimulator simulator = new BookingSimulator()
            {
                MarketInfo = dg.mar,
                ResourceSpace = dg.ResSet,
                NumOfThreads = 4
            };

            OpenAllStrategy OAS = new OpenAllStrategy()
            {
                DataAdapter = data
            };
            simulator.Controller = OAS;

            simulator.BatchProcess(pad, args[3]);

            // SimData.SRD.AverageRevenue = SimData.SRD.SrData.Sum(i => i.Revenue()) / SimData.SRD.SrData.Count;
            //Console.WriteLine("{0}完成仿真！平均收益:{1}", System.DateTime.Now.ToLongTimeString(), SimData.SRD.AverageRevenue);
            // SimData.SaveToXml(args[3]);
        }
        static void ssss()
        {
            Console.WriteLine("--------请输入数据EXCEL地址：--------");
            string path = Console.ReadLine();

            RailDataReader dg = new RailDataReader();
            dg.ReadXLS(path);

            //RailDataGenerator dg = new RailDataGenerator();
            //dg.ReadXLS(@"D:\code\PlanRailReservationSim\NRMSolver\Data\data_s.xls");
            //dg.GenTimetable();
            //dg.GenResSpace();
            //dg.GenProducts();
            //dg.GenMarket();
            //dg.GenRoute();

            NRMDataAdapter da = new NRMDataAdapter();
            da.MarketInfo = dg.mar;
            da.ProSpace = dg.proset;
            da.ResSpace = dg.ResSet;
            da.pathList = dg.pathList;
            da.InitialState = da.CreateOrFind(da.GenInitialState());
            da.TimeHorizon = dg.TimeHorizon;

            Console.WriteLine("--------问题生成结束--------");
            Console.WriteLine("--------资源数量:{0}--------", da.ResSpace.Count);
            Console.WriteLine("--------产品数量:{0}--------", da.ProSpace.Count);
            Console.WriteLine("--------路径数量:{0}--------", dg.pathList.Count);
            Console.WriteLine("--------市场数量:{0}--------", da.MarketInfo.Count);
            Console.WriteLine("--------时段数量:{0}--------", da.TimeHorizon);

            //RailwayNRMSolver_GCDLP d = new RailwayNRMSolver_GCDLP(); ; d.NumOfThreads = 8; d.Tolerance = 1e-2;
            //RailwayNRMSolver_CLP_v2 d = new RailwayNRMSolver_CLP_v2(); ; d.NumOfThreads = 8; d.Tolerance = 1e-2;
            //RailwayNRMSolver_DD d = new RailwayNRMSolver_DD(); d.NumOfThreads = 8; d.Tolerance = 1e-2; d.step = da.RS.Count;
            //Console.WriteLine("--------开始求解--------");

            //f.SetMDPProject(d);
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"D:\output.txt", false))
            {
                Stopwatch sw = new Stopwatch();

                RailwayNRMSolver_GCDLP d = new RailwayNRMSolver_GCDLP(); d.NumOfThreads = 8; d.Tolerance = 1e-2;
                d.SolverTextWriter = Console.Out;
                d.Data = da;
                sw.Start();
                d.Solve();
                sw.Stop();
                file.WriteLine("求解时间:{0}", sw.ElapsedMilliseconds);
                //Report(d.RMPModel, file);

                RailwayNRMSolver_CLP_v2 e = new RailwayNRMSolver_CLP_v2(); e.NumOfThreads = 8; e.Tolerance = 1e-2;
                e.SolverTextWriter = Console.Out;
                sw.Reset();
                e.Data = da;
                sw.Start();
                e.Solve();
                sw.Stop();
                file.WriteLine("求解时间:{0}", sw.ElapsedMilliseconds);
                //Report(e.RMPModel, file);

                RailwayNRMSolver_DD f = new RailwayNRMSolver_DD(); f.NumOfThreads = 8; f.Tolerance = 1e-2; f.step = da.RS.Count;
                sw.Reset();
                f.SolverTextWriter = Console.Out;
                f.Data = da;
                sw.Start();
                f.Solve();
                sw.Stop();
                file.WriteLine("求解时间:{0}", sw.ElapsedMilliseconds);
                //Report(f.RMPModel, file);
            }
            Console.WriteLine("-------- Press <Enter> to Exit --------");
            Console.ReadLine();
        }
        private static void Report(Cplex cplex, System.IO.TextWriter Writer, long minisecond, string alg)
        {
            //cplex.SetParam(Cplex.IntParam.RootAlg, 1);
            //cplex.SetParam(Cplex.BooleanParam.PreInd, false);
            //cplex.SetOut(null);
            if (cplex.Solve())
            {
                Writer.WriteLine("算法    求解时间  值");
                Writer.WriteLine("{0}   {1} {2}", alg, minisecond, cplex.ObjValue);
            }
        }
    }
}
