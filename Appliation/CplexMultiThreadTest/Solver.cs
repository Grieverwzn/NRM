using ILOG.Concert;
using ILOG.CPLEX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;

namespace CplexMultiThreadTest
{
    class Solver
    {

        #region Parallel
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
        List<Warp> list = new List<Warp>();
        INumVar[] Var = new INumVar[1000000];
        public Cplex RMPModel = new Cplex();
        public void Init()
        {
            Var = RMPModel.NumVarArray(1000000,0, double.MaxValue);
            list.Add(new Warp() { Val = 1 });
            list.Add(new Warp() { Val = 2 });
            list.Add(new Warp() { Val = 3 });
            #region //////////////生成目标//////////////
            INumExpr expr = RMPModel.NumExpr();
            expr = RMPModel.Sum(expr,Var[0]);
            IObjective cost = RMPModel.AddMinimize(expr);
            #endregion


            //for (int t = 0; t <100; t++)
            //{
            //    if (t < 99)
            //    {
            //        INumExpr expr1 = RMPModel.NumExpr();
            //        expr1 = RMPModel.Sum(Var[t], RMPModel.Prod(-1, Var[t + 1]));
            //        RMPModel.AddGe(expr1, 0);
            //    }
            //}
            RMPModel.SetOut(null);
        }

        protected IRange GenConstraint1(int i ,double w)
        {

            INumExpr exp1 = RMPModel.NumExpr();          
            exp1 = RMPModel.Sum(Var[i], 0);
            //Thread.Sleep(10);
            exp1 = RMPModel.Sum(exp1,RMPModel.Prod(-1,Var[i+1]));
            lock (RMPModel)
            {
                IRange r = RMPModel.Range(1, double.MaxValue);
                r.Expr = exp1;
                return r;
            }         
           
        }

        protected void AddConstraint1(int i, double w)
        {
            INumExpr exp1 = RMPModel.NumExpr();
            exp1 = RMPModel.Sum(Var[i], 0);
            //Thread.Sleep(10);
            exp1 = RMPModel.Sum(exp1, RMPModel.Prod(-1, Var[i + 1]));
            lock (RMPModel)
            {
                RMPModel.AddGe(exp1,1);
            }
        }

        public void DoCal()
        {
            Console.WriteLine("{0}：开始加载数据！", DateTime.Now.ToString());
            List<IRange> RangeList = new List<IRange>();
            for (int i = 0; i <999999; i++)
            {
                int k = i;
                Task ta = factory.StartNew(() =>
                {
                    //RangeList.Add(GenConstraint1(k, 1));
                    AddConstraint1(k, 1);
                }, cts.Token);
                tasks.Add(ta);                             
            }
            Task.WaitAll(tasks.ToArray());
            tasks.Clear();

            //
            //RMPModel.Add(RangeList.ToArray());

            Console.WriteLine("{0}：结束加载数据！", DateTime.Now.ToString());
            if(RMPModel.Solve())
            {
                Console.WriteLine(RMPModel.GetObjValue());
                //double[] res = RMPModel.GetValues(Var);
                //for(int i=0;i<10000;i++)
                //{
                //    if (i < 9999)
                //    {
                //        Console.WriteLine(res[i]);
                //    }
                //    else
                //    {
                //        Console.WriteLine(res[i]);
                //    }
                //}
            }
        }
    }
    class Warp
    {
        public int Val = 0;
    }
}
