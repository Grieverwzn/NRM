using System;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using System.Threading.Tasks;
using ILOG.CPLEX;
using com.foxmail.wyyuan1991.NRM.Common;
using com.foxmail.wyyuan1991.NRM.Data;
using com.foxmail.wyyuan1991.NRM.Simulator;

/*
 * 操作指令
 * 配置底层数据
 * 生成投标价格策略
 * 生成仿真到达
 * 模拟旅客选择
 * 统计仿真结果
 */
namespace com.foxmail.wyyuan1991.NRM.Command
{
    public abstract class Command
    {
        protected Warpper warpper;
        public Command(Warpper _warpper)
        {
            this.warpper = _warpper;
        }
        public abstract void ExecuteCommand();

        [HelpOption]
        public virtual string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
    //读取数据
    public class ReadDataCommand : Command
    {
        public ReadDataCommand(Warpper _warpper) : base(_warpper)
        {
        }

        [Option('p', "path", MetaValue = "EXCEL文件地址", Required = true, HelpText = "请填写Excel文件的完整路径")]
        public string Path { get; set; }

        public override void ExecuteCommand()
        {
            RailDataReader dg = new RailDataReader();
            dg.ReadXLS(Path);

            warpper.da.MarketInfo = dg.mar;
            warpper.da.ProSpace = dg.proset;
            warpper.da.ResSpace = dg.ResSet;
            warpper.da.RouteList = dg.pathList;
            warpper.da.TimeHorizon = dg.TimeHorizon;
            warpper.da.InitState = dg.InitState;
            warpper.da.InitialState = warpper.da.CreateOrFind(warpper.da.GenInitialState(dg.InitState));
            warpper.da.MetaResSpace = dg.MRS;

            Console.WriteLine("问题生成结束! ");
            Console.Write("资源数量:{0},", warpper.da.ResSpace.Count);
            Console.Write("产品数量:{0},", warpper.da.ProSpace.Count);
            Console.Write("路径数量:{0},", warpper.da.RouteList.Count);
            Console.Write("市场数量:{0},", warpper.da.MarketInfo.Count);
            Console.WriteLine("时段数量:{0}", warpper.da.TimeHorizon);
        }
    }
    //生成投标价格控制策略
    public class GenBidPriceCommand: Command
    {
        [Option('p', "Output Path", MetaValue = "输出文件地址", Required = true, HelpText = "请填写输出文件的地址")]
        public string Path { get; set; }

        [Option('n', "Number of Threads", MetaValue = "最大线程数", Required = false, HelpText = "请填写最大线程数", DefaultValue = 8)]
        public int NumberOfThreads { get; set; }

        [Option('t', "Tolerance", MetaValue = "误差容许值", Required = false, HelpText = "请填写误差容许值", DefaultValue = 1e-2)]
        public double Tolerance { get; set; }

        [Option('s', "Step", MetaValue = "搜索步长", Required = false, HelpText = "请填写步长，填写-1系统自动指定", DefaultValue = -1)]
        public int Step { get; set; }

        [Option('h', "Step", MetaValue = "改进容许值", Required = false, HelpText = "请填写步长，填写-1系统自动指定", DefaultValue = -1)]
        public double Threshold { get; set; }

        [Option('o', "Step", MetaValue = "允许在容许值外的次数", Required = false, HelpText = "请填写步长，填写-1系统自动指定", DefaultValue = -1)]
        public int ObeyTime { get; set; }

        public GenBidPriceCommand(Warpper _warpper) : base(_warpper)
        {

        }

