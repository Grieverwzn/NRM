//using com.foxmail.wyyuan1991.NRM.Data;
//using ILOG.CPLEX;
//using System;
//using System.Windows.Forms;
//using System.Diagnostics;

//namespace RailwayNRM
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            Console.WriteLine("--------请输入数据EXCEL地址：--------");
//            string path = Console.ReadLine();

//            RailDataReader dg = new RailDataReader();
//            dg.ReadXLS(path);

//            //RailDataGenerator dg = new RailDataGenerator();
//            //dg.ReadXLS(@"D:\code\PlanRailReservationSim\NRMSolver\Data\data_s.xls");
//            //dg.GenTimetable();
//            //dg.GenResSpace();
//            //dg.GenProducts();
//            //dg.GenMarket();
//            //dg.GenRoute();

//            NRMDataAdapter da = new NRMDataAdapter();
//            da.MarketInfo = dg.mar;
//            da.ProSpace = dg.proset;
//            da.ResSpace = dg.rs;
//            da.pathList = dg.pathList;
//            da.InitialState = da.CreateOrFind(da.GenInitialState());
//            da.TimeHorizon = dg.TimeHorizon;

//            Console.WriteLine("--------问题生成结束--------");
//            Console.WriteLine("--------资源数量:{0}--------", da.ResSpace.Count);
//            Console.WriteLine("--------产品数量:{0}--------", da.ProSpace.Count);
//            Console.WriteLine("--------路径数量:{0}--------", dg.pathList.Count);
//            Console.WriteLine("--------市场数量:{0}--------", da.MarketInfo.Count);
//            Console.WriteLine("--------时段数量:{0}--------", da.TimeHorizon);

//            //RailwayNRMSolver_GCDLP d = new RailwayNRMSolver_GCDLP(); ; d.NumOfThreads = 8; d.Tolerance = 1e-2;
//            //RailwayNRMSolver_CLP_v2 d = new RailwayNRMSolver_CLP_v2(); ; d.NumOfThreads = 8; d.Tolerance = 1e-2;
//            //RailwayNRMSolver_DD d = new RailwayNRMSolver_DD(); d.NumOfThreads = 8; d.Tolerance = 1e-2; d.step = da.RS.Count;
//            //Console.WriteLine("--------开始求解--------");

//            //f.SetMDPProject(d);
//            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"D:\output.txt", false))
//            {
//                RailwayNRMSolver_GCDLP d = new RailwayNRMSolver_GCDLP(); d.NumOfThreads = 8; d.Tolerance = 1e-2;
//                Stopwatch sw = new Stopwatch();
//                d.SolverTextWriter = Console.Out;
//                d.Data = da;
//                sw.Start();
//                d.Solve();
//                sw.Stop();
//                file.WriteLine("求解时间:{0}", sw.ElapsedMilliseconds);
//                Report(d.RMPModel, file);

//                RailwayNRMSolver_CLP_v2 e = new RailwayNRMSolver_CLP_v2(); e.NumOfThreads = 8; e.Tolerance = 1e-2;
//                e.SolverTextWriter = Console.Out;
//                sw.Reset();
//                e.Data = da;
//                sw.Start();
//                e.Solve();
//                sw.Stop();
//                file.WriteLine("求解时间:{0}", sw.ElapsedMilliseconds);
//                Report(e.RMPModel, file);

//                RailwayNRMSolver_DD f = new RailwayNRMSolver_DD(); f.NumOfThreads = 8; f.Tolerance = 1e-2; f.step = da.RS.Count;
//                sw.Reset();
//                f.SolverTextWriter = Console.Out;
//                f.Data = da;
//                sw.Start();
//                f.Solve();
//                sw.Stop();
//                file.WriteLine("求解时间:{0}", sw.ElapsedMilliseconds);
//                Report(f.RMPModel, file);
//            }
//            Console.WriteLine("-------- Press <Enter> to Exit --------");
//            Console.ReadLine();
//        }
//        private static void Report(Cplex cplex, System.IO.TextWriter Writer)
//        {
//            //cplex.SetParam(Cplex.IntParam.RootAlg, 1);
//            //cplex.SetParam(Cplex.BooleanParam.PreInd, false);
//            //cplex.SetOut(null);
//            if (cplex.Solve())
//            {
//                Writer.WriteLine("Solution status = " + cplex.GetStatus());
//                Writer.WriteLine("Solution value  = " + cplex.ObjValue);
//            }
//        }
//    }
//}
