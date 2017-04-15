//using com.foxmail.wyyuan1991.NRM.Simulator;
//using com.foxmail.wyyuan1991.NRM.Data;
//using RailwayNRM;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace RailSimulator
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            //数据载入
//            //RailDataGenerator dg = new RailDataGenerator();
//            //dg.GenDataWithoutCT(0.2, 0.5);
//            //dg.TimeHorizon = 200;
//            //Console.WriteLine("输入数据文件:");
//            string path1 = args[0];
//            //Console.WriteLine("输入到达文件:");
//            string path2 = args[1];
//            //Console.WriteLine("输入BPC文件:");
//            string path3 = args[2];
//            RailDataReader dg = new RailDataReader();
//            dg.ReadXLS(path1);

//            //RailDataGenerator dg = new RailDataGenerator();
//            //dg.ReadXLS(@"D:\code\PlanRailReservationSim\NRMSolver\Data\data_s.xls");
//            //dg.GenTimetable();
//            //dg.GenResSpace();
//            //dg.GenProducts();
//            //dg.GenMarket();
//            //dg.GenRoute();

//            NRMDataAdapter data = new NRMDataAdapter()
//            {
//                MarketInfo = dg.mar,
//                ProSpace = dg.proset,
//                ResSpace = dg.rs,
//                pathList = dg.pathList,
//                TimeHorizon = dg.TimeHorizon
//            };
//            data.InitialState = data.CreateOrFind(data.GenInitialState());
//            #region 生成到达
//            PrimalArrivalData pad = new PrimalArrivalData();//到达数据
//            //到达序列生成器
//            //ArrivalSimulator arr = new ArrivalSimulator()
//            //{
//            //    MaxLamada = 0.2,
//            //    TimeHorizon = dg.TimeHorizon,
//            //    MarketInfo = dg.mar
//            //};
//            //for (int i = 1; i <= 200; i++)
//            //{
//            //    pad.Data.Add(arr.Gen(i));
//            //}
//            //pad.SaveToXml("D:\\Res\\Arrival.xml");
//            pad.LoadFromXml(path2);
//            //PrimalArrivalList pa = arr.Gen(1);
//            #endregion

//            #region 购票仿真
//            //Parallel.ForEach(pad.Data, pa =>
//            // {
//            //     lock (da)
//            //     {
//            SellingRecordList sr;//售票记录
//            ControlRecordList cr;//控制记录
//            SimRecodData SimData = new SimRecodData();
//            BookingSimulator simulator = new BookingSimulator()
//            {
//                MarketInfo = dg.mar,
//                ResourceSpace = dg.rs,// (da.RS as com.foxmail.wyyuan1991.NRM.Common.IResourceSet),
//                //Controller = new OpenAllStrategy()
//                //{
//                //    market = dg.mar,
//                //    proSpace = dg.proset,
//                //    RS=dg.rs
//                //}

//            };
//            simulator.SimTextWriter = Console.Out;
//            //计算策略
//            Console.WriteLine("{0}初始化开始！", System.DateTime.Now.ToLongTimeString());
//            //RailwayNRMSolver_DD d = new RailwayNRMSolver_DD(); d.NumOfThreads = 8; d.Tolerance = 1e-2; d.step = data.RS.Count;
//            //d.SolverTextWriter = Console.Out;
//            //d.Data = data;
//            //d.Solve();
//            //d.SaveBidPrice(@"D:\Res\BDC.txt");
            
//            BidPriceControl BPC = new BidPriceControl()
//            {
//                DataAdapter = data
//            };
//            BPC.ReadFromXLS(path3);
//            OpenAllStrategy OAS = new OpenAllStrategy()
//            {
//                DataAdapter = data
//            };
//            Console.WriteLine("{0}初始化完成！", System.DateTime.Now.ToLongTimeString());


//            simulator.Controller = BPC;
//            //(simulator.Controller as BidPriceControl).SetOut(Console.Out);
//            int num = 1;
//            double exp = 0;
//            foreach (PrimalArrivalList pa in pad.Data)
//            {
//                simulator.Process(pa, out sr, out cr);
//                //foreach (var a in sr)
//                //{
//                //    Console.WriteLine(System.DateTime.Now.ToLongTimeString() + a.ToString());
//                //}
//                exp += sr.Revenue();
//                SimData.CRD.Add(cr);
//                SimData.SRD.Add(sr);
//                //Console.WriteLine("{0}完成第{1}次仿真！平均收益:{2}",System.DateTime.Now.ToLongTimeString(),num,exp/num);
//                num++;
//            }
//            Console.WriteLine("{0}完成仿真！平均收益:{1}", System.DateTime.Now.ToLongTimeString(), exp / (num - 1));
//            SimData.SaveToXml("D:\\Res\\");
//            //    }
//            //});
//            #endregion

//            Console.WriteLine("------ Press <Enter> to Exit ------");
//            Console.ReadLine();
//        }
//    }
//}