        public override void ExecuteCommand()
        {
            if(warpper.da==null)
            {
                Console.WriteLine("请先输入数据！");
                return;
            }

            warpper.solver.SolverTextWriter = Console.Out;
            warpper.solver.Data = warpper.da;

            warpper.solver.NumOfThreads = NumberOfThreads;
            warpper.solver.Tolerance = Tolerance;
            warpper.solver.step = Step>0? Step:warpper.da.RS.Count;
 

            warpper.solver.Solve();
            warpper.solver.SaveBidPrice(Path);

            Report(warpper.solver.RMPModel, Console.Out, 0, "BPC");
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
    //生成到达序列
    public class SimArrCommand: Command
    {
        [Option('p', "Output Path", MetaValue = "输出文件地址", Required = true, HelpText = "请填写输出文件的地址")]
        public string Path { get; set; }

        [Option('n', "Number of Generated Arrivals", MetaValue = "到达生成数", Required = false, HelpText = "请填写到达生成数", DefaultValue = 10)]
        public int Num { get; set; }

        public SimArrCommand(Warpper _warpper) : base(_warpper)
        {
        }

        public override void ExecuteCommand()
        {
            if (warpper.da == null)
            {
                Console.WriteLine("请先输入数据！");
                return;
            }

            PrimalArrivalData pad = new PrimalArrivalData();//到达数据
            ArrivalSimulator arr = new ArrivalSimulator()  //到达序列生成器
            {
                MaxLamada = 0,
                TimeHorizon = warpper.da.TimeHorizon,
                MarketInfo = warpper.da.MarketInfo
            };

            //判断文件路径是否存在，不存在则创建文件夹 
            if (!System.IO.Directory.Exists(Path))
            {
                System.IO.Directory.CreateDirectory(Path);//不存在就创建目录 
            }

            int n = 0;
            int total =(int)Math.Ceiling((double)Num / 10000);
            Console.Write("进度 000.00%");
            Parallel.For(0, Num, (i, loopState) =>
            {
                PrimalArrivalList pal = arr.Gen(i);
                pal.WriteToArrFile(Path + "\\" + pal.PAListID + ".arr");
                if (n++ % total == 0)
                {
                    lock (this)
                    {
                        Console.SetCursorPosition(Console.CursorLeft - 7, Console.CursorTop);
                        Console.Write("{0}%", String.Format("{0:000.00}", Math.Round(((double)n / (double)Num), 4) * 100));
                    }
                }
            });
            Console.SetCursorPosition(Console.CursorLeft - 7, Console.CursorTop);
            Console.WriteLine("100.00 %  完成！");
        }
    }
    //BPC策略下仿真
    public class SimBPCCommand: Command
    {
        [Option('i', "input Path", MetaValue = "输入文件地址", Required = true, HelpText = "请填写输出文件的地址")]
        public string inputPath { get; set; }

        [Option('o', "output Path", MetaValue = "输出文件地址", Required = true, HelpText = "请填写输出文件的地址")]
        public string outputPath { get; set; }

        [Option('c', "ctl Path", MetaValue = "控制文件地址", Required = true, HelpText = "请填写输出文件的地址")]
        public string ctlPath { get; set; }

        [Option('n', "Number of Threads", MetaValue = "最大线程数",Required = false, HelpText = "请填写最大线程数", DefaultValue = 8)]
        public int NumberOfThreads { get; set; }

        public SimBPCCommand(Warpper _warpper) : base(_warpper)
        {
        }
        public override void ExecuteCommand()
        {
            if (warpper.da == null)
            {
                Console.WriteLine("请先输入数据！");
                return;
            }
            BidPriceController BPC = new BidPriceController()
            {
                DataAdapter = warpper.da
            };
            BPC.ReadFromTXT(ctlPath);

            warpper.bookSim.MarketInfo = warpper.da.MarketInfo;
            //warpper.bookSim.ResourceSpace = warpper.da.ResSpace as IResourceSet;
            warpper.bookSim.Controller = BPC;
            warpper.bookSim.InitState = warpper.da.InitState;
            warpper.bookSim.SimTextWriter = Console.Out;
            warpper.bookSim.NumOfThreads = NumberOfThreads;
            warpper.bookSim.BatchProcess(inputPath, outputPath);
         
        }
    }
    //OAC策略下仿真
    public class SimOACCommand : Command
    {
        [Option('i', "input Path", MetaValue = "输入文件地址", Required = true, HelpText = "请填写输出文件的地址")]
        public string inputPath { get; set; }

        [Option('o', "output Path", MetaValue = "输出文件地址", Required = true, HelpText = "请填写输出文件的地址")]
        public string outputPath { get; set; }

        [Option('n', "Number of Threads", MetaValue = "最大线程数", Required = false, HelpText = "请填写最大线程数", DefaultValue = 8)]
        public int NumberOfThreads { get; set; }

        public SimOACCommand(Warpper _warpper) : base(_warpper)
        {

        }
        public override void ExecuteCommand()
        {
            if (warpper.da == null)
            {
                Console.WriteLine("请先输入数据！");
                return;
            }
            OpenAllStrategy OAS = new OpenAllStrategy()
            {
                DataAdapter = warpper.da
            };

            warpper.bookSim.MarketInfo = warpper.da.MarketInfo;
            //warpper.bookSim.ResourceSpace = warpper.da.ResSpace as IResourceSet;
            warpper.bookSim.Controller = OAS;
            warpper.bookSim.SimTextWriter = Console.Out;
            warpper.bookSim.NumOfThreads = NumberOfThreads;
            warpper.bookSim.InitState = warpper.da.InitState;
            warpper.bookSim.BatchProcess(inputPath, outputPath);

        }
    }
    //中国控制
    public class SimCNNestingCommand : Command
    {
        [Option('i', "input Path", MetaValue = "输入文件地址", Required = true, HelpText = "请填写输出文件的地址")]
        public string inputPath { get; set; }

        [Option('o', "output Path", MetaValue = "输出文件地址", Required = true, HelpText = "请填写输出文件的地址")]
        public string outputPath { get; set; }

        [Option('c', "control Path", MetaValue = "控制文件地址", Required = true, HelpText = "请填写控制文件地址")]
        public string controlPath { get; set; }

        [Option('n', "Number of Threads", MetaValue = "最大线程数", Required = false, HelpText = "请填写最大线程数", DefaultValue = 8)]
        public int NumberOfThreads { get; set; }

        public SimCNNestingCommand(Warpper _warpper) : base(_warpper)
        {

        }
        public override void ExecuteCommand()
        {
            if (warpper.da == null)
            {
                Console.WriteLine("请先输入数据！");
                return;
            }
            CnNesting CnNesting = new CnNesting()
            {
                DataAdapter = warpper.da,
                Path = controlPath
            };

            warpper.bookSim.MarketInfo = warpper.da.MarketInfo;
            //warpper.bookSim.ResourceSpace = warpper.da.ResSpace as IResourceSet;
            warpper.bookSim.Controller = CnNesting;
            warpper.bookSim.SimTextWriter = Console.Out;
            warpper.bookSim.NumOfThreads = NumberOfThreads;
            warpper.bookSim.InitState = warpper.da.InitState;
            warpper.bookSim.BatchProcess(inputPath, outputPath);
        }
    }
    //仿真分析
    public class AnalysisCommand: Command
    {

        [Option('i', null, MetaValue = "到达文件地址", Required = true, HelpText = "请填写到达文件地址")]
        public string arrPath { get; set; }

        [Option('s', null, MetaValue = "售票文件地址", Required = true, HelpText = "请填写售票文件地址")]
        public string srPath { get; set; }

        [Option('c', null, MetaValue = "控制文件地址", Required = false, HelpText = "请填写控制文件地址")]
        public string crPath { get; set; }

        [Option('o', null, MetaValue = "输出文件地址", Required = true, HelpText = "请填写输出文件的地址")]
        public string outPath { get; set; }

        [Option('n', null, MetaValue = "最大线程数", Required = false, HelpText = "请填写最大线程数", DefaultValue = 8)]
        public int NumberOfThreads { get; set; }

        public AnalysisCommand(Warpper _warpper) : base(_warpper)
        {
        }
        public override void ExecuteCommand()
        {
            if (warpper.da == null)
            {
                Console.WriteLine("请先输入数据！");
                return;
            }
            warpper.SA.MarketInfo = warpper.da.MarketInfo;
            warpper.SA.ResourceSpace = warpper.da.RS as IResourceSet;
            warpper.SA.ProSpace = warpper.da.ProSpace as IProductSet;
            warpper.SA.InitState = warpper.da.InitState;
            warpper.SA.NumOfThreads = NumberOfThreads;
            warpper.SA.Dowork(arrPath, srPath, crPath,outPath);

        }
    }
    public class ShowIndexsCommand:Command
    {
       
        [Option('o', null, MetaValue = "输出文件地址", Required = true, HelpText = "请填写输出文件的地址")]
        public string outPath { get; set; }

        public ShowIndexsCommand(Warpper _warpper) : base(_warpper)
        {
        }

        public override void ExecuteCommand()
        {
            if (warpper.da == null)
            {
                Console.WriteLine("请先输入数据！");
                return;
            }
            warpper.SA.PrintHead(this.outPath);

        }
    }
}
